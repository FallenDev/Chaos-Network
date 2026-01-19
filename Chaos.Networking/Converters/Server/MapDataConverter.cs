using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class MapDataConverter : PacketConverterBase<MapDataArgs>
{
    public override byte OpCode => (byte)ServerOpCode.MapData;

    public override void Serialize(ref SpanWriter writer, MapDataArgs args)
    {
        writer.WriteUInt16(args.CurrentYIndex);
        writer.WriteData(args.MapData);
    }
}