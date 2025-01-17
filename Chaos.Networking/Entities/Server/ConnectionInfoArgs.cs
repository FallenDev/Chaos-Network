using Chaos.Networking.Abstractions.Definitions;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of the <see cref="ServerOpCode.ConnectionInfo" /> packet
/// </summary>
public sealed record ConnectionInfoArgs : IPacketSerializable
{
    public ushort PortNumber { get; set; }
}