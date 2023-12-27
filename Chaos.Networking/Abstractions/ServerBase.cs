using System.Net;
using System.Net.Sockets;

using Chaos.Common.Synchronization;
using Chaos.Extensions.Common;
using Chaos.Networking.Entities.Client;
using Chaos.Networking.Options;
using Chaos.NLog.Logging.Definitions;
using Chaos.NLog.Logging.Extensions;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Represents a base class for server implementations.
/// </summary>
/// <typeparam name="T">The type of the socket client.</typeparam>
public abstract class ServerBase<T> : BackgroundService, IServer<T> where T : ISocketClient
{
    /// <summary>
    ///     Delegate for handling client packets.
    /// </summary>
    /// <param name="client">The client sending the packet.</param>
    /// <param name="packet">The client packet received.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    public delegate ValueTask ClientHandler(T client, in ClientPacket packet);

    /// <summary>
    ///     An array of client handlers for handling incoming client packets.
    /// </summary>
    protected ClientHandler?[] ClientHandlers { get; }
    /// <summary>
    ///     The client registry for managing connected clients.
    /// </summary>
    protected IClientRegistry<T> ClientRegistry { get; }
    /// <summary>
    ///     The logger for logging server-related events.
    /// </summary>
    protected ILogger<ServerBase<T>> Logger { get; }
    /// <summary>
    ///     The server options for configuring the server instance.
    /// </summary>
    protected ServerOptions Options { get; }
    /// <summary>
    ///     The packet serializer for serializing and deserializing packets.
    /// </summary>
    protected IPacketSerializer PacketSerializer { get; }
    /// <summary>
    ///     The redirect manager for handling client redirects.
    /// </summary>
    protected IRedirectManager RedirectManager { get; }
    /// <summary>
    ///     The socket used for handling incoming connections.
    /// </summary>
    protected Socket Socket { get; }
    /// <summary>
    ///     A semaphore for synchronizing access to the server.
    /// </summary>
    protected FifoAutoReleasingSemaphoreSlim Sync { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ServerBase{T}" /> class.
    /// </summary>
    /// <param name="redirectManager">An instance of a redirect manager.</param>
    /// <param name="packetSerializer">An instance of a packet serializer.</param>
    /// <param name="clientRegistry">An instance of a client registry.</param>
    /// <param name="options">Configuration options for the server.</param>
    /// <param name="logger">A logger for the server.</param>
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    protected ServerBase(
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        IClientRegistry<T> clientRegistry,
        IOptions<ServerOptions> options,
        ILogger<ServerBase<T>> logger
    )
    {
        Options = options.Value;
        RedirectManager = redirectManager;
        Logger = logger;
        ClientRegistry = clientRegistry;
        PacketSerializer = packetSerializer;
        ClientHandlers = new ClientHandler?[byte.MaxValue];
        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        ConfigureTcpSocket(Socket);
        Sync = new FifoAutoReleasingSemaphoreSlim(1, 1);
        IndexHandlers();
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var endPoint = new IPEndPoint(IPAddress.Any, Options.Port);
        Socket.Bind(endPoint);
        Socket.Listen(1000);
        Socket.BeginAccept(OnConnection, Socket);
        Logger.WithTopics(Topics.Actions.Listening)
              .LogInformation("Listening on {@EndPoint}", endPoint.Port.ToString());

        await stoppingToken.WaitTillCanceled();

        try
        {
            Socket.Shutdown(SocketShutdown.Receive);
        }
        catch
        {
            //ignored
        }

        await Parallel.ForEachAsync(ClientRegistry, stoppingToken, (client, _) =>
            {
                client.Disconnect();
                return default;
            });

        await Socket.DisconnectAsync(false, stoppingToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Called when a new connection is accepted by the server.
    /// </summary>
    /// <param name="ar">The asynchronous result of the operation.</param>
    protected abstract void OnConnection(IAsyncResult ar);

    #region Handlers
    /// <summary>
    ///     Initializes the client handlers for the server.
    /// </summary>
    protected virtual void IndexHandlers()
    {
        ClientHandlers[(byte)ClientOpCode.HeartBeat] = OnHeartBeatAsync;
        ClientHandlers[(byte)ClientOpCode.SequenceChange] = OnSequenceChangeAsync;
        ClientHandlers[(byte)ClientOpCode.SynchronizeTicks] = OnSynchronizeTicksAsync;
    }

    /// <inheritdoc />
    public virtual ValueTask HandlePacketAsync(T client, in ClientPacket packet)
    {
        var handler = ClientHandlers[(byte)packet.OpCode];

        if (handler is not null) return handler(client, in packet);
        client.Disconnect();
        return default;
    }

    /// <summary>
    ///     Executes an asynchronous action for a client within a sychronized context
    /// </summary>
    /// <param name="client">The client to execute the action against</param>
    /// <param name="args">The args deserialized from the packet</param>
    /// <param name="action">The action that uses the args</param>
    /// <typeparam name="TArgs">The type of the args that were deserialized</typeparam>
    public virtual async ValueTask ExecuteHandler<TArgs>(T client, TArgs args, Func<T, TArgs, ValueTask> action)
    {
        await using var @lock = await Sync.WaitAsync();

        try
        {
            await action(client, args);
        }
        catch (Exception e)
        {
            Logger.WithTopics(Topics.Entities.Packet, Topics.Actions.Processing)
                  .LogError(
                      e,
                      "{@ClientType} failed to execute inner handler with args type {@ArgsType} ({@Args})",
                      client.GetType().Name,
                      args!.GetType().Name,
                      args);
        }
    }

    /// <summary>
    ///     Executes an asynchronous action for a client within a sychronized context
    /// </summary>
    /// <param name="client">The client to execute the action against</param>
    /// <param name="action">The action to be executed</param>
    public virtual async ValueTask ExecuteHandler(T client, Func<T, ValueTask> action)
    {
        await using var @lock = await Sync.WaitAsync();

        try
        {
            await action(client);
        }
        catch (Exception e)
        {
            Logger.WithTopics(Topics.Entities.Packet, Topics.Actions.Processing)
                  .LogError(e, "{@ClientType} failed to execute inner handler", client.GetType().Name);
        }
    }

    /// <inheritdoc />
    public virtual ValueTask OnHeartBeatAsync(T client, in ClientPacket packet)
    {
        _ = PacketSerializer.Deserialize<HeartBeatArgs>(in packet);

        //do nothing

        return default;
    }

    /// <inheritdoc />
    public ValueTask OnSequenceChangeAsync(T client, in ClientPacket packet)
    {
        client.SetSequence(packet.Sequence);

        return default;
    }

    /// <inheritdoc />
    public virtual ValueTask OnSynchronizeTicksAsync(T client, in ClientPacket packet)
    {
        _ = PacketSerializer.Deserialize<SynchronizeTicksArgs>(in packet);

        //do nothing

        return default;
    }
    #endregion

    private void ConfigureTcpSocket(Socket tcpSocket)
    {
        // Don't allow another socket to bind to this port.
        tcpSocket.ExclusiveAddressUse = true;

        // The socket will linger for 10 seconds after
        // Socket.Close is called.
        tcpSocket.LingerState = new LingerOption(true, 5);

        // Disable the Nagle Algorithm for this tcp socket.
        tcpSocket.NoDelay = true;

        // Set the receive buffer size to 8k
        tcpSocket.ReceiveBufferSize = 8192;

        // Set the timeout for synchronous receive methods to
        // 1 second (1000 milliseconds.)
        tcpSocket.ReceiveTimeout = 1000;

        // Set the send buffer size to 8k.
        tcpSocket.SendBufferSize = 8192;

        // Set the timeout for synchronous send methods
        // to 1 second (1000 milliseconds.)
        tcpSocket.SendTimeout = 1000;

        // Set the Time To Live (TTL) to 42 router hops.
        tcpSocket.Ttl = 42;
    }
}