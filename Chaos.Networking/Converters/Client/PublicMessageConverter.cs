using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class PublicMessageConverter : PacketConverterBase<PublicMessageArgs>
{
    public override byte OpCode => (byte)ClientOpCode.PublicMessage;

    public override PublicMessageArgs Deserialize(ref SpanReader reader)
    {
        var publicMessageType = reader.ReadByte();
        var message = reader.ReadString8();

        return new PublicMessageArgs
        {
            PublicMessageType = (PublicMessageType)publicMessageType,
            Message = message
        };
    }
}