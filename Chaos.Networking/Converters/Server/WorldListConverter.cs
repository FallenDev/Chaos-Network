using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class WorldListConverter : PacketConverterBase<WorldListArgs>
{
    public override byte OpCode => (byte)ServerOpCode.WorldList;

    public override void Serialize(ref SpanWriter writer, WorldListArgs args)
    {
        writer.WriteUInt16(args.WorldMemberCount);
        writer.WriteUInt16((ushort)args.CountryList.Count);

        foreach (var user in args.CountryList)
        {
            writer.WriteByte((byte)user.BaseClass);
            writer.WriteByte((byte)user.Color);
            writer.WriteByte((byte)user.SocialStatus);
            writer.WriteString8(user.Title ?? string.Empty);
            writer.WriteBoolean(user.IsMaster);
            writer.WriteString8(user.Name);
        }
    }
}