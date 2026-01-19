using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class RemoveSkillFromPaneConverter : PacketConverterBase<RemoveSkillFromPaneArgs>
{
    public override byte OpCode => (byte)ServerOpCode.RemoveSkillFromPane;

    public override void Serialize(ref SpanWriter writer, RemoveSkillFromPaneArgs args) => writer.WriteByte(args.Slot);
}