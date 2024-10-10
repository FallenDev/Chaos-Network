using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="BodyAnimationArgs" />
/// </summary>
public sealed class BodyAnimationConverter : PacketConverterBase<BodyAnimationArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.BodyAnimation;

    /// <inheritdoc />
    public override BodyAnimationArgs Deserialize(ref SpanReader reader)
    {
        var sourceId = reader.ReadUInt32();
        var bodyAnimation = reader.ReadByte();
        var animationSpeed = reader.ReadUInt16();
        var sound = reader.ReadByte();

        return new BodyAnimationArgs
        {
            SourceId = sourceId,
            BodyAnimation = (BodyAnimation)bodyAnimation,
            AnimationSpeed = animationSpeed,
            Sound = sound == byte.MaxValue ? null : sound
        };
    }

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, BodyAnimationArgs args)
    {
        writer.WriteUInt32(args.SourceId);
        writer.WriteByte((byte)args.BodyAnimation);
        writer.WriteUInt16(args.AnimationSpeed);
        writer.WriteByte(args.Sound ?? byte.MaxValue);
    }
}