using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Chaos.Common.Identity;
using Chaos.Common.Synchronization;
using Chaos.Extensions.Networking;
using Chaos.NLog.Logging.Definitions;
using Chaos.NLog.Logging.Extensions;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Microsoft.Extensions.Logging;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Provides the ability to send and receive packets over a socket.
/// </summary>
public abstract class SocketClientBase : ISocketClient, IDisposable
{
    private readonly SslStream SslStream;

    private readonly ConcurrentQueue<SocketAsyncEventArgs> SocketArgsQueue;
    private int Count;
    private int Sequence;

    /// <inheritdoc />
    public bool Connected { get; set; }

    /// <summary>
    ///     Whether or not to log raw packet data to Trace.
    /// </summary>
    public bool LogRawPackets { get; set; }

    /// <inheritdoc />
    public uint Id { get; }

    /// <summary>
    ///     The logger for logging client-related events.
    /// </summary>
    protected ILogger<SocketClientBase> Logger { get; }

    private MemoryHandle MemoryHandle { get; }

    private IMemoryOwner<byte> MemoryOwner { get; }

    /// <summary>
    ///     The packet serializer for serializing and deserializing packets.
    /// </summary>
    protected IPacketSerializer PacketSerializer { get; }

    /// <inheritdoc />
    public FifoSemaphoreSlim ReceiveSync { get; }

    /// <inheritdoc />
    public IPAddress RemoteIp { get; }

    /// <inheritdoc />
    public Socket Socket { get; }

    private unsafe Span<byte> Buffer => new(MemoryHandle.Pointer, ushort.MaxValue * 4);

    private Memory<byte> Memory => MemoryOwner.Memory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SocketClientBase" /> class.
    /// </summary>
    /// <param name="socket"></param>
    /// <param name="serverCertificate"></param>
    /// <param name="packetSerializer"></param>
    /// <param name="logger"></param>
    protected SocketClientBase(
        Socket socket,
        X509Certificate serverCertificate, // Add server certificate parameter
        IPacketSerializer packetSerializer,
        ILogger<SocketClientBase> logger)
    {
        Id = SequentialIdGenerator<uint>.Shared.NextId;
        Socket = socket;

        // Wrap the socket in an SslStream
        SslStream = new SslStream(new NetworkStream(socket), false);

        // Authenticate as client (or server depending on the use case)
        SslStream.AuthenticateAsClient("ServerName"); // Replace with actual server name if needed.

        MemoryOwner = MemoryPool<byte>.Shared.Rent(ushort.MaxValue * 4);
        MemoryHandle = Memory.Pin();
        Logger = logger;
        PacketSerializer = packetSerializer;
        RemoteIp = (Socket.RemoteEndPoint as IPEndPoint)?.Address ?? IPAddress.None;
        ReceiveSync = new FifoSemaphoreSlim(1, 1, $"{GetType().Name} {RemoteIp} (Socket)");

        var initialArgs = Enumerable.Range(0, 5)
                                    .Select(_ => CreateArgs());
        SocketArgsQueue = new ConcurrentQueue<SocketAsyncEventArgs>(initialArgs);
        Connected = false;
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        SslStream.Dispose();
        GC.SuppressFinalize(this);

        try
        {
            MemoryHandle.Dispose();
        }
        catch
        {
            //ignored
        }

        try
        {
            MemoryOwner.Dispose();
        }
        catch
        {
            //ignored
        }

        try
        {
            Socket.Close();
        }
        catch
        {
            //ignored
        }
    }

    /// <inheritdoc />
    public event EventHandler? OnDisconnected;

    #region Actions
    /// <inheritdoc />
    public virtual void SetSequence(byte newSequence) => Sequence = newSequence;
    #endregion

    /// <summary>
    ///     Asynchronously handles a span buffer as a packet.
    /// </summary>
    /// <param name="span"></param>
    /// <returns></returns>
    protected abstract ValueTask HandlePacketAsync(Span<byte> span);

    #region Networking
    /// <inheritdoc />
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
        await ReceiveSync.WaitAsync()
                         .ConfigureAwait(false);

        try
        {
            var shouldReset = false;
            var count = e.BytesTransferred;

            if (count == 0)
            {
                Disconnect();
                return;
            }

            Count += count;
            var offset = 0;

            while (Count > 3)
            {
                var packetLength = (Buffer[offset + 1] << 8) + Buffer[offset + 2] + 3;

                if (Count < packetLength)
                    break;

                if (Count < 4)
                    break;

                try
                {
                    await HandlePacketAsync(Buffer.Slice(offset, packetLength))
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    void InnerCatch()
                    {
                        var buffer = Buffer.TrimEnd((byte)0);
                        var hex = BitConverter.ToString(buffer.ToArray())
                                              .Replace("-", " ");
                        var ascii = Encoding.ASCII.GetString(buffer);

                        Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet, Topics.Actions.Processing)
                              .WithProperty(this)
                              .LogError(
                                  ex,
                                  "Exception while handling a packet for {@ClientType}. (Count: {Count}, Offset: {Offset}, BufferHex: {BufferHex}, BufferAscii: {BufferAscii})",
                                  GetType().Name,
                                  Count,
                                  offset,
                                  hex,
                                  ascii);
                    }

                    InnerCatch();
                    shouldReset = true;
                }

                Count -= packetLength;
                offset += packetLength;
            }

            if (shouldReset)
                Count = 0;

            if (Count > 0)
                Buffer.Slice(offset, Count)
                      .CopyTo(Buffer);

            e.SetBuffer(Memory[Count..]);
            Socket.ReceiveAndForget(e, ReceiveEventHandler);
        }
        catch (Exception)
        {
            Disconnect();
        }
        finally
        {
            if (Connected)
                ReceiveSync.Release();
        }
    }

    /// <inheritdoc />
    public virtual void Send<T>(T obj) where T : IPacketSerializable
    {
        var packet = PacketSerializer.Serialize(obj);
        Send(ref packet);
    }

    /// <inheritdoc />
    public virtual void Send(ref Packet packet)
    {
        if (!Connected) return;

        var data = packet.ToMemory().ToArray();
        SslStream.Write(data, 0, data.Length);
    }

    /// <inheritdoc />
    public virtual void Disconnect()
    {
        if (!Connected)
            return;

        Connected = false;

        try
        {
            Socket.Shutdown(SocketShutdown.Receive);
        }
        catch
        {
            //ignored
        }

        try
        {
            OnDisconnected?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            //ignored
        }

        Dispose();
    }

    public virtual void Close() => Socket.Close();

    #endregion

    #region Utility
    private void ReuseSocketAsyncEventArgs(object? sender, SocketAsyncEventArgs e) => SocketArgsQueue.Enqueue(e);

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
