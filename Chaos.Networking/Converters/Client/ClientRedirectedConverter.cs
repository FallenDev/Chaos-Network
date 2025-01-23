using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

/// <summary>
///     Provides packet serialization and deserialization logic for <see cref="ClientRedirectedArgs" />
/// </summary>
public sealed class ClientRedirectedConverter : PacketConverterBase<ClientRedirectedArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ClientOpCode.ClientRedirected;

    /// <inheritdoc />
    public override ClientRedirectedArgs Deserialize(ref SpanReader reader)
    {
        var message = reader.ReadString();

        return new ClientRedirectedArgs
        {
            Message = message
        };
    }
}