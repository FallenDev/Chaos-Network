using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class NoticeRequestConverter : PacketConverterBase<NoticeRequestArgs>
{
    public override byte OpCode => (byte)ClientOpCode.NoticeRequest;

    public override NoticeRequestArgs Deserialize(ref SpanReader reader) => new();
}