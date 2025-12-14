using System.Buffers;
using System.Net;
using System.Net.Sockets;

using Chaos.Common.Identity;
using Chaos.Cryptography.Abstractions;
using Chaos.Extensions.Networking;
using Chaos.NLog.Logging.Definitions;
using Chaos.NLog.Logging.Extensions;
using Chaos.Packets;
using Chaos.Packets.Abstractions;

using Microsoft.Extensions.Logging;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Receive path is intentionally single-threaded per client:
///      - One async receive loop per connection (no SAEA callback reentrancy)
///      - Rolling buffer parses as many complete frames as possible per read
///      - Keeps leftover bytes and continues reading into remaining buffer space
///
///     This minimizes scheduling overhead and removes lock/semaphore contention
///     from the packet processing hot path.
/// </summary>
public abstract class SocketTransportBase : ISocketTransport, IDisposable
{
    private readonly ConcurrentQueue<SocketAsyncEventArgs> SocketArgsQueue;

    // Bytes currently in rolling buffer
    private int _count;
    private int _disconnecting;
    private int _receiveStarted;

    private const int ReceiveBufferSize = 64 * 1024;
    // Max packet length (including header) per protocol spec
    public virtual int MaxPacketLength { get; } = 4 * 1024;

    private int _sequence;

    public bool Connected { get; set; }
    public ICrypto Crypto { get; set; }
    public bool LogRawPackets { get; set; }
    public uint Id { get; }
    protected ILogger<SocketTransportBase> Logger { get; }
    protected IPacketSerializer PacketSerializer { get; }

    public IPAddress RemoteIp { get; }
    public Socket Socket { get; }

    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly MemoryHandle _memoryHandle;

    private Memory<byte> Memory => _memoryOwner.Memory;
    private Span<byte> Buffer => _memoryOwner.Memory.Span;

    private readonly CancellationTokenSource _cts = new();

    protected SocketTransportBase(
        Socket socket,
        ICrypto crypto,
        IPacketSerializer packetSerializer,
        ILogger<SocketTransportBase> logger)
    {
        Id = SequentialIdGenerator<uint>.Shared.NextId;
        Socket = socket;
        Crypto = crypto;

        _memoryOwner = MemoryPool<byte>.Shared.Rent(ReceiveBufferSize);
        _memoryHandle = _memoryOwner.Memory.Pin();

        Logger = logger;
        PacketSerializer = packetSerializer;

        RemoteIp = (Socket.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.None;

        var initialArgs = Enumerable.Range(0, 10).Select(_ => CreateArgs());
        SocketArgsQueue = new ConcurrentQueue<SocketAsyncEventArgs>(initialArgs);

        Connected = false;
    }

    public event EventHandler? OnDisconnected;

    public virtual void SetSequence(byte newSequence) => _sequence = newSequence;

    /// <summary>
    ///     Handle a fully framed packet:
    ///     [signature][len_hi][len_lo][opcode][sequence][payload...]
    /// </summary>
    protected abstract ValueTask OnPacketAsync(Span<byte> span);

    #region Networking

    /// <summary>
    ///     Starts the receive loop once per client
    /// </summary>
    public virtual void StartReceiveLoop()
    {
        if (!Socket.Connected) return;
        if (Interlocked.Exchange(ref _receiveStarted, 1) == 1) return;

        Connected = true;

        // Fire-and-forget: the loop owns the socket receive side until disconnect.
        _ = ReceiveLoopAsync(_cts.Token);
    }

    private async Task ReceiveLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && Socket.Connected)
            {
                // Read into remaining space in rolling buffer
                var writeMem = Memory.Slice(_count);

                int bytesRead;
                try
                {
                    bytesRead = await Socket.ReceiveAsync(writeMem, SocketFlags.None, token)
                                            .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    CloseTransport();
                    return;
                }

                if (bytesRead == 0)
                {
                    // Remote closed gracefully
                    CloseTransport();
                    return;
                }

                _count += bytesRead;

                int offset = 0;

                // Process as many complete frames as possible
                while (true)
                {
                    // Need at least 5 bytes: sig + len_hi + len_lo + opcode + seq
                    if (_count - offset < 5)
                        break;

                    // Signature check
                    byte sig = Buffer[offset];
                    if (sig != 0xAA)
                    {
                        Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet)
                              .WithProperty(this)
                              .LogWarning(
                                  "Disconnecting client {RemoteIp} due to bad signature: {Sig} (Buffered={Buffered})",
                                  RemoteIp,
                                  sig,
                                  _count);
                        CloseTransport();
                        return;
                    }

                    // Extract payload length
                    int lengthHi = Buffer[offset + 1];
                    int lengthLo = Buffer[offset + 2];
                    int payloadLength = (lengthHi << 8) | lengthLo;

                    // Full frame length = payload + 3 (sig+len_hi+len_lo)
                    int frameLength = payloadLength + 3;

                    // Hard safety rails
                    if (frameLength <= 0 || frameLength > MaxPacketLength)
                    {
                        Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet)
                              .WithProperty(this)
                              .LogWarning(
                                  "Disconnecting client {RemoteIp} due to invalid frame length. Payload={Payload}, Frame={Frame}, Buffered={Buffered}",
                                  RemoteIp,
                                  payloadLength,
                                  frameLength,
                                  _count);
                        CloseTransport();
                        return;
                    }

                    // Not enough bytes yet for full frame
                    if (_count - offset < frameLength)
                        break;

                    // We have a complete frame
                    var frame = Buffer.Slice(offset, frameLength);

                    try
                    {
                        // ToDo: HOT PATH
                        await OnPacketAsync(frame).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Heavy logging only when explicitly enabled; otherwise keep the hot path lean.
                        if (LogRawPackets)
                        {
                            try
                            {
                                Logger.WithTopics(
                                          Topics.Entities.Client,
                                          Topics.Entities.Packet,
                                          Topics.Actions.Processing)
                                      .WithProperty(this)
                                      .LogError(ex, "Error handling packet (Length={Length})", frameLength);
                            }
                            catch { }
                        }

                        // Reset buffer to avoid poisoned state / partial misalignment
                        _count = 0;
                        break;
                    }

                    offset += frameLength;

                    if (offset >= _count)
                        break;
                }

                // Shift leftover bytes to front
                if (offset > 0)
                {
                    _count -= offset;
                    if (_count > 0)
                        Buffer.Slice(offset, _count).CopyTo(Buffer);
                }

                // If the rolling buffer ever fills without producing a valid frame, drop
                // This is a "never should happen" rail if a client goes rogue
                if (_count >= ReceiveBufferSize)
                {
                    Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet)
                          .WithProperty(this)
                          .LogWarning("Disconnecting client {RemoteIp} due to receive buffer overflow ({ReceiveBufferSize})", RemoteIp, ReceiveBufferSize);
                    CloseTransport();
                    return;
                }
            }
        }
        catch
        {
            CloseTransport();
        }
    }

    public virtual void Send<T>(T obj) where T : IPacketSerializable
    {
        if (!Connected || !Socket.Connected) return;

        var packet = PacketSerializer.Serialize(obj);

        if (LogRawPackets)
        {
            Logger.WithTopics(
                      Topics.Qualifiers.Raw,
                      Topics.Entities.Client,
                      Topics.Entities.Packet,
                      Topics.Actions.Send)
                  .WithProperty(this)
                  .LogTrace("[Snd] {Packet}", packet.ToString());
        }

        packet.IsEncrypted = IsEncrypted(packet.OpCode);

        if (packet.IsEncrypted)
        {
            packet.Sequence = (byte)(Interlocked.Increment(ref _sequence) - 1);
            Encrypt(ref packet);
        }

        // [signature][len_hi][len_lo][opcode][sequence][payload]
        var memory = packet.ToMemory();

        var args = DequeueArgs(memory);
        Socket.SendAndForget(args, ReuseSocketAsyncEventArgs);
    }

    public virtual void Send(ref Packet packet)
    {
        if (!Connected || !Socket.Connected) return;

        if (LogRawPackets)
        {
            Logger.WithTopics(
                      Topics.Qualifiers.Raw,
                      Topics.Entities.Client,
                      Topics.Entities.Packet,
                      Topics.Actions.Send)
                  .WithProperty(this)
                  .LogTrace("[Snd] {Packet}", packet.ToString());
        }

        packet.IsEncrypted = IsEncrypted(packet.OpCode);

        if (packet.IsEncrypted)
        {
            packet.Sequence = (byte)(Interlocked.Increment(ref _sequence) - 1);
            Encrypt(ref packet);
        }

        var memory = packet.ToMemory();

        var args = DequeueArgs(memory);
        Socket.SendAndForget(args, ReuseSocketAsyncEventArgs);
    }

    public abstract bool IsEncrypted(byte opCode);
    public abstract void Encrypt(ref Packet packet);

    public virtual void CloseTransport()
    {
        if (Interlocked.Exchange(ref _disconnecting, 1) == 1) return;

        Connected = false;

        try { _cts.Cancel(); } catch { }

        try { Socket.Shutdown(SocketShutdown.Both); } catch { }
        try { OnDisconnected?.Invoke(this, EventArgs.Empty); } catch { }

        Dispose();
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);

        try { _cts.Cancel(); } catch { }
        try { _cts.Dispose(); } catch { }

        try { _memoryHandle.Dispose(); } catch { }
        try { _memoryOwner.Dispose(); } catch { }

        try { Socket.Close(); } catch { }
    }

    public virtual void Close() => Socket.Close();

    #endregion

    #region Utility (send pooling)

    /// <summary>
    ///     Disposes memory owner if present and recycles SAEA back to the queue.
    /// </summary>
    private void ReuseSocketAsyncEventArgs(object? sender, SocketAsyncEventArgs e)
    {
        if (e.UserToken is IMemoryOwner<byte> mem)
        {
            e.UserToken = null;
            mem.Dispose();
        }

        SocketArgsQueue.Enqueue(e);
    }

    private SocketAsyncEventArgs CreateArgs()
    {
        var args = new SocketAsyncEventArgs();
        args.Completed += ReuseSocketAsyncEventArgs;
        return args;
    }

    private SocketAsyncEventArgs DequeueArgs(Memory<byte> buffer)
    {
        if (!SocketArgsQueue.TryDequeue(out var args))
            args = CreateArgs();

        args.SetBuffer(buffer);
        return args;
    }

    #endregion
}
