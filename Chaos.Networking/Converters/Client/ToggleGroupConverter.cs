using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class ToggleGroupConverter : PacketConverterBase<ToggleGroupArgs>
{
    public override byte OpCode => (byte)ClientOpCode.ToggleGroup;

    public override ToggleGroupArgs Deserialize(ref SpanReader reader) => new();
}