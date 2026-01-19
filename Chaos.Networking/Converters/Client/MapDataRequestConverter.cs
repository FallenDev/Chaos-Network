using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class MapDataRequestConverter : PacketConverterBase<MapDataRequestArgs>
{
    public override byte OpCode => (byte)ClientOpCode.MapDataRequest;

    public override MapDataRequestArgs Deserialize(ref SpanReader reader) => new();
}