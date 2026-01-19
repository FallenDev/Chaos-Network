using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class SpellUseConverter : PacketConverterBase<SpellUseArgs>
{
    public override byte OpCode => (byte)ClientOpCode.SpellUse;

    public override SpellUseArgs Deserialize(ref SpanReader reader)
    {
        var sourceSlot = reader.ReadByte();
        var argsData = reader.ReadData();

        return new SpellUseArgs
        {
            SourceSlot = sourceSlot,
            ArgsData = argsData
        };
    }
}