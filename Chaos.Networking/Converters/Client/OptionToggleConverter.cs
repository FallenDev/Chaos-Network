using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class OptionToggleConverter : PacketConverterBase<OptionToggleArgs>
{
    public override byte OpCode => (byte)ClientOpCode.OptionToggle;

    public override OptionToggleArgs Deserialize(ref SpanReader reader)
    {
        var userOption = reader.ReadByte();

        return new OptionToggleArgs
        {
            UserOption = (UserOption)userOption
        };
    }
}