using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="DisplayMenuArgs" />
/// </summary>
public sealed class DisplayMenuConverter : PacketConverterBase<DisplayMenuArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.DisplayMenu;

    /// <inheritdoc />
    public override DisplayMenuArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, DisplayMenuArgs args)
    {
        var offsetSprite = args.Sprite;

        if (args.Sprite is not 0)
            switch (args.EntityType)
            {
                case EntityType.Item:
                    offsetSprite += NetworkingConstants.ItemSpriteOffset;

                    break;
                case EntityType.Aisling or EntityType.Creature:
                    offsetSprite += NetworkingConstants.CreatureSpriteOffset;

                    break;
            }

        writer.WriteByte((byte)args.MenuType);
        writer.WriteByte((byte)args.EntityType);
        writer.WriteUInt32(args.SourceId ?? 0);
        writer.WriteByte(0); //dunno
        writer.WriteUInt16(offsetSprite);
        writer.WriteByte((byte)args.Color);
        writer.WriteByte(0); //dunno
        writer.WriteUInt16(offsetSprite);
        writer.WriteByte((byte)args.Color);
        writer.WriteBoolean(args.ShouldIllustrate);
        writer.WriteString(args.Name);
        writer.WriteString(args.Text);

        switch (args.MenuType)
        {
            case MenuType.Menu:
            {
                writer.WriteByte((byte)args.Options!.Count);

                foreach (var option in args.Options)
                {
                    writer.WriteString(option.Text);
                    writer.WriteUInt16(option.Pursuit);
                }

                break;
            }
            case MenuType.MenuWithArgs:
            {
                writer.WriteString(args.Args!);
                writer.WriteByte((byte)args.Options!.Count);

                foreach (var option in args.Options)
                {
                    writer.WriteString(option.Text);
                    writer.WriteUInt16(option.Pursuit);
                }

                break;
            }
            case MenuType.TextEntry:
                writer.WriteUInt16(args.PursuitId);

                break;
            case MenuType.TextEntryWithArgs:
                writer.WriteString(args.Args!);
                writer.WriteUInt16(args.PursuitId);

                break;

            case MenuType.ShowItems:
                writer.WriteUInt16(args.PursuitId);
                writer.WriteUInt16((ushort)args.Items!.Count);

                foreach (var item in args.Items)
                {
                    writer.WriteUInt16((ushort)(item.Sprite + NetworkingConstants.ItemSpriteOffset));
                    writer.WriteByte((byte)item.Color);
                    writer.WriteUInt32((uint)item.Cost!.Value);
                    writer.WriteString(item.Name);

                    //TODO: figure out what this is, maybe something to do with metadatas
                    writer.WriteString("what is this");
                }

                break;
            case MenuType.ShowPlayerItems:
                writer.WriteUInt16(args.PursuitId);
                writer.WriteByte((byte)args.Slots!.Count);

                foreach (var slot in args.Slots)
                    writer.WriteByte(slot);

                break;
            case MenuType.ShowSpells:
                writer.WriteUInt16(args.PursuitId);
                writer.WriteUInt16((ushort)args.Spells!.Count);

                foreach (var spell in args.Spells)
                {
                    //0 = none
                    //1 = item (requires offset sprite)
                    //2 = spell icon
                    //3 = skill icon
                    //4 = monster sprite (requires offset sprite).. theyre all facing up?
                    writer.WriteByte(2);
                    writer.WriteUInt16(spell.Sprite);
                    writer.WriteByte(0); //color
                    writer.WriteString(spell.Name);
                }

                break;
            case MenuType.ShowSkills:
                writer.WriteUInt16((ushort)(args.PursuitId + 1));
                writer.WriteUInt16((ushort)args.Skills!.Count);

                foreach (var skill in args.Skills)
                {
                    writer.WriteByte(3);
                    writer.WriteUInt16(skill.Sprite);
                    writer.WriteByte(0); //color
                    writer.WriteString(skill.Name);
                }

                break;
            case MenuType.ShowPlayerSpells:
                writer.WriteUInt16(args.PursuitId);

                break;
            case MenuType.ShowPlayerSkills:
                writer.WriteUInt16(args.PursuitId);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(args.MenuType), args.MenuType, "Unknown menu type");
        }
    }
}