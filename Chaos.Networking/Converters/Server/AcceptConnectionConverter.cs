using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="AcceptConnectionArgs" />
/// </summary>
public class AcceptConnectionConverter : PacketConverterBase<AcceptConnectionArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.AcceptConnection;

    /// <inheritdoc />
    public override AcceptConnectionArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, AcceptConnectionArgs args)
    {
        writer.WriteString(args.Message);
    }
}