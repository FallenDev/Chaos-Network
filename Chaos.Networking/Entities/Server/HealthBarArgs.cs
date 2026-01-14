using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
/// Server packet 0x13 (HealthBar)
///     Wire format:
///         SourceId (u32)
///         Kind     (u8)  // sprite type: 0=Player, 1=NPC, 2=Monster
///         Percent  (u8)
///         Sound    (u8)  // 0xFF => no sound
///         Tail     (u8)? // Observed as 0x00; Present when Kind: 1 or 2
///         
/// Assumptions:
/// - Player health updates: Kind: 0x00, no Tail
/// - NPC health updates: Kind: 0x01, Tail: 0x00
/// - Monster health updates: Kind: 0x02, Tail: 0x00
/// </summary>
public sealed record HealthBarArgs : IPacketSerializable
{
    /// <summary>
    /// The id of the entity to display the health bar for
    /// </summary>
    public uint SourceId { get; set; }

    /// <summary>
    /// Values: 0 (Player), 1 (NPC), 2 (Monster).
    /// </summary>
    public byte Kind { get; set; }

    /// <summary>
    /// Health bar percentage (0-100).
    /// </summary>
    public byte HealthPercent { get; set; }

    /// <summary>
    /// Optional sound id; null maps to 0xFF on the wire.
    /// </summary>
    public byte? Sound { get; set; }

    /// <summary>
    /// Optional trailing byte. Observed as 0x00; commonly present when Kind 1 & 2. 
    /// </summary>
    public byte? Tail { get; set; }
}