using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class IgnoreConverter : PacketConverterBase<IgnoreArgs>
{
    public override byte OpCode => (byte)ClientOpCode.Ignore;

    public override IgnoreArgs Deserialize(ref SpanReader reader)
    {
        var ignoreType = reader.ReadByte();

        var args = new IgnoreArgs
        {
            IgnoreType = (IgnoreType)ignoreType
        };

        if (args.IgnoreType != IgnoreType.Request)
        {
            var targetName = reader.ReadString8();

            args.TargetName = targetName;
        }

        return args;
    }
}