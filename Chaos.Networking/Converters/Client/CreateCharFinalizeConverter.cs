using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class CreateCharFinalizeConverter : PacketConverterBase<CreateCharFinalizeArgs>
{
    public override byte OpCode => (byte)ClientOpCode.CreateCharFinalize;

    public override CreateCharFinalizeArgs Deserialize(ref SpanReader reader)
    {
        var hairStyle = reader.ReadByte();
        var gender = reader.ReadByte();
        var hairColor = reader.ReadByte();

        return new CreateCharFinalizeArgs
        {
            HairStyle = hairStyle,
            Gender = (Gender)gender,
            HairColor = (DisplayColor)hairColor
        };
    }
}