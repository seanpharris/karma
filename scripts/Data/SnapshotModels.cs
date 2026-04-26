using System.Collections.Generic;
using System.Linq;

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
    IReadOnlyList<string> InventoryItemIds,
    IReadOnlyDictionary<EquipmentSlot, string> EquipmentItemIds);

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
    int ScripReward = 0);

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
                player.Inventory.Select(item => item.Id).ToArray(),
                player.Equipment.ToDictionary(pair => pair.Key, pair => pair.Value.Id)))
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
