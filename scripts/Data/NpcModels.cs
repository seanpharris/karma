using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public static class NpcRoleTags
{
    // Function tags
    public const string Clinic = "clinic";
    public const string Vendor = "vendor";
    public const string Workshop = "workshop";
    public const string Saloon = "saloon";
    public const string Warden = "warden";
    public const string Dealer = "dealer";
    // Alignment tags
    public const string LawAligned = "law_aligned";
    public const string OutlawAligned = "outlaw_aligned";
}

public sealed record NpcProfile(
    string Id,
    string Name,
    string Role,
    string Personality,
    string Faction,
    string Need,
    string Secret,
    IReadOnlyCollection<string> Likes,
    IReadOnlyCollection<string> Dislikes,
    bool IsLawAligned = false,
    IReadOnlyCollection<string> Tags = null)
{
    public bool HasTag(string tag) => Tags is not null && Tags.Contains(tag);
}

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
        new[] { "betrayal", "public humiliation", "missing receipts" },
        IsLawAligned: true,
        Tags: new[] { NpcRoleTags.Clinic, NpcRoleTags.Vendor, NpcRoleTags.LawAligned });

    public static readonly NpcProfile Mara = new(
        "mara_venn",
        "Mara Venn",
        "Clinic Mechanic",
        "guarded, practical, secretly generous",
        "Free Settlers",
        "medicine filters for sick children",
        "steals parts from corporate drones",
        new[] { "honesty", "spare parts", "protecting workers" },
        new[] { "corporate loyalty", "waste", "threats" },
        IsLawAligned: true,
        Tags: new[] { NpcRoleTags.Clinic, NpcRoleTags.LawAligned });
}
