using Chaos.Geometry;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class ItemDropConverter : PacketConverterBase<ItemDropArgs>
{
    public override byte OpCode => (byte)ClientOpCode.ItemDrop;

    public override ItemDropArgs Deserialize(ref SpanReader reader)
    {
        var sourceSlot = reader.ReadByte();
        var destinationPoint = reader.ReadPoint16();
        var count = reader.ReadInt32();

        return new ItemDropArgs
        {
            SourceSlot = sourceSlot,
            DestinationPoint = (Point)destinationPoint,
            Count = count
        };
    }
}