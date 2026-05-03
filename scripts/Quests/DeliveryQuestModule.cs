using System.Collections.Generic;
using Karma.Data;

namespace Karma.Quests;

public sealed class DeliveryQuestModule : QuestModule
{
    private const string SourceRole = "market";
    private const string DestinationRole = "clinic";
    private const string DeliveryItemId = StarterItems.FilterCoreId;

    public override IReadOnlyList<string> StationRoles { get; } = [SourceRole];

    public override QuestDefinition CreateQuest(QuestCreationContext ctx)
    {
        var itemName = StarterItems.GetById(DeliveryItemId).Name;
        var steps = new[]
        {
            new QuestStep(
                $"{ctx.QuestId}_collect",
                $"Go to the {SourceRole} to pick up the {itemName}.",
                new QuestStepCondition(QuestStepConditionKind.NearStructureCategory, SourceRole),
                new[] { "helpful" },
                ScripReward: 0),
            new QuestStep(
                $"{ctx.QuestId}_hold",
                $"Acquire the {itemName}.",
                new QuestStepCondition(QuestStepConditionKind.HoldItem, DeliveryItemId),
                new[] { "helpful" },
                ScripReward: 1),
            new QuestStep(
                $"{ctx.QuestId}_deliver",
                $"Deliver the {itemName} to the {DestinationRole}.",
                new QuestStepCondition(QuestStepConditionKind.NearStructureCategory, DestinationRole),
                new[] { "helpful", "generous" },
                ScripReward: 3)
        };

        return new QuestDefinition(
            ctx.QuestId,
            ctx.LocationName,
            ctx.GiverNpcId,
            $"Collect {itemName} from the {SourceRole} and deliver it to the {DestinationRole}.",
            new[] { DeliveryItemId },
            $"generated_station_help:{ctx.LocationId}",
            ctx.ScripReward,
            steps);
    }
}
