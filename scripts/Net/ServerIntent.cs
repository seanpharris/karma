using System.Collections.Generic;
using Karma.Data;

namespace Karma.Net;

public enum IntentType
{
    Move,
    Interact,
    RequestDuel,
    AcceptDuel,
    Attack,
    UseItem,
    TransferItem,
    PlaceObject,
    StartDialogue,
    SelectDialogueChoice,
    StartQuest,
    CompleteQuest,
    StartEntanglement,
    ExposeEntanglement,
    KarmaAction,
    KarmaBreak
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

public sealed record PlayerInterest(
    string PlayerId,
    IReadOnlyCollection<string> VisiblePlayerIds,
    IReadOnlyCollection<string> VisibleEntityIds,
    IReadOnlyCollection<string> VisibleNpcIds);

public sealed record WorldItemEntity(
    string EntityId,
    GameItem Item,
    TilePosition Position,
    bool IsAvailable);

public sealed record WorldItemSnapshot(
    string EntityId,
    string ItemId,
    string Name,
    ItemCategory Category,
    int TileX,
    int TileY);

public sealed record NpcEntity(
    NpcProfile Profile,
    TilePosition Position);

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
    IReadOnlyList<WorldItemSnapshot> WorldItems,
    LeaderboardSnapshot Leaderboard,
    IReadOnlyList<Duel> Duels,
    IReadOnlyList<ServerEvent> ServerEvents,
    IReadOnlyList<WorldEvent> WorldEvents)
{
    public string Summary =>
        $"{Players.Count} visible players, {Npcs.Count} visible NPCs, {Dialogues.Count} dialogues, {Quests.Count} quests, {WorldItems.Count} visible items, {Duels.Count} duels, {ServerEvents.Count} server events, {WorldEvents.Count} world events";
}
