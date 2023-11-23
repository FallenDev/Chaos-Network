using Chaos.IO.Memory;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Deserializers;

/// <summary>
///     Deserializes a buffer into <see cref="ExceptionArgs" />
/// </summary>
public sealed record ExceptionDeserializer : ClientPacketDeserializer<ExceptionArgs>
{
    /// <inheritdoc />
    public override ClientOpCode ClientOpCode => ClientOpCode.Exception;

    /// <inheritdoc />
    public override ExceptionArgs Deserialize(ref SpanReader reader)
    {
        var exceptionString = reader.ReadString();

        return new ExceptionArgs(exceptionString);
    }
}