using Chaos.Extensions.Networking;
using Chaos.IO.Memory;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Serializers;

/// <summary>
///     Serializes a <see cref="CreatureWalkArgs" /> into a buffer
/// </summary>
public sealed record CreatureWalkSerializer : ServerPacketSerializer<CreatureWalkArgs>
{
    /// <inheritdoc />
    public override ServerOpCode ServerOpCode => ServerOpCode.CreatureWalk;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, CreatureWalkArgs args)
    {
        writer.WriteUInt32(args.SourceId);
        writer.WritePoint16(args.OldPoint);
        writer.WriteByte((byte)args.Direction);
        writer.WriteByte(0); //dunno
    }
}