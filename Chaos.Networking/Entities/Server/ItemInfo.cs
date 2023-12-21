using Chaos.Common.Definitions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of an item used by several packets
/// </summary>
public sealed record ItemInfo
{
    /// <summary>
    ///     The color of the item
    /// </summary>
    public required DisplayColor Color { get; init; } = DisplayColor.Default;

    /// <summary>
    ///     The cost of the item
    /// </summary>
    public required int? Cost { get; init; } = 0;

    /// <summary>
    ///     The count of the item
    /// </summary>
    public required uint? Count { get; init; } = 0;

    /// <summary>
    ///     The current durability of the item
    /// </summary>
    public required int CurrentDurability { get; init; } = 0;

    /// <summary>
    ///     The maximum durability of the item
    /// </summary>
    public required int MaxDurability { get; init; } = 0;

    /// <summary>
    ///     The name of the item
    /// </summary>
    public required string Name { get; init; } = "";

    /// <summary>
    ///     The group of the item
    /// </summary>
    public required string Group { get; init; } = "";

    /// <summary>
    ///     The slot of the item
    /// </summary>
    public required byte Slot { get; init; } = 0;

    /// <summary>
    ///     The sprite of the item
    /// </summary>
    public required ushort Sprite { get; init; } = 0;

    /// <summary>
    ///     Whether or not the item is stackable
    /// </summary>
    public required bool Stackable { get; init; } = false;
}