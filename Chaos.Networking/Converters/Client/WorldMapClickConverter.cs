using Chaos.Geometry;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class WorldMapClickConverter : PacketConverterBase<WorldMapClickArgs>
{
    public override byte OpCode => (byte)ClientOpCode.WorldMapClick;

    public override WorldMapClickArgs Deserialize(ref SpanReader reader)
    {
        var checkSum = reader.ReadUInt16();
        var mapId = reader.ReadUInt16();
        var point = reader.ReadPoint16();

        return new WorldMapClickArgs
        {
            CheckSum = checkSum,
            MapId = mapId,
            Point = (Point)point
        };
    }
}