using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

/// <summary>
///     Provides packet serialization and deserialization logic for <see cref="WhisperArgs" />
/// </summary>
public sealed class WhisperConverter : PacketConverterBase<WhisperArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ClientOpCode.Whisper;

    /// <inheritdoc />
    public override WhisperArgs Deserialize(ref SpanReader reader)
    {
        var targetName = reader.ReadString();
        var message = reader.ReadString();

        return new WhisperArgs
        {
            TargetName = targetName,
            Message = message
        };
    }

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, WhisperArgs args)
    {
        writer.WriteString(args.TargetName);
        writer.WriteString(args.Message);
    }
}