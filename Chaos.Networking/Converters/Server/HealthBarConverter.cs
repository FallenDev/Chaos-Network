using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class HealthBarConverter : PacketConverterBase<HealthBarArgs>
{
    public override byte OpCode => (byte)ServerOpCode.HealthBar;

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