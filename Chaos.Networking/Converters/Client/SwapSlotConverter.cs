using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class SwapSlotConverter : PacketConverterBase<SwapSlotArgs>
{
    public override byte OpCode => (byte)ClientOpCode.SwapSlot;

    public override SwapSlotArgs Deserialize(ref SpanReader reader)
    {
        var panelType = reader.ReadByte();
        var slot1 = reader.ReadByte();
        var slot2 = reader.ReadByte();

        return new SwapSlotArgs
        {
            PanelType = (PanelType)panelType,
            Slot1 = slot1,
            Slot2 = slot2
        };
    }
}