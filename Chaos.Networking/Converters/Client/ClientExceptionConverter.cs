using System.Text;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class ClientExceptionConverter : PacketConverterBase<ClientExceptionArgs>
{
    public override byte OpCode => (byte)ClientOpCode.ClientException;

    public override ClientExceptionArgs Deserialize(ref SpanReader reader)
    {
        var data = reader.ReadData();

        return new ClientExceptionArgs
        {
            ExceptionStr = Encoding.GetEncoding(949)
                .GetString(data)
        };
    }
}