using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class AddSpellToPaneConverter : PacketConverterBase<AddSpellToPaneArgs>
{
    public override byte OpCode => (byte)ServerOpCode.AddSpellToPane;

    public override void Serialize(ref SpanWriter writer, AddSpellToPaneArgs args)
    {
        writer.WriteByte(args.Spell.Slot);
        writer.WriteUInt16(args.Spell.Sprite);
        writer.WriteByte((byte)args.Spell.SpellType);
        writer.WriteString8(args.Spell.PanelName);
        writer.WriteString8(args.Spell.Prompt);
        writer.WriteByte(args.Spell.CastLines);
    }
}