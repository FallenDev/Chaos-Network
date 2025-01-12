using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;
using Microsoft.Extensions.Logging;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Represents a client connected to a login server.
/// </summary>
public abstract class LoginClientBase : ConnectedClientBase, ILoginClient
{
    /// <inheritdoc />
    protected LoginClientBase(
        Socket socket,
        IPacketSerializer packetSerializer,
        ILogger<ConnectedClientBase> logger)
        : base(
            socket,
            packetSerializer,
            logger) { }

    /// <inheritdoc />
    public virtual void SendLoginControl(LoginControlArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendLoginMessage(LoginMessageArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendLoginNotice(LoginNoticeArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendMetaData(MetaDataArgs args) => Send(args);
}