using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
public abstract class SocketClientBase : ISocketClient, IDisposable
{
    private const int DefaultMaxPacketLength = 12 * 1024;      // 12 KB cap (header + payload)
    private const int DefaultReceiveBufferSize = 32 * 1024;    // 32 KB rolling receive buffer
    private const int DefaultSendQueueCapacity = 1024;

    private readonly Channel<ReadOnlyMemory<byte>> _sendChannel;
    private readonly CancellationTokenSource _sendCancellation = new();

    private int Count;
    private int Sequence;

    public virtual int MaxPacketLength { get; } = DefaultMaxPacketLength;

    /// <inheritdoc />
    public bool Connected { get; set; }

    /// <inheritdoc />
    public ICrypto Crypto { get; set; }

    /// <summary>Whether or not to log raw packet data to Trace</summary>
    public bool LogRawPackets { get; set; }

    /// <inheritdoc />
    public uint Id { get; }

    /// <summary>The logger for logging client-related events</summary>
    protected ILogger<SocketClientBase> Logger { get; }

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

    protected SocketClientBase(
        Socket socket,
        ICrypto crypto,
        IPacketSerializer packetSerializer,
        ILogger<SocketClientBase> logger)
    {
        Id = SequentialIdGenerator<uint>.Shared.NextId;
        Socket = socket;
        Crypto = crypto;
        PacketSerializer = packetSerializer;
        Logger = logger;

        // Per-client receive buffer. Pool may round this up, which is fine.
        MemoryOwner = MemoryPool<byte>.Shared.Rent(DefaultReceiveBufferSize);
        MemoryHandle = Memory.Pin();

        RemoteIp = (Socket.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.None;
        ReceiveSync = new FifoSemaphoreSlim(1, 1, $"{GetType().Name} {RemoteIp} (Socket)");

        // Bounded send queue with DropOldest semantics.
        // Under load, old buffered packets are discarded in favor of new ones.
        var channelOptions = new BoundedChannelOptions(DefaultSendQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        };

        _sendChannel = Channel.CreateBounded<ReadOnlyMemory<byte>>(channelOptions);

        Connected = false;

        // Dedicated send loop per client
        _ = RunSendLoopAsync();
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);

        try { _sendCancellation.Cancel(); } catch { }
        try { _sendCancellation.Dispose(); } catch { }

        try { MemoryHandle.Dispose(); } catch { }
        try { MemoryOwner.Dispose(); } catch { }

        try { Socket.Close(); } catch { }
    }

    public event EventHandler? OnDisconnected;

    #region Actions
    public virtual void SetSequence(byte newSequence) => Sequence = newSequence;
    #endregion

    protected abstract ValueTask HandlePacketAsync(Span<byte> span);

    #region Networking
    public virtual async void BeginReceive()
    {
        if (!Socket.Connected)
            return;

        Connected = true;
        await Task.Yield();

        var args = new SocketAsyncEventArgs();
        args.SetBuffer(Memory);
        args.Completed += ReceiveEventHandler;

        Socket.ReceiveAndForget(args, ReceiveEventHandler);
    }

    private async void ReceiveEventHandler(object? sender, SocketAsyncEventArgs e)
    {
        await ReceiveSync.WaitAsync().ConfigureAwait(false);

        try
        {
            int bytesRead = e.BytesTransferred;

            if (bytesRead == 0)
            {
                Disconnect();
                return;
            }

            // Total bytes accumulated in the rolling buffer
            Count += bytesRead;

            int offset = 0;

            // Process as many complete packets as possible
            while (true)
            {
                // Need at least header length
                if (Count - offset < 5)
                    break;

                // Signature check
                byte sig = Buffer[offset];
                if (sig != 170) // 0xAA
                {
                    Disconnect();
                    return;
                }

                // Extract length
                int lengthHi = Buffer[offset + 1];
                int lengthLo = Buffer[offset + 2];
                int payloadLength = (lengthHi << 8) | lengthLo;

                // Full frame length = payload + 3 header bytes
                int frameLength = payloadLength + 3;

                if (frameLength <= 0 || frameLength > MaxPacketLength)
                {
                    Disconnect();
                    return;
                }

                // Wait if not enough bytes for full frame
                if (Count - offset < frameLength)
                    break;

                // We have a complete packet: slice it safely
                var frame = Buffer.Slice(offset, frameLength);

                try
                {
                    await HandlePacketAsync(frame).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogPacketError(ex, offset, frameLength);
                    // Reset to avoid poisoned state
                    Count = 0;
                    break;
                }

                offset += frameLength;

                if (offset >= Count)
                    break;
            }

            // Shift leftover bytes to beginning of buffer
            if (offset > 0)
            {
                Count -= offset;
                if (Count > 0)
                    Buffer.Slice(offset, Count).CopyTo(Buffer);
            }

            // Continue reading after leftover bytes
            e.SetBuffer(Memory.Slice(Count));
            Socket.ReceiveAndForget(e, ReceiveEventHandler);
        }
        catch
        {
            Disconnect();
        }
        finally
        {
            ReceiveSync.Release();
        }
    }

    private void LogPacketError(Exception ex, int offset, int length)
    {
        try
        {
            var slice = Buffer.Slice(offset, length);
            var hex = BitConverter.ToString(slice.ToArray()).Replace("-", " ");
            var ascii = Encoding.ASCII.GetString(slice);

            Logger.WithTopics(
                      Topics.Entities.Client,
                      Topics.Entities.Packet,
                      Topics.Actions.Processing)
                  .WithProperty(this)
                  .LogError(
                      ex,
                      "Error handling packet (Offset={Offset}, Length={Length})\nHex: {Hex}\nASCII: {Ascii}",
                      offset,
                      length,
                      hex,
                      ascii);
        }
        catch
        {
            // If logging the packet fails, swallow to avoid bringing down the server.
        }
    }

    /// <inheritdoc />
    public virtual void Send<T>(T obj) where T : IPacketSerializable
    {
        if (!Connected || !Socket.Connected)
            return;

        var packet = PacketSerializer.Serialize(obj);

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

        var memory = packet.ToMemory();

        EnqueueSend(memory);
    }

    public virtual void Send(ref Packet packet)
    {
        if (!Connected || !Socket.Connected)
            return;

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

        var memory = packet.ToMemory();

        EnqueueSend(memory);
    }

    public abstract bool IsEncrypted(byte opCode);
    public abstract void Encrypt(ref Packet packet);

    public virtual void Disconnect()
    {
        if (!Connected)
            return;

        Connected = false;

        try { _sendChannel.Writer.TryComplete(); } catch { }
        try { _sendCancellation.Cancel(); } catch { }

        try { Socket.Shutdown(SocketShutdown.Receive); } catch { }
        try { OnDisconnected?.Invoke(this, EventArgs.Empty); } catch { }

        Dispose();
    }

    public virtual void Close() => Socket.Close();
    #endregion

    #region Utility
    private void EnqueueSend(ReadOnlyMemory<byte> buffer)
    {
        // DropOldest mode:
        // - If there's space, TryWrite enqueues normally.
        // - If full, the channel drops one old item internally to make room for this one.
        // TryWrite only returns false if the writer is completed/closed.
        _sendChannel.Writer.TryWrite(buffer);
    }

    private async Task RunSendLoopAsync()
    {
        try
        {
            while (await _sendChannel.Reader
                       .WaitToReadAsync(_sendCancellation.Token)
                       .ConfigureAwait(false))
            {
                while (_sendChannel.Reader.TryRead(out var buffer))
                {
                    if (!Connected || !Socket.Connected)
                        return;

                    try
                    {
                        var remaining = buffer.Length;
                        var offset = 0;

                        while (remaining > 0)
                        {
                            var slice = buffer.Slice(offset, remaining);
                            var sent = await Socket
                                .SendAsync(slice, SocketFlags.None, _sendCancellation.Token)
                                .ConfigureAwait(false);

                            if (sent <= 0)
                            {
                                Disconnect();
                                return;
                            }

                            offset += sent;
                            remaining -= sent;
                        }
                    }
                    catch
                    {
                        Disconnect();
                        return;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal during shutdown/disconnect.
        }
        catch
        {
            Disconnect();
        }
    }
    #endregion
}
