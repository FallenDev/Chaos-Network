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
    public override HealthBarArgs Deserialize(ref SpanReader reader)
    {
        var sourceId = reader.ReadUInt32();
        var kind = reader.ReadByte();
        var healthPercent = reader.ReadByte();
        var soundRaw = reader.ReadByte();

        byte? tail = null;
        if (reader.Remaining > 0)
            tail = reader.ReadByte();

        return new HealthBarArgs
        {
            SourceId = sourceId,
            Kind = kind,
            HealthPercent = healthPercent,
            Sound = soundRaw == byte.MaxValue ? null : soundRaw,
            Tail = tail
        };
    }

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, HealthBarArgs args)
    {
        writer.WriteUInt32(args.SourceId);
        writer.WriteByte(args.Kind);
        writer.WriteByte(args.HealthPercent);
        writer.WriteByte(args.Sound ?? byte.MaxValue);

        if (args.Tail.HasValue)
            writer.WriteByte(args.Tail.Value);
    }
}