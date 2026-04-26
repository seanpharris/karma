using System;
using System.Linq;
using Karma.Core;
using Karma.Data;

namespace Karma.Generation;

public sealed class DeterministicWorldContentModel : IWorldContentModel
{
    private static readonly string[] NamePrefixes =
    {
        "Copper",
        "Velvet",
        "Static",
        "Pickle",
        "Orbit"
    };

    private static readonly string[] NameSuffixes =
    {
        "Marl",
        "Jun",
        "Pax",
        "Nix",
        "Voss"
    };

    public string ModelId => "deterministic-prototype-content-v1";

    public WorldContentModelResult Generate(WorldContentPrompt prompt)
    {
        var random = new Random(prompt.Seed);
        var factionCount = Math.Clamp(prompt.TargetFactionCount, 1, WorldContentProposalValidator.MaxFactions);
        var npcCount = Math.Clamp(prompt.TargetNpcCount, 1, WorldContentProposalValidator.MaxNpcs);
        var questCount = Math.Clamp(prompt.TargetQuestCount, 0, WorldContentProposalValidator.MaxQuests);

        var factions = Enumerable.Range(0, factionCount)
            .Select(index => new FactionProposal(
                $"generated_faction_{prompt.Seed}_{index}",
                $"{Pick(NamePrefixes, random)} Accord",
                $"A {prompt.ThemeHint} faction with suspiciously formal snack rules."))
            .ToArray();

        var npcs = Enumerable.Range(0, npcCount)
            .Select(index =>
            {
                var faction = factions[index % factions.Length];
                return new NpcProposal(
                    $"generated_npc_{prompt.Seed}_{index}",
                    $"{Pick(NamePrefixes, random)} {Pick(NameSuffixes, random)}",
                    index % 2 == 0 ? "Oddity Broker" : "Karma Clerk",
                    index % 2 == 0 ? "helpful but theatrically confused" : "polite, ominous, deeply punctual",
                    faction.Name,
                    index % 2 == 0 ? "needs a deflated balloon for morale" : "needs proof that kindness is taxable",
                    index % 2 == 0 ? "keeps emergency confetti in their boots" : "once audited a ghost");
            })
            .ToArray();

        var quests = Enumerable.Range(0, Math.Min(questCount, npcs.Length))
            .Select(index => new QuestProposal(
                $"generated_quest_{prompt.Seed}_{index}",
                index % 2 == 0 ? "The Balloon Inquiry" : "Receipt For Mercy",
                npcs[index].Id,
                index % 2 == 0
                    ? "Deliver something useless with complete sincerity."
                    : "Prove a good deed happened before the paperwork expires.",
                index % 2 == 0
                    ? new[] { StarterItems.DeflatedBalloonId }
                    : new[] { StarterItems.RepairKitId },
                PrototypeActions.HelpMaraId))
            .ToArray();

        var proposal = new WorldContentProposal(
            string.IsNullOrWhiteSpace(prompt.ThemeHint) ? "generated-absurd-frontier" : prompt.ThemeHint,
            npcs,
            quests,
            new[] { StarterItems.DeflatedBalloonId, StarterItems.WhoopieCushionId },
            factions);
        var validation = WorldContentProposalValidator.Validate(proposal);
        return new WorldContentModelResult(ModelId, prompt, proposal, validation);
    }

    private static string Pick(string[] values, Random random)
    {
        return values[random.Next(values.Length)];
    }
}
