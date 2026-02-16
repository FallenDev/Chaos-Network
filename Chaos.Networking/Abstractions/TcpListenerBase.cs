using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

using Chaos.Common.Synchronization;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Networking.Extensions;
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
public abstract class TcpListenerBase<T> : BackgroundService, ITcpListener<T> where T : IConnectedClient
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
    protected ILogger<TcpListenerBase<T>> Logger { get; }

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
    ///     Tracks connection attempts per IPv4 address.
    ///
    ///     Key is an IPv4 address packed into a UInt32 in network byte order.
    ///     We intentionally do not accept IPv6 for this legacy client/server.
    /// </summary>
    private readonly ConcurrentDictionary<uint, (int Count, long FirstAttemptTicks)> ConnectionAttemptsV4 = [];

    /// <summary>
    ///     The maximum number of connections allowed per minute.
    /// </summary>
    private const int MaxConnectionsPerMinute = 5;

    /// <summary>
    ///     The window of time to track connection attempts.
    /// </summary>
    private readonly TimeSpan ConnectionWindow = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Represents the interval, in milliseconds, at which connection-attempt tracking is pruned to prevent unbounded growth.
    /// </summary>
    /// <remarks>A value of 3,600,000 milliseconds corresponds to a pruning interval of one hour. Adjust this
    /// value to control how frequently stale connection-attempt data is removed.</remarks>
    private const int PruneIntervalMs = 60 * 60 * 1000;
    private const long PruneIntervalTicks = PruneIntervalMs * TimeSpan.TicksPerMillisecond;
    private long _nextPruneTicks = DateTime.UtcNow.Ticks + PruneIntervalTicks;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TcpListenerBase{T}" /> class.
    /// </summary>
    /// <param name="redirectManager">An instance of a redirect manager.</param>
    /// <param name="packetSerializer">An instance of a packet serializer.</param>
    /// <param name="clientRegistry">An instance of a client registry.</param>
    /// <param name="options">Configuration options for the server.</param>
    /// <param name="logger">A logger for the server.</param>
    protected TcpListenerBase(
        IRedirectManager redirectManager,
        IPacketSerializer packetSerializer,
        IClientRegistry<T> clientRegistry,
        IOptions<ServerOptions> options,
        ILogger<TcpListenerBase<T>> logger)
    {
        Options = options.Value;
        RedirectManager = redirectManager;
        Logger = logger;
        ClientRegistry = clientRegistry;
        PacketSerializer = packetSerializer;
        ClientHandlers = new ClientHandler?[byte.MaxValue];
        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        SocketExtensions.ConfigureTcpSocket(Socket);
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
        catch { }

        base.Dispose();
    }

    /// <summary>
    ///    Starts the server listener and runs the main accept loop.
    ///
    ///    IMPORTANT:
    ///    This method does NOT spin or consume CPU while idle.
    ///    The await on AcceptAsync yields control to the OS until a client connects.
    /// 
    ///     - Bind() and Listen() are called ONCE during startup.
    ///     - AcceptAsync() waits for incoming connections.
    ///     - Each iteration returns ONE client socket, which is handed off to OnConnected().
    ///     - The loop exits cleanly when the CancellationToken is triggered or the socket is closed.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Ensure this method executes asynchronously
        await Task.Yield();

        var endPoint = new IPEndPoint(IPAddress.Any, Options.Port);

        try
        {
            // Server setup
            Socket.Bind(endPoint);
            Socket.Listen(backlog: 64);

            Logger.WithTopics(Topics.Actions.Listening)
                  .LogInformation("Listening on {@EndPoint}", endPoint.Port.ToString());
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to start {ServerType} on port {Port}",
                GetType().Name,
                Options.Port);

            return;
        }

        try
        {
            // Main accept loop
            while (!stoppingToken.IsCancellationRequested)
            {
                Socket clientSocket;

                try
                {
                    clientSocket = await Socket.AcceptAsync(stoppingToken)
                                               .ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (ObjectDisposedException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (SocketException ex)
                {
                    if (Socket.IsBound)
                        Logger.LogError(ex, "Accept failed on {ServerType}", GetType().Name);

                    continue;
                }

                // Legacy client: IPv4 only. Immediately drop any non-IPv4 endpoint.
                if (clientSocket.RemoteEndPoint is not IPEndPoint { AddressFamily: AddressFamily.InterNetwork })
                {
                    try { clientSocket.Close(); } catch { }
                    continue;
                }

                if (!ShouldAcceptConnection(clientSocket, out var rejectReason))
                {
                    if (!string.IsNullOrWhiteSpace(rejectReason))
                    {
                        Logger.WithTopics(Topics.Actions.Listening)
                              .LogInformation("{RejectReason}", rejectReason);
                    }

                    try { clientSocket.Close(); } catch { }
                    continue;
                }

                try
                {
                    SocketExtensions.ConfigureTcpSocket(clientSocket);
                    OnConnected(clientSocket);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error handling accepted connection on {ServerType}", GetType().Name);
                    try { clientSocket.Close(); } catch { }
                }
            }
        }
        finally
        {
            // Stop accepting new connections
            try { Socket.Close(); } catch { }

            // Disconnect all connected clients
            foreach (var client in ClientRegistry.ToArray())
            {
                try { client.CloseTransport(); } catch { }
            }

            Dispose();
        }
    }

    /// <summary>
    ///     Allows derived servers to apply connection gating (rate limiting, allowlists, etc.).
    ///     Return true to accept the connection; false to immediately close it.
    ///     Default: allow all.
    /// </summary>
    protected virtual bool ShouldAcceptConnection(Socket clientSocket, out string? rejectReason)
    {
        rejectReason = null;
        return true;
    }

    /// <summary>
    ///     Called when a new connection is accepted by the server. 
    ///     Is overloaded in derived classes to handle each server (Lobby, Login, World)
    /// </summary>
    /// <param name="clientSocket">
    ///     The socket that connected to the server
    /// </param>
    protected abstract void OnConnected(Socket clientSocket);

    /// <summary>
    ///     Checks if the IP address is allowed to make a connection based on rate-limiting rules.
    /// </summary>
    /// <param name="ipAddress">The IP address of the connecting client</param>
    /// <returns>True if the connection is allowed, otherwise false</returns>
    protected bool IsConnectionAllowed(string ipAddress)
    {
        // This server only accepts IPv4.
        if (!IPAddress.TryParse(ipAddress, out var address) || address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        return IsConnectionAllowed(address);
    }

    /// <summary>
    ///     Rate limiter
    /// </summary>
    protected bool IsConnectionAllowed(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork)
            return false;

        if (!TryPackIPv4(address, out var ipKey))
            return false;

        var nowTicks = DateTime.UtcNow.Ticks;

        // Prune old connection attempts
        PruneOldConnectionAttempts(nowTicks);

        var windowTicks = ConnectionWindow.Ticks;

        // Lock-free CAS loop to avoid lost updates under concurrency
        while (true)
        {
            if (!ConnectionAttemptsV4.TryGetValue(ipKey, out var current))
            {
                // First attempt for this IP in the current map state
                if (ConnectionAttemptsV4.TryAdd(ipKey, (1, nowTicks)))
                    return true;

                // Another thread added it first; retry
                continue;
            }

            // Window rolled over -> reset counter to 1 at current timestamp
            if ((nowTicks - current.FirstAttemptTicks) > windowTicks)
            {
                if (ConnectionAttemptsV4.TryUpdate(ipKey, (1, nowTicks), current))
                    return true;

                // Contended update; retry with fresh value
                continue;
            }

            // Still within window
            if (current.Count >= MaxConnectionsPerMinute)
                return false;

            var next = (current.Count + 1, current.FirstAttemptTicks);

            if (ConnectionAttemptsV4.TryUpdate(ipKey, next, current))
                return true;
        }
    }

    /// <summary>
    ///    Prunes stale IP connection-attempt entries at most once per hour.
    ///
    ///    This method is designed to be:
    ///     - Lock-free and allocation-free on the hot path
    ///     - Safe under heavy concurrent access from multiple accept loops
    ///
    ///    How it works:
    ///     - Uses Volatile.Read to ensure all threads observe the most recent
    ///     scheduled prune time without relying on locks.
    ///     - Uses Interlocked.CompareExchange to guarantee that only ONE thread
    ///     performs the prune when the interval elapses.
    ///     - All other threads immediately exit once another thread has claimed
    ///     the prune window.
    ///
    ///    This prevents unbounded growth of the connection-attempt cache while
    ///    keeping connection acceptance fast and contention-free.
    /// </summary>
    private void PruneOldConnectionAttempts(long nowTicks)
    {
        // Fast path: Gives the most recent value without locking
        var nextTicks = Volatile.Read(ref _nextPruneTicks);
        if (nowTicks < nextTicks) return;

        // Single-writer gate so only one thread prunes
        var newNext = nowTicks + PruneIntervalTicks;
        
        if (Interlocked.CompareExchange(ref _nextPruneTicks, newNext, nextTicks) != nextTicks) return;

        // Prune anything stale (older than 2x the window)
        var cutoffTicks = nowTicks - (ConnectionWindow.Ticks * 2);

        foreach (var kvp in ConnectionAttemptsV4)
        {
            if (kvp.Value.FirstAttemptTicks < cutoffTicks)
                ConnectionAttemptsV4.TryRemove(kvp.Key, out _);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryPackIPv4(IPAddress address, out uint key)
    {
        Span<byte> bytes = stackalloc byte[4];
        if (!address.TryWriteBytes(bytes, out var written) || written != 4)
        {
            key = 0;
            return false;
        }

        // network order: a.b.c.d => 0xAABBCCDD
        key = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static HandlerCategory GetHandlerCategory<TArgs>() => IsRealTime(typeof(TArgs)) ? HandlerCategory.RealTime : HandlerCategory.Standard;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsRealTime(Type t)
    {
        return t == typeof(BeginChantArgs)
            || t == typeof(ChantArgs)
            || t == typeof(ClientRedirectedArgs)
            || t == typeof(ClientWalkArgs)
            || t == typeof(ExchangeInteractionArgs)
            || t == typeof(ExitRequestArgs)
            || t == typeof(GoldDropArgs)
            || t == typeof(GoldDroppedOnCreatureArgs)
            || t == typeof(HeartBeatArgs)
            || t == typeof(ItemDropArgs)
            || t == typeof(ItemDroppedOnCreatureArgs)
            || t == typeof(ItemUseArgs)
            || t == typeof(LoginArgs)
            || t == typeof(PickupArgs)
            || t == typeof(RaiseStatArgs)
            || t == typeof(SequenceChangeArgs)
            || t == typeof(ServerTableRequestArgs)
            || t == typeof(SkillUseArgs)
            || t == typeof(SpellUseArgs)
            || t == typeof(SwapSlotArgs)
            || t == typeof(SynchronizeTicksArgs)
            || t == typeof(TurnArgs)
            || t == typeof(UnequipArgs);
    }

    /// <summary>
    ///     Executes an asynchronous action for a client within a Real-Time context.
    /// </summary>
    /// <param name="client">The client to execute the action against</param>
    /// <param name="args">The args deserialized from the packet</param>
    /// <param name="action">The action that uses the args</param>
    /// <typeparam name="TArgs">The type of the args that were deserialized</typeparam>
    public virtual async ValueTask ExecuteHandler<TArgs>(T client, TArgs args, Func<T, TArgs, ValueTask> action)
    {
        var category = GetHandlerCategory<TArgs>();

        if (category == HandlerCategory.RealTime)
        {
            // Higher priority -> Direct execution
            await TryExecuteActionWithArgs(client, args, action);
            return;
        }

        // Lower priority -> Sync + contention handling
        await using var @lock = await Sync.WaitAsync(TimeSpan.FromMilliseconds(500));

        if (@lock == null)
        {
            LogDroppedDueToLag(client, action.Method.Name, 500);
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

            return;
        }

        await using var @lock = await Sync.WaitAsync(TimeSpan.FromMilliseconds(300));

        if (@lock == null)
        {
            LogDroppedDueToLag(client, action.Method.Name, 300);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogDroppedDueToLag(T client, string actionName, int waitMs)
    {
        Logger.WithTopics(Topics.Entities.Client, Topics.Entities.Packet, Topics.Actions.Processing)
              .WithProperty(client)
              .LogInformation(
                  "Client lag detected; dropped handler {ActionName} after waiting {WaitMs}ms (ClientId={ClientId}, RemoteIp={RemoteIp})",
                  actionName,
                  waitMs,
                  client.Id,
                  client.RemoteIp);
    }
}
