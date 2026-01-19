using Chaos.Geometry.Abstractions.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class TurnConverter : PacketConverterBase<TurnArgs>
{
    public override byte OpCode => (byte)ClientOpCode.Turn;

    public override TurnArgs Deserialize(ref SpanReader reader)
    {
        var direction = reader.ReadByte();

        return new TurnArgs
        {
            Direction = (Direction)direction
        };
    }
}