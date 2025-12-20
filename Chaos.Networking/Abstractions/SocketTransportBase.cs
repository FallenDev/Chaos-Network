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
    private readonly ConcurrentQueue<SocketAsyncEventArgs> _socketArgsQueue;

    private const int ReceiveBufferSize = 64 * 1024;
    public virtual int MaxPacketLength { get; } = 8 * 1024;

    private int _count;
    private int _sequence;
    private int _disconnecting;
    private int _receiveStarted;
    private int _disposed;

    public bool Connected { get; set; }
    public ICrypto Crypto { get; set; }
    public bool LogRawPackets { get; set; }
    public uint Id { get; }

    protected ILogger<SocketTransportBase> Logger { get; }
    protected IPacketSerializer PacketSerializer { get; }

    public IPAddress RemoteIp { get; }
    public Socket Socket { get; }

    private readonly IMemoryOwner<byte> _recvOwner;
    private readonly MemoryHandle _recvHandle;
    private Memory<byte> RecvMemory => _recvOwner.Memory;
    private Span<byte> RecvBuffer => _recvOwner.Memory.Span;

    private readonly MemoryPool<byte> _sendPool = MemoryPool<byte>.Shared;
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
        PacketSerializer = packetSerializer;
        Logger = logger;
        SocketExtensions.ConfigureTcpSocket(Socket);

        RemoteIp = (Socket.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.None;

        // Memory Setup
        _recvOwner = MemoryPool<byte>.Shared.Rent(ReceiveBufferSize);
        _recvHandle = _recvOwner.Memory.Pin();


        var initialArgs = Enumerable.Range(0, 10).Select(_ => CreateSendArgs());
        _socketArgsQueue = new ConcurrentQueue<SocketAsyncEventArgs>(initialArgs);

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

        _ = StartReceiveAsync(_cts.Token);
    }

    private async Task StartReceiveAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && Socket.Connected)
            {
                // Read into remaining space in rolling buffer
                var writeMem = RecvMemory.Slice(_count);

                int bytesRead;
                try
                {
                    bytesRead = await Socket.ReceiveAsync(writeMem, SocketFlags.None, token)
                                            .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { CloseTransport(); return; }

                // Remote closed connection
                if (bytesRead == 0)
                {
                    CloseTransport();
                    return;
                }

                _count += bytesRead;

                int offset = 0;

                while (true)
                {
                    // Need at least 5 bytes: sig + len_hi + len_lo + opcode + seq
                    if (_count - offset < 5)
                        break;

                    // Signature check
                    byte sig = RecvBuffer[offset];
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
                    int lengthHi = RecvBuffer[offset + 1];
                    int lengthLo = RecvBuffer[offset + 2];
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

                    // Not enough bytes yet for a full frame
                    if (_count - offset < frameLength)
                        break;

                    var frame = RecvBuffer.Slice(offset, frameLength);

                    try
                    {
                        // ToDo: HOT PATH
                        var vt = OnPacketAsync(frame);
                        if (!vt.IsCompletedSuccessfully)
                            await vt.ConfigureAwait(false);
                        else
                            vt.GetAwaiter().GetResult();
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
                        RecvBuffer.Slice(offset, _count).CopyTo(RecvBuffer);
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
        var packet = PacketSerializer.Serialize(obj);
        Send(ref packet);
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

        var wireLength = packet.GetWireLength();
        var args = DequeueSendArgs(wireLength);

        var state = (SendArgsState)args.UserToken!;
        packet.WriteTo(state.Current.Span);

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
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        GC.SuppressFinalize(this);

        try { _cts.Cancel(); } catch { }
        try { _cts.Dispose(); } catch { }

        while (_socketArgsQueue.TryDequeue(out var args))
        {
            try
            {
                if (args.UserToken is SendArgsState state)
                {
                    args.UserToken = null;
                    state.Dispose();
                }
            }
            catch { }

            try { args.Dispose(); } catch { }
        }

        try { _recvHandle.Dispose(); } catch { }
        try { _recvOwner.Dispose(); } catch { }

        try { Socket.Close(); } catch { }
    }

    public virtual void Close() => Socket.Close();

    #endregion

    #region Utility (send pooling)

    private void ReuseSocketAsyncEventArgs(object? sender, SocketAsyncEventArgs e)
    {
        var disposed = Volatile.Read(ref _disposed) == 1;

        if (!disposed && e.SocketError != SocketError.Success)
        {
            try
            {
                Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet)
                      .WithProperty(this)
                      .LogDebug(
                          "SocketAsyncEventArgs completed with error. Op={Op} Error={Error} Bytes={Bytes}",
                          e.LastOperation,
                          e.SocketError,
                          e.BytesTransferred);
            }
            catch { }

            try { CloseTransport(); } catch { }
            disposed = Volatile.Read(ref _disposed) == 1;
        }

        if (disposed)
        {
            try
            {
                if (e.UserToken is SendArgsState state)
                {
                    e.UserToken = null;
                    state.Dispose();
                }
            }
            catch { }

            try { e.Dispose(); } catch { }
            return;
        }

        try
        {
            if (e.UserToken is SendArgsState state)
                e.SetBuffer(state.Owner.Memory.Slice(0, 0));
            else
                e.SetBuffer(Memory<byte>.Empty);
        }
        catch
        {
            try { CloseTransport(); } catch { }
            return;
        }

        _socketArgsQueue.Enqueue(e);
    }

    private SocketAsyncEventArgs CreateSendArgs()
    {
        var args = new SocketAsyncEventArgs();
        args.Completed += ReuseSocketAsyncEventArgs;

        var owner = _sendPool.Rent(MaxPacketLength);
        args.UserToken = new SendArgsState(owner);

        args.SetBuffer(owner.Memory.Slice(0, 0));
        return args;
    }

    private SocketAsyncEventArgs DequeueSendArgs(int requiredLength)
    {
        if (!_socketArgsQueue.TryDequeue(out var args))
            args = CreateSendArgs();

        if (args.UserToken is not SendArgsState state)
        {
            var owner = _sendPool.Rent(Math.Max(requiredLength, MaxPacketLength));
            state = new SendArgsState(owner);
            args.UserToken = state;
        }

        state.EnsureCapacity(requiredLength, _sendPool);
        args.SetBuffer(state.Current);

        return args;
    }

    private sealed class SendArgsState(IMemoryOwner<byte> owner) : IDisposable
    {
        public IMemoryOwner<byte> Owner = owner;
        public int Capacity = owner.Memory.Length;
        public Memory<byte> Current = default;

        public void EnsureCapacity(int requiredLength, MemoryPool<byte> pool)
        {
            if (Capacity >= requiredLength)
            {
                Current = Owner.Memory.Slice(0, requiredLength);
                return;
            }

            Owner.Dispose();
            Owner = pool.Rent(requiredLength);
            Capacity = Owner.Memory.Length;
            Current = Owner.Memory.Slice(0, requiredLength);
        }

        public void Dispose() => Owner.Dispose();
    }

    #endregion
}
