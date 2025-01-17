using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="ConnectionInfoArgs" />
/// </summary>
public sealed class ConnectionInfoConverter : PacketConverterBase<ConnectionInfoArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.ConnectionInfo;

    /// <inheritdoc />
    public override ConnectionInfoArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, ConnectionInfoArgs args)
    {
        writer.WriteUInt16(args.PortNumber);
    }
}