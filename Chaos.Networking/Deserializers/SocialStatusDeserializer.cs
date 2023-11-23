using Chaos.Common.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Deserializers;

/// <summary>
///     Deserializes a buffer into <see cref="SocialStatusArgs" />
/// </summary>
public sealed record SocialStatusDeserializer : ClientPacketDeserializer<SocialStatusArgs>
{
    /// <inheritdoc />
    public override ClientOpCode ClientOpCode => ClientOpCode.SocialStatus;

    /// <inheritdoc />
    public override SocialStatusArgs Deserialize(ref SpanReader reader)
    {
        var socialStatus = (SocialStatus)reader.ReadByte();

        return new SocialStatusArgs(socialStatus);
    }
}