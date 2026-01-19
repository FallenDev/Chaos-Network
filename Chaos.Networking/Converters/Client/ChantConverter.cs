using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class ChantConverter : PacketConverterBase<ChantArgs>
{
    public override byte OpCode => (byte)ClientOpCode.Chant;

    public override ChantArgs Deserialize(ref SpanReader reader)
    {
        var chantMessage = reader.ReadString8();

        return new ChantArgs
        {
            ChantMessage = chantMessage
        };
    }
}