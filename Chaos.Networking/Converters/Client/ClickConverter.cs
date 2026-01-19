using Chaos.DarkAges.Definitions;
using Chaos.Geometry;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class ClickConverter : PacketConverterBase<ClickArgs>
{
    public override byte OpCode => (byte)ClientOpCode.Click;

    public override ClickArgs Deserialize(ref SpanReader reader)
    {
        var clickType = reader.ReadByte();

        var args = new ClickArgs
        {
            ClickType = (ClickType)clickType
        };

        switch (args.ClickType)
        {
            case ClickType.TargetId:
                args.TargetId = reader.ReadUInt32();
                break;
            case ClickType.TargetPoint:
                args.TargetPoint = (Point)reader.ReadPoint16();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(clickType), clickType, "Unknown enum value");
        }

        return args;
    }
}