using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Serializes a <see cref="ServerTableResponseArgs" /> into a buffer
/// </summary>
public sealed class ServerTableResponseConverter : PacketConverterBase<ServerTableResponseArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.ServerTableResponse;

    /// <inheritdoc />
    public override ServerTableResponseArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, ServerTableResponseArgs args) => writer.WriteData(args.ServerTable);
}