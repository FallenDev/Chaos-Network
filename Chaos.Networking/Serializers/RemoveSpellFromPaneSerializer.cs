using Chaos.IO.Memory;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Serializers;

/// <summary>
///     Serializes a <see cref="RemoveSpellFromPaneArgs" /> into a buffer
/// </summary>
public sealed record RemoveSpellFromPaneSerializer : ServerPacketSerializer<RemoveSpellFromPaneArgs>
{
    /// <inheritdoc />
    public override ServerOpCode ServerOpCode => ServerOpCode.RemoveSpellFromPane;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, RemoveSpellFromPaneArgs args) => writer.WriteByte(args.Slot);
}