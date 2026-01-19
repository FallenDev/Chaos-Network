using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class RemoveItemFromPaneConverter : PacketConverterBase<RemoveItemFromPaneArgs>
{
    public override byte OpCode => (byte)ServerOpCode.RemoveItemFromPane;

    public override void Serialize(ref SpanWriter writer, RemoveItemFromPaneArgs args) => writer.WriteByte(args.Slot);
}