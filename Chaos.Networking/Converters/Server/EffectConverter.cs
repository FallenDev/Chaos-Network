using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class EffectConverter : PacketConverterBase<EffectArgs>
{
    public override byte OpCode => (byte)ServerOpCode.Effect;

    public override void Serialize(ref SpanWriter writer, EffectArgs args)
    {
        writer.WriteUInt16(args.EffectIcon);
        writer.WriteByte((byte)args.EffectColor);
    }
}