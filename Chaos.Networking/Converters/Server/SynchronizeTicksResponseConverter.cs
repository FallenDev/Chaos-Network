using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class SynchronizeTicksResponseConverter : PacketConverterBase<SynchronizeTicksResponseArgs>
{
    public override byte OpCode => (byte)ServerOpCode.SynchronizeTicksResponse;

    public override void Serialize(ref SpanWriter writer, SynchronizeTicksResponseArgs responseArgs)
        => writer.WriteInt32(responseArgs.Ticks);
}