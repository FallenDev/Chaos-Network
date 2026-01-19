using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class WorldListRequestConverter : PacketConverterBase<WorldListRequestArgs>
{
    public override byte OpCode => (byte)ClientOpCode.WorldListRequest;

    public override WorldListRequestArgs Deserialize(ref SpanReader reader) => new();
}