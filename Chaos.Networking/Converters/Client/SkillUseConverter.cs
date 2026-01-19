using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class SkillUseConverter : PacketConverterBase<SkillUseArgs>
{
    public override byte OpCode => (byte)ClientOpCode.SkillUse;

    public override SkillUseArgs Deserialize(ref SpanReader reader)
    {
        var sourceSlot = reader.ReadByte();

        return new SkillUseArgs
        {
            SourceSlot = sourceSlot
        };
    }
}