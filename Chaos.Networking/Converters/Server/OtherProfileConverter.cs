using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Serializes a <see cref="OtherProfileArgs" /> into a buffer
/// </summary>
public sealed class OtherProfileConverter : PacketConverterBase<OtherProfileArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.OtherProfile;

    /// <inheritdoc />
    public override OtherProfileArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, OtherProfileArgs args)
    {
        writer.WriteUInt32(args.Id);

        foreach (var slot in NetworkingConstants.ProfileEquipmentslotOrder)
        {
            args.Equipment.TryGetValue(slot, out var item);

            var offsetSprite = item?.Sprite ?? 0;

            if (offsetSprite is not 0)
                offsetSprite += NetworkingConstants.ItemSpriteOffset;

            writer.WriteUInt16(offsetSprite);
            writer.WriteByte((byte)(item?.Color ?? DisplayColor.Default));
        }

        writer.WriteByte((byte)args.SocialStatus);
        writer.WriteString(args.Name);
        writer.WriteByte((byte)args.Nation);
        writer.WriteString(args.Title ?? string.Empty);
        writer.WriteBoolean(args.GroupOpen);
        writer.WriteString(args.GuildRank ?? string.Empty);
        writer.WriteString(args.JobClass != JobClass.None ? args.JobClass.ToString() : args.DisplayClass);
        writer.WriteString(args.GuildName ?? string.Empty);
        writer.WriteByte((byte)Math.Min(byte.MaxValue, args.LegendMarks.Count));

        foreach (var mark in args.LegendMarks)
        {
            writer.WriteByte((byte)mark.Icon);
            writer.WriteByte((byte)mark.Color);
            writer.WriteString(mark.Key);
            writer.WriteString(mark.Text);
        }

        var remaining = args.Portrait.Length;
        remaining += args.ProfileText?.Length ?? 0;

        //if theres no portrait or profile data, just write 0
        if (remaining == 0)
            writer.WriteUInt16(0);
        else //if there's data, write the length of the data + 4 for prefixes
        {
            writer.WriteUInt16((ushort)(remaining + 4));
            writer.WriteData(args.Portrait); //2 + length
            writer.WriteString(args.ProfileText ?? string.Empty); //2 + length
        }

        //nfi
        writer.WriteByte(0);
    }
}