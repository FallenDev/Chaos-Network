using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class ExitResponseConverter : PacketConverterBase<ExitResponseArgs>
{
    public override byte OpCode => (byte)ServerOpCode.ExitResponse;

    public override void Serialize(ref SpanWriter writer, ExitResponseArgs args)
    {
        writer.WriteBoolean(args.ExitConfirmed);
        writer.WriteBytes(new byte[2]);
    }
}