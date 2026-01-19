using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class ServerTableRequestConverter : PacketConverterBase<ServerTableRequestArgs>
{
    public override byte OpCode => (byte)ClientOpCode.ServerTableRequest;

    public override ServerTableRequestArgs Deserialize(ref SpanReader reader)
    {
        var serverTableRequestType = reader.ReadByte();

        var args = new ServerTableRequestArgs
        {
            ServerTableRequestType = (ServerTableRequestType)serverTableRequestType
        };

        if (args.ServerTableRequestType == ServerTableRequestType.ServerId)
        {
            var serverId = reader.ReadByte();

            args.ServerId = serverId;
        }

        return args;
    }
}