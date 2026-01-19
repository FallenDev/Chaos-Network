using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class BeginChantConverter : PacketConverterBase<BeginChantArgs>
{
    public override byte OpCode => (byte)ClientOpCode.BeginChant;

    public override BeginChantArgs Deserialize(ref SpanReader reader)
    {
        var castLineCount = reader.ReadByte();

        return new BeginChantArgs
        {
            CastLineCount = castLineCount
        };
    }
}