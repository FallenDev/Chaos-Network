using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class BodyAnimationConverter : PacketConverterBase<BodyAnimationArgs>
{
    public override byte OpCode => (byte)ServerOpCode.BodyAnimation;

    public override void Serialize(ref SpanWriter writer, BodyAnimationArgs args)
    {
        writer.WriteUInt32(args.SourceId);
        writer.WriteByte((byte)args.BodyAnimation);
        writer.WriteUInt16(args.AnimationSpeed);
        writer.WriteByte(args.Sound ?? byte.MaxValue);
    }
}