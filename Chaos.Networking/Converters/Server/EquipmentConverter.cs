using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="EquipmentArgs" />
/// </summary>
public sealed class EquipmentConverter : PacketConverterBase<EquipmentArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.Equipment;

    /// <inheritdoc />
    public override EquipmentArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, EquipmentArgs args)
    {
        writer.WriteByte((byte)args.Slot);
        writer.WriteUInt16((ushort)(args.Item.Sprite + NetworkingConstants.ItemSpriteOffset));
        writer.WriteByte((byte)args.Item.Color);
        writer.WriteString(args.Item.Name);
        writer.WriteByte(0); //LI: what is this for?
        writer.WriteUInt32((uint)args.Item.MaxDurability);
        writer.WriteUInt32((uint)args.Item.CurrentDurability);
    }
}