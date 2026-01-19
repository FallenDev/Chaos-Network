using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class SocialStatusConverter : PacketConverterBase<SocialStatusArgs>
{
    public override byte OpCode => (byte)ClientOpCode.SocialStatus;

    public override SocialStatusArgs Deserialize(ref SpanReader reader)
    {
        var socialStatus = reader.ReadByte();

        return new SocialStatusArgs
        {
            SocialStatus = (SocialStatus)socialStatus
        };
    }
}