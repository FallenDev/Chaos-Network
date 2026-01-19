using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class AddSkillToPaneConverter : PacketConverterBase<AddSkillToPaneArgs>
{
    public override byte OpCode => (byte)ServerOpCode.AddSkillToPane;

    public override void Serialize(ref SpanWriter writer, AddSkillToPaneArgs args)
    {
        writer.WriteByte(args.Skill.Slot);
        writer.WriteUInt16(args.Skill.Sprite);
        writer.WriteString8(args.Skill.PanelName);
    }
}