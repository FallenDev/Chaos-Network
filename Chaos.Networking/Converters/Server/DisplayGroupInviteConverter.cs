using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="DisplayGroupInviteArgs" />
/// </summary>
public sealed class DisplayGroupInviteConverter : PacketConverterBase<DisplayGroupInviteArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.DisplayGroupInvite;

    /// <inheritdoc />
    public override DisplayGroupInviteArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, DisplayGroupInviteArgs args)
    {
        writer.WriteByte((byte)args.ServerGroupSwitch);
        writer.WriteString(args.SourceName);

        if (args.ServerGroupSwitch == ServerGroupSwitch.ShowGroupBox)
        {
            writer.WriteString(args.GroupBoxInfo!.Name);
            writer.WriteString(args.GroupBoxInfo.Note);
            writer.WriteByte(args.GroupBoxInfo.MinLevel);
            writer.WriteByte(args.GroupBoxInfo.MaxLevel);

            writer.WriteByte(args.GroupBoxInfo.MaxWarriors);
            writer.WriteByte(args.GroupBoxInfo.CurrentWarriors);

            writer.WriteByte(args.GroupBoxInfo.MaxWizards);
            writer.WriteByte(args.GroupBoxInfo.CurrentWizards);

            writer.WriteByte(args.GroupBoxInfo.MaxRogues);
            writer.WriteByte(args.GroupBoxInfo.CurrentRogues);

            writer.WriteByte(args.GroupBoxInfo.MaxPriests);
            writer.WriteByte(args.GroupBoxInfo.CurrentPriests);

            writer.WriteByte(args.GroupBoxInfo.MaxMonks);
            writer.WriteByte(args.GroupBoxInfo.CurrentMonks);

            writer.WriteByte(0);
        }
    }
}