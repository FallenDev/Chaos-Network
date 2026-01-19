using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class MapInfoConverter : PacketConverterBase<MapInfoArgs>
{
    public override byte OpCode => (byte)ServerOpCode.MapInfo;

    public override void Serialize(ref SpanWriter writer, MapInfoArgs args)
    {
        writer.WriteInt16(args.MapId);
        writer.WriteByte(args.Width);
        writer.WriteByte(args.Height);
        writer.WriteByte(args.Flags);
        writer.WriteBytes(new byte[2]);
        writer.WriteUInt16(args.CheckSum);
        writer.WriteString8(args.Name);
    }
}