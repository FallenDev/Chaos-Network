using Chaos.Geometry;
using Chaos.IO.Memory;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Deserializers;

/// <summary>
///     Deserializes a buffer into <see cref="WorldMapClickArgs" />
/// </summary>
public sealed record WorldMapClickDeserializer : ClientPacketDeserializer<WorldMapClickArgs>
{
    /// <inheritdoc />
    public override ClientOpCode ClientOpCode => ClientOpCode.WorldMapClick;

    /// <inheritdoc />
    public override WorldMapClickArgs Deserialize(ref SpanReader reader)
    {
        var nodeCheckSum = reader.ReadUInt16();
        var mapId = reader.ReadUInt16();
        Point point = reader.ReadPoint16();

        return new WorldMapClickArgs(nodeCheckSum, mapId, point);
    }
}