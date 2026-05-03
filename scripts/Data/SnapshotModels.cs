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
    string InsideStructureId = "",
    int Stamina = 0,
    int MaxStamina = 0,
    int CurrentAmmo = 0,
    int MaxAmmo = 0,
    WeaponKind EquippedWeaponKind = WeaponKind.None,
    int MaxInventorySlots = 0,
    int Hunger = 0,
    int MaxHunger = 0,
    string PosseName = "",
    string PosseLeaderId = "",
    string LpcBundleId = "");

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

public sealed record PlayerMatchHighlights(
    int MostKarmaGained,
    int MostKarmaLost,
    int LongestSpree,
    int BountyClaimed,
    int RescuesPerformed);

public sealed record MatchSummarySnapshot(
    LeaderboardSnapshot Winners,
    IReadOnlyList<PlayerMatchSummary> Players,
    IReadOnlyDictionary<string, PlayerMatchHighlights> Highlights = null);

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
        return PlayersFrom(players, standing, statusEffectsFor, insideStructureFor, _ => null);
    }

    public static IReadOnlyList<PlayerSnapshot> PlayersFrom(
        IEnumerable<PlayerState> players,
        LeaderboardStanding standing,
        Func<PlayerState, IReadOnlyList<string>> statusEffectsFor,
        Func<PlayerState, string> insideStructureFor,
        Func<PlayerState, PosseInfo> posseInfoFor)
    {
        return players
            .OrderBy(player => player.Id)
            .Select(player =>
            {
                var posseInfo = posseInfoFor(player);
                return new PlayerSnapshot(
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
                    insideStructureFor(player),
                    player.Stamina,
                    player.MaxStamina,
                    player.CurrentAmmo,
                    player.MaxAmmo,
                    ResolveEquippedWeaponKind(player),
                    player.MaxInventorySlots,
                    player.Hunger,
                    player.MaxHunger,
                    posseInfo?.Name ?? string.Empty,
                    posseInfo?.LeaderId ?? string.Empty,
                    player.LpcBundleId);
            })
            .ToArray();
    }

    private static WeaponKind ResolveEquippedWeaponKind(PlayerState player)
    {
        return player.Equipment.TryGetValue(EquipmentSlot.MainHand, out var weapon)
            ? weapon.WeaponKind
            : WeaponKind.None;
    }

    public static float CalculateSpeedModifier(PlayerState player, LeaderboardStanding standing)
    {
        var modifier = 1f;
        if (PerkCatalog.GetForPlayer(player, standing).Any(p => p.Id == PerkCatalog.WraithId))
        {
            var lowHp = player.MaxHealth > 0 &&
                        player.Health <= (int)(player.MaxHealth * PerkCatalog.WraithLowHpPercent);
            if (lowHp)
                modifier *= PerkCatalog.WraithSpeedModifier;
        }

        if (player.Hunger <= 0)
            modifier *= 0.6f;
        else if (player.Hunger <= 25)
            modifier *= 0.85f;

        return modifier;
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
