using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class LoginNoticeConverter : PacketConverterBase<LoginNoticeArgs>
{
    public override byte OpCode => (byte)ServerOpCode.LoginNotice;

    public override void Serialize(ref SpanWriter writer, LoginNoticeArgs args)
    {
        writer.WriteBoolean(args.IsFullResponse);

        if (args.IsFullResponse)
            writer.WriteData16(args.Data!);
        else
            writer.WriteUInt32(args.CheckSum!.Value);
    }
}