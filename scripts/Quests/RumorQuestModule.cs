using System.Collections.Generic;
using Karma.Data;
using Karma.Npc;

namespace Karma.Quests;

public sealed class RumorQuestModule : QuestModule
{
    public override IReadOnlyList<string> StationRoles { get; } = ["notice-board"];
    public override string CompletionActionPrefix => "rumor_resolve:";

    public override QuestDefinition CreateQuest(QuestCreationContext ctx)
    {
        var targetNpcId = ctx.OtherPlacements.Count > 0
            ? ctx.OtherPlacements[0].NpcId
            : StarterNpcs.Mara.Id;

        var steps = new[]
        {
            new QuestStep(
                $"{ctx.QuestId}_discover",
                "Visit the notice-board to read the details of the rumor.",
                new QuestStepCondition(QuestStepConditionKind.NearStructureCategory, "notice-board"),
                new[] { "curious" },
                ScripReward: 0),
            new QuestStep(
                $"{ctx.QuestId}_confront",
                "Find the person named in the rumor.",
                new QuestStepCondition(QuestStepConditionKind.NearNpc, targetNpcId),
                new[] { "curious" },
                ScripReward: 1)
        };

        return new QuestDefinition(
            ctx.QuestId,
            ctx.LocationName,
            ctx.GiverNpcId,
            $"A rumor is circulating about someone at {ctx.LocationName}. Return to the notice-board clerk and decide: expose the secret or keep it buried.",
            System.Array.Empty<string>(),
            $"rumor_resolve:{ctx.LocationId}",
            ctx.ScripReward,
            steps);
    }

    public override KarmaAction ResolveCompletion(
        string playerId,
        QuestDefinition questDef,
        IReadOnlyDictionary<string, string> payload)
    {
        payload.TryGetValue("choice", out var choice);
        choice = choice is "expose" or "bury" ? choice : "bury";
        var tags = choice == "expose"
            ? new[] { "helpful", "lawful" }
            : new[] { "protective", "generous" };
        return new KarmaAction(playerId, questDef.GiverNpcId, tags, $"Rumor {choice}d: {questDef.Title}");
    }

    public override IReadOnlyDictionary<string, string> GetCompletionEventData(
        QuestDefinition questDef,
        IReadOnlyDictionary<string, string> payload)
    {
        payload.TryGetValue("choice", out var choice);
        choice = choice is "expose" or "bury" ? choice : "bury";
        return new Dictionary<string, string> { ["rumorChoice"] = choice };
    }
}
