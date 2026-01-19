using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class ConnectionInfoConverter : PacketConverterBase<ConnectionInfoArgs>
{
    public override byte OpCode => (byte)ServerOpCode.ConnectionInfo;

    public override void Serialize(ref SpanWriter writer, ConnectionInfoArgs args)
    {
        writer.WriteByte(0);
        writer.WriteUInt32(args.TableCheckSum);
        writer.WriteByte(args.Seed);
        writer.WriteString8(args.Key);
    }
}