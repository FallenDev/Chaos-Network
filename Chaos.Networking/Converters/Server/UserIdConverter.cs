using Chaos.DarkAges.Definitions;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="UserIdArgs" />
/// </summary>
public sealed class UserIdConverter : PacketConverterBase<UserIdArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.UserId;

    /// <inheritdoc />
    public override UserIdArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, UserIdArgs args)
    {
        writer.WriteUInt32(args.Id);
        writer.WriteByte((byte)args.Direction);
        writer.WriteByte(0); //LI: what is this for?
        writer.WriteByte((byte)args.BaseClass);
        writer.WriteByte(0); //LI: what is this for?
        writer.WriteByte(0); //LI: what is this for?
        writer.WriteByte(0); //LI: what is this for?
    }
}