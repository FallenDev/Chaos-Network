using Chaos.Common.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Deserializers;

/// <summary>
///     Deserializes a buffer into <see cref="DialogResponseArgs" />
/// </summary>
public sealed record DialogResponseDeserializer : ClientPacketDeserializer<DialogResponseArgs>
{
    /// <inheritdoc />
    public override ClientOpCode ClientOpCode => ClientOpCode.DialogResponse;

    /// <inheritdoc />
    public override DialogResponseArgs Deserialize(ref SpanReader reader)
    {
        var worldObjectType = (EntityType)reader.ReadByte();
        var objectId = reader.ReadUInt32();
        var pursuitId = reader.ReadUInt16();
        var dialogId = reader.ReadUInt16();
        var dialogArgsType = default(DialogArgsType);
        var option = default(byte?);
        var args = default(List<string>?);

        if (!reader.EndOfSpan)
        {
            dialogArgsType = (DialogArgsType)reader.ReadByte();

            switch (dialogArgsType)
            {
                case DialogArgsType.MenuResponse:
                    option = reader.ReadByte();

                    break;
                case DialogArgsType.TextResponse:
                {
                    args = reader.ReadArgs8();

                    if (!args.Any())
                        args = null;

                    break;
                }
                case DialogArgsType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return new DialogResponseArgs(
            worldObjectType,
            objectId,
            pursuitId,
            dialogId,
            dialogArgsType,
            option,
            args);
    }
}