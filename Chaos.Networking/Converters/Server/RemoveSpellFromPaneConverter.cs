using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class RemoveSpellFromPaneConverter : PacketConverterBase<RemoveSpellFromPaneArgs>
{
    public override byte OpCode => (byte)ServerOpCode.RemoveSpellFromPane;

    public override void Serialize(ref SpanWriter writer, RemoveSpellFromPaneArgs args) => writer.WriteByte(args.Slot);
}