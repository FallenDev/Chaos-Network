using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class VersionConverter : PacketConverterBase<VersionArgs>
{
    public override byte OpCode => (byte)ClientOpCode.Version;

    public override VersionArgs Deserialize(ref SpanReader reader)
    {
        var version = reader.ReadUInt16();

        return new VersionArgs
        {
            Version = version
        };
    }
}