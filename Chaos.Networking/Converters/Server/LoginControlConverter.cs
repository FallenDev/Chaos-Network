using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Server;

/// <summary>
///     Provides serialization and deserialization logic for <see cref="LoginControlArgs" />
/// </summary>
public sealed class LoginControlConverter : PacketConverterBase<LoginControlArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ServerOpCode.LoginControl;

    /// <inheritdoc />
    public override LoginControlArgs Deserialize(ref SpanReader reader) => null;

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, LoginControlArgs args)
    {
        writer.WriteByte((byte)args.LoginControlsType);
        writer.WriteString(args.Message);
    }
}