using Chaos.IO.Memory;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Serializers;

/// <summary>
///     Serializes a <see cref="LightLevelArgs" /> into a buffer
/// </summary>
public sealed record LightLevelSerializer : ServerPacketSerializer<LightLevelArgs>
{
    /// <inheritdoc />
    public override ServerOpCode ServerOpCode => ServerOpCode.LightLevel;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, LightLevelArgs args) => writer.WriteByte((byte)args.LightLevel);
    //writer.WriteByte(1);
}