using Chaos.Extensions.Networking;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class ClientWalkResponseConverter : PacketConverterBase<ClientWalkResponseArgs>
{
    public override byte OpCode => (byte)ServerOpCode.ClientWalkResponse;

    public override void Serialize(ref SpanWriter writer, ClientWalkResponseArgs args)
    {
        writer.WriteBytes((byte)args.Direction);
        writer.WritePoint16(args.OldPoint);
        writer.WriteUInt16(11);
        writer.WriteUInt16(11);
        writer.WriteByte(1);
    }
}