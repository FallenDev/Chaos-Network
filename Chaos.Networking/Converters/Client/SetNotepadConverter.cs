using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class SetNotepadConverter : PacketConverterBase<SetNotepadArgs>
{
    public override byte OpCode => (byte)ClientOpCode.SetNotepad;

    public override SetNotepadArgs Deserialize(ref SpanReader reader)
    {
        var slot = reader.ReadByte();
        var message = reader.ReadString16();

        return new SetNotepadArgs
        {
            Slot = slot,
            Message = message
        };
    }
}