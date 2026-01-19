using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public class MapChangePendingConverter : PacketConverterBase<MapChangePendingArgs>
{
    public override byte OpCode => (byte)ServerOpCode.MapChangePending;

    public override void Serialize(ref SpanWriter writer, MapChangePendingArgs args)
        => writer.WriteBytes(
            3,
            0,
            0,
            0,
            0,
            0);
}