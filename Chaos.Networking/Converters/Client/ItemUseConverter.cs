using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class ItemUseConverter : PacketConverterBase<ItemUseArgs>
{
    public override byte OpCode => (byte)ClientOpCode.ItemUse;

    public override ItemUseArgs Deserialize(ref SpanReader reader)
    {
        var sourceSlot = reader.ReadByte();

        return new ItemUseArgs
        {
            SourceSlot = sourceSlot
        };
    }
}