using System;
using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public sealed class RelationshipLedger
{
    private readonly Dictionary<string, Dictionary<string, int>> _opinions = new();

    public IReadOnlyList<RelationshipSnapshot> Snapshot()
    {
        return _opinions
            .SelectMany(npc => npc.Value.Select(player => new RelationshipSnapshot(
                npc.Key,
                player.Key,
                player.Value)))
            .OrderBy(snapshot => snapshot.NpcId)
            .ThenBy(snapshot => snapshot.PlayerId)
            .ToArray();
    }

    public int GetOpinion(string npcId, string playerId)
    {
        return _opinions.TryGetValue(npcId, out var byPlayer) &&
               byPlayer.TryGetValue(playerId, out var opinion)
            ? opinion
            : 0;
    }

    public int Apply(string npcId, string playerId, int delta)
    {
        if (!_opinions.TryGetValue(npcId, out var byPlayer))
        {
            byPlayer = new Dictionary<string, int>();
            _opinions[npcId] = byPlayer;
        }

        var next = Math.Clamp(GetOpinion(npcId, playerId) + delta, -100, 100);
        byPlayer[playerId] = next;
        return next;
    }

    public void Clear() => _opinions.Clear();
}

public static class RelationshipRules
{
    private static readonly Dictionary<string, int> TagWeights = new()
    {
        ["helpful"] = 5,
        ["generous"] = 4,
        ["protective"] = 4,
        ["funny"] = 1,
        ["lawful"] = 1,
        ["harmful"] = -6,
        ["humiliating"] = -5,
        ["violent"] = -10,
        ["deceptive"] = -4,
        ["selfish"] = -4,
        ["betrayal"] = -12,
        ["forbidden"] = -8
    };

    public static bool TargetsNpc(KarmaAction action)
    {
        return action.TargetId.StartsWith("mara_") || action.TargetId.StartsWith("generated_npc_");
    }

    public static int CalculateDelta(KarmaAction action)
    {
        if (!TargetsNpc(action))
        {
            return 0;
        }

        var delta = action.Tags.Sum(tag => TagWeights.GetValueOrDefault(tag, 0));
        return Math.Clamp(delta, -25, 25);
    }

    public static string GetOpinionLabel(int opinion)
    {
        return opinion switch
        {
            >= 50 => "Devoted",
            >= 20 => "Friendly",
            > -20 => "Neutral",
            > -50 => "Wary",
            _ => "Hostile"
        };
    }
}
