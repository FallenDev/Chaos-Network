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
    public override DisplayMenuArgs Deserialize(ref SpanReader reader)
    {
        var menuType = reader.ReadByte();
        var entityType = reader.ReadByte();
        var sourceId = reader.ReadUInt32();
        _ = reader.ReadByte(); //LI: what is this for?
        var sprite = reader.ReadUInt16();
        var color = reader.ReadByte();
        _ = reader.ReadByte(); //LI: what is this for?
        var sprite2 = reader.ReadUInt16();
        var color2 = reader.ReadByte();
        var shouldIllustrate = reader.ReadBoolean();
        var name = reader.ReadString();
        var text = reader.ReadString();

        if (sprite == 0)
            sprite = sprite2;

        if (color == 0)
            color = color2;

        switch (sprite)
        {
            case > NetworkingConstants.ItemSpriteOffset:
                sprite -= NetworkingConstants.ItemSpriteOffset;

                break;
            case > NetworkingConstants.CreatureSpriteOffset:
                sprite -= NetworkingConstants.CreatureSpriteOffset;

                break;
        }

        var menuArgs = new DisplayMenuArgs
        {
            MenuType = (MenuType)menuType,
            EntityType = (EntityType)entityType,
            SourceId = sourceId,
            Sprite = sprite,
            Color = (DisplayColor)color,
            Name = name,
            Text = text,
            ShouldIllustrate = shouldIllustrate
        };

        switch (menuArgs.MenuType)
        {
            case MenuType.Menu:
            {
                var optionCount = reader.ReadByte();
                var options = new List<(string Text, ushort Pursuit)>(optionCount);

                for (var i = 0; i < optionCount; i++)
                {
                    var optionText = reader.ReadString();
                    var optionPursuit = reader.ReadUInt16();

                    options.Add((optionText, optionPursuit));
                }

                menuArgs.Options = options;

                break;
            }
            case MenuType.MenuWithArgs:
            {
                var args = reader.ReadString();
                var optionCount = reader.ReadByte();
                var options = new List<(string Text, ushort Pursuit)>(optionCount);

                for (var i = 0; i < optionCount; i++)
                {
                    var optionText = reader.ReadString();
                    var optionPursuit = reader.ReadUInt16();

                    options.Add((optionText, optionPursuit));
                }

                menuArgs.Args = args;
                menuArgs.Options = options;

                break;
            }
            case MenuType.TextEntry:
            {
                var pursuitId = reader.ReadUInt16();

                menuArgs.PursuitId = pursuitId;

                break;
            }
            case MenuType.TextEntryWithArgs:
            {
                var args = reader.ReadString();
                var pursuitId = reader.ReadUInt16();

                menuArgs.Args = args;
                menuArgs.PursuitId = pursuitId;

                break;
            }
            case MenuType.ShowItems:
            {
                var pursuitId = reader.ReadUInt16();
                var itemCount = reader.ReadUInt16();
                var items = new List<ItemInfo>(itemCount);

                for (var i = 0; i < itemCount; i++)
                {
                    var itemSprite = reader.ReadUInt16();
                    var itemColor = reader.ReadByte();
                    var cost = reader.ReadInt32();
                    var itemName = reader.ReadString();
                    _ = reader.ReadString(); //LI: what is this for?

                    items.Add(
                        new ItemInfo
                        {
                            Sprite = (ushort)(itemSprite - NetworkingConstants.ItemSpriteOffset),
                            Color = (DisplayColor)itemColor,
                            Cost = cost,
                            Name = itemName
                        });
                }

                menuArgs.PursuitId = pursuitId;
                menuArgs.Items = items;

                break;
            }
            case MenuType.ShowPlayerItems:
            {
                var pursuitId = reader.ReadUInt16();
                var slotCount = reader.ReadByte();
                var slots = new List<byte>(slotCount);

                for (var i = 0; i < slotCount; i++)
                    slots.Add(reader.ReadByte());

                menuArgs.PursuitId = pursuitId;
                menuArgs.Slots = slots;

                break;
            }
            case MenuType.ShowSpells:
            {
                var pursuitId = reader.ReadUInt16();
                var spellCount = reader.ReadUInt16();
                var spells = new List<SpellInfo>(spellCount);

                for (var i = 0; i < spellCount; i++)
                {
                    _ = reader.ReadByte(); //EntityType (see below)
                    var icon = reader.ReadUInt16();
                    _ = reader.ReadByte(); //color if entityType is item
                    var spellName = reader.ReadString();

                    spells.Add(
                        new SpellInfo
                        {
                            Sprite = icon,
                            Name = spellName
                        });
                }

                menuArgs.PursuitId = pursuitId;
                menuArgs.Spells = spells;

                break;
            }
            case MenuType.ShowSkills:
            {
                var pursuitId = reader.ReadUInt16();
                var skillCount = reader.ReadUInt16();
                var skills = new List<SkillInfo>(skillCount);

                for (var i = 0; i < skillCount; i++)
                {
                    _ = reader.ReadByte(); //EntityType (see below)
                    var icon = reader.ReadUInt16();
                    _ = reader.ReadByte(); //color if entityType is item
                    var skillName = reader.ReadString();

                    skills.Add(
                        new SkillInfo
                        {
                            Sprite = icon,
                            Name = skillName
                        });
                }

                menuArgs.PursuitId = pursuitId;
                menuArgs.Skills = skills;

                break;
            }
            case MenuType.ShowPlayerSpells:
            case MenuType.ShowPlayerSkills:
            {
                var pursuitId = reader.ReadUInt16();

                menuArgs.PursuitId = pursuitId;

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(menuArgs.MenuType),
                    "Encountered unknown menu type value during deserialization");
        }

        return menuArgs;
    }

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