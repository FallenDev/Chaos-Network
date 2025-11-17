using System.Net;
using System.Net.Sockets;

using Chaos.Common.Synchronization;
using Chaos.Extensions.Common;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Networking.Entities.Server;
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
        ClientHandlers[(byte)ClientOpCode.SequenceChange] = OnSequenceChangeAsync;
        ClientHandlers[(byte)ClientOpCode.SynchronizeTicks] = OnSynchronizeTicksAsync;
    }

    /// <inheritdoc />
    public virtual ValueTask HandlePacketAsync(T client, in Packet packet)
    {
        var handler = ClientHandlers[packet.OpCode];
        return handler?.Invoke(client, in packet) ?? default;
    }

    public enum HandlerCategory
    {
        RealTime,
        Standard
    }

    /// <summary>
    /// Client handler categories mapped by argument type.
    /// </summary>
    private static readonly Dictionary<Type, HandlerCategory> HandlerCategories = new()
    {
        // RealTime Handlers
        { typeof(BeginChantArgs), HandlerCategory.RealTime }, // Spellcasting
        { typeof(ChantArgs), HandlerCategory.RealTime }, // Spellcasting
        { typeof(ClientRedirectedArgs), HandlerCategory.RealTime }, // Server Redirection
        { typeof(ClientWalkArgs), HandlerCategory.RealTime }, // Client Movement
        { typeof(ExchangeInteractionArgs), HandlerCategory.RealTime }, // Player Trading
        { typeof(ExitRequestArgs), HandlerCategory.RealTime }, // Game Exit
        { typeof(GoldDropArgs), HandlerCategory.RealTime }, // Gold Drop on Ground
        { typeof(HeartBeatArgs), HandlerCategory.RealTime }, // Ping/Pong
        { typeof(ItemDropArgs), HandlerCategory.RealTime }, // Item Drop on Ground
        { typeof(ItemUseArgs), HandlerCategory.RealTime }, // Item Use
        { typeof(LoginArgs), HandlerCategory.RealTime }, // User Login
        { typeof(MapDataRequestArgs), HandlerCategory.RealTime }, // Map Data Request
        { typeof(NoticeRequestArgs), HandlerCategory.RealTime }, // Intro Notice Board
        { typeof(PickupArgs), HandlerCategory.RealTime }, // Item/Gold Pickup from Ground
        { typeof(RaiseStatArgs), HandlerCategory.RealTime }, // Player Stat Raise
        { typeof(SequenceChangeArgs), HandlerCategory.RealTime }, // Packet Sequence Change
        { typeof(ServerTableRequestArgs), HandlerCategory.RealTime }, // Server List
        { typeof(SkillUseArgs), HandlerCategory.RealTime }, // Skill Use
        { typeof(SpacebarArgs), HandlerCategory.RealTime }, // Skill Use
        { typeof(SpellUseArgs), HandlerCategory.RealTime }, // Spellcasting
        { typeof(SwapSlotArgs), HandlerCategory.RealTime }, // Ability & Item Slot Swap
        { typeof(SynchronizeTicksArgs), HandlerCategory.RealTime }, // Server Synchronization
        { typeof(TurnArgs), HandlerCategory.RealTime }, // Client Movement
        { typeof(UnequipArgs), HandlerCategory.RealTime }, // Player Unequip Item

        // Standard Handlers
        { typeof(BoardInteractionArgs), HandlerCategory.Standard }, // Board Interaction
        { typeof(ClickArgs), HandlerCategory.Standard }, // On Click Actions
        { typeof(ClientExceptionArgs), HandlerCategory.Standard }, // Client Exceptions
        { typeof(CreateCharFinalizeArgs), HandlerCategory.Standard }, // Character Creation
        { typeof(CreateCharInitialArgs), HandlerCategory.Standard }, // Character Creation
        { typeof(CreateGroupBoxInfo), HandlerCategory.Standard }, // Group Management
        { typeof(DialogInteractionArgs), HandlerCategory.Standard }, // NPC Interaction
        { typeof(DisplayEntityRequestArgs), HandlerCategory.Standard }, // Client Entity ID Request
        { typeof(EditableProfileArgs), HandlerCategory.Standard }, // Profile Editing
        { typeof(EmoteArgs), HandlerCategory.Standard }, // Emote Animation
        { typeof(GoldDroppedOnCreatureArgs), HandlerCategory.Standard }, // Gold Drop on NPC
        { typeof(HomepageRequestArgs), HandlerCategory.Standard }, // Homepage Request
        { typeof(IgnoreArgs), HandlerCategory.Standard }, // Player Ignore List
        { typeof(ItemDroppedOnCreatureArgs), HandlerCategory.Standard }, // Item Drop on NPC
        { typeof(MenuInteractionArgs), HandlerCategory.Standard }, // NPC Menu Interaction
        { typeof(MetaDataRequestArgs), HandlerCategory.Standard }, // Metafile Data Request
        { typeof(OptionToggleArgs), HandlerCategory.Standard }, // Player Settings Toggle
        { typeof(PasswordChangeArgs), HandlerCategory.Standard }, // Player Password Change
        { typeof(PublicMessageArgs), HandlerCategory.Standard }, // Public Player Chat
        { typeof(RefreshRequestArgs), HandlerCategory.Standard }, // Map Refresh Request
        { typeof(SelfProfileRequestArgs), HandlerCategory.Standard }, // Player Self-Profile Request
        { typeof(SetNotepadArgs), HandlerCategory.Standard }, // Player Notepad Update
        { typeof(SocialStatusArgs), HandlerCategory.Standard }, // Player Social Status Update
        { typeof(ToggleGroupArgs), HandlerCategory.Standard }, // Player Group Toggle
        { typeof(VersionArgs), HandlerCategory.Standard }, // Client Version Check
        { typeof(WhisperArgs), HandlerCategory.Standard }, // Private Player Chat
        { typeof(WorldListRequestArgs), HandlerCategory.Standard }, // Player Worldlist Request
        { typeof(WorldMapClickArgs), HandlerCategory.Standard }, // Player World Map Interaction
    };

    private static HandlerCategory GetHandlerCategory(object args)
    {
        if (HandlerCategories.TryGetValue(args.GetType(), out var category))
            return category;

        // Default new/unknown handlers to Standard for backpressure safety
        return HandlerCategory.Standard;
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
        var category = GetHandlerCategory(args!);

        if (category == HandlerCategory.RealTime)
        {
            // Higher priority -> Direct execution
            await TryExecuteActionWithArgs(client, args, action);
            return;
        }

        // Lower priority -> Sync + contention handling
        await using var @lock = await Sync.WaitAsync(TimeSpan.FromMilliseconds(300));

        if (@lock == null)
        {
            Logger.LogInformation($"Contention on {action.Method.Name}");
            return;
        }

        await TryExecuteActionWithArgs(client, args, action);
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
    public virtual async ValueTask ExecuteHandler(T client, HandlerCategory category, Func<T, ValueTask> action)
    {
        if (category == HandlerCategory.RealTime)
        {
            await ExecuteHandlerCore(client, action);
            return;
        }

        await using var @lock = await Sync.WaitAsync(TimeSpan.FromMilliseconds(300));

        if (@lock == null)
        {
            Logger.LogInformation($"Contention on {action.Method.Name}");
            return;
        }

        await ExecuteHandlerCore(client, action);
    }

    private async ValueTask ExecuteHandlerCore(T client, Func<T, ValueTask> action)
    {
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

        // Concurrency comes from async usage; this flag makes sync operations also non-blocking
        tcpSocket.Blocking = false;

        // Kernel buffers sized for typical game traffic (tuned separately from app-level buffers)
        tcpSocket.ReceiveBufferSize = 64 * 1024;
        tcpSocket.SendBufferSize = 64 * 1024;

        // Enable TCP keep-alive to detect stale connections
        tcpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
    }

    private readonly SocketAsyncEventArgs _acceptArgs = new();

    private void StartAcceptLoop()
    {
        _acceptArgs.Completed += OnAcceptCompleted;

        try
        {
            if (!Socket.AcceptAsync(_acceptArgs))
                OnAcceptCompleted(Socket, _acceptArgs);
        }
        catch (ObjectDisposedException)
        {
            // Server stopping, listener already closed
        }
    }

    private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs e)
    {
        if (e.SocketError != SocketError.Success)
        {
            if (!Socket.IsBound) return;
            Logger.LogError("Socket accept failed with error: {SocketError}", e.SocketError);
        }

        var clientSocket = e.AcceptSocket;
        e.AcceptSocket = null;

        if (clientSocket != null)
        {
            try
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
            catch
            {
                try { clientSocket.Close(); } catch { }
            }
        }

        try
        {
            if (!Socket.AcceptAsync(e))
                OnAcceptCompleted(Socket, e);
        }
        catch (ObjectDisposedException)
        {
            // Server is stopping, listener has been closed – exit gracefully
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calling AcceptAsync on {ServerType}", GetType().Name);
        }
    }
}
