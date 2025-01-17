using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="AddItemToPaneArgs" />
/// </summary>
public sealed class AddItemToPaneConverter : PacketConverterBase<AddItemToPaneArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.AddItemToPane;

    /// <inheritdoc />
    public override AddItemToPaneArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, AddItemToPaneArgs args)
    {
        writer.WriteByte(args.Item.Slot);
        writer.WriteUInt16((ushort)(args.Item.Sprite + NetworkingConstants.ItemSpriteOffset));
        writer.WriteByte((byte)args.Item.Color);
        writer.WriteString(args.Item.Name);
        writer.WriteUInt32(args.Item.Count!.Value);
        writer.WriteBoolean(args.Item.Stackable);
        writer.WriteUInt32((uint)args.Item.MaxDurability);
        writer.WriteUInt32((uint)args.Item.CurrentDurability);

        //nfi
        if (args.Item.Stackable)
            writer.WriteByte(0);
    }
}