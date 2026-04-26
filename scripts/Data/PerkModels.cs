using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public enum PerkPath
{
    Ascension,
    Descension,
    Standing
}

public sealed record KarmaPerk(
    string Id,
    string Name,
    PerkPath Path,
    int RequiredMagnitude,
    string Description);

public static class PerkCatalog
{
    public const string TrustedDiscountId = "trusted_discount";
    public const string ShiftyPricesId = "shifty_prices";
    public const string CalmingPresenceId = "calming_presence";
    public const string BeaconAuraId = "beacon_aura";
    public const string RenegadeNerveId = "renegade_nerve";
    public const string DreadReputationId = "dread_reputation";

    private static readonly KarmaPerk[] AscensionPerks =
    {
        new(TrustedDiscountId, "Trusted Discount", PerkPath.Ascension, 10, "Helpful NPCs offer small discounts."),
        new(CalmingPresenceId, "Calming Presence", PerkPath.Ascension, 20, "Negative NPC reactions are softened."),
        new(BeaconAuraId, "Beacon Aura", PerkPath.Ascension, 35, "Nearby allies recover confidence faster, and your stamina recovers faster."),
        new("paragon_favor", "Paragon Favor", PerkPath.Ascension, 50, "Town allies may defend you."),
        new("exalted_grace", "Exalted Grace", PerkPath.Ascension, 100, "One severe social consequence can be softened.")
    };

    private static readonly KarmaPerk[] DescensionPerks =
    {
        new(ShiftyPricesId, "Shifty Prices", PerkPath.Descension, 10, "Shady traders offer better deals."),
        new("rumorcraft", "Rumorcraft", PerkPath.Descension, 20, "Rumors spread farther when you start them."),
        new(RenegadeNerveId, "Renegade Nerve", PerkPath.Descension, 35, "Intimidation attempts become more reliable, and sprinting costs less stamina."),
        new(DreadReputationId, "Dread Reputation", PerkPath.Descension, 50, "Fear softens negative NPC reactions to harmful, violent, or deceptive actions."),
        new("abyssal_mark", "Abyssal Mark", PerkPath.Descension, 100, "Criminal factions may protect you.")
    };

    private static readonly KarmaPerk SaintStanding = new(
        "standing_saint",
        "Saint",
        PerkPath.Standing,
        1,
        "Current highest-karma player on the server.");

    private static readonly KarmaPerk ScourgeStanding = new(
        "standing_scourge",
        "Scourge",
        PerkPath.Standing,
        1,
        "Current lowest-karma player on the server.");

    public static IReadOnlyList<KarmaPerk> GetForPlayer(
        PlayerState player,
        LeaderboardStanding standing)
    {
        var perks = new List<KarmaPerk>();
        var score = player.Karma.Score;

        if (score > 0)
        {
            perks.AddRange(AscensionPerks.Where(perk => score >= perk.RequiredMagnitude));
            AddInfiniteRankPerk(perks, player.Karma.Rank, PerkPath.Ascension);
        }
        else if (score < 0)
        {
            var magnitude = -score;
            perks.AddRange(DescensionPerks.Where(perk => magnitude >= perk.RequiredMagnitude));
            AddInfiniteRankPerk(perks, player.Karma.Rank, PerkPath.Descension);
        }

        if (standing.ParagonPlayerId == player.Id && score > 0)
        {
            perks.Add(SaintStanding);
        }

        if (standing.RenegadePlayerId == player.Id && score < 0)
        {
            perks.Add(ScourgeStanding);
        }

        return perks;
    }

    private static void AddInfiniteRankPerk(ICollection<KarmaPerk> perks, KarmaRank rank, PerkPath path)
    {
        if (rank.Rank <= 1 || rank.Name is not ("Exalted" or "Abyssal"))
        {
            return;
        }

        var idPrefix = path == PerkPath.Ascension ? "exalted_rank" : "abyssal_rank";
        var description = path == PerkPath.Ascension
            ? "Repeat ascension rank bonus from uncapped karma."
            : "Repeat descension rank bonus from uncapped karma.";

        perks.Add(new KarmaPerk(
            $"{idPrefix}_{rank.Rank}",
            rank.DisplayName,
            path,
            100 + ((rank.Rank - 1) * 100),
            description));
    }

    public static string Format(IReadOnlyList<KarmaPerk> perks)
    {
        return perks.Count == 0
            ? "Perks: none"
            : $"Perks: {string.Join(", ", perks.Select(perk => perk.Name))}";
    }
}
