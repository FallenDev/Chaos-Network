using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of the <see cref="Chaos.Packets.Abstractions.Definitions.ServerOpCode.Sound" /> packet
/// </summary>
public sealed record SoundArgs : ISendArgs
{
    /// <summary>
    ///     Whether or not the sound is a music track
    /// </summary>
    public bool IsMusic { get; set; }
    /// <summary>
    ///     The sound or music track index to play
    /// </summary>
    public byte Sound { get; set; }
}