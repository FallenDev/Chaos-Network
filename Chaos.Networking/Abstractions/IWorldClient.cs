using Chaos.Networking.Abstractions.Definitions;
using Chaos.Networking.Entities.Server;

namespace Chaos.Networking.Abstractions;

/// <summary>
///     Represents a client that is connected to the world server.
/// </summary>
public interface IWorldClient : IConnectedClient
{
    /// <summary>
    ///     Sends a packet to display an item in a pane.
    /// </summary>
    void SendAddItemToPane(AddItemToPaneArgs args);

    /// <summary>
    ///     Sends a packet to display a skill in a pane.
    /// </summary>
    void SendAddSkillToPane(AddSkillToPaneArgs args);

    /// <summary>
    ///     Sends a packet to display a spell in a pane.
    /// </summary>
    void SendAddSpellToPane(AddSpellToPaneArgs args);

    /// <summary>
    ///     Sends a packet to display an animation.
    /// </summary>
    void SendAnimation(AnimationArgs args);

    /// <summary>
    ///     Sends a packet to update the client's attributes.
    /// </summary>
    void SendAttributes(AttributesArgs args);

    /// <summary>
    ///     Sends a packet to animate an aisling's body.
    /// </summary>
    void SendBodyAnimation(BodyAnimationArgs args);

    /// <summary>
    ///     Sends a packet signaling the client to cancel casting.
    /// </summary>
    void SendCancelCasting();

    /// <summary>
    ///     Sends a packet to respond to a client's walk request.
    /// </summary>
    void SendClientWalkResponse(ClientWalkResponseArgs args);

    /// <summary>
    ///     Sends a packet to start the cooldown of a skill or spell.
    /// </summary>
    void SendCooldown(CooldownArgs args);

    /// <summary>
    ///     Sends a packet to turn a creature in a specific direction.
    /// </summary>
    void SendCreatureTurn(CreatureTurnArgs args);

    /// <summary>
    ///     Sends a packet to move a creature in a specific direction.
    /// </summary>
    void SendCreatureWalk(CreatureWalkArgs args);

    /// <summary>
    ///     Sends a packet to display an aisling.
    /// </summary>
    void SendDisplayAisling(DisplayAislingArgs args);

    /// <summary>
    ///     Sends a packet to display a board, list of boards, or a board response.
    /// </summary>
    void SendDisplayBoard(DisplayBoardArgs args);

    /// <summary>
    ///     Sends a packet to display a dialog.
    /// </summary>
    void SendDisplayDialog(DisplayDialogArgs args);

    /// <summary>
    ///     Sends a packet to display, close, or update an exchange.
    /// </summary>
    void SendDisplayExchange(DisplayExchangeArgs args);

    /// <summary>
    ///     Sends a packet to display a group invite.
    /// </summary>
    void SendDisplayGroupInvite(DisplayGroupInviteArgs args);

    /// <summary>
    ///     Sends a packet to display a public message.
    /// </summary>
    void SendDisplayPublicMessage(DisplayPublicMessageArgs args);

    /// <summary>
    ///     Sends a packet to unequip an item.
    /// </summary>
    void SendDisplayUnequip(DisplayUnequipArgs args);

    /// <summary>
    ///     Sends a packet to display visible entities to the client.
    /// </summary>
    void SendDisplayVisibleEntities(DisplayVisibleEntitiesArgs args);

    /// <summary>
    ///     Sends a packet to display doors.
    /// </summary>
    void SendDoors(DoorArgs args);

    /// <summary>
    ///     Sends a packet to request the editable profile from the client.
    /// </summary>
    void SendEditableProfileRequest();

    /// <summary>
    ///     Sends a packet to display an effect in the effect bar.
    /// </summary>
    void SendEffect(EffectArgs args);

    /// <summary>
    ///     Sends a packet to display an item in the equipment pane.
    /// </summary>
    void SendEquipment(EquipmentArgs args);

    /// <summary>
    ///     Sends a packet to respond to a client's exit request.
    /// </summary>
    void SendExitResponse(ExitResponseArgs args);

    /// <summary>
    ///     Sends a packet to force a client to send a packet to the server.
    /// </summary>
    void SendForceClientPacket(ForceClientPacketArgs args);

    /// <summary>
    ///     Sends a packet to display a health bar.
    /// </summary>
    void SendHealthBar(HealthBarArgs args);

    /// <summary>
    ///     Sends a packet to update the light level of the client.
    /// </summary>
    void SendLightLevel(LightLevelArgs args);

    /// <summary>
    ///     Sends a packet to update the client's location.
    /// </summary>
    void SendLocation(LocationArgs args);

    /// <summary>
    ///     Sends a packet to signal the client that a map change is complete.
    /// </summary>
    void SendMapChangeComplete();

    /// <summary>
    ///     Sends a packet to signal the client that a map change is pending.
    /// </summary>
    void SendMapChangePending();

    /// <summary>
    ///     Sends a packet to send map data to the client.
    /// </summary>
    void SendMapData(MapDataArgs args);

    /// <summary>
    ///     Sends a packet to send map information to the client.
    /// </summary>
    void SendMapInfo(MapInfoArgs args);

    /// <summary>
    ///     Sends a packet to signal the client that the map has finished loading.
    /// </summary>
    void SendMapLoadComplete();

    /// <summary>
    ///     Sends metadata to the client.
    /// </summary>
    void SendMetaData(MetaDataArgs args);

    /// <summary>
    ///     Sends a packet to display an editable notepad.
    /// </summary>
    void SendNotepad(NotepadArgs args);

    /// <summary>
    ///     Sends a packet to display another player's profile.
    /// </summary>
    void SendOtherProfile(OtherProfileArgs args);

    /// <summary>
    ///     Sends a packet to respond to a refresh request.
    /// </summary>
    void SendRefreshResponse();

    /// <summary>
    ///     Sends a packet to remove an entity from the client.
    /// </summary>
    void SendRemoveEntity(RemoveEntityArgs args);

    /// <summary>
    ///     Sends a packet to remove an item from a pane.
    /// </summary>
    void SendRemoveItemFromPane(RemoveItemFromPaneArgs args);

    /// <summary>
    ///     Sends a packet to remove a skill from a pane.
    /// </summary>
    void SendRemoveSkillFromPane(RemoveSkillFromPaneArgs args);

    /// <summary>
    ///     Sends a packet to remove a spell from a pane.
    /// </summary>
    void SendRemoveSpellFromPane(RemoveSpellFromPaneArgs args);

    /// <summary>
    ///     Sends a packet containing the client's profile.
    /// </summary>
    void SendSelfProfile(SelfProfileArgs args);

    /// <summary>
    ///     Sends a packet to display a server message.
    /// </summary>
    void SendServerMessage(ServerMessageArgs args);

    /// <summary>
    ///     Sends a packet to play a sound.
    /// </summary>
    void SendSound(SoundArgs args);

    /// <summary>
    ///     Sends a packet to give the client its user ID.
    /// </summary>
    void SendUserId(UserIdArgs args);

    /// <summary>
    ///     Sends a packet to display a world list to the client.
    /// </summary>
    void SendWorldList(WorldListArgs args);

    /// <summary>
    ///     Sends a packet to display a world map to the client.
    /// </summary>
    void SendWorldMap(WorldMapArgs args);
}
