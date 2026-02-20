using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

using Chaos.Common.Identity;
using Chaos.Common.Synchronization;
using Chaos.Cryptography.Abstractions;
using Chaos.Networking.Extensions;
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
    private int _inReceiveCallback;
    private int _inSendCallback;

    private const int ReceiveBufferSize = 64 * 1024;
    public virtual int MaxPacketLength { get; } = 8 * 1024;
    private int Sequence;

    public bool Connected { get; set; }

    public ICrypto Crypto { get; set; }

    /// <summary>Whether or not to log raw packet data to Trace</summary>
    public bool LogRawPackets { get; set; }

    public uint Id { get; }

    /// <summary>The logger for logging client-related events</summary>
    protected ILogger<SocketTransportBase> Logger { get; }

    /// <summary>The packet serializer for serializing and deserializing packets</summary>
    protected IPacketSerializer PacketSerializer { get; }

    public FifoSemaphoreSlim ReceiveSync { get; }

    public IPAddress RemoteIp { get; }

    public Socket Socket { get; }

    private readonly byte[] ReceiveBuffer = GC.AllocateUninitializedArray<byte>(ReceiveBufferSize);
    private Span<byte> Buffer => ReceiveBuffer;

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

        Logger = logger;
        PacketSerializer = packetSerializer;
        RemoteIp = (Socket.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.None;

        ReceiveSync = new FifoSemaphoreSlim(1, 1, $"{GetType().Name} {RemoteIp} (Socket)");

        SocketArgsQueue = new ConcurrentQueue<SocketAsyncEventArgs>();

        for (var i = 0; i < 50; i++)
            SocketArgsQueue.Enqueue(CreateArgs());

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
        _receiveArgs.SetBuffer(ReceiveBuffer, 0, ReceiveBufferSize);
        _receiveArgs.Completed += ReceiveCompleted;

        ArmReceive(_receiveArgs);
    }

    private void ReceiveCompleted(object? sender, SocketAsyncEventArgs e)
    {
        // Force sequential processing
        if (Interlocked.Exchange(ref _inReceiveCallback, 1) == 1)
            return;

        try
        {
            // Drive the receive chain without recursion even if completions are synchronous
            while (Connected && Socket.Connected)
            {
                var disp = AppendAndProcess(e);
                if (disp == ReceiveDisposition.Closed)
                    return;
                if (disp == ReceiveDisposition.Paused)
                    return;

                // Re-arm for the next receive
                e.SetBuffer(_bytesReadIn, ReceiveBufferSize - _bytesReadIn);
                if (Socket.ReceiveAsync(e))
                    break;
            }
        }
        catch
        {
            CloseTransport();
        }
        finally
        {
            Volatile.Write(ref _inReceiveCallback, 0);
        }
    }

    private void ArmReceive(SocketAsyncEventArgs e)
    {
        if (!Socket.ReceiveAsync(e))
            ReceiveCompleted(this, e);
    }

    private enum ReceiveDisposition : byte
    {
        Closed = 0,
        Continue = 1,
        Paused = 2
    }

    private ReceiveDisposition AppendAndProcess(SocketAsyncEventArgs e)
    {
        var bytesRead = e.BytesTransferred;

        // Transport rails
        if (bytesRead == 0 || e.SocketError != SocketError.Success)
        {
            CloseTransport();
            return ReceiveDisposition.Closed;
        }

        // Update count of valid bytes in receive buffer
        _bytesReadIn += bytesRead;

        if ((uint)_bytesReadIn > (uint)ReceiveBufferSize)
        {
            Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet)
                  .WithProperty(this)
                  .LogWarning(
                      "Disconnecting client {RemoteIp} due to receive buffer overflow ({ReceiveBufferSize})",
                      RemoteIp,
                      ReceiveBufferSize);

            CloseTransport();
            return ReceiveDisposition.Closed;
        }

        return ProcessBufferedFrames();
    }

    private ReceiveDisposition ProcessBufferedFrames()
    {
        var offset = 0;

        // Minimum wire frame is 4 bytes: [signature][len_hi][len_lo][opcode]
        while ((uint)(_bytesReadIn - offset) >= 4u)
        {
            // Signature rail
            byte sig = Buffer[offset];
            if (sig != 0xAA)
            {
                Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet)
                      .WithProperty(this)
                      .LogWarning(
                          "Disconnecting client {RemoteIp} due to bad signature: {Sig} (BytesIn={BytesIn}, Offset={Offset})",
                          RemoteIp,
                          sig,
                          _bytesReadIn,
                          offset);

                CloseTransport();
                return ReceiveDisposition.Closed;
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
                return ReceiveDisposition.Closed;
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
                LogPacketHandlingError(ex, offset, frameLength);
                _bytesReadIn = 0;
                return ReceiveDisposition.Continue;
            }

            offset += frameLength;

            if (!vt.IsCompletedSuccessfully)
            {
                // Preserve ordering: do NOT arm another receive until this packet finishes
                SlideLeftover(offset);
                _ = AwaitPacketAsync(vt, offset - frameLength, frameLength);
                return ReceiveDisposition.Paused;
            }
        }

        SlideLeftover(offset);
        return ReceiveDisposition.Continue;
    }

    private void SlideLeftover(int offset)
    {
        if (offset <= 0) return;

        var leftover = _bytesReadIn - offset;
        _bytesReadIn = leftover;

        if (leftover > 0)
            Buffer.Slice(offset, leftover).CopyTo(Buffer);
    }

    private async Task AwaitPacketAsync(ValueTask vt, int offset, int frameLength)
    {
        try
        {
            await vt.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogPacketHandlingError(ex, offset, frameLength);
            _bytesReadIn = 0;
        }

        // Resume receive processing (parse any already-buffered bytes first)
        var args = _receiveArgs;
        if (args == null || !Connected || !Socket.Connected)
            return;

        try
        {
            // Process anything already in the buffer (from the previous read)
            var disp = ProcessBufferedFrames();
            if (disp != ReceiveDisposition.Continue)
                return;

            args.SetBuffer(_bytesReadIn, ReceiveBufferSize - _bytesReadIn);
            ArmReceive(args);
        }
        catch
        {
            CloseTransport();
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

    public virtual void Send<T>(T obj) where T : IPacketSerializable
    {
        if (!Connected || !Socket.Connected) return;

        if (PacketSerializer is IPooledPacketSerializer pooled && pooled.TrySerializePooled(obj, out var opCode, out var payloadBuffer, out var payloadLength))
        {
            try
            {
                var payloadSpan = payloadBuffer.AsSpan(0, payloadLength);
                var packet = new Packet(opCode)
                {
                    Buffer = payloadSpan
                };

                Send(ref packet);
                return;
            }
            finally
            {
                pooled.ReturnPooled(payloadBuffer);
            }
        }

        // Fallback
        var packetFallback = PacketSerializer.Serialize(obj);
        Send(ref packetFallback);
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

        // Start first send (handle sync completion)
        StartSend(args);
    }

    private void StartSend(SocketAsyncEventArgs args)
    {
        try
        {
            if (!Socket.SendAsync(args))
                ReuseSocketAsyncEventArgs(this, args);
        }
        catch
        {
            FailSendAndClose(args);
        }
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

        try { _receiveArgs?.Dispose(); } catch { }

        // Drain pool to dispose any outstanding rented send buffers
        while (SocketArgsQueue.TryDequeue(out var e))
        {
            try
            {
                if (e.UserToken is SendState state)
                {
                    ReturnSendBuffer(state);
                    e.UserToken = null;
                }
                else if (e.UserToken is byte[] legacyBuffer)
                {
                    e.UserToken = null;
                    SendBufferPool.Return(legacyBuffer);
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
        // Serialize completion processing even for sync-completion chains
        if (Interlocked.Exchange(ref _inSendCallback, 1) == 1)
            return;

        try
        {
            while (true)
            {
                if (e.UserToken is not SendState state)
                {
                    RecycleArgs(e);
                    return;
                }

                if (e.SocketError != SocketError.Success)
                {
                    FailSendAndClose(e, state);
                    return;
                }

                var sent = e.BytesTransferred;
                if (sent <= 0)
                {
                    FailSendAndClose(e, state);
                    return;
                }

                state.Offset += sent;
                state.Remaining -= sent;

                if (state.Remaining <= 0)
                {
                    CompleteSend(e, state);
                    return;
                }

                // Continue partial send
                e.SetBuffer(state.Buffer, state.Offset, state.Remaining);

                if (Socket.SendAsync(e))
                    return; // async completion will continue here later

                // sync completion -> loop and continue progress accounting
            }
        }
        catch
        {
            FailSendAndClose(e, e.UserToken as SendState);
        }
        finally
        {
            Volatile.Write(ref _inSendCallback, 0);
        }
    }

    private void CompleteSend(SocketAsyncEventArgs e, SendState state)
    {
        ReturnSendBuffer(state);
        e.SetBuffer(null, 0, 0);
        e.BufferList = null;
        SocketArgsQueue.Enqueue(e);
    }

    private void FailSendAndClose(SocketAsyncEventArgs e, SendState? state = null)
    {
        var sendState = state ?? e.UserToken as SendState;
        if (sendState is not null)
            ReturnSendBuffer(sendState);

        e.SetBuffer(null, 0, 0);
        e.BufferList = null;

        try { CloseTransport(); } catch { }
    }

    private static void ReturnSendBuffer(SendState state)
    {
        var buffer = state.Buffer;
        state.Buffer = [];
        state.Offset = 0;
        state.Remaining = 0;

        if (buffer is { Length: > 0 })
            SendBufferPool.Return(buffer);
    }

    private void RecycleArgs(SocketAsyncEventArgs e)
    {
        e.SetBuffer(null, 0, 0);
        e.BufferList = null;
        SocketArgsQueue.Enqueue(e);
    }

    private SocketAsyncEventArgs CreateArgs()
    {
        var args = new SocketAsyncEventArgs
        {
            UserToken = new SendState()
        };

        args.Completed += ReuseSocketAsyncEventArgs;
        return args;
    }

    private SocketAsyncEventArgs DequeueArgs(byte[] buffer, int length)
    {
        if (!SocketArgsQueue.TryDequeue(out var args))
            args = CreateArgs();

        if (args.UserToken is not SendState state)
        {
            state = new SendState();
            args.UserToken = state;
        }

        state.Buffer = buffer;
        state.Offset = 0;
        state.Remaining = length;

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
