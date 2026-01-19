using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class UserIdConverter : PacketConverterBase<UserIdArgs>
{
    public override byte OpCode => (byte)ServerOpCode.UserId;

    public override void Serialize(ref SpanWriter writer, UserIdArgs args)
    {
        writer.WriteUInt32(args.Id);
        writer.WriteByte((byte)args.Direction);
        writer.WriteByte(0);
        writer.WriteByte((byte)args.BaseClass);
        writer.WriteByte(0);
        writer.WriteByte(0);
        writer.WriteByte(0);
    }
}