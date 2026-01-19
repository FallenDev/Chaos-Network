using Chaos.Geometry;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class GoldDropConverter : PacketConverterBase<GoldDropArgs>
{
    public override byte OpCode => (byte)ClientOpCode.GoldDrop;

    public override GoldDropArgs Deserialize(ref SpanReader reader)
    {
        var amount = reader.ReadInt32();
        var destinationPoint = reader.ReadPoint16();

        return new GoldDropArgs
        {
            Amount = amount,
            DestinationPoint = (Point)destinationPoint
        };
    }
}