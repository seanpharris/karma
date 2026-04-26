using System;
using System.Text.Json;
using Karma.Data;

namespace Karma.Generation;

public sealed record ProposalJsonParseResult(
    bool WasParsed,
    WorldContentProposal Proposal,
    ProposalValidationResult Validation,
    string Error)
{
    public bool IsUsable => WasParsed && Validation.IsValid;
}

public static class WorldContentProposalJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static string Write(WorldContentProposal proposal)
    {
        return JsonSerializer.Serialize(proposal, Options);
    }

    public static ProposalJsonParseResult ParseAndValidate(string json)
    {
        try
        {
            var proposal = JsonSerializer.Deserialize<WorldContentProposal>(json, Options);
            if (proposal is null)
            {
                return new ProposalJsonParseResult(
                    false,
                    null,
                    new ProposalValidationResult(false, new[] { "proposal json was empty" }),
                    "proposal json was empty");
            }

            var validation = WorldContentProposalValidator.Validate(proposal);
            return new ProposalJsonParseResult(validation.IsValid, proposal, validation, string.Empty);
        }
        catch (Exception exception)
        {
            return new ProposalJsonParseResult(
                false,
                null,
                new ProposalValidationResult(false, new[] { exception.Message }),
                exception.Message);
        }
    }
}
