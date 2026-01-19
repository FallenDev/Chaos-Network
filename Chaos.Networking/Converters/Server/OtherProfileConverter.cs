using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class OtherProfileConverter : PacketConverterBase<OtherProfileArgs>
{
    public override byte OpCode => (byte)ServerOpCode.OtherProfile;

    public override void Serialize(ref SpanWriter writer, OtherProfileArgs args)
    {
        writer.WriteUInt32(args.Id);

        foreach (var slot in NETWORKING_CONSTANTS.PROFILE_EQUIPMENTSLOT_ORDER)
        {
            args.Equipment.TryGetValue(slot, out var item);

            var offsetSprite = item?.Sprite ?? 0;

            if (offsetSprite is not 0)
                offsetSprite += NETWORKING_CONSTANTS.ITEM_SPRITE_OFFSET;

            writer.WriteUInt16(offsetSprite);
            writer.WriteByte((byte)(item?.Color ?? DisplayColor.Default));
        }

        writer.WriteByte((byte)args.SocialStatus);
        writer.WriteString8(args.Name);
        writer.WriteByte((byte)args.Nation);
        writer.WriteString8(args.Title ?? string.Empty);
        writer.WriteBoolean(args.GroupOpen);
        writer.WriteString8(args.GuildRank ?? string.Empty);
        writer.WriteString8(args.JobClass != JobClass.None ? args.JobClass.ToString() : args.DisplayClass);
        writer.WriteString8(args.GuildName ?? string.Empty);
        writer.WriteByte((byte)Math.Min(byte.MaxValue, args.LegendMarks.Count));

        foreach (var mark in args.LegendMarks)
        {
            writer.WriteByte((byte)mark.Icon);
            writer.WriteByte((byte)mark.Color);
            writer.WriteString8(mark.Key);
            writer.WriteString8(mark.Text);
        }

        var remaining = args.Portrait.Length;
        remaining += args.ProfileText?.Length ?? 0;

        // If theres no portrait or profile data, just write 0
        if (remaining == 0)
            writer.WriteUInt16(0);
        else // If there's data, write the length of the data + 4 for prefixes
        {
            writer.WriteUInt16((ushort)(remaining + 4));
            writer.WriteData16(args.Portrait); // 2 + length
            writer.WriteString16(args.ProfileText ?? string.Empty); // 2 + length
        }
            
        writer.WriteByte(0);
    }
}