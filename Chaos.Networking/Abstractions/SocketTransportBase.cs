using System.Buffers;
using System.Diagnostics;
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
    private readonly MemoryPool<byte> _sendPool = MemoryPool<byte>.Shared;
    private readonly int _defaultSendCapacity;
    private int _disposed;


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
        _defaultSendCapacity = Math.Max(1024, MaxPacketLength);

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
                long receives = 0;
                long bytes = 0;
                var last = Stopwatch.GetTimestamp();

                while (!token.IsCancellationRequested && Socket.Connected)
                {
                    var writeMem = Memory.Slice(_count);

                    int bytesRead;
                    bytesRead = await Socket.ReceiveAsync(writeMem, SocketFlags.None, token)
                                            .ConfigureAwait(false);
                    Interlocked.Increment(ref receives);
                    Interlocked.Add(ref bytes, bytesRead);

                    if (Stopwatch.GetElapsedTime(last) >= TimeSpan.FromSeconds(1))
                    {
                        var r = Interlocked.Exchange(ref receives, 0);
                        var b = Interlocked.Exchange(ref bytes, 0);
                        Logger.LogInformation("RX: {Receives}/s, {Bytes}/s, buffered={Buffered}", r, b, _count);
                        last = Stopwatch.GetTimestamp();
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

        var wireLength = packet.GetWireLength();
        var args = DequeueArgs(wireLength);

        // write into the reusable per-SAEA buffer
        var state = (SendArgsState)args.UserToken!;
        packet.WriteTo(state.Current.Span);
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

        var wireLength = packet.GetWireLength();
        var args = DequeueArgs(wireLength);

        // write into the reusable per-SAEA buffer
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

        // Drain SAEA pool and dispose their buffers.
        while (SocketArgsQueue.TryDequeue(out var args))
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

        try { _memoryHandle.Dispose(); } catch { }
        try { _memoryOwner.Dispose(); } catch { }

        try { Socket.Close(); } catch { }
    }

    public virtual void Close() => Socket.Close();

    #endregion

    #region Utility (send pooling)

    private void ReuseSocketAsyncEventArgs(object? sender, SocketAsyncEventArgs e)
    {
        // Snapshot disposed flag once
        var disposed = Volatile.Read(ref _disposed) == 1;

        // If the socket op failed, we should close the transport (unless we're already disposing).
        // IMPORTANT: this callback runs on the completion path for BOTH send+receive in your extension style.
        // If you later share the same callback for receive args too, consider branching based on LastOperation.
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
            // Tear down the per-SAEA buffer and the args themselves
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

        // Reset view to empty so we don't accidentally resend old length.
        try
        {
            if (e.UserToken is SendArgsState state)
                e.SetBuffer(state.Owner.Memory.Slice(0, 0));
            else
                e.SetBuffer(Memory<byte>.Empty);
        }
        catch
        {
            // If SetBuffer throws (rare, but can happen if args is in a bad state), don't poison the pool.
            try { CloseTransport(); } catch { }
            return;
        }

        SocketArgsQueue.Enqueue(e);
    }

    private SocketAsyncEventArgs CreateArgs()
    {
        var args = new SocketAsyncEventArgs();
        args.Completed += ReuseSocketAsyncEventArgs;

        var owner = _sendPool.Rent(_defaultSendCapacity);
        args.UserToken = new SendArgsState(owner);

        // Start empty; we set the slice per send.
        args.SetBuffer(owner.Memory.Slice(0, 0));
        return args;
    }

    private SocketAsyncEventArgs DequeueArgs(int requiredLength)
    {
        if (!SocketArgsQueue.TryDequeue(out var args))
            args = CreateArgs();

        if (args.UserToken is not SendArgsState state)
        {
            // Shouldn't happen unless something overwrote UserToken.
            var owner = _sendPool.Rent(Math.Max(requiredLength, _defaultSendCapacity));
            state = new SendArgsState(owner);
            args.UserToken = state;
        }

        state.EnsureCapacity(requiredLength, _sendPool);
        args.SetBuffer(state.Current);

        return args;
    }

    #endregion

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

        public void Dispose()
        {
            Owner.Dispose();
        }
    }
}
