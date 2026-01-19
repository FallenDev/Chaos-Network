using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class DisplayEntityRequestConverter : PacketConverterBase<DisplayEntityRequestArgs>
{
    public override byte OpCode => (byte)ClientOpCode.DisplayEntityRequest;

    public override DisplayEntityRequestArgs Deserialize(ref SpanReader reader)
    {
        var targetId = reader.ReadUInt32();

        return new DisplayEntityRequestArgs
        {
            TargetId = targetId
        };
    }
}