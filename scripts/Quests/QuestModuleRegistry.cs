using System.Collections.Generic;
using System.Linq;

namespace Karma.Quests;

/// <summary>
/// Quest module registry. Built-in modules (Repair, Delivery, Rumor, Posse)
/// are registered automatically at type-init. Additional modules can be
/// registered at runtime via <see cref="Register"/> — the registry rebuilds
/// its station-role and completion-prefix indices on each registration so
/// new quest types become discoverable without recompiling the registry.
/// </summary>
public static class QuestModuleRegistry
{
    public static readonly RepairMissionModule Repair = new();
    public static readonly DeliveryQuestModule Delivery = new();
    public static readonly RumorQuestModule Rumor = new();
    public static readonly PosseQuestModule Posse = new();

    private static readonly List<QuestModule> _modules = new();
    private static readonly Dictionary<string, QuestModule> _byStationRole = new();
    private static readonly List<QuestModule> _withCompletionPrefix = new();

    static QuestModuleRegistry()
    {
        Register(Repair);
        Register(Delivery);
        Register(Rumor);
        Register(Posse);
    }

    /// <summary>
    /// Add a module to the registry. Idempotent — re-registering the same
    /// instance is a no-op. Replaces any existing module that claims the
    /// same station role.
    /// </summary>
    public static void Register(QuestModule module)
    {
        if (module is null) return;
        if (_modules.Contains(module)) return;
        _modules.Add(module);
        foreach (var role in module.StationRoles)
            _byStationRole[role] = module;
        if (!string.IsNullOrEmpty(module.CompletionActionPrefix))
            _withCompletionPrefix.Add(module);
    }

    /// <summary>Read-only view of every registered module.</summary>
    public static IReadOnlyList<QuestModule> All => _modules;

    public static QuestModule GetForStation(string stationRole) =>
        _byStationRole.TryGetValue(stationRole, out var m) ? m : null;

    public static QuestModule GetForCompletion(string completionActionId) =>
        _withCompletionPrefix.FirstOrDefault(m => completionActionId.StartsWith(m.CompletionActionPrefix));
}
