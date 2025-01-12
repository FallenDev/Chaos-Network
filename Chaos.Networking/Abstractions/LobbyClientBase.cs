using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;
using Microsoft.Extensions.Logging;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Represents a client connected to a lobby server.
/// </summary>
public abstract class LobbyClientBase : ConnectedClientBase, ILobbyClient
{
    /// <inheritdoc />
    protected LobbyClientBase(
        Socket socket,
        IPacketSerializer packetSerializer,
        ILogger<ConnectedClientBase> logger)
        : base(
            socket,
            packetSerializer,
            logger) { }

    /// <inheritdoc />
    public virtual void SendConnectionInfo(ConnectionInfoArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendServerTableResponse(ServerTableResponseArgs args) => Send(args);
}