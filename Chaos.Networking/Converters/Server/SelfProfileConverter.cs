using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class SelfProfileConverter : PacketConverterBase<SelfProfileArgs>
{
    public override byte OpCode => (byte)ServerOpCode.SelfProfile;

    public override void Serialize(ref SpanWriter writer, SelfProfileArgs args)
    {
        writer.WriteByte((byte)args.Nation);
        writer.WriteString8(args.GuildRank ?? string.Empty);
        writer.WriteString8(args.Title ?? string.Empty);

        var str = args.GroupString;

        if (string.IsNullOrEmpty(str))
            str = !string.IsNullOrEmpty(args.SpouseName) ? $"Spouse: {args.SpouseName}" : "Adventuring alone";

        writer.WriteString8(str);
        writer.WriteBoolean(args.GroupOpen);
        writer.WriteBoolean(false);
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
        writer.WriteString8(classTitle);
        writer.WriteString8(args.GuildName ?? string.Empty);
        writer.WriteByte((byte)Math.Min(byte.MaxValue, args.LegendMarks.Count));

        foreach (var mark in args.LegendMarks.Take(byte.MaxValue))
        {
            writer.WriteByte((byte)mark.Icon);
            writer.WriteByte((byte)mark.Color);
            writer.WriteString8(mark.Key);
            writer.WriteString8(mark.Text);
        }
    }
}