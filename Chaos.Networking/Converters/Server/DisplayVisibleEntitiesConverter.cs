using Chaos.DarkAges.Definitions;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Serializes a <see cref="DisplayVisibleEntitiesArgs" /> into a buffer
/// </summary>
public sealed class DisplayVisibleEntitiesConverter : PacketConverterBase<DisplayVisibleEntitiesArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.DisplayVisibleEntities;

    /// <inheritdoc />
    public override DisplayVisibleEntitiesArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, DisplayVisibleEntitiesArgs args)
    {
        writer.WriteUInt16((ushort)args.VisibleObjects.Count);

        foreach (var obj in args.VisibleObjects)
        {
            writer.WritePoint16((ushort)obj.X, (ushort)obj.Y);
            writer.WriteUInt32(obj.Id);

            switch (obj)
            {
                case CreatureInfo creature:
                {
                    writer.WriteUInt16((ushort)(obj.Sprite + NetworkingConstants.CreatureSpriteOffset));
                    writer.WriteBytes(new byte[4]); //LI: what is this for?
                    writer.WriteByte((byte)creature.Direction);
                    writer.WriteByte(0); //LI: what is this for?
                    writer.WriteByte((byte)creature.CreatureType);

                    if (creature.CreatureType == CreatureType.Merchant)
                        writer.WriteString(creature.Name);

                    break;
                }
                case GroundItemInfo groundItem:
                    writer.WriteUInt16((ushort)(obj.Sprite + NetworkingConstants.ItemSpriteOffset));
                    writer.WriteByte((byte)groundItem.Color);
                    writer.WriteBytes(new byte[2]);

                    break;
            }
        }
    }
}