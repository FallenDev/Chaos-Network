using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class DoorConverter : PacketConverterBase<DoorArgs>
{
    public override byte OpCode => (byte)ServerOpCode.Door;

    public override void Serialize(ref SpanWriter writer, DoorArgs args)
    {
        writer.WriteByte((byte)args.Doors.Count);

        foreach (var door in args.Doors)
        {
            writer.WritePoint8((byte)door.X, (byte)door.Y);
            writer.WriteBoolean(door.Closed);
            writer.WriteBoolean(door.OpenRight);
        }
    }
}