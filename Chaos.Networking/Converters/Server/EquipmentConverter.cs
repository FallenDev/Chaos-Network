using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class EquipmentConverter : PacketConverterBase<EquipmentArgs>
{
    public override byte OpCode => (byte)ServerOpCode.Equipment;

    public override void Serialize(ref SpanWriter writer, EquipmentArgs args)
    {
        writer.WriteByte((byte)args.Slot);
        writer.WriteUInt16((ushort)(args.Item.Sprite + NETWORKING_CONSTANTS.ITEM_SPRITE_OFFSET));
        writer.WriteByte((byte)args.Item.Color);
        writer.WriteString8(args.Item.Name);
        writer.WriteByte(0);
        writer.WriteUInt32((uint)args.Item.MaxDurability);
        writer.WriteUInt32((uint)args.Item.CurrentDurability);
    }
}