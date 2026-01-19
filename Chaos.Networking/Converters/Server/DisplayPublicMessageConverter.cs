using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class DisplayPublicMessageConverter : PacketConverterBase<DisplayPublicMessageArgs>
{
    public override byte OpCode => (byte)ServerOpCode.DisplayPublicMessage;

    public override void Serialize(ref SpanWriter writer, DisplayPublicMessageArgs args)
    {
        writer.WriteByte((byte)args.PublicMessageType);
        writer.WriteUInt32(args.SourceId);
        writer.WriteString8(args.Message);
    }
}