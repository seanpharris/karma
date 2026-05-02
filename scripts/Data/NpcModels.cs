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
    IReadOnlyCollection<string> Tags = null,
    string DialogueTreeId = "")
{
    public bool HasTag(string tag) => Tags is not null && Tags.Contains(tag);
}

public static class StarterNpcs
{
    public static readonly NpcProfile Dallen = new(
        "dallen_venn",
        "Dallen Venn",
        "Tavernkeeper",
        "earnest, observant, too polite for his own good",
        "Village Freeholders",
        "proof that the tavern accounts have been altered",
        "knows Mara is hiding a lord's broken war-gear",
        new[] { "loyalty", "plain speech", "balanced books" },
        new[] { "betrayal", "public humiliation", "missing receipts" },
        IsLawAligned: true,
        Tags: new[] { NpcRoleTags.Clinic, NpcRoleTags.Vendor, NpcRoleTags.LawAligned },
        DialogueTreeId: DialogueRegistry.DallenShopkeeperTreeId);

    public static readonly NpcProfile Mara = new(
        "mara_venn",
        "Mara Venn",
        "Blacksmith",
        "guarded, practical, secretly generous",
        "Village Freeholders",
        "iron fittings for sick children's cots",
        "salvages metal from a baron's forbidden stores",
        new[] { "honesty", "spare parts", "protecting workers" },
        new[] { "tax collectors", "waste", "threats" },
        IsLawAligned: true,
        Tags: new[] { NpcRoleTags.Clinic, NpcRoleTags.LawAligned });
        // DialogueTreeId intentionally unset: legacy tests + procedural choices
        // depend on the existing GetChoicesFor path. The walker engages only
        // when a tree id is bound — left for a follow-up that migrates Mara's
        // dialogue into the tree without losing the procedural conditionals
        // (vendor browse_wares / station state checks).
}
