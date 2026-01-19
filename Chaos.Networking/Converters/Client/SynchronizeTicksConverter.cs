using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class SynchronizeTicksConverter : PacketConverterBase<SynchronizeTicksArgs>
{
    public override byte OpCode => (byte)ClientOpCode.SynchronizeTicks;

    public override SynchronizeTicksArgs Deserialize(ref SpanReader reader)
    {
        var serverTicks = reader.ReadUInt32();
        var clientTicks = reader.ReadUInt32();

        return new SynchronizeTicksArgs
        {
            ServerTicks = serverTicks,
            ClientTicks = clientTicks
        };
    }
}