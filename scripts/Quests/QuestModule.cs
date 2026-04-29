using System.Collections.Generic;
using Karma.Data;

namespace Karma.Quests;

public sealed record QuestCreationContext(
    string QuestId,
    string LocationId,
    string LocationName,
    string LocationRole,
    string GiverNpcId,
    int ScripReward,
    IReadOnlyList<QuestPlacementInfo> OtherPlacements);

public sealed record QuestPlacementInfo(string NpcId, string LocationId);

public abstract class QuestModule
{
    public abstract IReadOnlyList<string> StationRoles { get; }

    // Non-empty only for modules with custom completion logic.
    public virtual string CompletionActionPrefix => string.Empty;

    public abstract QuestDefinition CreateQuest(QuestCreationContext context);

    // Returns null to use the default action resolution (TryResolveQuestCompletionAction).
    public virtual KarmaAction ResolveCompletion(
        string playerId,
        QuestDefinition questDef,
        IReadOnlyDictionary<string, string> payload) => null;

    // Extra fields merged into the quest_completed event data.
    public virtual IReadOnlyDictionary<string, string> GetCompletionEventData(
        QuestDefinition questDef,
        IReadOnlyDictionary<string, string> payload) => new Dictionary<string, string>();
}
