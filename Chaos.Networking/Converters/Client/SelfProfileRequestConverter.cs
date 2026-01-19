using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class SelfProfileRequestConverter : PacketConverterBase<SelfProfileRequestArgs>
{
    public override byte OpCode => (byte)ClientOpCode.SelfProfileRequest;

    public override SelfProfileRequestArgs Deserialize(ref SpanReader reader) => new();
}