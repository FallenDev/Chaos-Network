using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class HeartBeatConverter : PacketConverterBase<HeartBeatArgs>
{
    public override byte OpCode => (byte)ClientOpCode.HeartBeat;

    public override HeartBeatArgs Deserialize(ref SpanReader reader)
    {
        var first = reader.ReadByte();
        var second = reader.ReadByte();

        return new HeartBeatArgs
        {
            First = first,
            Second = second
        };
    }
}