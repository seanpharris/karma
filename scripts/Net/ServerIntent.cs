using System.Collections.Generic;
using Karma.Data;

namespace Karma.Net;

public enum PlayerStatusKind
{
    Poisoned,
    Burning,
    Chilled,
    Silenced
}

public enum IntentType
{
    Move,
    Interact,
    RequestDuel,
    AcceptDuel,
    Attack,
    UseItem,
    PurchaseItem,
    TransferItem,
    TransferCurrency,
    PlaceObject,
    StartDialogue,
    SelectDialogueChoice,
    StartQuest,
    CompleteQuest,
    AdvanceQuestStep,
    StartEntanglement,
    ExposeEntanglement,
    SetAppearance,
    SendLocalChat,
    KarmaAction,
    KarmaBreak,
    InvitePosse,
    AcceptPosse,
    LeavePosse,
    SendPosseChat,
    Rescue,
    Mount,
    Dismount,
    IssueWanted,
    ReadyUp,
    ClaimStation
}

public sealed record ServerIntent(
    string PlayerId,
    int Sequence,
    IntentType Type,
    IReadOnlyDictionary<string, string> Payload);

public sealed record ServerEvent(
    string EventId,
    string WorldId,
    long Tick,
    string Description,
    IReadOnlyDictionary<string, string> Data);

public sealed record LocalChatMessageSnapshot(
    string MessageId,
    long Tick,
    string SpeakerId,
    string SpeakerName,
    string Text,
    int SpeakerTileX,
    int SpeakerTileY,
    int DistanceTiles,
    float Volume,
    string Channel = "local");

public sealed record PlayerInterest(
    string PlayerId,
    IReadOnlyCollection<string> VisiblePlayerIds,
    IReadOnlyCollection<string> VisibleEntityIds,
    IReadOnlyCollection<string> VisibleStructureIds,
    IReadOnlyCollection<string> VisibleNpcIds);

public sealed record WorldItemEntity(
    string EntityId,
    GameItem Item,
    TilePosition Position,
    bool IsAvailable,
    string DropOwnerId = "",
    string DropOwnerName = "");

public sealed record WorldItemSnapshot(
    string EntityId,
    string ItemId,
    string Name,
    ItemCategory Category,
    int TileX,
    int TileY,
    string DropOwnerId = "",
    string DropOwnerName = "");

public sealed record WorldStructureEntity(
    string EntityId,
    string StructureId,
    string Name,
    string Category,
    TilePosition Position,
    bool IsVisible,
    bool IsInteractable,
    string InteractionPrompt,
    string InteractionResult,
    int Integrity = 100,
    string FactionId = StarterFactions.CivicRepairGuildId,
    string LocationId = "",
    string ClaimingPosseId = "");

public sealed record WorldStructureSnapshot(
    string EntityId,
    string StructureId,
    string Name,
    string Category,
    int TileX,
    int TileY,
    int WidthPx,
    int HeightPx,
    bool IsInteractable,
    string InteractionPrompt,
    int Integrity,
    string Condition,
    string ClaimingPosseId = "");

public sealed record ShopOfferSnapshot(
    string OfferId,
    string VendorNpcId,
    string ItemId,
    string ItemName,
    ItemCategory Category,
    int Price,
    string Currency,
    string RequiredFactionId = "",
    int MinReputation = 0);

public sealed record MapTileSnapshot(
    int TileX,
    int TileY,
    string FloorId,
    string StructureId,
    string ZoneId);

public sealed record MapChunkSnapshot(
    string ChunkKey,
    int Revision,
    int ChunkX,
    int ChunkY,
    int Left,
    int Top,
    int Width,
    int Height,
    IReadOnlyList<MapTileSnapshot> Tiles);

public sealed record NpcEntity(
    NpcProfile Profile,
    TilePosition Position,
    string LocationId = "",
    IReadOnlyList<TilePosition>? PatrolWaypoints = null,
    int PatrolIndex = 0);

public sealed record MountEntity(
    string EntityId,
    string Name,
    TilePosition Position,
    float SpeedModifier,
    bool IsParked,
    string OccupantPlayerId = "");

public sealed record MountSnapshot(
    string EntityId,
    string Name,
    int TileX,
    int TileY,
    float SpeedModifier,
    bool IsParked,
    string OccupantPlayerId);

public sealed record NpcSnapshot(
    string Id,
    string Name,
    string Role,
    string Faction,
    int TileX,
    int TileY);

public sealed record NpcDialogueChoice(
    string Id,
    string Label,
    string ActionId,
    string RequiredItemId = "");

public sealed record NpcDialogueSnapshot(
    string NpcId,
    string NpcName,
    string Prompt,
    IReadOnlyList<NpcDialogueChoice> Choices);

public sealed record InterestSnapshotSyncHint(
    long AfterTick,
    bool IsDelta,
    int ServerEventCount,
    int WorldEventCount,
    int VisibleMapChunkCount,
    int VisibleMapRevision);

public sealed record ClientInterestSnapshot(
    string WorldId,
    long Tick,
    string PlayerId,
    int InterestRadiusTiles,
    PlayerInterest Interest,
    IReadOnlyList<PlayerSnapshot> Players,
    IReadOnlyList<NpcSnapshot> Npcs,
    IReadOnlyList<NpcDialogueSnapshot> Dialogues,
    IReadOnlyList<QuestSnapshot> Quests,
    IReadOnlyList<MapChunkSnapshot> MapChunks,
    IReadOnlyList<WorldItemSnapshot> WorldItems,
    IReadOnlyList<WorldStructureSnapshot> Structures,
    IReadOnlyList<ShopOfferSnapshot> ShopOffers,
    IReadOnlyList<LocalChatMessageSnapshot> LocalChatMessages,
    LeaderboardSnapshot Leaderboard,
    MatchSnapshot Match,
    IReadOnlyList<Duel> Duels,
    InterestSnapshotSyncHint SyncHint,
    IReadOnlyList<ServerEvent> ServerEvents,
    IReadOnlyList<WorldEvent> WorldEvents,
    IReadOnlyList<MountSnapshot> Mounts,
    MatchSummarySnapshot MatchSummary = null)
{
    public string Summary =>
        $"{Players.Count} visible players, {Npcs.Count} visible NPCs, {Dialogues.Count} dialogues, {Quests.Count} quests, {MapChunks.Count} map chunks, {WorldItems.Count} visible items, {Structures.Count} visible structures, {ShopOffers.Count} shop offers, {LocalChatMessages.Count} local chat messages, {Duels.Count} duels, {Match.Summary}, {SyncHint.ServerEventCount} server events, {SyncHint.WorldEventCount} world events, {Mounts.Count} mounts";
}
