using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class RedirectConverter : PacketConverterBase<RedirectArgs>
{
    public override byte OpCode => (byte)ServerOpCode.Redirect;

    public override void Serialize(ref SpanWriter writer, RedirectArgs args)
    {
        writer.WriteBytes(
            args.EndPoint
                .Address
                .GetAddressBytes()
                .Reverse()
                .ToArray());
        writer.WriteUInt16((ushort)args.EndPoint.Port);

        var remaining = args.Key.Length;

        remaining += writer.Encoding.GetBytes(args.Name)
                           .Length;
        remaining += 7;

        writer.WriteByte((byte)remaining);
        writer.WriteByte(args.Seed);
        writer.WriteString8(args.Key); // 1 + length
        writer.WriteString8(args.Name); // 1 + length
        writer.WriteUInt32(args.Id); // 4
    }
}