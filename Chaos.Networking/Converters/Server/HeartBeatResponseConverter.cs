using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="HeartBeatResponseArgs" />
/// </summary>
public sealed class HeartBeatResponseConverter : PacketConverterBase<HeartBeatResponseArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.HeartBeatResponse;

    /// <inheritdoc />
    public override HeartBeatResponseArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, HeartBeatResponseArgs responseArgs)
    {
        writer.WriteByte(responseArgs.First);
        writer.WriteByte(responseArgs.Second);
    }
}