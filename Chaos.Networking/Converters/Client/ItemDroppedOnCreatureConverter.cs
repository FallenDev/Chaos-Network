using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class ItemDroppedOnCreatureConverter : PacketConverterBase<ItemDroppedOnCreatureArgs>
{
    public override byte OpCode => (byte)ClientOpCode.ItemDroppedOnCreature;

    public override ItemDroppedOnCreatureArgs Deserialize(ref SpanReader reader)
    {
        var sourceSlot = reader.ReadByte();
        var targetId = reader.ReadUInt32();
        var count = reader.ReadByte();

        return new ItemDroppedOnCreatureArgs
        {
            SourceSlot = sourceSlot,
            TargetId = targetId,
            Count = count
        };
    }
}