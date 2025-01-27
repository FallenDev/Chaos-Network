using System.Net;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="RedirectArgs" />
/// </summary>
public sealed class RedirectConverter : PacketConverterBase<RedirectArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.Redirect;

    /// <inheritdoc />
    public override RedirectArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, RedirectArgs args)
    {
        writer.WriteUInt16((ushort)args.EndPoint.Port);

        var remaining = args.Key.Length;

        remaining += 7;

        writer.WriteByte((byte)remaining);
        writer.WriteByte(args.Seed); //1
        writer.WriteString(args.Key); //1 + length
        writer.WriteString(args.Name); //1 + length
        writer.WriteUInt32(args.Id); //4
    }
}