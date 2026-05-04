using System.Collections.Generic;
using System.Linq;
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
    TransferPosseLeadership,
    SendPosseChat,
    Rescue,
    Mount,
    Dismount,
    MountBagTransfer,
    IssueWanted,
    ReadyUp,
    ClaimStation,
    CraftItem,
    SellItem,
    Reload,
    RepairItem,
    StartPosseQuest
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
    IReadOnlyDictionary<string, string> Data,
    IReadOnlyList<string> Witnesses = null);

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
    string DropOwnerName = "",
    long DropOwnerExpiresTick = 0);

public sealed record WorldItemSnapshot(
    string EntityId,
    string ItemId,
    string Name,
    ItemCategory Category,
    int TileX,
    int TileY,
    string DropOwnerId = "",
    string DropOwnerName = "",
    long DropOwnerExpiresTick = 0);

public sealed record BuildingInterior(
    int MinX,
    int MinY,
    int Width,
    int Height,
    IReadOnlyList<TilePosition> DoorTiles)
{
    public bool Contains(TilePosition tile) =>
        tile.X >= MinX && tile.X < MinX + Width &&
        tile.Y >= MinY && tile.Y < MinY + Height;

    public bool IsDoor(TilePosition tile) =>
        DoorTiles.Any(d => d.X == tile.X && d.Y == tile.Y);
}

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
    string ClaimingPosseId = "",
    BuildingInterior Interior = null);

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
    string ClaimingPosseId = "",
    int InteriorMinX = 0,
    int InteriorMinY = 0,
    int InteriorWidth = 0,
    int InteriorHeight = 0);

public sealed record ShopOfferSnapshot(
    string OfferId,
    string VendorNpcId,
    string ItemId,
    string ItemName,
    ItemCategory Category,
    int Price,
    string Currency,
    string RequiredFactionId = "",
    int MinReputation = 0,
    int BasePrice = 0,
    string PricingBreakdown = "");

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
    int PatrolIndex = 0,
    string ResidentStructureEntityId = "",
    string LpcBundleId = "");

public sealed record MountEntity(
    string EntityId,
    string Name,
    TilePosition Position,
    float SpeedModifier,
    bool IsParked,
    string OccupantPlayerId = "",
    IReadOnlyList<string> BagItemIds = null);

public sealed record MountSnapshot(
    string EntityId,
    string Name,
    int TileX,
    int TileY,
    float SpeedModifier,
    bool IsParked,
    string OccupantPlayerId,
    IReadOnlyList<string> BagItemIds = null);

public sealed record NpcSnapshot(
    string Id,
    string Name,
    string Role,
    string Faction,
    int TileX,
    int TileY,
    string LpcBundleId = "");

public sealed record NpcDialogueChoice(
    string Id,
    string Label,
    string ActionId,
    string RequiredItemId = "",
    string ResponseLine = "");

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
    IReadOnlyList<FactionSnapshot> Factions,
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
        $"{Players.Count} visible players, {Npcs.Count} visible NPCs, {Dialogues.Count} dialogues, {Quests.Count} quests, {MapChunks.Count} map chunks, {WorldItems.Count} visible items, {Structures.Count} visible structures, {ShopOffers.Count} shop offers, {LocalChatMessages.Count} local chat messages, {Factions.Count} faction standings, {Duels.Count} duels, {Match.Summary}, {SyncHint.ServerEventCount} server events, {SyncHint.WorldEventCount} world events, {Mounts.Count} mounts";
}
