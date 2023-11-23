using Chaos.Common.Definitions;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of the
///     <see cref="Chaos.Packets.Abstractions.Definitions.ServerOpCode.BodyAnimation" /> packet
/// </summary>
public sealed record BodyAnimationArgs : ISendArgs
{
    /// <summary>
    ///     The speed of the animation
    /// </summary>
    public ushort AnimationSpeed { get; set; }
    /// <summary>
    ///     The body animation to play on the creature
    /// </summary>
    public BodyAnimation BodyAnimation { get; set; }
    /// <summary>
    ///     An optional sound to play with the animation
    /// </summary>
    public byte? Sound { get; set; }
    /// <summary>
    ///     The id of the creature the animation will play on
    /// </summary>
    public uint SourceId { get; set; }
}