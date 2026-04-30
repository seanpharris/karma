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
    string PosseId = "",
    int KarmaPeak = 0,
    int KarmaFloor = 0,
    float SpeedModifier = 1f,
    string InsideStructureId = "");

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

public sealed record PlayerMatchSummary(
    string Id,
    string DisplayName,
    int FinalKarma,
    string Tier,
    int KarmaPeak,
    int KarmaFloor,
    int QuestsCompleted,
    int Kills);

public sealed record MatchSummarySnapshot(
    LeaderboardSnapshot Winners,
    IReadOnlyList<PlayerMatchSummary> Players);

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
        return PlayersFrom(players, standing, statusEffectsFor, _ => string.Empty);
    }

    public static IReadOnlyList<PlayerSnapshot> PlayersFrom(
        IEnumerable<PlayerState> players,
        LeaderboardStanding standing,
        Func<PlayerState, IReadOnlyList<string>> statusEffectsFor,
        Func<PlayerState, string> insideStructureFor)
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
                player.TeamId,
                player.Karma.KarmaPeak,
                player.Karma.KarmaFloor,
                CalculateSpeedModifier(player, standing),
                insideStructureFor(player)))
            .ToArray();
    }

    public static float CalculateSpeedModifier(PlayerState player, LeaderboardStanding standing)
    {
        if (!PerkCatalog.GetForPlayer(player, standing).Any(p => p.Id == PerkCatalog.WraithId))
        {
            return 1f;
        }

        var lowHp = player.MaxHealth > 0 &&
                    player.Health <= (int)(player.MaxHealth * PerkCatalog.WraithLowHpPercent);
        return lowHp ? PerkCatalog.WraithSpeedModifier : 1f;
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
