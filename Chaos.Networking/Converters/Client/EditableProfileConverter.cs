using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class EditableProfileConverter : PacketConverterBase<EditableProfileArgs>
{
    public override byte OpCode => (byte)ClientOpCode.EditableProfile;

    public override EditableProfileArgs Deserialize(ref SpanReader reader)
    {
        var totalLength = reader.ReadUInt16();

        if (totalLength == 0)
            return new EditableProfileArgs
            {
                PortraitData = [],
                ProfileMessage = string.Empty
            };

        var portraitData = reader.ReadData16();
        var profileMessage = reader.ReadString16();

        return new EditableProfileArgs
        {
            PortraitData = portraitData,
            ProfileMessage = profileMessage
        };
    }
}