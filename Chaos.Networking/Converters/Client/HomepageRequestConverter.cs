using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class HomepageRequestConverter : PacketConverterBase<HomepageRequestArgs>
{
    public override byte OpCode => (byte)ClientOpCode.HomepageRequest;

    public override HomepageRequestArgs Deserialize(ref SpanReader reader) => new();
}