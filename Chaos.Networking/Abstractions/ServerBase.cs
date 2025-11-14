using System.Net;
using System.Net.Sockets;

using Chaos.Common.Synchronization;
using Chaos.Extensions.Common;
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
    ///     Delegate for handling client packets.
    /// </summary>
    /// <param name="client">
    ///     The client sending the packet.
    /// </param>
    /// <param name="packet">
    ///     The client packet received.
    /// </param>
    /// <returns>
    ///     A ValueTask representing the asynchronous operation.
    /// </returns>
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
    ///     The concurrent dictionary to track connection attempts.
    /// </summary>
    private readonly ConcurrentDictionary<string, (int Count, DateTime LastConnection)> ConnectionAttempts = [];

    /// <summary>
    ///     The maximum number of connections allowed per minute.
    /// </summary>
    private const int MaxConnectionsPerMinute = 5;

    /// <summary>
    ///     The window of time to track connection attempts.
    /// </summary>
    private readonly TimeSpan ConnectionWindow = TimeSpan.FromMinutes(1);

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

        StartAcceptLoop();

        await stoppingToken.WaitTillCanceled();

        try
        {
            Socket.Shutdown(SocketShutdown.Receive);
        }
        catch
        {
            //ignored
        }

        await Parallel.ForEachAsync(ClientRegistry, stoppingToken, static (client, _) =>
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

        // If the last connection is older than the window, reset the count
        if ((now - connectionData.LastConnection) > ConnectionWindow)
        {
            // Reset the count and update the timestamp
            ConnectionAttempts[ipAddress] = (1, now);
            return true;
        }

        // If the connection count exceeds the maximum allowed, reject the connection
        if (connectionData.Count >= MaxConnectionsPerMinute) return false;

        // Otherwise, increment the count and allow the connection
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
        // List of real-time args
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

        // Check if the args can execute in real-time
        if (validArgsTypes.Contains(args.GetType().Name))
        {
            await TryExecuteActionWithArgs(client, args, action);
        }
        else
        {
            // Wait for lock if packets are lower priority
            await using var @lock = await Sync.WaitAsync(TimeSpan.FromMilliseconds(300));

            // If no lock could be acquired, log the information and return
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

        //do nothing

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

        //do nothing

        return default;
    }

    #endregion

    private static void ConfigureTcpSocket(Socket tcpSocket)
    {
        // The socket will not linger when Socket.Close is called
        tcpSocket.LingerState = new LingerOption(false, 0);

        // Disable the Nagle Algorithm for low-latency communication
        tcpSocket.NoDelay = true;

        // Allows server to process multiple clients concurrently without blocking until data is read/written
        tcpSocket.Blocking = false;

        // Smaller buffer size (8 KB) to ensure latency remains low, especially for legacy clients
        tcpSocket.ReceiveBufferSize = 16384;
        tcpSocket.SendBufferSize = 16384;

        // Short timeouts to avoid latency buildup
        tcpSocket.ReceiveTimeout = 1000;
        tcpSocket.SendTimeout = 1000;

        // Enable TCP keep-alive to detect stale connections
        tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
    }

    private readonly SocketAsyncEventArgs _acceptArgs = new();

    private void StartAcceptLoop()
    {
        _acceptArgs.Completed += OnAcceptCompleted;
        if (!Socket.AcceptAsync(_acceptArgs))
            OnAcceptCompleted(Socket, _acceptArgs);
    }

    private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs e)
    {
        var clientSocket = e.AcceptSocket;
        e.AcceptSocket = null;

        if (clientSocket != null)
        {
            try
            {
                if (clientSocket.Connected)
                {
                    var ipAddress = ((IPEndPoint)clientSocket.RemoteEndPoint!).Address.ToString();
                    if (IsConnectionAllowed(ipAddress))
                    {
                        ConfigureTcpSocket(clientSocket);
                        OnConnected(clientSocket);
                    }
                    else
                    {
                        clientSocket.Close();
                    }
                }
            }
            catch
            {
                try { clientSocket?.Close(); } catch { }
            }
        }

        if (!Socket.AcceptAsync(e))
            OnAcceptCompleted(Socket, e);
    }

}
