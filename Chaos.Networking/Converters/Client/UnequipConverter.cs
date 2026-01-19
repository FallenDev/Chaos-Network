using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class UnequipConverter : PacketConverterBase<UnequipArgs>
{
    public override byte OpCode => (byte)ClientOpCode.Unequip;

    public override UnequipArgs Deserialize(ref SpanReader reader)
    {
        var equipmentSlot = reader.ReadByte();

        return new UnequipArgs
        {
            EquipmentSlot = (EquipmentSlot)equipmentSlot
        };
    }
}