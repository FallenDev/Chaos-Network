using Chaos.Common.Definitions;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Entities.Server;

/// <summary>
///     Represents the serialization of the <see cref="Chaos.Packets.Abstractions.Definitions.ServerOpCode.Dialog" />
///     packet
/// </summary>
public sealed record DialogArgs : ISendArgs
{
    /// <summary>
    ///     The color associated with the source of the dialog. (for items and aislings)
    /// </summary>
    public required DisplayColor Color { get; set; }
    /// <summary>
    ///     The id of the dialog
    /// </summary>
    public required ushort DialogId { get; set; }
    /// <summary>
    ///     The type of dialog
    /// </summary>
    public required DialogType DialogType { get; set; }
    /// <summary>
    ///     The entity type of the source of the dialog. (item, creature, aisling, etc)
    /// </summary>
    public required EntityType EntityType { get; set; }
    /// <summary>
    ///     Whether or not the dialog has a next button
    /// </summary>
    public required bool HasNextButton { get; set; }
    /// <summary>
    ///     Whether or not the dialog has a prev button
    /// </summary>
    public required bool HasPreviousButton { get; set; }
    /// <summary>
    ///     The name of the source entity associated with the dialog
    /// </summary>
    public required string Name { get; set; } = null!;
    /// <summary>
    ///     If the dialog has options, this is a collection of strings that represent selectable options in the dialog's menu
    /// </summary>
    public ICollection<string>? Options { get; set; }
    /// <summary>
    ///     If the dialog is part of a pursuit chain, this is the id of that pursuit
    /// </summary>
    public ushort? PursuitId { get; set; }
    /// <summary>
    ///     The id of the source entity associated with the dialog. This isn't really required for any practical purpose
    /// </summary>
    public uint? SourceId { get; set; }
    /// <summary>
    ///     The sprite of the source entity associated with the dialog
    /// </summary>
    public required ushort Sprite { get; set; }
    /// <summary>
    ///     The text of the dialog
    /// </summary>
    public required string Text { get; set; } = null!;
    /// <summary>
    ///     When the <see cref="DialogType" /> is DialogTextEntry, this will limit the length of the input text box
    /// </summary>
    public ushort? TextBoxLength { get; set; }

    /// <summary>
    ///     When the <see cref="DialogType" /> is DialogTextEntry, this will be the message displayed above the text box
    ///     (optional)
    /// </summary>
    public string? TextBoxPrompt { get; set; }
}