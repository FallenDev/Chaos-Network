using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public class MapLoadCompleteConverter : PacketConverterBase<MapLoadCompleteArgs>
{
    public override byte OpCode => (byte)ServerOpCode.MapLoadComplete;

    public override void Serialize(ref SpanWriter writer, MapLoadCompleteArgs args) => writer.WriteByte(0);
}