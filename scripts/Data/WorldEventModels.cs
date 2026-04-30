using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public enum WorldEventType
{
    Rumor,
    Quest,
    Combat,
    Karma,
    Structure,
    System,
    SupplyDrop
}

public sealed record WorldEvent(
    string Id,
    WorldEventType Type,
    string Summary,
    string SourcePlayerId,
    string TargetId)
{
    public const string GlobalTargetId = "*";

    public bool IsGlobal => TargetId == GlobalTargetId;
}

public sealed class WorldEventLog
{
    private readonly List<WorldEvent> _events = new();

    public IReadOnlyList<WorldEvent> Events => _events;

    public WorldEvent Add(
        WorldEventType type,
        string summary,
        string sourcePlayerId,
        string targetId)
    {
        var worldEvent = new WorldEvent(
            $"world_event_{_events.Count + 1}",
            type,
            summary,
            sourcePlayerId,
            targetId);
        _events.Add(worldEvent);
        return worldEvent;
    }

    public string FormatLatestRumor()
    {
        var rumor = _events.LastOrDefault(worldEvent => worldEvent.Type == WorldEventType.Rumor);
        return rumor is null ? "Rumors: quiet" : $"Rumor: {rumor.Summary}";
    }

    public string FormatLatestSummary()
    {
        var latest = _events.LastOrDefault();
        return latest is null ? "World Events: none" : $"{latest.Type}: {latest.Summary}";
    }
}
