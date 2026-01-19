using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public class AcceptConnectionConverter : PacketConverterBase<AcceptConnectionArgs>
{
    public override byte OpCode => (byte)ServerOpCode.AcceptConnection;

    public override void Serialize(ref SpanWriter writer, AcceptConnectionArgs args)
    {
        writer.WriteByte(27);
        writer.WriteString(args.Message, true);
    }
}