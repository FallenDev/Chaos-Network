using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class RefreshRequestConverter : PacketConverterBase<RefreshRequestArgs>
{
    public override byte OpCode => (byte)ClientOpCode.RefreshRequest;

    public override RefreshRequestArgs Deserialize(ref SpanReader reader) => new();
}