using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="SelfProfileArgs" />
/// </summary>
public sealed class SelfProfileConverter : PacketConverterBase<SelfProfileArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.SelfProfile;

    /// <inheritdoc />
    public override SelfProfileArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, SelfProfileArgs args)
    {
        writer.WriteByte((byte)args.Nation);
        writer.WriteString(args.GuildRank ?? string.Empty);
        writer.WriteString(args.Title ?? string.Empty);

        var str = args.GroupString;

        if (string.IsNullOrEmpty(str))
            str = !string.IsNullOrEmpty(args.SpouseName) ? $"Spouse: {args.SpouseName}" : "Adventuring alone";

        writer.WriteString(str);
        writer.WriteBoolean(args.GroupOpen);
        writer.WriteBoolean(false); //TODO: groupbox fml
        /*
         *  if(user.Group?.Box != null)
            {
                packet.WriteString8(user.Group.Leader.Name);
                packet.WriteString8(user.Group.Box.Text);
                packet.Write(new byte[13]); //other groupbox stuff will add later
            }
         */

        var classTitle = args.BaseClass.ToString();

        if (args.EnableMasterQuestMetaData)
            classTitle = "Master";

        if (args.JobClass.HasValue && args.JobClass != JobClass.None)
            classTitle = args.JobClass.ToString();

        writer.WriteByte((byte)args.BaseClass);
        writer.WriteBoolean(args.EnableMasterAbilityMetaData);
        writer.WriteBoolean(args.EnableMasterQuestMetaData);
        writer.WriteString(classTitle);
        writer.WriteString(args.GuildName ?? string.Empty);
        writer.WriteByte((byte)Math.Min(byte.MaxValue, args.LegendMarks.Count));

        foreach (var mark in args.LegendMarks.Take(byte.MaxValue))
        {
            writer.WriteByte((byte)mark.Icon);
            writer.WriteByte((byte)mark.Color);
            writer.WriteString(mark.Key);
            writer.WriteString(mark.Text);
        }
    }
}