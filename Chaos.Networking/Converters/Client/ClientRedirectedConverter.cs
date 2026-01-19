using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class ClientRedirectedConverter : PacketConverterBase<ClientRedirectedArgs>
{
    public override byte OpCode => (byte)ClientOpCode.ClientRedirected;

    public override ClientRedirectedArgs Deserialize(ref SpanReader reader)
    {
        var seed = reader.ReadByte();
        var key = reader.ReadString8();
        var name = reader.ReadString8();
        var id = reader.ReadUInt32();

        return new ClientRedirectedArgs
        {
            Seed = seed,
            Key = key,
            Name = name,
            Id = id
        };
    }
}