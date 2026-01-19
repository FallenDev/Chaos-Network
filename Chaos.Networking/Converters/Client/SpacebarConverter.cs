using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class SpacebarConverter : PacketConverterBase<SpacebarArgs>
{
    public override byte OpCode => (byte)ClientOpCode.Spacebar;

    public override SpacebarArgs Deserialize(ref SpanReader reader) => new();
}