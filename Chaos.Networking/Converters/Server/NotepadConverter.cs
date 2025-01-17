using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="NotepadArgs" />
/// </summary>
public sealed class NotepadConverter : PacketConverterBase<NotepadArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.Notepad;

    /// <inheritdoc />
    public override NotepadArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, NotepadArgs args)
    {
        writer.WriteByte(args.Slot);
        writer.WriteByte((byte)args.NotepadType);
        writer.WriteByte(args.Height);
        writer.WriteByte(args.Width);
        writer.WriteString(args.Message);
    }
}