using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of the <see cref="Chaos.Packets.Abstractions.Definitions.ServerOpCode.CreatureTurn" />
///     packet
/// </summary>
public sealed record CreatureTurnArgs : ISendArgs
{
    /// <summary>
    ///     The new direction the creature should face
    /// </summary>
    public Direction Direction { get; set; }
    /// <summary>
    ///     The id of the creature
    /// </summary>
    public uint SourceId { get; set; }
}