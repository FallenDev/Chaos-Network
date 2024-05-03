using Chaos.Common.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Serializers;

/// <summary>
///     Serializes a <see cref="ProfileArgs" /> into a buffer
/// </summary>
public sealed record ProfileSerializer : ServerPacketSerializer<ProfileArgs>
{
    /// <inheritdoc />
    public override ServerOpCode ServerOpCode => ServerOpCode.Profile;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, ProfileArgs args)
    {
        writer.WriteUInt32(args.Id);

        foreach (var slot in NETWORKING_CONSTANTS.PROFILE_EQUIPMENTSLOT_ORDER)
        {
            args.Equipment.TryGetValue(slot, out var item);
            if (item == null)
            {
                writer.WriteUInt16(ushort.MinValue);
                writer.WriteByte(0x00);
                continue;
            }

            var offsetSprite = item.Sprite;

            if (offsetSprite is not 0)
                offsetSprite += NETWORKING_CONSTANTS.ITEM_SPRITE_OFFSET;

            writer.WriteUInt16(offsetSprite);
            writer.WriteByte((byte)(item.Color));
        }

        writer.WriteByte((byte)args.SocialStatus);
        writer.WriteString8(args.Name);
        writer.WriteByte((byte)args.Nation);
        writer.WriteString8(args.Title ?? string.Empty);
        writer.WriteBoolean(args.GroupOpen);
        writer.WriteString8(args.GuildRank ?? string.Empty);
        writer.WriteString8(args.JobClass != JobClass.None ? args.JobClass.ToString() : args.BaseClass.ToString());
        writer.WriteString8(args.GuildName ?? string.Empty);
        writer.WriteByte((byte)Math.Min(byte.MaxValue, args.LegendMarks.Count));

        foreach (var mark in args.LegendMarks.Take(byte.MaxValue))
        {
            writer.WriteByte((byte)mark.Icon);
            writer.WriteByte((byte)mark.Color);
            writer.WriteString8(mark.Key);
            writer.WriteString8(mark.Text);
        }

        var remaining = args.Portrait.Length;
        remaining += args.ProfileText?.Length ?? 0;

        //if theres no portrait or profile data, just write 0
        if (remaining == 0)
            writer.WriteUInt16(0);
        else //if there's data, write the length of the data + 4 for prefixes
        {
            writer.WriteUInt16((ushort)(remaining + 4));
            writer.WriteData16(args.Portrait); //2 + length
            writer.WriteString16(args.ProfileText ?? string.Empty); //2 + length
        }

        //nfi
        writer.WriteByte(0);
    }
}