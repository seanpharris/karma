using System.Collections.Generic;
using System.Linq;
using System;

namespace Karma.Data;

public sealed record GameSnapshot(
    IReadOnlyList<PlayerSnapshot> Players,
    LeaderboardSnapshot Leaderboard,
    IReadOnlyList<string> InventoryItemIds,
    IReadOnlyList<QuestSnapshot> Quests,
    IReadOnlyList<RelationshipSnapshot> Relationships,
    IReadOnlyList<FactionSnapshot> Factions,
    IReadOnlyList<Entanglement> Entanglements,
    IReadOnlyList<Duel> Duels,
    IReadOnlyList<WorldEvent> WorldEvents)
{
    public string Summary =>
        $"{Players.Count} players, {InventoryItemIds.Count} inventory items, " +
        $"{Quests.Count} quests, {Factions.Count} faction standings, {Duels.Count} duels, {WorldEvents.Count} world events";
}

public sealed record PlayerSnapshot(
    string Id,
    string DisplayName,
    int Karma,
    string Tier,
    int KarmaRank,
    string KarmaProgress,
    LeaderboardRole Standing,
    int TileX,
    int TileY,
    int Health,
    int MaxHealth,
    int Scrip,
    PlayerAppearanceSelection Appearance,
    IReadOnlyList<string> InventoryItemIds,
    IReadOnlyDictionary<EquipmentSlot, string> EquipmentItemIds,
    IReadOnlyList<string> StatusEffects,
    string PosseId = "");

public sealed record LeaderboardSnapshot(
    string SaintPlayerId,
    string SaintName,
    int SaintScore,
    string ScourgePlayerId,
    string ScourgeName,
    int ScourgeScore);

public sealed record QuestSnapshot(
    string Id,
    QuestStatus Status,
    int ScripReward = 0,
    int CurrentStep = 0,
    int TotalSteps = 0,
    string CurrentStepDescription = "");

public sealed record RelationshipSnapshot(
    string NpcId,
    string PlayerId,
    int Opinion);

public static class SnapshotBuilder
{
    public static IReadOnlyList<PlayerSnapshot> PlayersFrom(
        IReadOnlyDictionary<string, PlayerState> players,
        LeaderboardStanding standing)
    {
        return PlayersFrom(players.Values, standing);
    }

    public static IReadOnlyList<PlayerSnapshot> PlayersFrom(
        IEnumerable<PlayerState> players,
        LeaderboardStanding standing)
    {
        return PlayersFrom(players, standing, _ => System.Array.Empty<string>());
    }

    public static IReadOnlyList<PlayerSnapshot> PlayersFrom(
        IEnumerable<PlayerState> players,
        LeaderboardStanding standing,
        Func<PlayerState, IReadOnlyList<string>> statusEffectsFor)
    {
        return players
            .OrderBy(player => player.Id)
            .Select(player => new PlayerSnapshot(
                player.Id,
                player.DisplayName,
                player.Karma.Score,
                player.Karma.TierName,
                player.Karma.Rank.Rank,
                player.Karma.RankProgress.Summary,
                standing.GetRole(player.Id, player.Karma.Score),
                player.Position.X,
                player.Position.Y,
                player.Health,
                player.MaxHealth,
                player.Scrip,
                player.Appearance,
                player.Inventory.Select(item => item.Id).ToArray(),
                player.Equipment.ToDictionary(pair => pair.Key, pair => pair.Value.Id),
                statusEffectsFor(player),
                player.TeamId))
            .ToArray();
    }

    public static LeaderboardSnapshot LeaderboardFrom(LeaderboardStanding standing)
    {
        return new LeaderboardSnapshot(
            standing.SaintPlayerId,
            standing.SaintName,
            standing.ParagonScore,
            standing.ScourgePlayerId,
            standing.ScourgeName,
            standing.RenegadeScore);
    }
}
