using Chaos.Geometry.Abstractions.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class ClientWalkConverter : PacketConverterBase<ClientWalkArgs>
{
    public override byte OpCode => (byte)ClientOpCode.ClientWalk;

    public override ClientWalkArgs Deserialize(ref SpanReader reader)
    {
        var direction = reader.ReadByte();
        var stepCount = reader.ReadByte();

        return new ClientWalkArgs
        {
            Direction = (Direction)direction,
            StepCount = stepCount
        };
    }
}