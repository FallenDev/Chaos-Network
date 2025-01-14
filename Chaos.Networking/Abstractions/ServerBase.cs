using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Chaos.Common.Identity;
using Chaos.Common.Synchronization;
using Chaos.Extensions.Networking;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Networking.Options;
using Chaos.NLog.Logging.Definitions;
using Chaos.NLog.Logging.Extensions;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Chaos.Extensions.Common;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Represents a base class for server implementations.
/// </summary>
/// <typeparam name="T">
///     The type of the socket client.
/// </typeparam>
public abstract class ServerBase<T> : BackgroundService, IServer<T> where T : IConnectedClient
{
    /// <summary>
    ///     Implements a thread-safe dictionary for storing connection attempts.
    /// </summary>
    private readonly ConcurrentDictionary<string, (int Count, DateTime LastConnection)> ConnectionAttempts = [];
    private const int MaxConnectionsPerMinute = 5;
    private readonly TimeSpan ConnectionWindow = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Delegate for handling client packets.
    /// </summary>
    public delegate ValueTask ClientHandler(T client, in Packet packet);

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
    protected ServerBase(
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        IClientRegistry<T> clientRegistry,
        IOptions<ServerOptions> options,
        ILogger<ServerBase<T>> logger)
    {
        Options = options.Value;
        RedirectManager = redirectManager;
        Logger = logger;
        ClientRegistry = clientRegistry;
        PacketSerializer = packetSerializer;
        ClientHandlers = new ClientHandler?[byte.MaxValue];

        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        ConfigureTcpSocket(Socket);
        Sync = new FifoAutoReleasingSemaphoreSlim(1, 15, $"{GetType().Name}");
        IndexHandlers();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        GC.SuppressFinalize(this);

        try
        {
            Socket.Close();
        }
        catch
        {
            //ignored
        }

        base.Dispose();
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var endPoint = new IPEndPoint(IPAddress.Any, Options.Port);
        Socket.Bind(endPoint);
        Socket.Listen(100);

        Logger.WithTopics(Topics.Actions.Listening)
              .LogInformation("Listening on {@EndPoint}", endPoint.Port.ToString());

        Socket.BeginAccept(OnConnection, Socket);

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
            try
            {
                client.Disconnect();
            }
            catch
            {
                //ignored
            }

            return default;
        });

        Dispose();
    }

    /// <summary>
    ///     Called when a new connection is accepted by the server.
    /// </summary>
    /// <param name="clientSocket">
    ///     The socket that connected to the server
    /// </param>
    protected abstract void OnConnected(Socket clientSocket);

    /// <summary>
    ///     Called when a new connection is accepted by the server.
    /// </summary>
    /// <param name="ar">
    ///     The result of the asynchronous connection operation
    /// </param>
    protected virtual void OnConnection(IAsyncResult ar)
    {
        var serverSocket = (Socket)ar.AsyncState!;
        Socket clientSocket = null;

        try
        {
            clientSocket = serverSocket.EndAccept(ar);
        }
        catch
        {
            // ignored
        }
        finally
        {
            serverSocket.BeginAccept(OnConnection, serverSocket);
        }

        if (clientSocket is null || !clientSocket.Connected) return;
        var ipAddress = ((IPEndPoint)clientSocket.RemoteEndPoint!).Address.ToString();

        // Check if the connection from this IP exceeds the rate limit
        if (IsConnectionAllowed(ipAddress))
        {
            // Connection is allowed, configure the socket and handle the connection
            ConfigureTcpSocket(clientSocket);
            OnConnected(clientSocket);
        }
        else
        {
            // If the connection is not allowed, we reject the connection by closing the socket immediately
            clientSocket.Close();
        }
    }

    /// <summary>
    ///     Checks if the IP address is allowed to make a connection based on rate-limiting rules.
    /// </summary>
    /// <param name="ipAddress">The IP address of the connecting client</param>
    /// <returns>True if the connection is allowed, otherwise false</returns>
    private bool IsConnectionAllowed(string ipAddress)
    {
        var now = DateTime.UtcNow;
        var connectionData = ConnectionAttempts.GetOrAdd(ipAddress, _ => (0, now));

        if ((now - connectionData.LastConnection) > ConnectionWindow)
        {
            ConnectionAttempts[ipAddress] = (1, now);
            return true;
        }

        if (connectionData.Count >= MaxConnectionsPerMinute) return false;

        ConnectionAttempts[ipAddress] = (connectionData.Count + 1, connectionData.LastConnection);
        return true;
    }

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
    public virtual ValueTask HandlePacketAsync(T client, in Packet packet)
    {
        var handler = ClientHandlers[packet.OpCode];
        return handler?.Invoke(client, in packet) ?? default;
    }

    /// <summary>
    /// Executes an asynchronous action for a client within a synchronized context.
    /// </summary>
    /// <param name="client">The client to execute the action against</param>
    /// <param name="args">The args deserialized from the packet</param>
    /// <param name="action">The action that uses the args</param>
    /// <typeparam name="TArgs">The type of the args that were deserialized</typeparam>
    public virtual async ValueTask ExecuteHandler<TArgs>(T client, TArgs args, Func<T, TArgs, ValueTask> action)
    {
        var validArgsTypes = new HashSet<string>
        {
            "MapDataArgs",
            "ClientWalkArgs",
            "PickupArgs",
            "ItemDropArgs",
            "ExitRequestArgs",
            "ClientRedirectedArgs",
            "TurnArgs",
            "ItemUseArgs",
            "GoldDropArgs",
            "ItemDroppedOnCreatureArgs",
            "GoldDroppedOnCreatureArgs",
            "SwapSlotArgs",
            "UnequipArgs",
            "HeartBeatArgs",
            "RaiseStatArgs",
            "ExchangeInteractionArgs",
            "MetaDataRequestArgs"
        };

        if (validArgsTypes.Contains(args.GetType().Name))
        {
            await TryExecuteActionWithArgs(client, args, action);
        }
        else
        {
            await using var @lock = await Sync.WaitAsync(TimeSpan.FromMilliseconds(300));

            if (@lock == null)
            {
                Logger.LogInformation($"Contention on {action.Method.Name}");
                return;
            }

            await TryExecuteActionWithArgs(client, args, action);
        }
    }

    /// <summary>
    ///     Attempts to execute the action and logs any exceptions that occur.
    /// </summary>
    /// <param name="client">The client to execute the action against</param>
    /// <param name="args">The args deserialized from the packet</param>
    /// <param name="action">The action that uses the args</param>
    private async Task TryExecuteActionWithArgs<TArgs>(T client, TArgs args, Func<T, TArgs, ValueTask> action)
    {
        try
        {
            await action(client, args);
        }
        catch (Exception e)
        {
            Logger.WithTopics(Topics.Entities.Packet, Topics.Actions.Processing)
                  .WithProperty(client)
                  .LogError(e, "{@ClientType} failed to execute inner handler with args type {@ArgsType} ({@Args})", client.GetType().Name, args.GetType().Name, args);
        }
    }

    /// <summary>
    ///     Executes an asynchronous action for a client within a sychronized context
    /// </summary>
    /// <param name="client">The client to execute the action against</param>
    /// <param name="action">The action to be executed</param>
    public virtual async ValueTask ExecuteHandler(T client, Func<T, ValueTask> action)
    {
        await using var @lock = await Sync.WaitAsync(TimeSpan.FromMilliseconds(300));

        if (@lock == null)
        {
            Logger.LogInformation($"Contention on {action.Method.Name}");
            return;
        }

        try
        {
            await action(client);
        }
        catch (Exception e)
        {
            Logger.WithTopics(Topics.Entities.Packet, Topics.Actions.Processing)
                  .WithProperty(client)
                  .LogError(e, "{@ClientType} failed to execute inner handler", client.GetType().Name);
        }
    }

    /// <inheritdoc />
    public virtual ValueTask OnHeartBeatAsync(T client, in Packet packet)
    {
        _ = PacketSerializer.Deserialize<HeartBeatArgs>(in packet);
        return default;
    }

    /// <inheritdoc />
    public ValueTask OnSequenceChangeAsync(T client, in Packet packet)
    {
        client.SetSequence(packet.Sequence);
        return default;
    }

    /// <inheritdoc />
    public virtual ValueTask OnSynchronizeTicksAsync(T client, in Packet packet)
    {
        _ = PacketSerializer.Deserialize<SynchronizeTicksArgs>(in packet);
        return default;
    }

    #endregion

    private static void ConfigureTcpSocket(Socket tcpSocket)
    {
        tcpSocket.LingerState = new LingerOption(false, 0);
        tcpSocket.NoDelay = true;
        tcpSocket.Blocking = true;
        tcpSocket.ReceiveBufferSize = 32768;
        tcpSocket.SendBufferSize = 32768;
        // ToDo: Adjust timeout to be double the expected ping time - for now leave it at 30 seconds
        tcpSocket.ReceiveTimeout = 30000;
        tcpSocket.SendTimeout = 30000;
        tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
    }
}
