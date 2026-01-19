using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public class MapChangeCompleteConverter : PacketConverterBase<MapChangeCompleteArgs>
{
    public override byte OpCode => (byte)ServerOpCode.MapChangeComplete;

    public override void Serialize(ref SpanWriter writer, MapChangeCompleteArgs args) => writer.WriteBytes(0, 0);
}