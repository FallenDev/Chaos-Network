using Chaos.Extensions.Networking;
using Chaos.Geometry;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="WorldMapArgs" />
/// </summary>
public sealed class WorldMapConverter : PacketConverterBase<WorldMapArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.WorldMap;

    /// <inheritdoc />
    public override WorldMapArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, WorldMapArgs args)
    {
        writer.WriteString(args.FieldName);
        writer.WriteByte((byte)args.Nodes.Count);
        writer.WriteByte(args.FieldIndex);

        foreach (var node in args.Nodes)
        {
            writer.WritePoint16(node.ScreenPosition);
            writer.WriteString(node.Text);
            writer.WriteUInt16(node.CheckSum);
            writer.WriteUInt16(node.MapId);
            writer.WritePoint16(node.DestinationPoint);
        }
    }
}