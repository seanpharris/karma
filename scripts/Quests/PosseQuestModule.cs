using System.Collections.Generic;
using Karma.Data;

namespace Karma.Quests;

public sealed class PosseQuestModule : QuestModule
{
    public const string PosseStationRole = "posse_outpost";
    public const string PosseCompletionPrefix = "posse_complete_";

    public override IReadOnlyList<string> StationRoles => new[] { PosseStationRole };
    public override string CompletionActionPrefix => PosseCompletionPrefix;

    public override QuestDefinition CreateQuest(QuestCreationContext context)
    {
        var steps = new List<QuestStep>
        {
            new(
                $"{context.QuestId}_step_1",
                "Rally posse members at the outpost.",
                new QuestStepCondition(QuestStepConditionKind.NearStructureCategory, "outpost"),
                new[] { "cooperative", "loyal" }),
            new(
                $"{context.QuestId}_step_2",
                "Deliver supplies together to seal the deal.",
                new QuestStepCondition(QuestStepConditionKind.HoldItem, StarterItems.RepairKitId),
                new[] { "cooperative", "helpful" })
        };

        return new QuestDefinition(
            context.QuestId,
            "Posse Run",
            context.GiverNpcId,
            "Lead your posse on a coordinated supply run.",
            new[] { StarterItems.RepairKitId },
            $"{PosseCompletionPrefix}{context.QuestId}",
            context.ScripReward,
            steps);
    }
}
