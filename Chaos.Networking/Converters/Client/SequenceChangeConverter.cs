using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class SequenceChangeConverter : PacketConverterBase<SequenceChangeArgs>
{
    public override byte OpCode => (byte)ClientOpCode.SequenceChange;

    public override SequenceChangeArgs Deserialize(ref SpanReader reader) => new();
}