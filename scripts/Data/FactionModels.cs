using System;
using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public sealed record FactionProfile(
    string Id,
    string Name,
    string Description);

public sealed class FactionLedger
{
    private readonly Dictionary<string, Dictionary<string, int>> _reputation = new();

    public int GetReputation(string factionId, string playerId)
    {
        return _reputation.TryGetValue(factionId, out var byPlayer) &&
               byPlayer.TryGetValue(playerId, out var reputation)
            ? reputation
            : 0;
    }

    public int Apply(string factionId, string playerId, int delta)
    {
        if (!_reputation.TryGetValue(factionId, out var byPlayer))
        {
            byPlayer = new Dictionary<string, int>();
            _reputation[factionId] = byPlayer;
        }

        var next = Math.Clamp(GetReputation(factionId, playerId) + delta, -100, 100);
        byPlayer[playerId] = next;
        return next;
    }

    public void Decay()
    {
        foreach (var byPlayer in _reputation.Values)
        {
            foreach (var playerId in byPlayer.Keys.ToArray())
            {
                var current = byPlayer[playerId];
                if (current > 0) byPlayer[playerId]--;
                else if (current < 0) byPlayer[playerId]++;
            }
        }
    }

    public IReadOnlyList<FactionSnapshot> Snapshot()
    {
        return _reputation
            .SelectMany(faction => faction.Value.Select(player => new FactionSnapshot(
                faction.Key,
                player.Key,
                player.Value)))
            .OrderBy(snapshot => snapshot.FactionId)
            .ThenBy(snapshot => snapshot.PlayerId)
            .ToArray();
    }
}

public sealed record FactionSnapshot(
    string FactionId,
    string PlayerId,
    int Reputation);

public static class StarterFactions
{
    public const string FreeSettlersId = "free_settlers";
    public const string CivicRepairGuildId = "civic_repair_guild";
    public const string BackroomMerchantsId = "backroom_merchants";

    public static IReadOnlyList<FactionProfile> All { get; } = new[]
    {
        new FactionProfile(FreeSettlersId, "Free Settlers", "Neighbors trying to keep the town livable."),
        new FactionProfile(CivicRepairGuildId, "Civic Repair Guild", "Fixers, tinkerers, and people with opinions about bolts."),
        new FactionProfile(BackroomMerchantsId, "Backroom Merchants", "Useful traders with selective memories.")
    };

    public static string ToId(string factionName)
    {
        return factionName.Trim().ToLowerInvariant().Replace(" ", "_");
    }
}
