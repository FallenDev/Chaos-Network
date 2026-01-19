using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public class CancelCastingConverter : PacketConverterBase<CancelCastingArgs>
{
    public override byte OpCode => (byte)ServerOpCode.CancelCasting;

    public override void Serialize(ref SpanWriter writer, CancelCastingArgs args) { }
}