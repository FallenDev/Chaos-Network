using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="RemoveEntityArgs" />
/// </summary>
public sealed class RemoveEntityConverter : PacketConverterBase<RemoveEntityArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.RemoveEntity;

    /// <inheritdoc />
    public override RemoveEntityArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, RemoveEntityArgs args) => writer.WriteUInt32(args.SourceId);
}