using System.Collections.Generic;
using System.Linq;

namespace Karma.Quests;

public static class QuestModuleRegistry
{
    public static readonly RepairMissionModule Repair = new();
    public static readonly DeliveryQuestModule Delivery = new();
    public static readonly RumorQuestModule Rumor = new();
    public static readonly PosseQuestModule Posse = new();

    private static readonly Dictionary<string, QuestModule> _byStationRole;
    private static readonly List<QuestModule> _withCompletionPrefix;

    static QuestModuleRegistry()
    {
        QuestModule[] modules = [Repair, Delivery, Rumor, Posse];
        _byStationRole = new Dictionary<string, QuestModule>();
        _withCompletionPrefix = new List<QuestModule>();

        foreach (var module in modules)
        {
            foreach (var role in module.StationRoles)
                _byStationRole[role] = module;

            if (!string.IsNullOrEmpty(module.CompletionActionPrefix))
                _withCompletionPrefix.Add(module);
        }
    }

    public static QuestModule GetForStation(string stationRole) =>
        _byStationRole.TryGetValue(stationRole, out var m) ? m : null;

    public static QuestModule GetForCompletion(string completionActionId) =>
        _withCompletionPrefix.FirstOrDefault(m => completionActionId.StartsWith(m.CompletionActionPrefix));
}
