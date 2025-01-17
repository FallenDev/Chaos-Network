using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="LoginNoticeArgs" />
/// </summary>
public sealed class LoginNoticeConverter : PacketConverterBase<LoginNoticeArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.LoginNotice;

    /// <inheritdoc />
    public override LoginNoticeArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, LoginNoticeArgs args)
    {
        writer.WriteBoolean(args.IsFullResponse);

        if (args.IsFullResponse)
            writer.WriteData(args.Data!);
        else
            writer.WriteUInt32(args.CheckSum!.Value);
    }
}