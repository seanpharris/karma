using System.Collections.Generic;
using Karma.Data;

namespace Karma.Quests;

public sealed class RepairMissionModule : QuestModule
{
    public override IReadOnlyList<string> StationRoles { get; } = ["workshop", "clinic"];

    public override QuestDefinition CreateQuest(QuestCreationContext ctx)
    {
        var role = ctx.LocationRole;
        var steps = new[]
        {
            new QuestStep(
                $"{ctx.QuestId}_locate",
                $"Find the damaged {role} fixture.",
                new QuestStepCondition(QuestStepConditionKind.NearStructureCategory, role),
                new[] { "helpful" },
                ScripReward: 0),
            new QuestStep(
                $"{ctx.QuestId}_equip",
                "Acquire a repair tool (multi-tool or welding torch).",
                new QuestStepCondition(QuestStepConditionKind.HoldRepairTool),
                new[] { "helpful", "lawful" },
                ScripReward: 2),
            new QuestStep(
                $"{ctx.QuestId}_fix",
                $"Return to the {role} fixture with the tool and repair it.",
                new QuestStepCondition(QuestStepConditionKind.NearStructureCategory, role),
                new[] { "helpful", "generous" },
                ScripReward: 3)
        };

        return new QuestDefinition(
            ctx.QuestId,
            ctx.LocationName,
            ctx.GiverNpcId,
            $"Help restore the {role} infrastructure. Locate the damaged fixture, get a repair tool, fix it, and report back.",
            System.Array.Empty<string>(),
            $"generated_station_help:{ctx.LocationId}",
            ctx.ScripReward,
            steps);
    }
}
