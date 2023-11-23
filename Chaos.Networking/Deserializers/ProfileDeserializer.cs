using Chaos.IO.Memory;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Deserializers;

/// <summary>
///     Deserializes a buffer into <see cref="ProfileArgs" />
/// </summary>
public sealed record ProfileDeserializer : ClientPacketDeserializer<ProfileArgs>
{
    /// <inheritdoc />
    public override ClientOpCode ClientOpCode => ClientOpCode.Profile;

    /// <inheritdoc />
    public override ProfileArgs Deserialize(ref SpanReader reader)
    {
        // ReSharper disable once UnusedVariable
        var totalLength = reader.ReadUInt16();
        var portraitLength = reader.ReadUInt16();
        var portraitData = reader.ReadBytes(portraitLength);
        var profileMessage = reader.ReadString16();

        return new ProfileArgs(portraitData, profileMessage);
    }
}