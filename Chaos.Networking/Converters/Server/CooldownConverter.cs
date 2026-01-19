using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class CooldownConverter : PacketConverterBase<CooldownArgs>
{
    public override byte OpCode => (byte)ServerOpCode.Cooldown;

    public override void Serialize(ref SpanWriter writer, CooldownArgs args)
    {
        writer.WriteBoolean(args.IsSkill);
        writer.WriteByte(args.Slot);
        writer.WriteUInt32(args.CooldownSecs);
    }
}