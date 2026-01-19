using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class MenuInteractionConverter : PacketConverterBase<MenuInteractionArgs>
{
    public override byte OpCode => (byte)ClientOpCode.MenuInteraction;

    public override MenuInteractionArgs Deserialize(ref SpanReader reader)
    {
        var entityType = reader.ReadByte();
        var entityId = reader.ReadUInt32();
        var pursuitId = reader.ReadUInt16();

        var args = new MenuInteractionArgs
        {
            EntityType = (EntityType)entityType,
            EntityId = entityId,
            PursuitId = pursuitId
        };

        if (reader.Remaining == 1)
        {
            var slot = reader.ReadByte();

            if (slot > 0)
                args.Slot = slot;
        } else
        {
            var textArgs = reader.ReadArgs8();

            if (textArgs.Count != 0)
                args.Args = textArgs.ToArray();
        }

        return args;
    }
}