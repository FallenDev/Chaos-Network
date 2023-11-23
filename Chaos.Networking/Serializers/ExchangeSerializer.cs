using Chaos.Common.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Serializers;

/// <summary>
///     Serializes a <see cref="ExchangeArgs" /> into a buffer
/// </summary>
public sealed record ExchangeSerializer : ServerPacketSerializer<ExchangeArgs>
{
    /// <inheritdoc />
    public override ServerOpCode ServerOpCode => ServerOpCode.Exchange;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, ExchangeArgs args)
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
                writer.WriteString8("Exchange cancelled.");

                break;
            case ExchangeResponseType.Accept:
                writer.WriteBoolean(args.PersistExchange!.Value);
                writer.WriteString8("Exchange completed.");

                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(args.ExchangeResponseType),
                    args.ExchangeResponseType,
                    "Unknown exchange response type");
        }
    }
}