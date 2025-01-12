using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

using Chaos.Networking.Entities.Server;
using Chaos.Packets.Abstractions;
using Microsoft.Extensions.Logging;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Represents a client connected to the world server
/// </summary>
public abstract class WorldClientBase : ConnectedClientBase, IWorldClient
{
    /// <inheritdoc />
    protected WorldClientBase(
        Socket socket,
        IPacketSerializer packetSerializer,
        ILogger<ConnectedClientBase> logger)
        : base(
            socket,
            packetSerializer,
            logger) { }

    /// <inheritdoc />
    public virtual void SendAddItemToPane(AddItemToPaneArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendAddSkillToPane(AddSkillToPaneArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendAddSpellToPane(AddSpellToPaneArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendAnimation(AnimationArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendAttributes(AttributesArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendBodyAnimation(BodyAnimationArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendCancelCasting() => Send(new CancelCastingArgs());

    /// <inheritdoc />
    public virtual void SendClientWalkResponse(ClientWalkResponseArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendCooldown(CooldownArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendCreatureTurn(CreatureTurnArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendCreatureWalk(CreatureWalkArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendDisplayAisling(DisplayAislingArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendDisplayBoard(DisplayBoardArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendDisplayDialog(DisplayDialogArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendDisplayExchange(DisplayExchangeArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendDisplayGroupInvite(DisplayGroupInviteArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendDisplayPublicMessage(DisplayPublicMessageArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendDisplayUnequip(DisplayUnequipArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendDisplayVisibleEntities(DisplayVisibleEntitiesArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendDoors(DoorArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendEditableProfileRequest() => Send(new EditableProfileRequestArgs());

    /// <inheritdoc />
    public virtual void SendEffect(EffectArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendEquipment(EquipmentArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendExitResponse(ExitResponseArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendForceClientPacket(ForceClientPacketArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendHealthBar(HealthBarArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendLightLevel(LightLevelArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendLocation(LocationArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendMapChangeComplete() => Send(new MapChangeCompleteArgs());

    /// <inheritdoc />
    public virtual void SendMapChangePending() => Send(new MapChangePendingArgs());

    /// <inheritdoc />
    public virtual void SendMapData(MapDataArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendMapInfo(MapInfoArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendMapLoadComplete() => Send(new MapLoadCompleteArgs());

    /// <inheritdoc />
    public virtual void SendMetaData(MetaDataArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendNotepad(NotepadArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendOtherProfile(OtherProfileArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendRefreshResponse() => Send(new RefreshResponseArgs());

    /// <inheritdoc />
    public virtual void SendRemoveEntity(RemoveEntityArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendRemoveItemFromPane(RemoveItemFromPaneArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendRemoveSkillFromPane(RemoveSkillFromPaneArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendRemoveSpellFromPane(RemoveSpellFromPaneArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendSelfProfile(SelfProfileArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendServerMessage(ServerMessageArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendSound(SoundArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendUserId(UserIdArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendWorldList(WorldListArgs args) => Send(args);

    /// <inheritdoc />
    public virtual void SendWorldMap(WorldMapArgs args) => Send(args);
}
