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

    // Wipe all quest progress and re-seed the ledger from the given
    // definitions. Used by GameState.ResetForNewMatch so a fresh round
    // doesn't inherit completed/active quests from the previous one.
    public void Reset(IEnumerable<QuestDefinition> definitions)
    {
        _quests.Clear();
        foreach (var definition in definitions)
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


public static class StarterQuests
{
    public const string MaraClinicFiltersId = "mara_clinic_filters";
    public const string GarrickBladeOrderId = "garrick_blade_order";
    public const string MeriCellarStockId = "meri_cellar_stock";
    public const string CaldenChapelTitheId = "calden_chapel_tithe";
    public const string WaceBarracksWatchId = "wace_barracks_watch";
    public const string YsoltHerbalRemedyId = "ysolt_herbal_remedy";

    public static readonly QuestDefinition MaraClinicFilters = new(
        MaraClinicFiltersId,
        "Clinic Filters",
        StarterNpcs.Mara.Id,
        "Mara needs help repairing filters before the clinic air gets dramatic.",
        new[] { StarterItems.RepairKitId },
        Core.PrototypeActions.HelpMaraId,
        ScripReward: 12);

    public static readonly QuestDefinition GarrickBladeOrder = new(
        GarrickBladeOrderId,
        "Blade for the Watch",
        "blacksmith_garrick",
        "Garrick can finish a watch blade if someone brings serviceable tools and scrap.",
        new[] { StarterItems.MultiToolId, StarterItems.BoltCuttersId },
        "deliver_garrick_blade_parts",
        ScripReward: 15);

    public static readonly QuestDefinition MeriCellarStock = new(
        MeriCellarStockId,
        "Cellar Stock",
        "tavernkeep_meri",
        "Meri needs travel rations and a flask to keep the tavern fed through market day.",
        new[] { StarterItems.RationPackId, StarterItems.ChemInjectorId },
        "deliver_meri_cellar_stock",
        ScripReward: 10);

    public static readonly QuestDefinition CaldenChapelTithe = new(
        CaldenChapelTitheId,
        "Chapel Tithe",
        "priest_calden",
        "Father Calden asks for a token of peace and clean supplies for the almshouse.",
        new[] { StarterItems.ApologyFlowerId, StarterItems.RepairKitId },
        "deliver_calden_tithe",
        ScripReward: 12);

    public static readonly QuestDefinition WaceBarracksWatch = new(
        WaceBarracksWatchId,
        "Barracks Watch",
        "captain_wace",
        "Captain Wace wants a torch and sturdy vest before assigning the evening patrol.",
        new[] { StarterItems.FlashlightId, StarterItems.WorkVestId },
        "deliver_wace_watch_kit",
        ScripReward: 14);

    public static readonly QuestDefinition YsoltHerbalRemedy = new(
        YsoltHerbalRemedyId,
        "Herbal Remedy",
        "herbalist_ysolt",
        "Ysolt can brew a remedy for the sick if the right tincture reaches her hut.",
        new[] { StarterItems.MediPatchId, StarterItems.RationPackId },
        "deliver_ysolt_remedy",
        ScripReward: 13);

    public static IReadOnlyList<QuestDefinition> All { get; } = new[]
    {
        MaraClinicFilters,
        GarrickBladeOrder,
        MeriCellarStock,
        CaldenChapelTithe,
        WaceBarracksWatch,
        YsoltHerbalRemedy
    };
}
