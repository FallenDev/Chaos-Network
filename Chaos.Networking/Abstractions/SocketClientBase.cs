using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
    private readonly ConcurrentQueue<SocketAsyncEventArgs> SocketArgsQueue;
    private int Count;

    public virtual int MaxPacketLength { get; } = 25 * 1024; // 25 KB
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

        // Rent receive buffer to match the rail (pool may round up)
        MemoryOwner = MemoryPool<byte>.Shared.Rent(MaxPacketLength);
        MemoryHandle = Memory.Pin();

        Logger = logger;
        PacketSerializer = packetSerializer;
        RemoteIp = (Socket.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.None;
        ReceiveSync = new FifoSemaphoreSlim(1, 1, $"{GetType().Name} {RemoteIp} (Socket)");

        var initialArgs = Enumerable.Range(0, 5).Select(_ => CreateArgs());
        SocketArgsQueue = new ConcurrentQueue<SocketAsyncEventArgs>(initialArgs);

        Connected = false;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
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
        if (!Socket.Connected) return;
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

                // Signature check (optional but strongly recommended)
                byte sig = Buffer[offset];
                if (sig != 170) // 0xAA
                {
                    // Bad signature ? malformed stream. Disconnect.
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

            // Continue reading into the remainder of available space
            e.SetBuffer(Memory.Slice(Count));
            Socket.ReceiveAndForget(e, ReceiveEventHandler);
        }
        catch
        {
            Disconnect();
        }
        finally
        {
            if (Connected)
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
                      "Error handling packet (Offset={Offset}, Length={Length})\nHex: {Hex}\nAscii: {Ascii}",
                      offset, length, hex, ascii);
        }
        catch
        {
            // ignore logging failures
        }
    }

    public virtual void Send<T>(T obj) where T : IPacketSerializable
    {
        if (!Connected || !Socket.Connected) return;

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

        // [signature][len_hi][len_lo][opcode][sequence][payload]
        var memory = packet.ToMemory();

        var args = DequeueArgs(memory);
        Socket.SendAndForget(args, ReuseSocketAsyncEventArgs);
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
        var memory = packet.ToMemory();

        var args = DequeueArgs(memory);
        Socket.SendAndForget(args, ReuseSocketAsyncEventArgs);
    }

    public abstract bool IsEncrypted(byte opCode);
    public abstract void Encrypt(ref Packet packet);

    public virtual void Disconnect()
    {
        if (!Connected) return;

        Connected = false;

        try { Socket.Shutdown(SocketShutdown.Receive); } catch { }
        try { OnDisconnected?.Invoke(this, EventArgs.Empty); } catch { }

        Dispose();
    }

    public virtual void Close() => Socket.Close();
    #endregion

    #region Utility
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
