using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class DisplayDialogConverter : PacketConverterBase<DisplayDialogArgs>
{
    public override byte OpCode => (byte)ServerOpCode.DisplayDialog;

    public override void Serialize(ref SpanWriter writer, DisplayDialogArgs args)
    {
        writer.WriteByte((byte)args.DialogType);

        if (args.DialogType == DialogType.CloseDialog)
            return;

        var offsetSprite = args.Sprite;

        if (args.Sprite is not 0)
            switch (args.EntityType)
            {
                case EntityType.Item:
                    offsetSprite += NETWORKING_CONSTANTS.ITEM_SPRITE_OFFSET;

                    break;
                case EntityType.Aisling or EntityType.Creature:
                    offsetSprite += NETWORKING_CONSTANTS.CREATURE_SPRITE_OFFSET;

                    break;
            }

        writer.WriteByte((byte)args.EntityType);
        writer.WriteUInt32(args.SourceId ?? 0);
        writer.WriteByte(0);
        writer.WriteUInt16(offsetSprite);
        writer.WriteByte((byte)args.Color);
        writer.WriteByte(0);
        writer.WriteUInt16(offsetSprite);
        writer.WriteByte((byte)args.Color);
        writer.WriteUInt16(args.PursuitId ?? 0);
        writer.WriteUInt16(args.DialogId);
        writer.WriteBoolean(args.HasPreviousButton);
        writer.WriteBoolean(args.HasNextButton);
        writer.WriteBoolean(args.ShouldIllustrate);
        writer.WriteString8(args.Name);
        writer.WriteString16(args.Text);

        switch (args.DialogType)
        {
            case DialogType.Normal:
                break;
            case DialogType.DialogMenu:
                writer.WriteByte((byte)args.Options!.Count);

                foreach (var option in args.Options)
                {
                    writer.WriteString8(option.Text);
                    writer.WriteUInt16(option.Pursuit);
                }

                break;
            case DialogType.TextEntry:
                writer.WriteString8(args.TextBoxPrompt ?? string.Empty);
                writer.WriteByte((byte)(args.TextBoxLength ?? 0));

                break;
            case DialogType.Speak:
                break;
            case DialogType.CreatureMenu:
                writer.WriteByte((byte)args.Options!.Count);

                foreach (var option in args.Options)
                {
                    writer.WriteString8(option.Text);
                    writer.WriteUInt16(option.Pursuit);
                }

                break;
            case DialogType.Protected:
                break;
            case DialogType.CloseDialog:
                throw new InvalidOperationException("This should never happen, CloseDialog is handled above");
            default:
                throw new ArgumentOutOfRangeException(nameof(args.DialogType), args.DialogType, "Unknown dialog type");
        }
    }
}