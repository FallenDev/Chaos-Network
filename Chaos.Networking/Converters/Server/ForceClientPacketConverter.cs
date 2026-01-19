using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class ForceClientPacketConverter : PacketConverterBase<ForceClientPacketArgs>
{
    public override byte OpCode => (byte)ServerOpCode.ForceClientPacket;

    public override void Serialize(ref SpanWriter writer, ForceClientPacketArgs args)
    {
        writer.WriteUInt16((ushort)(args.Data.Length + 1));
        writer.WriteByte((byte)args.ClientOpCode);
        writer.WriteData(args.Data);
    }
}