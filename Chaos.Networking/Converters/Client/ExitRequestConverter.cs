using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class ExitRequestConverter : PacketConverterBase<ExitRequestArgs>
{
    public override byte OpCode => (byte)ClientOpCode.ExitRequest;

    public override ExitRequestArgs Deserialize(ref SpanReader reader)
    {
        var isRequest = reader.ReadBoolean();

        return new ExitRequestArgs
        {
            IsRequest = isRequest
        };
    }
}