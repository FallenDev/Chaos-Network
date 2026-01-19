using Chaos.Extensions.Networking;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class AnimationConverter : PacketConverterBase<AnimationArgs>
{
    public override byte OpCode => (byte)ServerOpCode.Animation;

    public override void Serialize(ref SpanWriter writer, AnimationArgs args)
    {
        if (args.TargetPoint.HasValue)
        {
            writer.WriteUInt32(0);
            writer.WriteUInt16(args.TargetAnimation);
            writer.WriteUInt16(args.AnimationSpeed);
            writer.WritePoint16(args.TargetPoint.Value);
        } else
        {
            writer.WriteUInt32(args.TargetId ?? 0);
            writer.WriteUInt32(args.SourceId ?? 0);
            writer.WriteUInt16(args.TargetAnimation);
            writer.WriteUInt16(args.SourceAnimation);
            writer.WriteUInt16(args.AnimationSpeed);
        }
    }
}