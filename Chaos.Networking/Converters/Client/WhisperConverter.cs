using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class WhisperConverter : PacketConverterBase<WhisperArgs>
{
    public override byte OpCode => (byte)ClientOpCode.Whisper;

    public override WhisperArgs Deserialize(ref SpanReader reader)
    {
        var targetName = reader.ReadString8();
        var message = reader.ReadString8();

        return new WhisperArgs
        {
            TargetName = targetName,
            Message = message
        };
    }
}