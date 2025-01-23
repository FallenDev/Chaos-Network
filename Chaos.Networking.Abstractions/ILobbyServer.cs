using Chaos.Packets;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Defines a pattern for server that presents a list of available login servers to connect to
/// </summary>
public interface ILobbyServer<in TClient> : IServer<TClient> where TClient: IConnectedClient
{
    ValueTask OnVersion(TClient client, in Packet packet);
}