using Chaos.Common.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Serializers;

/// <summary>
///     Serializes a <see cref="DisplayAislingArgs" /> into a buffer
/// </summary>
public sealed record DisplayAislingSerializer : ServerPacketSerializer<DisplayAislingArgs>
{
    /// <inheritdoc />
    public override ServerOpCode ServerOpCode => ServerOpCode.DisplayAisling;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, DisplayAislingArgs args)
    {
        writer.WritePoint16((ushort)args.X, (ushort)args.Y);
        writer.WriteByte((byte)args.Direction);
        writer.WriteUInt32(args.Id);

        if (args is { IsHidden: true })
        {
            writer.WriteUInt16(0);
            writer.WriteByte((byte)BodySprite.None);
            writer.WriteBytes(new byte[25]);
            writer.WriteBoolean(args.IsHidden);
            writer.WriteByte(0);
        } else if (args.Sprite.HasValue)
        {
            writer.WriteUInt16(ushort.MaxValue);
            writer.WriteUInt16((ushort)(args.Sprite.Value + NETWORKING_CONSTANTS.CREATURE_SPRITE_OFFSET));
            writer.WriteByte((byte)args.HeadColor);
            writer.WriteByte((byte)args.BootsColor);
            writer.WriteBytes(new byte[6]);
        } else if (args.IsDead)
        {
            writer.WriteUInt16(args.HeadSprite);
            writer.WriteByte((byte)(args.Gender == Gender.Male ? BodySprite.MaleGhost : BodySprite.FemaleGhost));
            writer.WriteBytes(new byte[25]);
            writer.WriteBoolean(args.IsTransparent);
            writer.WriteByte(args.FaceSprite);
        } else
        {
            var pantsColor = (byte)(args.PantsColor ?? 0);
            var bodySprite = (byte)(args.BodySprite + pantsColor);

            writer.WriteUInt16(args.HeadSprite);
            writer.WriteByte(bodySprite);
            writer.WriteUInt16(args.ArmorSprite1);
            writer.WriteByte(args.BootsSprite);
            writer.WriteUInt16(args.ArmorSprite2);
            writer.WriteByte(args.ShieldSprite);
            writer.WriteUInt16(args.WeaponSprite);
            writer.WriteByte((byte)args.HeadColor);
            writer.WriteByte((byte)args.BootsColor);
            writer.WriteByte((byte)args.AccessoryColor1);
            writer.WriteUInt16(args.AccessorySprite1);
            writer.WriteByte((byte)args.AccessoryColor2);
            writer.WriteUInt16(args.AccessorySprite2);
            writer.WriteByte((byte)args.AccessoryColor3);
            writer.WriteUInt16(args.AccessorySprite3);
            writer.WriteByte((byte)args.LanternSize);
            writer.WriteByte((byte)args.RestPosition);
            writer.WriteUInt16(args.OvercoatSprite);
            writer.WriteByte((byte)args.OvercoatColor);
            writer.WriteByte((byte)args.BodyColor);
            writer.WriteBoolean(args.IsTransparent);
            writer.WriteByte(args.FaceSprite);
        }

        writer.WriteByte((byte)args.NameTagStyle);
        writer.WriteString8(args.Name);
        writer.WriteString8(args.GroupBoxText ?? string.Empty);
    }
}