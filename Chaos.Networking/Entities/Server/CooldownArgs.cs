using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of the <see cref="Chaos.Packets.Abstractions.Definitions.ServerOpCode.Cooldown" />
///     packet
/// </summary>
public sealed record CooldownArgs : ISendArgs
{
    /// <summary>
    ///     The cooldown of the ability in seconds
    /// </summary>
    public uint CooldownSecs { get; set; }
    /// <summary>
    ///     Whether or not the ability is a skill
    /// </summary>
    public bool IsSkill { get; set; }
    /// <summary>
    ///     The slot the ability is in
    /// </summary>
    public byte Slot { get; set; }
}