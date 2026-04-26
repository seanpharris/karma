using System;
using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public sealed record WorldContentProposal(
    string Theme,
    IReadOnlyList<NpcProposal> Npcs,
    IReadOnlyList<QuestProposal> Quests,
    IReadOnlyList<string> OddityItemIds,
    IReadOnlyList<FactionProposal> Factions);

public sealed record FactionProposal(
    string Id,
    string Name,
    string Description);

public sealed record NpcProposal(
    string Id,
    string Name,
    string Role,
    string Personality,
    string Faction,
    string Need,
    string Secret);

public sealed record QuestProposal(
    string Id,
    string Title,
    string GiverNpcId,
    string Description,
    IReadOnlyList<string> RequiredItemIds,
    string CompletionActionId);

public sealed record ProposalValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static ProposalValidationResult Valid { get; } = new(true, System.Array.Empty<string>());
}

public static class WorldContentProposalValidator
{
    public const int MaxNpcs = 200;
    public const int MaxQuests = 200;
    public const int MaxFactions = 40;
    public const int MaxTextLength = 280;

    public static ProposalValidationResult Validate(WorldContentProposal proposal)
    {
        var errors = new List<string>();
        RequireText(proposal.Theme, "theme", errors);

        if (proposal.Npcs.Count > MaxNpcs)
        {
            errors.Add($"too many NPCs: {proposal.Npcs.Count}");
        }

        if (proposal.Quests.Count > MaxQuests)
        {
            errors.Add($"too many quests: {proposal.Quests.Count}");
        }

        if (proposal.Factions.Count > MaxFactions)
        {
            errors.Add($"too many factions: {proposal.Factions.Count}");
        }

        foreach (var faction in proposal.Factions)
        {
            ValidateFaction(faction, errors);
        }

        foreach (var npc in proposal.Npcs)
        {
            ValidateNpc(npc, errors);
        }

        var npcIds = proposal.Npcs.Select(npc => npc.Id)
            .Concat(new[] { StarterNpcs.Mara.Id, StarterNpcs.Dallen.Id })
            .ToHashSet();

        foreach (var quest in proposal.Quests)
        {
            ValidateQuest(quest, npcIds, errors);
        }

        foreach (var itemId in proposal.OddityItemIds)
        {
            if (StarterItems.GetById(itemId).Id != itemId)
            {
                errors.Add($"unknown oddity item id: {itemId}");
            }
        }

        return errors.Count == 0
            ? ProposalValidationResult.Valid
            : new ProposalValidationResult(false, errors);
    }

    public static NpcProfile ToNpcProfile(NpcProposal proposal)
    {
        return new NpcProfile(
            proposal.Id,
            proposal.Name,
            proposal.Role,
            proposal.Personality,
            proposal.Faction,
            proposal.Need,
            proposal.Secret,
            new[] { "honesty", "useful junk" },
            new[] { "threats", "betrayal" });
    }

    public static QuestDefinition ToQuestDefinition(QuestProposal proposal)
    {
        return new QuestDefinition(
            proposal.Id,
            proposal.Title,
            proposal.GiverNpcId,
            proposal.Description,
            proposal.RequiredItemIds,
            proposal.CompletionActionId);
    }

    public static FactionProfile ToFactionProfile(FactionProposal proposal)
    {
        return new FactionProfile(proposal.Id, proposal.Name, proposal.Description);
    }

    public static ProposalApplyResult ApplyToGeneratedWorld(
        GeneratedWorldAdapter world,
        WorldContentProposal proposal)
    {
        var result = Validate(proposal);
        if (!result.IsValid)
        {
            return new ProposalApplyResult(false, result.Errors, world);
        }

        var applied = world with
        {
            Theme = proposal.Theme,
            Npcs = world.Npcs.Concat(proposal.Npcs.Select(ToNpcProfile)).ToArray(),
            Quests = proposal.Quests.Select(ToQuestDefinition).ToArray(),
            Oddities = world.Oddities.Concat(proposal.OddityItemIds.Select(StarterItems.GetById)).DistinctBy(item => item.Id).ToArray(),
            Factions = world.Factions.Concat(proposal.Factions.Select(ToFactionProfile)).DistinctBy(faction => faction.Id).ToArray()
        };

        return new ProposalApplyResult(true, Array.Empty<string>(), applied);
    }

    private static void ValidateNpc(NpcProposal npc, List<string> errors)
    {
        RequireText(npc.Id, "npc.id", errors);
        RequireText(npc.Name, "npc.name", errors);
        RequireText(npc.Role, "npc.role", errors);
        RequireText(npc.Personality, "npc.personality", errors);
        RequireText(npc.Faction, "npc.faction", errors);
        RequireText(npc.Need, "npc.need", errors);
        RequireText(npc.Secret, "npc.secret", errors);
    }

    private static void ValidateQuest(QuestProposal quest, HashSet<string> npcIds, List<string> errors)
    {
        RequireText(quest.Id, "quest.id", errors);
        RequireText(quest.Title, "quest.title", errors);
        RequireText(quest.Description, "quest.description", errors);

        if (!npcIds.Contains(quest.GiverNpcId))
        {
            errors.Add($"unknown quest giver npc id: {quest.GiverNpcId}");
        }

        foreach (var itemId in quest.RequiredItemIds)
        {
            if (StarterItems.GetById(itemId).Id != itemId)
            {
                errors.Add($"unknown required item id: {itemId}");
            }
        }

        if (!Core.PrototypeActions.TryGet(quest.CompletionActionId, out _))
        {
            errors.Add($"unknown completion action id: {quest.CompletionActionId}");
        }
    }

    private static void ValidateFaction(FactionProposal faction, List<string> errors)
    {
        RequireText(faction.Id, "faction.id", errors);
        RequireText(faction.Name, "faction.name", errors);
        RequireText(faction.Description, "faction.description", errors);
    }

    private static void RequireText(string value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"missing {fieldName}");
            return;
        }

        if (value.Length > MaxTextLength)
        {
            errors.Add($"{fieldName} too long");
        }
    }
}

public sealed record GeneratedWorldAdapter(
    string Theme,
    IReadOnlyList<NpcProfile> Npcs,
    IReadOnlyList<QuestDefinition> Quests,
    IReadOnlyList<GameItem> Oddities,
    IReadOnlyList<FactionProfile> Factions);

public sealed record ProposalApplyResult(
    bool WasApplied,
    IReadOnlyList<string> Errors,
    GeneratedWorldAdapter World);
