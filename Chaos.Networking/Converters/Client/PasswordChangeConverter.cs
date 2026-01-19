using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class PasswordChangeConverter : PacketConverterBase<PasswordChangeArgs>
{
    public override byte OpCode => (byte)ClientOpCode.PasswordChange;

    public override PasswordChangeArgs Deserialize(ref SpanReader reader)
    {
        var name = reader.ReadString8();
        var currentPw = reader.ReadString8();
        var newPw = reader.ReadString8();

        return new PasswordChangeArgs
        {
            Name = name,
            CurrentPassword = currentPw,
            NewPassword = newPw
        };
    }
}