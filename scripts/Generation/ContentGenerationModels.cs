using Karma.Data;

namespace Karma.Generation;

public sealed record WorldContentPrompt(
    string WorldId,
    string ThemeHint,
    int TargetNpcCount,
    int TargetQuestCount,
    int TargetFactionCount,
    int Seed);

public sealed record WorldContentModelResult(
    string ModelId,
    WorldContentPrompt Prompt,
    WorldContentProposal Proposal,
    ProposalValidationResult Validation)
{
    public bool IsUsable => Validation.IsValid;
}

public interface IWorldContentModel
{
    string ModelId { get; }

    WorldContentModelResult Generate(WorldContentPrompt prompt);
}
