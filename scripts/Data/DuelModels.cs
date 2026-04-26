using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public enum DuelStatus
{
    Requested,
    Active,
    Ended
}

public sealed record Duel(
    string Id,
    string ChallengerId,
    string TargetId,
    DuelStatus Status);

public sealed class DuelLedger
{
    private readonly List<Duel> _duels = new();

    public IReadOnlyList<Duel> All => _duels;

    public Duel Request(string challengerId, string targetId)
    {
        var existing = _duels.FirstOrDefault(duel =>
            duel.Status != DuelStatus.Ended &&
            IsSamePair(duel, challengerId, targetId));
        if (existing is not null)
        {
            return existing;
        }

        var duel = new Duel($"duel_{_duels.Count + 1}", challengerId, targetId, DuelStatus.Requested);
        _duels.Add(duel);
        return duel;
    }

    public bool Accept(string playerId, string challengerId, out Duel duel)
    {
        var index = _duels.FindIndex(candidate =>
            candidate.ChallengerId == challengerId &&
            candidate.TargetId == playerId &&
            candidate.Status == DuelStatus.Requested);
        if (index < 0)
        {
            duel = null;
            return false;
        }

        duel = _duels[index] with { Status = DuelStatus.Active };
        _duels[index] = duel;
        return true;
    }

    public bool IsActive(string playerAId, string playerBId)
    {
        return _duels.Any(duel => duel.Status == DuelStatus.Active && IsSamePair(duel, playerAId, playerBId));
    }

    public void EndForPlayer(string playerId)
    {
        for (var i = 0; i < _duels.Count; i++)
        {
            var duel = _duels[i];
            if (duel.Status != DuelStatus.Ended &&
                (duel.ChallengerId == playerId || duel.TargetId == playerId))
            {
                _duels[i] = duel with { Status = DuelStatus.Ended };
            }
        }
    }

    public string FormatSummary()
    {
        var activeCount = _duels.Count(duel => duel.Status == DuelStatus.Active);
        var requestedCount = _duels.Count(duel => duel.Status == DuelStatus.Requested);
        return activeCount == 0 && requestedCount == 0
            ? "Duels: none"
            : $"Duels: {activeCount} active, {requestedCount} requested";
    }

    private static bool IsSamePair(Duel duel, string playerAId, string playerBId)
    {
        return (duel.ChallengerId == playerAId && duel.TargetId == playerBId) ||
               (duel.ChallengerId == playerBId && duel.TargetId == playerAId);
    }
}
