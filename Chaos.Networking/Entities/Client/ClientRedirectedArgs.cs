using Chaos.Networking.Abstractions.Definitions;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Client;

/// <summary>
///     Represents the serialization of the <see cref="ClientOpCode.ClientRedirected" /> packet
/// </summary>
public sealed record ClientRedirectedArgs : IPacketSerializable
{
    public required string Message { get; set; }
}