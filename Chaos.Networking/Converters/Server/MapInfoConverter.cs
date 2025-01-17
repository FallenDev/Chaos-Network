using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="MapInfoArgs" />
/// </summary>
public sealed class MapInfoConverter : PacketConverterBase<MapInfoArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.MapInfo;

    /// <inheritdoc />
    public override MapInfoArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, MapInfoArgs args)
    {
        writer.WriteInt16(args.MapId);
        writer.WriteByte(args.Width);
        writer.WriteByte(args.Height);
        writer.WriteByte(args.Flags);
        writer.WriteBytes(new byte[2]); //LI: what is this for?
        writer.WriteUInt16(args.CheckSum);
        writer.WriteString(args.Name);
    }
}