using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="AttributesArgs" />
/// </summary>
public sealed class AttributesConverter : PacketConverterBase<AttributesArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.Attributes;

    /// <inheritdoc />
    public override AttributesArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, AttributesArgs args)
    {
        var updateType = args.StatUpdateType;

        if (args.IsAdmin)
            updateType |= StatUpdateType.GameMasterA;
        else
            if (args.IsSwimming)
                updateType |= StatUpdateType.Swimming;

        if (args.HasUnreadMail)
            updateType |= StatUpdateType.UnreadMail;

        writer.WriteByte((byte)updateType);

        if (args.StatUpdateType.AttributeFlagIsSet(StatUpdateType.Primary))
        {
            writer.WriteBytes(1, 0, 0); //LI: what is this for?
            writer.WriteByte(args.Level);
            writer.WriteByte(args.Ability);
            writer.WriteUInt32(args.MaximumHp);
            writer.WriteUInt32(args.MaximumMp);
            writer.WriteByte(args.Str);
            writer.WriteByte(args.Int);
            writer.WriteByte(args.Wis);
            writer.WriteByte(args.Con);
            writer.WriteByte(args.Dex);
            writer.WriteBoolean(args.HasUnspentPoints);
            writer.WriteByte(args.UnspentPoints);
            writer.WriteInt16(args.MaxWeight);
            writer.WriteInt16(args.CurrentWeight);
            writer.WriteBytes(new byte[4]); //LI: what is this for?  42 00 88 2E
        }

        if (args.StatUpdateType.AttributeFlagIsSet(StatUpdateType.Vitality))
        {
            writer.WriteUInt32(args.CurrentHp);
            writer.WriteUInt32(args.CurrentMp);
        }

        if (args.StatUpdateType.AttributeFlagIsSet(StatUpdateType.ExpGold))
        {
            writer.WriteUInt32(args.TotalExp);
            writer.WriteUInt32(args.ToNextLevel);
            writer.WriteUInt32(args.TotalAbility);
            writer.WriteUInt32(args.ToNextAbility);
            writer.WriteUInt32(args.GamePoints);
            writer.WriteUInt32(args.Gold);
        }

        if (args.StatUpdateType.AttributeFlagIsSet(StatUpdateType.Secondary))
        {
            writer.WriteByte(0); //LI: what is this for?
            writer.WriteByte((byte)(args.Blind ? 8 : 0));
            writer.WriteBytes(new byte[3]); //LI: what is this for?
            writer.WriteByte((byte)(args.HasUnreadMail ? MailFlag.HasMail : MailFlag.None));
            writer.WriteByte((byte)args.OffenseElement);
            writer.WriteByte((byte)args.DefenseElement);
            writer.WriteByte(args.MagicResistance);
            writer.WriteByte(0); //LI: what is this for?
            writer.WriteSByte(args.Ac);
            writer.WriteByte(args.Dmg);
            writer.WriteByte(args.Hit);
        }
    }
}

public static class AttributeExtensions
{
    public static bool AttributeFlagIsSet(this StatUpdateType self, StatUpdateType flag) => (self & flag) == flag;
}