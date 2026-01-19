using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class GoldDroppedOnCreatureConverter : PacketConverterBase<GoldDroppedOnCreatureArgs>
{
    public override byte OpCode => (byte)ClientOpCode.GoldDroppedOnCreature;

    public override GoldDroppedOnCreatureArgs Deserialize(ref SpanReader reader)
    {
        var amount = reader.ReadInt32();
        var targetId = reader.ReadUInt32();

        return new GoldDroppedOnCreatureArgs
        {
            Amount = amount,
            TargetId = targetId
        };
    }
}