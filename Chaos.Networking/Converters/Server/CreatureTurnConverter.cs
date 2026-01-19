using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class CreatureTurnConverter : PacketConverterBase<CreatureTurnArgs>
{
    public override byte OpCode => (byte)ServerOpCode.CreatureTurn;

    public override void Serialize(ref SpanWriter writer, CreatureTurnArgs args)
    {
        writer.WriteUInt32(args.SourceId);
        writer.WriteByte((byte)args.Direction);
    }
}