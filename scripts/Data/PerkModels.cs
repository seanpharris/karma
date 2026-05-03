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
    public const string ExaltedFavorId = "exalted_favor";
    public const string RumorcraftId = "rumorcraft";
    public const string AbyssalNerveId = "abyssal_nerve";
    public const string DreadReputationId = "dread_reputation";
    public const string RenegadeMarkId = "renegade_mark";
    public const string WardenId = "warden";
    public const int WardenThreshold = 150;
    public const string WraithId = "wraith_surge";
    public const int WraithThreshold = 150;
    public const float WraithSpeedModifier = 1.5f;
    public const float WraithLowHpPercent = 0.3f;

    private static readonly KarmaPerk[] AscensionPerks =
    {
        new(TrustedDiscountId, "Trusted Discount", PerkPath.Ascension, 10, "Helpful NPCs offer small discounts."),
        new(CalmingPresenceId, "Calming Presence", PerkPath.Ascension, 20, "Negative NPC reactions are softened."),
        new(BeaconAuraId, "Beacon Aura", PerkPath.Ascension, 35, "Nearby allies recover confidence faster, and your stamina recovers faster."),
        new("exalted_favor", "Exalted Favor", PerkPath.Ascension, 50, "Town allies may defend you."),
        new("paragon_grace", "Paragon Grace", PerkPath.Ascension, 100, "One severe social consequence can be softened."),
        new(WardenId, "Warden", PerkPath.Ascension, WardenThreshold, "Issue Wanted warrants on players who have committed crimes. Others earn karma for bringing them down.")
    };

    private static readonly KarmaPerk[] DescensionPerks =
    {
        new(ShiftyPricesId, "Shifty Prices", PerkPath.Descension, 10, "Shady traders offer better deals."),
        new(RumorcraftId, "Rumorcraft", PerkPath.Descension, 20, "Rumors spread globally when you expose them."),
        new(AbyssalNerveId, "Abyssal Nerve", PerkPath.Descension, 35, "Intimidation attempts become more reliable, and sprinting costs less stamina."),
        new(DreadReputationId, "Dread Reputation", PerkPath.Descension, 50, "Fear softens negative NPC reactions to harmful, violent, or deceptive actions."),
        new("renegade_mark", "Renegade Mark", PerkPath.Descension, 100, "Criminal factions may protect you."),
        new(WraithId, "Wraith Surge", PerkPath.Descension, WraithThreshold, "At ≤ 30% HP, gain a 50% speed boost that makes you nearly impossible to pin down.")
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
        if (rank.Rank <= 1 || rank.Name is not ("Paragon" or "Renegade"))
        {
            return;
        }

        var idPrefix = path == PerkPath.Ascension ? "paragon_rank" : "renegade_rank";
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
