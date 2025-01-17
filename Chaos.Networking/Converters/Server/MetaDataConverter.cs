using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="MetaDataArgs" />
/// </summary>
public sealed class MetaDataConverter : PacketConverterBase<MetaDataArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.MetaData;

    /// <inheritdoc />
    public override MetaDataArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, MetaDataArgs args)
    {
        writer.WriteByte((byte)args.MetaDataRequestType);

        switch (args.MetaDataRequestType)
        {
            case MetaDataRequestType.DataByName:
                writer.WriteString(args.MetaDataInfo!.Name);
                writer.WriteUInt32(args.MetaDataInfo!.CheckSum);
                writer.WriteData(args.MetaDataInfo!.Data);

                break;
            case MetaDataRequestType.AllCheckSums:
                writer.WriteUInt16((byte)args.MetaDataCollection!.Count);

                foreach (var info in args.MetaDataCollection!)
                {
                    writer.WriteString(info.Name);
                    writer.WriteUInt32(info.CheckSum);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(args.MetaDataRequestType), args.MetaDataRequestType, "Unknown enum value");
        }
    }
}