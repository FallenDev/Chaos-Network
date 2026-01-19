using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class LightLevelConverter : PacketConverterBase<LightLevelArgs>
{
    public override byte OpCode => (byte)ServerOpCode.LightLevel;

    public override void Serialize(ref SpanWriter writer, LightLevelArgs args) => writer.WriteByte((byte)args.LightLevel);
}