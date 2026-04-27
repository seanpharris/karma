using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public enum QuestStatus
{
    Available,
    Active,
    Completed,
    Failed
}

public sealed record QuestDefinition(
    string Id,
    string Title,
    string GiverNpcId,
    string Description,
    IReadOnlyCollection<string> RequiredItemIds,
    string CompletionActionId,
    int ScripReward = 0);

public sealed class QuestState
{
    public QuestState(QuestDefinition definition)
    {
        Definition = definition;
    }

    public QuestDefinition Definition { get; }
    public QuestStatus Status { get; private set; } = QuestStatus.Available;

    public void Start()
    {
        if (Status == QuestStatus.Available)
        {
            Status = QuestStatus.Active;
        }
    }

    public void Complete()
    {
        if (Status is QuestStatus.Available or QuestStatus.Active)
        {
            Status = QuestStatus.Completed;
        }
    }

    public void Fail()
    {
        if (Status != QuestStatus.Completed)
        {
            Status = QuestStatus.Failed;
        }
    }
}

public sealed class QuestLedger
{
    private readonly Dictionary<string, QuestState> _quests;

    public QuestLedger(IEnumerable<QuestDefinition> definitions)
    {
        _quests = definitions.ToDictionary(
            definition => definition.Id,
            definition => new QuestState(definition));
    }

    public IReadOnlyDictionary<string, QuestState> Quests => _quests;

    public QuestState Get(string questId)
    {
        return _quests[questId];
    }

    public void AddDefinition(QuestDefinition definition)
    {
        if (!_quests.ContainsKey(definition.Id))
        {
            _quests[definition.Id] = new QuestState(definition);
        }
    }

    public string FormatActiveSummary()
    {
        var active = _quests.Values
            .Where(quest => quest.Status is QuestStatus.Available or QuestStatus.Active)
            .Select(FormatQuest);

        var text = string.Join(", ", active);
        return string.IsNullOrWhiteSpace(text) ? "Quests: none" : $"Quests: {text}";
    }

    public IReadOnlyList<QuestSnapshot> Snapshot()
    {
        return _quests.Values
            .OrderBy(quest => quest.Definition.Id)
            .Select(quest => new QuestSnapshot(quest.Definition.Id, quest.Status, quest.Definition.ScripReward))
            .ToArray();
    }

    private static string FormatQuest(QuestState quest)
    {
        var required = quest.Definition.RequiredItemIds.Count == 0
            ? "no items"
            : string.Join("+", quest.Definition.RequiredItemIds.Select(id => StarterItems.GetById(id).Name));
        return $"{quest.Definition.Title}: {quest.Status} [{required}]";
    }
}

public static class StarterQuests
{
    public const string MaraClinicFiltersId = "mara_clinic_filters";

    public static readonly QuestDefinition MaraClinicFilters = new(
        MaraClinicFiltersId,
        "Clinic Filters",
        StarterNpcs.Mara.Id,
        "Mara needs help repairing filters before the clinic air gets dramatic.",
        new[] { StarterItems.RepairKitId },
        Core.PrototypeActions.HelpMaraId,
        ScripReward: 12);

    public static IReadOnlyList<QuestDefinition> All { get; } = new[]
    {
        MaraClinicFilters
    };
}
