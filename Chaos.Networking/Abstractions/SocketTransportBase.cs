using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;

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
/// </summary>
public abstract class SocketTransportBase : ISocketTransport, IDisposable
{
    private readonly ConcurrentQueue<SocketAsyncEventArgs> _socketArgsQueue;

    private const int ReceiveBufferSize = 64 * 1024;
    // Max packet length (including header) per protocol spec
    public virtual int MaxPacketLength { get; } = 8 * 1024;

    // Bytes currently in rolling buffer
    private int _count;
    private int _sequence;

    private int _disconnecting;
    private int _receiveStarted;
    private int _disposed;

    // Receive SAEA + reentrancy gate
    private readonly SocketAsyncEventArgs _recvArgs;
    private readonly SemaphoreSlim _recvGate = new(1, 1);

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

    // Send pooling
    private readonly MemoryPool<byte> _sendPool = MemoryPool<byte>.Shared;
    private readonly int _defaultSendCapacity;

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

        SocketExtensions.ConfigureTcpSocket(Socket);

        Logger = logger;
        PacketSerializer = packetSerializer;

        RemoteIp = (Socket.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.None;

        _recvOwner = MemoryPool<byte>.Shared.Rent(ReceiveBufferSize);
        _recvHandle = _recvOwner.Memory.Pin();

        _defaultSendCapacity = Math.Max(1024, MaxPacketLength);

        var initialArgs = Enumerable.Range(0, 10).Select(_ => CreateSendArgs());
        _socketArgsQueue = new ConcurrentQueue<SocketAsyncEventArgs>(initialArgs);

        _recvArgs = new SocketAsyncEventArgs();
        _recvArgs.Completed += ReceiveCompleted;

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

        _ = StartReceiveCoreAsync();
    }

    private async Task StartReceiveCoreAsync()
    {
        await Task.Yield();

        if (!Connected || !Socket.Connected) return;

        try
        {
            _recvArgs.SetBuffer(RecvMemory.Slice(_count));
            Socket.ReceiveAndForget(_recvArgs, ReceiveCompleted);
        }
        catch
        {
            CloseTransport();
        }
    }

    private async void ReceiveCompleted(object? sender, SocketAsyncEventArgs e)
    {
        await _recvGate.WaitAsync().ConfigureAwait(false);

        try
        {
            if (Volatile.Read(ref _disposed) == 1 || !Connected)
                return;

            if (e.SocketError != SocketError.Success)
            {
                CloseTransport();
                return;
            }

            var bytesRead = e.BytesTransferred;

            // Remote closed gracefully
            if (bytesRead == 0)
            {
                CloseTransport();
                return;
            }

            _count += bytesRead;

            var offset = 0;
            var shouldReset = false;

            while (_count > 3)
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

                int payloadLength = (RecvBuffer[offset + 1] << 8) | RecvBuffer[offset + 2];
                int frameLength = payloadLength + 3;

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

                if (_count - offset < frameLength)
                    break;

                try
                {
                    await OnPacketAsync(RecvBuffer.Slice(offset, frameLength)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
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

                    shouldReset = true;
                }

                _count -= frameLength;
                offset += frameLength;
            }

            if (shouldReset)
            {
                _count = 0;
                offset = 0;
            }

            if (_count > 0 && offset > 0)
            {
                RecvBuffer.Slice(offset, _count).CopyTo(RecvBuffer);
            }

            if (_count >= ReceiveBufferSize)
            {
                Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet)
                      .WithProperty(this)
                      .LogWarning("Disconnecting client {RemoteIp} due to receive buffer overflow ({ReceiveBufferSize})", RemoteIp, ReceiveBufferSize);

                CloseTransport();
                return;
            }

            // Post next receive into remaining space
            if (Connected && Socket.Connected && Volatile.Read(ref _disposed) == 0)
            {
                e.SetBuffer(RecvMemory.Slice(_count));
                Socket.ReceiveAndForget(e, ReceiveCompleted);
            }
        }
        catch
        {
            CloseTransport();
        }
        finally
        {
            try { _recvGate.Release(); } catch { }
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

        try { _recvArgs.Completed -= ReceiveCompleted; } catch { }
        try { _recvArgs.Dispose(); } catch { }

        try { _recvGate.Dispose(); } catch { }

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

        var owner = _sendPool.Rent(_defaultSendCapacity);
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
            var owner = _sendPool.Rent(Math.Max(requiredLength, _defaultSendCapacity));
            state = new SendArgsState(owner);
            args.UserToken = state;
        }

        state.EnsureCapacity(requiredLength, _sendPool);
        args.SetBuffer(state.Current);

        return args;
    }

    private sealed class SendArgsState : IDisposable
    {
        public IMemoryOwner<byte> Owner;
        public int Capacity;
        public Memory<byte> Current;

        public SendArgsState(IMemoryOwner<byte> owner)
        {
            Owner = owner;
            Capacity = owner.Memory.Length;
            Current = default;
        }

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
