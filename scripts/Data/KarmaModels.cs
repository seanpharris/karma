using System;
using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public enum KarmaDirection
{
    Neutral,
    Ascend,
    Descend
}

public sealed class PlayerKarma
{
    public int Score { get; private set; }
    public int KarmaPeak { get; private set; }
    public int KarmaFloor { get; private set; }

    public KarmaDirection Path => Score switch
    {
        > 0 => KarmaDirection.Ascend,
        < 0 => KarmaDirection.Descend,
        _ => KarmaDirection.Neutral
    };

    public string TierName => KarmaTiers.GetTierName(Score);
    public KarmaRank Rank => KarmaTiers.GetRank(Score);
    public KarmaRankProgress RankProgress => KarmaTiers.GetRankProgress(Score);

    public void Apply(int amount)
    {
        Score += amount;
        if (Score > KarmaPeak) KarmaPeak = Score;
        if (Score < KarmaFloor) KarmaFloor = Score;
    }

    public void Reset()
    {
        Score = 0;
        // KarmaPeak and KarmaFloor are match-scoped — they survive Karma Break
    }
}

public sealed record KarmaAction(
    string ActorId,
    string TargetId,
    IReadOnlyCollection<string> Tags,
    string Context,
    int BaseMagnitude = 1);

public sealed record KarmaShift(int Amount, KarmaDirection Direction, string Reason);

public sealed record KarmaRank(string Name, int Rank)
{
    public string DisplayName => Rank <= 1 ? Name : $"{Name} {Rank}";
}

public sealed record KarmaRankProgress(
    string CurrentRankName,
    string NextRankName,
    int CurrentMagnitude,
    int CurrentRankStart,
    int NextRankAt)
{
    public int Progress => Math.Max(0, CurrentMagnitude - CurrentRankStart);
    public int RankSize => Math.Max(0, NextRankAt - CurrentRankStart);
    public int Remaining => Math.Max(0, NextRankAt - CurrentMagnitude);

    public string Summary => RankSize == 0
        ? "Progress: awaiting a path"
        : $"Progress: {Progress}/{RankSize} toward {NextRankName}";
}

public static class KarmaRules
{
    private static readonly Dictionary<string, int> TagWeights = new()
    {
        ["helpful"] = 4,
        ["generous"] = 3,
        ["protective"] = 5,
        ["lawful"] = 1,
        ["funny"] = 1,
        ["harmful"] = -4,
        ["humiliating"] = -2,
        ["violent"] = -6,
        ["deceptive"] = -3,
        ["selfish"] = -3,
        ["romantic"] = 0,
        ["betrayal"] = -8,
        ["chaotic"] = -1,
        ["forbidden"] = -5
    };

    public static KarmaShift CalculateShift(KarmaAction action)
    {
        var weighted = action.Tags.Sum(tag => TagWeights.GetValueOrDefault(tag, 0));
        var amount = Math.Clamp(weighted * Math.Max(1, action.BaseMagnitude), -20, 20);
        var direction = amount switch
        {
            > 0 => KarmaDirection.Ascend,
            < 0 => KarmaDirection.Descend,
            _ => KarmaDirection.Neutral
        };

        return new KarmaShift(amount, direction, action.Context);
    }
}

public static class KarmaTiers
{
    private static readonly (int Threshold, string Name)[] Positive =
    {
        (100, "Paragon"),
        (75, "Luminary"),
        (50, "Exalted"),
        (35, "Beacon"),
        (20, "Advocate"),
        (10, "Trusted")
    };

    private static readonly (int Threshold, string Name)[] Negative =
    {
        (-100, "Renegade"),
        (-75, "Wraith"),
        (-50, "Dread"),
        (-35, "Abyssal"),
        (-20, "Outlaw"),
        (-10, "Shifty")
    };

    public static string GetTierName(int score)
    {
        return GetRank(score).DisplayName;
    }

    public static KarmaRank GetRank(int score)
    {
        if (score > 0)
        {
            var tier = Positive.FirstOrDefault(candidate => score >= candidate.Threshold);
            if (string.IsNullOrWhiteSpace(tier.Name))
            {
                return new KarmaRank("Unmarked", 0);
            }

            return tier.Name == "Paragon"
                ? new KarmaRank(tier.Name, GetInfiniteRank(score))
                : new KarmaRank(tier.Name, 1);
        }

        if (score < 0)
        {
            var tier = Negative.FirstOrDefault(candidate => score <= candidate.Threshold);
            if (string.IsNullOrWhiteSpace(tier.Name))
            {
                return new KarmaRank("Unmarked", 0);
            }

            return tier.Name == "Renegade"
                ? new KarmaRank(tier.Name, GetInfiniteRank(-score))
                : new KarmaRank(tier.Name, 1);
        }

        return new KarmaRank("Unmarked", 0);
    }

    public static KarmaRankProgress GetRankProgress(int score)
    {
        if (score >= 100)
        {
            return GetInfiniteProgress(score, "Paragon");
        }

        if (score <= -100)
        {
            return GetInfiniteProgress(-score, "Renegade");
        }

        if (score > 0)
        {
            return GetFiniteProgress(score, GetRank(score).DisplayName, Positive.OrderBy(candidate => candidate.Threshold).ToArray());
        }

        if (score < 0)
        {
            return GetFiniteProgress(-score, GetRank(score).DisplayName, Negative.Select(candidate => (-candidate.Threshold, candidate.Name))
                .OrderBy(candidate => candidate.Item1)
                .ToArray());
        }

        return new KarmaRankProgress("Unmarked", "Trusted", 0, 0, 10);
    }

    private static int GetInfiniteRank(int magnitude)
    {
        return Math.Max(1, ((magnitude - 100) / 100) + 1);
    }

    private static KarmaRankProgress GetInfiniteProgress(int magnitude, string rankName)
    {
        var currentRank = GetInfiniteRank(magnitude);
        var currentRankStart = 100 + ((currentRank - 1) * 100);
        var nextRankAt = currentRankStart + 100;
        var nextRank = new KarmaRank(rankName, currentRank + 1);

        return new KarmaRankProgress(
            new KarmaRank(rankName, currentRank).DisplayName,
            nextRank.DisplayName,
            magnitude,
            currentRankStart,
            nextRankAt);
    }

    private static KarmaRankProgress GetFiniteProgress(
        int magnitude,
        string currentRankName,
        IReadOnlyList<(int Threshold, string Name)> tiers)
    {
        var previousThreshold = tiers.LastOrDefault(candidate => magnitude >= candidate.Threshold).Threshold;
        var nextTier = tiers.FirstOrDefault(candidate => magnitude < candidate.Threshold);

        if (string.IsNullOrWhiteSpace(nextTier.Name))
        {
            return GetInfiniteProgress(magnitude, currentRankName);
        }

        return new KarmaRankProgress(
            currentRankName,
            nextTier.Name,
            magnitude,
            previousThreshold,
            nextTier.Threshold);
    }
}
