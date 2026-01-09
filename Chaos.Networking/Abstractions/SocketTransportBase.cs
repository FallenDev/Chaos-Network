using System.Buffers;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Chaos.Common.Identity;
using Chaos.Common.Synchronization;
using Chaos.Cryptography.Abstractions;
using Chaos.Extensions.Networking;
using Chaos.NLog.Logging.Definitions;
using Chaos.NLog.Logging.Extensions;
using Chaos.Packets;
using Chaos.Packets.Abstractions;

using Microsoft.Extensions.Logging;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Provides the ability to send and receive packets over a socket
/// </summary>
public abstract class SocketTransportBase : ISocketTransport, IDisposable
{
    private readonly ConcurrentQueue<SocketAsyncEventArgs> SocketArgsQueue;
    private SocketAsyncEventArgs? _receiveArgs;
    private int _bytesReadIn;
    private int _disconnecting;
    private int _disposed;
    private int _receiveStarted;

    private const int ReceiveBufferSize = 64 * 1024;
    public virtual int MaxPacketLength { get; } = 8 * 1024;
    private int Sequence;

    /// <inheritdoc />
    public bool Connected { get; set; }

    /// <inheritdoc />
    public ICrypto Crypto { get; set; }

    /// <summary>Whether or not to log raw packet data to Trace</summary>
    public bool LogRawPackets { get; set; }

    /// <inheritdoc />
    public uint Id { get; }

    /// <summary>The logger for logging client-related events</summary>
    protected ILogger<SocketTransportBase> Logger { get; }

    private MemoryHandle MemoryHandle { get; }
    private IMemoryOwner<byte> MemoryOwner { get; }

    /// <summary>The packet serializer for serializing and deserializing packets</summary>
    protected IPacketSerializer PacketSerializer { get; }

    /// <inheritdoc />
    public FifoSemaphoreSlim ReceiveSync { get; }

    /// <inheritdoc />
    public IPAddress RemoteIp { get; }

    /// <inheritdoc />
    public Socket Socket { get; }

    private Span<byte> Buffer => Memory.Span;
    private Memory<byte> Memory => MemoryOwner.Memory;
    private static readonly ArrayPool<byte> SendBufferPool = ArrayPool<byte>.Shared;

    protected SocketTransportBase(
        Socket socket,
        ICrypto crypto,
        IPacketSerializer packetSerializer,
        ILogger<SocketTransportBase> logger)
    {
        Id = SequentialIdGenerator<uint>.Shared.NextId;
        Socket = socket;
        Crypto = crypto;

        SocketExtensions.ConfigureTcpSocket(Socket);

        // Rent a receive buffer bigger than max packet to allow multiple packets per read
        MemoryOwner = MemoryPool<byte>.Shared.Rent(ReceiveBufferSize);
        MemoryHandle = Memory.Pin();

        Logger = logger;
        PacketSerializer = packetSerializer;
        RemoteIp = (Socket.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.None;

        ReceiveSync = new FifoSemaphoreSlim(1, 1, $"{GetType().Name} {RemoteIp} (Socket)");

        var initialArgs = Enumerable.Range(0, 50).Select(_ => CreateArgs());
        SocketArgsQueue = new ConcurrentQueue<SocketAsyncEventArgs>(initialArgs);

        Connected = false;
    }

    public event EventHandler? OnDisconnected;
    public virtual void SetSequence(byte newSequence) => Sequence = newSequence;
    protected abstract ValueTask OnPacketAsync(Span<byte> span);

    #region Networking

    public virtual void StartReceiveLoop()
    {
        if (!Socket.Connected) return;
        if (Interlocked.Exchange(ref _receiveStarted, 1) == 1) return;

        Connected = true;
        _bytesReadIn = 0;

        _receiveArgs = new SocketAsyncEventArgs();
        _receiveArgs.SetBuffer(Memory);
        _receiveArgs.Completed += ReceiveEventHandler;

        Socket.ReceiveAndForget(_receiveArgs, ReceiveEventHandler);
    }

    private async void ReceiveEventHandler(object? sender, SocketAsyncEventArgs e)
    {
        await ReceiveSync.WaitAsync().ConfigureAwait(false);

        try
        {
            var bytesRead = e.BytesTransferred;

            // Transport rails
            if (bytesRead == 0 || e.SocketError != SocketError.Success)
            {
                CloseTransport();
                return;
            }

            // Update count of valid bytes in receive buffer
            _bytesReadIn += bytesRead;

            if ((uint)_bytesReadIn > (uint)ReceiveBufferSize)
            {
                Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet)
                      .WithProperty(this)
                      .LogWarning("Disconnecting client {RemoteIp} due to receive buffer overflow ({ReceiveBufferSize})",
                      RemoteIp,
                      ReceiveBufferSize);

                CloseTransport();
                return;
            }

            var offset = 0;
            var resetBuffer = false;

            // Minimum wire frame is 4 bytes: [signature][len_hi][len_lo][opcode]
            while ((uint)(_bytesReadIn - offset) >= 4u)
            {
                // Signature rail
                byte sig = Buffer[offset];
                if (sig != 0xAA)
                {
                    Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet)
                          .WithProperty(this)
                          .LogWarning("Disconnecting client {RemoteIp} due to bad signature: {Sig} (BytesIn={BytesIn}, Offset={Offset})",
                          RemoteIp,
                          sig,
                          _bytesReadIn,
                          offset);

                    CloseTransport();
                    return;
                }

                // Body length is big-endian, includes opcode + (maybe) sequence + payload
                var bodyLength = (Buffer[offset + 1] << 8) | Buffer[offset + 2];

                // Body length rails - must be at least 1 (opcode) and not exceed max packet length - 3 (header)
                // Also prevents overflow when adding header size (3)
                if ((uint)bodyLength < 1u || (uint)bodyLength > (uint)(MaxPacketLength - 3))
                {
                    Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet)
                          .WithProperty(this)
                          .LogWarning(
                              "Disconnecting client {RemoteIp} due to invalid body length. Body={BodyLength}, BytesIn={BytesIn}, Offset={Offset}",
                              RemoteIp,
                              bodyLength,
                              _bytesReadIn,
                              offset);

                    CloseTransport();
                    return;
                }

                // Full frame length = 3 header bytes + bodyLength
                var frameLength = bodyLength + 3;

                // Not enough bytes yet for the whole frame
                if ((uint)(_bytesReadIn - offset) < (uint)frameLength)
                    break;

                ValueTask vt;

                try
                {
                    vt = OnPacketAsync(Buffer.Slice(offset, frameLength));
                }
                catch (Exception ex)
                {
                    // Synchronous exception from OnPacketAsync call-site (rare)
                    LogPacketHandlingError(ex, offset, frameLength);
                    resetBuffer = true;
                    break;
                }

                if (!vt.IsCompletedSuccessfully)
                {
                    try
                    {
                        await vt.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        LogPacketHandlingError(ex, offset, frameLength);
                        resetBuffer = true;
                        break;
                    }
                }

                offset += frameLength;
            }

            // Buffer management
            if (resetBuffer)
            {
                _bytesReadIn = 0;
            }
            else if (offset > 0)
            {
                var leftover = _bytesReadIn - offset;
                _bytesReadIn = leftover;

                // We have _bytesReadIn bytes left starting at "offset"
                // Slide them to the front so that next receive appends after them
                if (leftover > 0)
                    Buffer.Slice(offset, leftover).CopyTo(Buffer);
            }

            // Re-arm receive
            if (Connected && Socket.Connected)
            {
                e.SetBuffer(Memory[_bytesReadIn..]);
                Socket.ReceiveAndForget(e, ReceiveEventHandler);
            }
        }
        catch
        {
            CloseTransport();
        }
        finally
        {
            ReceiveSync.Release();
        }
    }

    private void LogPacketHandlingError(Exception ex, int offset, int frameLength)
    {
        try
        {
            var slice = Buffer.Slice(offset, frameLength);
            var snapshot = slice.ToArray();
            var hex = BitConverter.ToString(snapshot).Replace("-", " ");
            var ascii = Encoding.ASCII.GetString(snapshot);

            Logger.WithTopics(
                      Topics.Entities.Client,
                      Topics.Entities.Packet,
                      Topics.Actions.Processing)
                  .WithProperty(this)
                  .LogError(
                      ex,
                      "Error handling packet (Offset={Offset}, Length={Length})\nHex: {Hex}\nASCII: {Ascii}",
                      offset,
                      frameLength,
                      hex,
                      ascii);
        }
        catch { }
    }

    /// <inheritdoc />
    public virtual void Send<T>(T obj) where T : IPacketSerializable
    {
        if (!Connected || !Socket.Connected) return;

        var packet = PacketSerializer.Serialize(obj);
        Send(ref packet);
    }

    public virtual void Send(ref Packet packet)
    {
        if (!Connected || !Socket.Connected) return;

        if (LogRawPackets)
            Logger.WithTopics(
                      Topics.Qualifiers.Raw,
                      Topics.Entities.Client,
                      Topics.Entities.Packet,
                      Topics.Actions.Send)
                  .WithProperty(this)
                  .LogTrace("[Snd] {Packet}", packet.ToString());

        packet.IsEncrypted = IsEncrypted(packet.OpCode);

        if (packet.IsEncrypted)
        {
            packet.Sequence = (byte)(Interlocked.Increment(ref Sequence) - 1);
            Encrypt(ref packet);
        }

        // [signature][len_hi][len_lo][opcode][sequence][payload]
        var buffer = RentWireBuffer(ref packet, out var length);
        var args = DequeueArgs(buffer, length);
        Socket.SendAndForget(args, ReuseSocketAsyncEventArgs);
    }

    public abstract bool IsEncrypted(byte opCode);
    public abstract void Encrypt(ref Packet packet);

    public virtual void CloseTransport()
    {
        if (Interlocked.Exchange(ref _disconnecting, 1) == 1) return;

        Connected = false;

        try { Socket.Shutdown(SocketShutdown.Both); } catch { }
        try { OnDisconnected?.Invoke(this, EventArgs.Empty); } catch { }

        Dispose();
    }

    public virtual void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        GC.SuppressFinalize(this);

        try { MemoryHandle.Dispose(); } catch { }
        try { MemoryOwner.Dispose(); } catch { }
        try { _receiveArgs?.Dispose(); } catch { }

        // Drain pool to dispose any outstanding rented send buffers
        while (SocketArgsQueue.TryDequeue(out var e))
        {
            try
            {
                if (e.UserToken is byte[] buffer)
                {
                    e.UserToken = null;
                    SendBufferPool.Return(buffer);
                }
            }
            catch { }

            try { e.Dispose(); } catch { }
        }

        try { Socket.Close(); } catch { }
    }

    public virtual void Close() => Socket.Close();

    #endregion

    #region Utility

    /// <summary>
    /// Disposes memory owner if present and recycles SAEA back to the queue
    /// </summary>
    private void ReuseSocketAsyncEventArgs(object? sender, SocketAsyncEventArgs e)
    {
        if (e.UserToken is byte[] buffer)
        {
            e.UserToken = null;
            SendBufferPool.Return(buffer);
        }

        if (e.SocketError != SocketError.Success)
        {
            try { CloseTransport(); } catch { }
            return;
        }

        e.BufferList = null;
        SocketArgsQueue.Enqueue(e);
    }

    private SocketAsyncEventArgs CreateArgs()
    {
        var args = new SocketAsyncEventArgs();
        args.Completed += ReuseSocketAsyncEventArgs;
        return args;
    }

    private SocketAsyncEventArgs DequeueArgs(byte[] buffer, int length)
    {
        if (!SocketArgsQueue.TryDequeue(out var args))
            args = CreateArgs();

        args.UserToken = buffer;
        args.SetBuffer(buffer, 0, length);
        return args;
    }

    private static byte[] RentWireBuffer(ref Packet packet, out int length)
    {
        length = packet.GetWireLength();
        var buffer = SendBufferPool.Rent(length);
        packet.WriteTo(buffer.AsSpan(0, length));
        return buffer;
    }

    #endregion
}
