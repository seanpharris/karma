using Karma.Data;

namespace Karma.Generation;

public sealed record WorldContentGenerationResult(
    WorldContentModelResult ModelResult,
    ProposalApplyResult ApplyResult)
{
    public bool WasApplied => ApplyResult.WasApplied;
}

public sealed class WorldContentGenerationService
{
    private readonly IWorldContentModel _model;

    public WorldContentGenerationService(IWorldContentModel model)
    {
        _model = model;
    }

    public WorldContentGenerationResult GenerateAndApply(
        GeneratedWorldAdapter world,
        WorldContentPrompt prompt)
    {
        var modelResult = _model.Generate(prompt);
        var applyResult = modelResult.IsUsable
            ? WorldContentProposalValidator.ApplyToGeneratedWorld(world, modelResult.Proposal)
            : new ProposalApplyResult(false, modelResult.Validation.Errors, world);

        return new WorldContentGenerationResult(modelResult, applyResult);
    }
}
