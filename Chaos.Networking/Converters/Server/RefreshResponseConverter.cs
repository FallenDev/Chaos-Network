using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public class RefreshResponseConverter : PacketConverterBase<RefreshResponseArgs>
{
    public override byte OpCode => (byte)ServerOpCode.RefreshResponse;

    public override void Serialize(ref SpanWriter writer, RefreshResponseArgs args) { }
}