using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="ServerMessageArgs" />
/// </summary>
public sealed class ServerMessageConverter : PacketConverterBase<ServerMessageArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.ServerMessage;

    /// <inheritdoc />
    public override ServerMessageArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, ServerMessageArgs args)
    {
        writer.WriteByte((byte)args.ServerMessageType);
        writer.WriteString(args.Message);
    }
}