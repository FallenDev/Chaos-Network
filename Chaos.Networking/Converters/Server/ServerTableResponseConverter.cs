using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class ServerTableResponseConverter : PacketConverterBase<ServerTableResponseArgs>
{
    public override byte OpCode => (byte)ServerOpCode.ServerTableResponse;

    public override void Serialize(ref SpanWriter writer, ServerTableResponseArgs args) => writer.WriteData16(args.ServerTable);
}