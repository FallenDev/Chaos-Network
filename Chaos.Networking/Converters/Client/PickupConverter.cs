using Chaos.Geometry;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class PickupConverter : PacketConverterBase<PickupArgs>
{
    public override byte OpCode => (byte)ClientOpCode.Pickup;

    public override PickupArgs Deserialize(ref SpanReader reader)
    {
        var destinationSlot = reader.ReadByte();
        var sourcePoint = reader.ReadPoint16();

        return new PickupArgs
        {
            DestinationSlot = destinationSlot,
            SourcePoint = (Point)sourcePoint
        };
    }
}