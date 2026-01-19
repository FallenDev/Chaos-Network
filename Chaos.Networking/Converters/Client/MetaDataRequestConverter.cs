using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class MetaDataRequestConverter : PacketConverterBase<MetaDataRequestArgs>
{
    public override byte OpCode => (byte)ClientOpCode.MetaDataRequest;

    public override MetaDataRequestArgs Deserialize(ref SpanReader reader)
    {
        var metadataRequestType = reader.ReadByte();

        var args = new MetaDataRequestArgs
        {
            MetaDataRequestType = (MetaDataRequestType)metadataRequestType
        };

        switch (args.MetaDataRequestType)
        {
            case MetaDataRequestType.DataByName:
            {
                var name = reader.ReadString8();

                args.Name = name;

                break;
            }
            case MetaDataRequestType.AllCheckSums:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return args;
    }
}