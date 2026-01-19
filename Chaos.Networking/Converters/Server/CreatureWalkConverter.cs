using Chaos.Extensions.Networking;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class CreatureWalkConverter : PacketConverterBase<CreatureWalkArgs>
{
    public override byte OpCode => (byte)ServerOpCode.CreatureWalk;

    public override void Serialize(ref SpanWriter writer, CreatureWalkArgs args)
    {
        writer.WriteUInt32(args.SourceId);
        writer.WritePoint16(args.OldPoint);
        writer.WriteByte((byte)args.Direction);
        writer.WriteByte(0);
    }
}