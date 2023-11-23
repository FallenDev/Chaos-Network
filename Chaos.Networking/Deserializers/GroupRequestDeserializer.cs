using Chaos.Common.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;
using Chaos.Packets.Abstractions.Definitions;

namespace Chaos.Networking.Deserializers;

/// <summary>
///     Deserializes a buffer into <see cref="GroupRequestArgs" />
/// </summary>
public sealed record GroupRequestDeserializer : ClientPacketDeserializer<GroupRequestArgs>
{
    /// <inheritdoc />
    public override ClientOpCode ClientOpCode => ClientOpCode.GroupRequest;

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "UnusedVariable")]
    public override GroupRequestArgs Deserialize(ref SpanReader reader)
    {
        var groupRequestType = (GroupRequestType)reader.ReadByte();

        if (groupRequestType == GroupRequestType.Groupbox)
        {
            var leader = reader.ReadString8();
            var test = reader.ReadString8();
            reader.ReadByte(); //unknown
            var minLevel = reader.ReadByte();
            var maxLevel = reader.ReadByte();
            var maxOfClass = new byte[6];

            maxOfClass[(byte)BaseClass.Berserker | (byte)BaseClass.Defender] = reader.ReadByte();
            maxOfClass[(byte)BaseClass.Arcanus] = reader.ReadByte();
            maxOfClass[(byte)BaseClass.Assassin] = reader.ReadByte();
            maxOfClass[(byte)BaseClass.Cleric] = reader.ReadByte();
            maxOfClass[(byte)BaseClass.Monk] = reader.ReadByte();
        }

        var targetName = reader.ReadString8();

        return new GroupRequestArgs(groupRequestType, targetName);
    }
}
