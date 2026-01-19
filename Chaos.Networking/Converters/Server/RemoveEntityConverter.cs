using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class RemoveEntityConverter : PacketConverterBase<RemoveEntityArgs>
{
    public override byte OpCode => (byte)ServerOpCode.RemoveEntity;

    public override void Serialize(ref SpanWriter writer, RemoveEntityArgs args) => writer.WriteUInt32(args.SourceId);
}