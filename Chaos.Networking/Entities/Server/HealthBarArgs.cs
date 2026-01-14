using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
/// Server packet 0x13 (HealthBar)
///     Wire format:
///         SourceId (u32)
///         Kind     (u8)  // observed: 0, 2
///         Percent  (u8)
///         Sound    (u8)  // 0xFF => no sound
///         Tail     (u8)? // optional, observed as 0x00; commonly present when Kind==2
///         
/// Assumptions:
/// - Player health updates: Kind=0, no Tail
/// - Monster health updates: Kind=2, Tail=0
/// </summary>
public sealed record HealthBarArgs : IPacketSerializable
{
    /// <summary>
    /// The id of the entity to display the health bar for
    /// </summary>
    public uint SourceId { get; set; }

    /// <summary>
    /// Mode/subtype byte carried on the wire. Do not hardcode.
    /// Observed values: 0 and 2 (1 may exist in other flows).
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
    /// Optional trailing byte. Observed as 0x00; commonly present when Kind==2. 
    /// </summary>
    public byte? Tail { get; set; }
}