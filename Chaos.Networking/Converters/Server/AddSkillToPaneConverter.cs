using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="AddSkillToPaneArgs" />
/// </summary>
public sealed class AddSkillToPaneConverter : PacketConverterBase<AddSkillToPaneArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.AddSkillToPane;

    /// <inheritdoc />
    public override AddSkillToPaneArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, AddSkillToPaneArgs args)
    {
        writer.WriteByte(args.Skill.Slot);
        writer.WriteUInt16(args.Skill.Sprite);
        writer.WriteString(args.Skill.PanelName);
    }
}