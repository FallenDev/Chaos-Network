using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

/// <summary>
///     Provides packet serialization and deserialization logic for <see cref="PasswordChangeArgs" />
/// </summary>
public sealed class PasswordChangeConverter : PacketConverterBase<PasswordChangeArgs>
{
    /// <inheritdoc />
    public override byte OpCode => (byte)ClientOpCode.PasswordChange;

    /// <inheritdoc />
    public override PasswordChangeArgs Deserialize(ref SpanReader reader)
    {
        var name = reader.ReadString();
        var currentPw = reader.ReadString();
        var newPw = reader.ReadString();

        return new PasswordChangeArgs
        {
            Name = name,
            CurrentPassword = currentPw,
            NewPassword = newPw
        };
    }

    /// <inheritdoc />
    public override void Serialize(ref SpanWriter writer, PasswordChangeArgs args)
    {
        writer.WriteString(args.Name);
        writer.WriteString(args.CurrentPassword);
        writer.WriteString(args.NewPassword);
    }
}