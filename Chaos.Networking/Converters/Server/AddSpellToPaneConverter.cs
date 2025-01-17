using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="AddSpellToPaneArgs" />
/// </summary>
public sealed class AddSpellToPaneConverter : PacketConverterBase<AddSpellToPaneArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.AddSpellToPane;

    /// <inheritdoc />
    public override AddSpellToPaneArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, AddSpellToPaneArgs args)
    {
        writer.WriteByte(args.Spell.Slot);
        writer.WriteUInt16(args.Spell.Sprite);
        writer.WriteByte((byte)args.Spell.SpellType);
        writer.WriteString(args.Spell.PanelName);
        writer.WriteString(args.Spell.Prompt);
        writer.WriteByte(args.Spell.CastLines);
    }
}