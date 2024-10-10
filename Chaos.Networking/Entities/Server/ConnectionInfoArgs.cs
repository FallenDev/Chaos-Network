using Chaos.Networking.Abstractions.Definitions;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of the <see cref="ServerOpCode.ConnectionInfo" /> packet
/// </summary>
public sealed record ConnectionInfoArgs : IPacketSerializable
{
    /// <summary>
    ///     The encryption key that the client should use to encrypt packets
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     The seed that the client should use to encrypt packets
    /// </summary>
    public byte Seed { get; set; }

    /// <summary>
    ///     The checksum of the server table used to display the servers available to connect to. If this checksum is does not
    ///     much what the client has, the client will request the table from the server
    /// </summary>
    public uint TableCheckSum { get; set; }
}