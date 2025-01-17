using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="WorldListArgs" />
/// </summary>
public sealed class WorldListConverter : PacketConverterBase<WorldListArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.WorldList;

    /// <inheritdoc />
    public override WorldListArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, WorldListArgs args)
    {
        writer.WriteUInt16(args.WorldMemberCount);
        writer.WriteUInt16((ushort)args.CountryList.Count);

        foreach (var user in args.CountryList)
        {
            writer.WriteByte((byte)user.BaseClass);
            writer.WriteByte((byte)user.Color);
            writer.WriteByte((byte)user.SocialStatus);
            writer.WriteString(user.Title ?? string.Empty);
            writer.WriteBoolean(user.IsMaster);
            writer.WriteString(user.Name);
        }
    }
}