using Chaos.DarkAges.Definitions;
using Chaos.IO.Memory;
using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Client;
using Chaos.Packets.Abstractions;

namespace Chaos.Networking.Converters.Client;

public sealed class GroupInviteConverter : PacketConverterBase<GroupInviteArgs>
{
    public override byte OpCode => (byte)ClientOpCode.GroupInvite;

    public override GroupInviteArgs Deserialize(ref SpanReader reader)
    {
        var groupRequestType = (ClientGroupSwitch)reader.ReadByte();
        var targetName = reader.ReadString8();
        var groupBoxInfo = default(CreateGroupBoxInfo);

        if (groupRequestType == ClientGroupSwitch.CreateGroupbox)
        {
            var name = reader.ReadString8();
            var note = reader.ReadString8();
            var minLevel = reader.ReadByte();
            var maxLevel = reader.ReadByte();
            var maxWarriors = reader.ReadByte();
            var maxWizards = reader.ReadByte();
            var maxRogues = reader.ReadByte();
            var maxPriests = reader.ReadByte();
            var maxMonks = reader.ReadByte();

            groupBoxInfo = new CreateGroupBoxInfo
            {
                Name = name,
                Note = note,
                MinLevel = minLevel,
                MaxLevel = maxLevel,
                MaxWarriors = maxWarriors,
                MaxWizards = maxWizards,
                MaxRogues = maxRogues,
                MaxPriests = maxPriests,
                MaxMonks = maxMonks
            };
        }

        var args = new GroupInviteArgs
        {
            ClientGroupSwitch = groupRequestType,
            TargetName = targetName,
            GroupBoxInfo = groupBoxInfo
        };

        return args;
    }
}