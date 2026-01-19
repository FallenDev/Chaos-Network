using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class LoginConverter : PacketConverterBase<LoginArgs>
{
    public override byte OpCode => (byte)ClientOpCode.Login;

    public override LoginArgs Deserialize(ref SpanReader reader)
    {
        var name = reader.ReadString8();
        var pw = reader.ReadString8();

        return new LoginArgs
        {
            Name = name,
            Password = pw
        };
    }
}