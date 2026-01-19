using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class LoginMessageConverter : PacketConverterBase<LoginMessageArgs>
{
    public override byte OpCode => (byte)ServerOpCode.LoginMessage;

    public override void Serialize(ref SpanWriter writer, LoginMessageArgs args)
    {
        writer.WriteByte((byte)args.LoginMessageType);
        writer.WriteString8(args.LoginMessageType == LoginMessageType.Confirm ? "\0" : args.Message!);
    }
}