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

public enum QuestStepConditionKind
{
    None,
    HoldItem,
    HoldRepairTool,
    NearNpc,
    NearStructureCategory
}

public sealed record QuestStepCondition(
    QuestStepConditionKind Kind,
    string TargetId = "")
{
    public static readonly QuestStepCondition None = new(QuestStepConditionKind.None);
}

public sealed record QuestStep(
    string Id,
    string Description,
    QuestStepCondition Condition,
    IReadOnlyCollection<string> KarmaTags,
    int ScripReward = 0);

public sealed record QuestDefinition(
    string Id,
    string Title,
    string GiverNpcId,
    string Description,
    IReadOnlyCollection<string> RequiredItemIds,
    string CompletionActionId,
    int ScripReward = 0,
    IReadOnlyList<QuestStep> Steps = null);

public sealed class QuestState
{
    public QuestState(QuestDefinition definition)
    {
        Definition = definition;
    }

    public QuestDefinition Definition { get; }
    public QuestStatus Status { get; private set; } = QuestStatus.Available;
    public int CurrentStepIndex { get; private set; } = 0;

    public bool IsMultiStep => Definition.Steps is { Count: > 0 };
    public bool AllStepsDone => !IsMultiStep || CurrentStepIndex >= Definition.Steps.Count;
    public QuestStep CurrentStep => IsMultiStep && CurrentStepIndex < Definition.Steps.Count
        ? Definition.Steps[CurrentStepIndex]
        : null;

    public void Start()
    {
        if (Status == QuestStatus.Available)
        {
            Status = QuestStatus.Active;
        }
    }

    public bool AdvanceStep()
    {
        if (Status != QuestStatus.Active || !IsMultiStep)
            return false;
        if (CurrentStepIndex >= Definition.Steps.Count)
            return false;
        CurrentStepIndex++;
        return true;
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
            .Select(quest => new QuestSnapshot(
                quest.Definition.Id,
                quest.Status,
                quest.Definition.ScripReward,
                quest.CurrentStepIndex,
                quest.Definition.Steps?.Count ?? 0,
                quest.CurrentStep?.Description ?? ""))
            .ToArray();
    }

    private static string FormatQuest(QuestState quest)
    {
        var required = quest.Definition.RequiredItemIds.Count == 0
            ? "no items"
            : string.Join("+", quest.Definition.RequiredItemIds.Select(id => StarterItems.GetById(id).Name));
        var stepSuffix = quest.IsMultiStep ? $" step {quest.CurrentStepIndex}/{quest.Definition.Steps.Count}" : "";
        return $"{quest.Definition.Title}: {quest.Status}{stepSuffix} [{required}]";
    }
}

public static class RepairMissionQuests
{
    public static QuestDefinition Create(
        string id,
        string title,
        string giverNpcId,
        string locationId,
        string structureRole,
        int scripReward)
    {
        var steps = new[]
        {
            new QuestStep(
                $"{id}_locate",
                $"Find the damaged {structureRole} fixture.",
                new QuestStepCondition(QuestStepConditionKind.NearStructureCategory, structureRole),
                new[] { "helpful" },
                ScripReward: 0),
            new QuestStep(
                $"{id}_equip",
                "Acquire a repair tool (multi-tool or welding torch).",
                new QuestStepCondition(QuestStepConditionKind.HoldRepairTool),
                new[] { "helpful", "lawful" },
                ScripReward: 2),
            new QuestStep(
                $"{id}_fix",
                $"Return to the {structureRole} fixture with the tool and repair it.",
                new QuestStepCondition(QuestStepConditionKind.NearStructureCategory, structureRole),
                new[] { "helpful", "generous" },
                ScripReward: 3)
        };

        return new QuestDefinition(
            id,
            title,
            giverNpcId,
            $"Help restore the {structureRole} infrastructure. Locate the damaged fixture, get a repair tool, fix it, and report back.",
            System.Array.Empty<string>(),
            $"generated_station_help:{locationId}",
            scripReward,
            steps);
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
