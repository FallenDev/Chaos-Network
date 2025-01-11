using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Represents a client that is connected to the login server.
/// </summary>
public interface ILoginClient : IConnectedClient
{
    /// <summary>
    ///     Sends a login control message to the client.
    /// </summary>
    void SendLoginControl(LoginControlArgs args);

    /// <summary>
    ///     Sends a login message to the client.
    /// </summary>
    void SendLoginMessage(LoginMessageArgs args);

    /// <summary>
    ///     Sends a login notice to the client.
    /// </summary>
    void SendLoginNotice(LoginNoticeArgs args);

    /// <summary>
    ///     Sends metadata to the client.
    /// </summary>
    void SendMetaData(MetaDataArgs args);
}