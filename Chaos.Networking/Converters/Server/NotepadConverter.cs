using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public sealed class NotepadConverter : PacketConverterBase<NotepadArgs>
{
    public override byte OpCode => (byte)ServerOpCode.Notepad;

    public override void Serialize(ref SpanWriter writer, NotepadArgs args)
    {
        writer.WriteByte(args.Slot);
        writer.WriteByte((byte)args.NotepadType);
        writer.WriteByte(args.Height);
        writer.WriteByte(args.Width);
        writer.WriteString16(args.Message);
    }
}