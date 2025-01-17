using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="HealthBarArgs" />
/// </summary>
public sealed class HealthBarConverter : PacketConverterBase<HealthBarArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.HealthBar;

    /// <inheritdoc />
    public override HealthBarArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, HealthBarArgs args)
    {
        writer.WriteUInt32(args.SourceId);
        writer.WriteByte(0);
        writer.WriteByte(args.HealthPercent);
        writer.WriteByte(args.Sound ?? byte.MaxValue);
    }
}