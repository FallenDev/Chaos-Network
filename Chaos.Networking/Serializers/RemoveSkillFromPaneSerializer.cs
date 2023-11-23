using Chaos.IO.Memory;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Serializers;

/// <summary>
///     Serializes a <see cref="RemoveSkillFromPaneArgs" /> into a buffer
/// </summary>
public sealed record RemoveSkillFromPaneSerializer : ServerPacketSerializer<RemoveSkillFromPaneArgs>
{
    /// <inheritdoc />
    public override ServerOpCode ServerOpCode => ServerOpCode.RemoveSkillFromPane;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, RemoveSkillFromPaneArgs args) => writer.WriteByte(args.Slot);
}