using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class EmoteConverter : PacketConverterBase<EmoteArgs>
{
    public override byte OpCode => (byte)ClientOpCode.Emote;

    public override EmoteArgs Deserialize(ref SpanReader reader)
    {
        var bodyAnimation = reader.ReadByte();

        return new EmoteArgs
        {
            BodyAnimation = (BodyAnimation)(bodyAnimation + 9)
        };
    }
}