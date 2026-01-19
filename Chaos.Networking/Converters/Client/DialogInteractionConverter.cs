using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class DialogInteractionConverter : PacketConverterBase<DialogInteractionArgs>
{
    public override byte OpCode => (byte)ClientOpCode.DialogInteraction;

    public override DialogInteractionArgs Deserialize(ref SpanReader reader)
    {
        var entityType = reader.ReadByte();
        var entityId = reader.ReadUInt32();
        var pursuitId = reader.ReadUInt16();
        var dialogId = reader.ReadUInt16();

        var args = new DialogInteractionArgs
        {
            EntityType = (EntityType)entityType,
            EntityId = entityId,
            PursuitId = pursuitId,
            DialogId = dialogId
        };

        if (!reader.EndOfSpan)
        {
            var dialogArgsType = reader.ReadByte();

            args.DialogArgsType = (DialogArgsType)dialogArgsType;

            switch (args.DialogArgsType)
            {
                case DialogArgsType.MenuResponse:
                {
                    var option = reader.ReadByte();

                    args.Option = option;

                    break;
                }
                case DialogArgsType.TextResponse:
                {
                    var dialogArgs = reader.ReadArgs8();

                    if (dialogArgs.Count != 0)
                        args.Args = dialogArgs;

                    break;
                }
                case DialogArgsType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return args;
    }
}