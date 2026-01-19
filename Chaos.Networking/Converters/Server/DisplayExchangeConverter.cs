using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class DisplayExchangeConverter : PacketConverterBase<DisplayExchangeArgs>
{
    public override byte OpCode => (byte)ServerOpCode.DisplayExchange;

    public override void Serialize(ref SpanWriter writer, DisplayExchangeArgs args)
    {
        writer.WriteByte((byte)args.ExchangeResponseType);

        switch (args.ExchangeResponseType)
        {
            case ExchangeResponseType.StartExchange:
                writer.WriteUInt32(args.OtherUserId!.Value);
                writer.WriteString8(args.OtherUserName);

                break;
            case ExchangeResponseType.RequestAmount:
                writer.WriteByte(args.FromSlot!.Value);

                break;
            case ExchangeResponseType.AddItem:
                writer.WriteBoolean(args.RightSide!.Value);
                writer.WriteByte(args.ExchangeIndex!.Value);
                writer.WriteUInt16((ushort)(args.ItemSprite!.Value + NETWORKING_CONSTANTS.ITEM_SPRITE_OFFSET));
                writer.WriteByte((byte)args.ItemColor!.Value);
                writer.WriteString8(args.ItemName!);

                break;
            case ExchangeResponseType.SetGold:
                writer.WriteBoolean(args.RightSide!.Value);
                writer.WriteInt32(args.GoldAmount!.Value);

                break;
            case ExchangeResponseType.Cancel:
                writer.WriteBoolean(args.RightSide!.Value);
                writer.WriteString8(args.Message!);

                break;
            case ExchangeResponseType.Accept:
                writer.WriteBoolean(args.PersistExchange!.Value);
                writer.WriteString8(args.Message!);

                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(args.ExchangeResponseType),
                    args.ExchangeResponseType,
                    "Unknown exchange response type");
        }
    }
}