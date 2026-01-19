using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class LocationConverter : PacketConverterBase<LocationArgs>
{
    public override byte OpCode => (byte)ServerOpCode.Location;

    public override void Serialize(ref SpanWriter writer, LocationArgs args) => writer.WritePoint16((ushort)args.X, (ushort)args.Y);
}