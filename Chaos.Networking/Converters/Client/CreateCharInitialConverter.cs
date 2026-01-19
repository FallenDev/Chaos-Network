using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class CreateCharInitialConverter : PacketConverterBase<CreateCharInitialArgs>
{
    public override byte OpCode => (byte)ClientOpCode.CreateCharInitial;

    public override CreateCharInitialArgs Deserialize(ref SpanReader reader)
    {
        var name = reader.ReadString8();
        var pw = reader.ReadString8();

        return new CreateCharInitialArgs
        {
            Name = name,
            Password = pw
        };
    }
}