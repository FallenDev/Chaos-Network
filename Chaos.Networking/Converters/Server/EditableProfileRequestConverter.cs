using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

public class EditableProfileRequestConverter : PacketConverterBase<EditableProfileRequestArgs>
{
    public override byte OpCode => (byte)ServerOpCode.EditableProfileRequest;

    public override void Serialize(ref SpanWriter writer, EditableProfileRequestArgs args)
        => writer.WriteBytes(
            3,
            0,
            0,
            0,
            0,
            0);
}