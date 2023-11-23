using System.Text;
using Chaos.IO.Memory;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Deserializers;

/// <summary>
///     Deserializes a buffer into <see cref="ClientRedirectedArgs" />
/// </summary>
public sealed record ClientRedirectedDeserializer : ClientPacketDeserializer<ClientRedirectedArgs>
{
    /// <inheritdoc />
    public override ClientOpCode ClientOpCode => ClientOpCode.ClientRedirected;

    /// <inheritdoc />
    public override ClientRedirectedArgs Deserialize(ref SpanReader reader)
    {
        var seed = reader.ReadByte();
        var key = reader.ReadString8();
        var name = reader.ReadString8();
        var id = reader.ReadUInt32();

        return new ClientRedirectedArgs(
            seed,
            Encoding.ASCII.GetBytes(key),
            name,
            id);
    }
}