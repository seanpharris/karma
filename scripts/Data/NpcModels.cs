using System.Collections.Generic;

namespace Karma.Data;

public sealed record NpcProfile(
    string Id,
    string Name,
    string Role,
    string Personality,
    string Faction,
    string Need,
    string Secret,
    IReadOnlyCollection<string> Likes,
    IReadOnlyCollection<string> Dislikes);

public static class StarterNpcs
{
    public static readonly NpcProfile Dallen = new(
        "dallen_venn",
        "Dallen Venn",
        "Clinic Bookkeeper",
        "earnest, observant, too polite for his own good",
        "Free Settlers",
        "proof that the clinic ledger has been altered",
        "knows Mara is hiding corporate drone parts",
        new[] { "loyalty", "plain speech", "balanced books" },
        new[] { "betrayal", "public humiliation", "missing receipts" });

    public static readonly NpcProfile Mara = new(
        "mara_venn",
        "Mara Venn",
        "Clinic Mechanic",
        "guarded, practical, secretly generous",
        "Free Settlers",
        "medicine filters for sick children",
        "steals parts from corporate drones",
        new[] { "honesty", "spare parts", "protecting workers" },
        new[] { "corporate loyalty", "waste", "threats" });
}
