using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class RaiseStatConverter : PacketConverterBase<RaiseStatArgs>
{
    public override byte OpCode => (byte)ClientOpCode.RaiseStat;

    public override RaiseStatArgs Deserialize(ref SpanReader reader)
    {
        var stat = reader.ReadByte();

        return new RaiseStatArgs
        {
            Stat = (Stat)stat
        };
    }
}