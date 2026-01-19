using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class DisplayUnequipConverter : PacketConverterBase<DisplayUnequipArgs>
{
    public override byte OpCode => (byte)ServerOpCode.DisplayUnequip;

    public override void Serialize(ref SpanWriter writer, DisplayUnequipArgs args) => writer.WriteByte((byte)args.EquipmentSlot);
}