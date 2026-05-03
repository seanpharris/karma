using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public enum EntanglementType
{
    Romantic,
    Blackmail,
    Debt,
    Rivalry
}

public enum EntanglementStatus
{
    Secret,
    Exposed,
    Resolved
}

public sealed record Entanglement(
    string Id,
    string PlayerId,
    string NpcId,
    string AffectedNpcId,
    EntanglementType Type,
    EntanglementStatus Status,
    string Summary);

public sealed class EntanglementLedger
{
    private readonly List<Entanglement> _entanglements = new();

    public IReadOnlyList<Entanglement> All => _entanglements;

    public Entanglement Add(
        string playerId,
        string npcId,
        string affectedNpcId,
        EntanglementType type,
        string summary)
    {
        var entanglement = new Entanglement(
            $"entanglement_{_entanglements.Count + 1}",
            playerId,
            npcId,
            affectedNpcId,
            type,
            EntanglementStatus.Secret,
            summary);
        _entanglements.Add(entanglement);
        return entanglement;
    }

    public bool HasActive(string playerId, string npcId, EntanglementType type)
    {
        return _entanglements.Any(entanglement =>
            entanglement.PlayerId == playerId &&
            entanglement.NpcId == npcId &&
            entanglement.Type == type &&
            entanglement.Status != EntanglementStatus.Resolved);
    }

    public Entanglement Get(string entanglementId)
    {
        return _entanglements.First(entanglement => entanglement.Id == entanglementId);
    }

    public bool TryGetActive(
        string playerId,
        string npcId,
        EntanglementType type,
        out Entanglement entanglement)
    {
        entanglement = _entanglements.FirstOrDefault(candidate =>
            candidate.PlayerId == playerId &&
            candidate.NpcId == npcId &&
            candidate.Type == type &&
            candidate.Status != EntanglementStatus.Resolved);
        return entanglement is not null;
    }

    public bool Expose(string entanglementId)
    {
        return ChangeStatus(entanglementId, EntanglementStatus.Exposed);
    }

    public bool Resolve(string entanglementId)
    {
        return ChangeStatus(entanglementId, EntanglementStatus.Resolved);
    }

    public void Clear() => _entanglements.Clear();

    public string FormatSummary()
    {
        return _entanglements.Count == 0
            ? "Entanglements: none"
            : $"Entanglements: {_entanglements.Count(entanglement => entanglement.Status != EntanglementStatus.Resolved)} active";
    }

    private bool ChangeStatus(string entanglementId, EntanglementStatus status)
    {
        var index = _entanglements.FindIndex(entanglement => entanglement.Id == entanglementId);
        if (index < 0 || _entanglements[index].Status == EntanglementStatus.Resolved)
        {
            return false;
        }

        _entanglements[index] = _entanglements[index] with { Status = status };
        return true;
    }
}
