using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Client;

public sealed record LoginArgs : IPacketSerializable
{
    public required string Name { get; set; }
    public required string Password { get; set; }
}