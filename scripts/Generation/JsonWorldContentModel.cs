using System;
using Karma.Data;

namespace Karma.Generation;

public sealed class JsonWorldContentModel : IWorldContentModel
{
    private readonly Func<WorldContentPrompt, string> _generateJson;

    public JsonWorldContentModel(string modelId, Func<WorldContentPrompt, string> generateJson)
    {
        ModelId = modelId;
        _generateJson = generateJson;
    }

    public string ModelId { get; }

    public WorldContentModelResult Generate(WorldContentPrompt prompt)
    {
        var json = _generateJson(prompt);
        var parsed = WorldContentProposalJson.ParseAndValidate(json);
        var proposal = parsed.Proposal ?? EmptyProposal(prompt);
        return new WorldContentModelResult(ModelId, prompt, proposal, parsed.Validation);
    }

    private static WorldContentProposal EmptyProposal(WorldContentPrompt prompt)
    {
        return new WorldContentProposal(
            string.IsNullOrWhiteSpace(prompt.ThemeHint) ? "invalid-empty-proposal" : prompt.ThemeHint,
            Array.Empty<NpcProposal>(),
            Array.Empty<QuestProposal>(),
            Array.Empty<string>(),
            Array.Empty<FactionProposal>());
    }
}
