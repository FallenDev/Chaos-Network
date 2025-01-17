using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="DisplayExchangeArgs" />
/// </summary>
public sealed class DisplayExchangeConverter : PacketConverterBase<DisplayExchangeArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.DisplayExchange;

    /// <inheritdoc />
    public override DisplayExchangeArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, DisplayExchangeArgs args)
    {
        writer.WriteByte((byte)args.ExchangeResponseType);

        switch (args.ExchangeResponseType)
        {
            case ExchangeResponseType.StartExchange:
                writer.WriteUInt32(args.OtherUserId!.Value);
                writer.WriteString(args.OtherUserName);

                break;
            case ExchangeResponseType.RequestAmount:
                writer.WriteByte(args.FromSlot!.Value);

                break;
            case ExchangeResponseType.AddItem:
                writer.WriteBoolean(args.RightSide!.Value);
                writer.WriteByte(args.ExchangeIndex!.Value);
                writer.WriteUInt16((ushort)(args.ItemSprite!.Value + NetworkingConstants.ItemSpriteOffset));
                writer.WriteByte((byte)args.ItemColor!.Value);
                writer.WriteString(args.ItemName!);

                break;
            case ExchangeResponseType.SetGold:
                writer.WriteBoolean(args.RightSide!.Value);
                writer.WriteInt32(args.GoldAmount!.Value);

                break;
            case ExchangeResponseType.Cancel:
                writer.WriteBoolean(args.RightSide!.Value);
                writer.WriteString(args.Message!);

                break;
            case ExchangeResponseType.Accept:
                writer.WriteBoolean(args.PersistExchange!.Value);
                writer.WriteString(args.Message!);

                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(args.ExchangeResponseType),
                    args.ExchangeResponseType,
                    "Unknown exchange response type");
        }
    }
}