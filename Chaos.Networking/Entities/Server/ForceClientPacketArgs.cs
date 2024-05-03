using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of the
///     <see cref="Chaos.Packets.Abstractions.Definitions.ServerOpCode.ForceClientPacket" /> packet
/// </summary>
public sealed record ForceClientPacketArgs : ISendArgs
{
    /// <summary>
    ///     The opcode of the packet the server is forcing the client to send back to it
    /// </summary>
    public ClientOpCode ClientOpCode { get; set; }

    /// <summary>
    ///     The data of the packet the server is forcing the client to send back to it
    /// </summary>
    public byte[] Data { get; set; } = [];
}