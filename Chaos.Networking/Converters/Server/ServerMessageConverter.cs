using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class ServerMessageConverter : PacketConverterBase<ServerMessageArgs>
{
    public override byte OpCode => (byte)ServerOpCode.ServerMessage;

    public override void Serialize(ref SpanWriter writer, ServerMessageArgs args)
    {
        writer.WriteByte((byte)args.ServerMessageType);
        writer.WriteString16(args.Message);
    }
}