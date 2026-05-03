using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Karma.Art;

// Lookup helper for the medieval art set under
// assets/art/themes/medieval/<category>/. Loads PNGs via Godot's
// Image API (bypassing the import system so freshly-generated files
// show up without an editor re-scan), caches the resulting
// Texture2Ds, and resolves variants by deterministic
// (worldId, entityId) hash.
//
// Categories that exist on disk: items, buildings, structures,
// banners, decals, status_icons, hud_chrome, map_icons,
// quest_glyphs, mounts, environment, tiles, npc_portraits.
public static class ThemedArtRegistry
{
    public const string ThemeRoot = "res://assets/art/themes/medieval/";

    private static readonly Dictionary<string, IReadOnlyList<string>> _variantsByKindKey = new();
    private static readonly Dictionary<string, Texture2D> _textureCache = new();
    private static bool _scanned;

    // Map a structure category / SocialStation archetype id to a
    // building "kind" prefix that lives under
    // `assets/art/themes/medieval/buildings/`. Multiple suffixed PNGs
    // (e.g. smithy_a.png, smithy_b.png) become variants. Keep keys
    // lower-snake to match how the world-generator stamps them.
    public static readonly IReadOnlyDictionary<string, string[]> CategoryToBuildingKinds =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["clinic"] = new[] { "smithy" },
            ["care"] = new[] { "smithy" },
            ["forge"] = new[] { "smithy" },
            ["smithy"] = new[] { "smithy" },
            ["market"] = new[] { "tavern", "market_stall", "wine_shop", "spice_shop" },
            ["trade"] = new[] { "tavern", "market_stall", "guildhall" },
            ["tavern"] = new[] { "tavern", "rural_inn" },
            ["workshop"] = new[] { "smithy", "carpenter", "mason" },
            ["repair"] = new[] { "smithy", "carpenter" },
            ["notice-board"] = new[] { "notice_post" },
            ["notice_board"] = new[] { "notice_post" },
            ["rumor"] = new[] { "notice_post", "scribe" },
            ["social-hub"] = new[] { "tavern", "rural_inn" },
            ["social_hub"] = new[] { "tavern", "rural_inn" },
            ["relationship"] = new[] { "tavern", "rural_inn" },
            ["restricted-storage"] = new[] { "tithe_barn", "granary" },
            ["restricted_storage"] = new[] { "tithe_barn", "granary" },
            ["temptation"] = new[] { "tithe_barn" },
            ["oddity-yard"] = new[] { "shrine", "stone_circle" },
            ["oddity_yard"] = new[] { "shrine" },
            ["chaos"] = new[] { "shrine", "stone_circle" },
            ["duel-ring"] = new[] { "duel_ring" },
            ["duel_ring"] = new[] { "duel_ring" },
            ["combat"] = new[] { "duel_ring", "barracks" },
            ["farm-plot"] = new[] { "well", "vineyard_row", "wheat_patch" },
            ["farm_plot"] = new[] { "well" },
            ["sustenance"] = new[] { "well", "bakery" },
            ["black-market"] = new[] { "tithe_barn", "alchemist" },
            ["black_market"] = new[] { "tithe_barn", "alchemist" },
            ["crime"] = new[] { "alchemist", "burned_house" },
            ["memory-shrine"] = new[] { "shrine", "memorial" },
            ["memory_shrine"] = new[] { "shrine", "memorial" },
            ["redemption"] = new[] { "shrine", "memorial" },
            ["broadcast-tower"] = new[] { "bell_tower", "watchtower" },
            ["broadcast_tower"] = new[] { "bell_tower", "watchtower" },
            ["broadcast"] = new[] { "bell_tower", "watchtower" },
            ["war-memorial"] = new[] { "memorial" },
            ["war_memorial"] = new[] { "memorial" },
            ["loyalty"] = new[] { "memorial" },
            ["court-of-crows"] = new[] { "council_hall", "chapel" },
            ["court_of_crows"] = new[] { "council_hall", "chapel" },
            ["judgment"] = new[] { "council_hall", "chapel" },
        };

    // Keyword fallbacks. The world generator stamps every social
    // station with category="station", so the archetype isn't in the
    // category column — it's encoded in the structure Name. Match by
    // keyword in the Name (case-insensitive). Order matters: longer /
    // more specific phrases first.
    public static readonly IReadOnlyList<(string keyword, string[] kinds)> NameKeywordToKinds =
        new (string keyword, string[] kinds)[]
        {
            ("bell tower", new[] { "bell_tower", "watchtower" }),
            ("notice", new[] { "notice_post", "scribe" }),
            ("notice wall", new[] { "notice_post" }),
            ("forge", new[] { "smithy" }),
            ("smithy", new[] { "smithy" }),
            ("anvil", new[] { "smithy" }),
            ("tavern", new[] { "tavern", "rural_inn" }),
            ("alehouse", new[] { "tavern", "wine_shop" }),
            ("inn", new[] { "rural_inn", "tavern" }),
            ("ledger", new[] { "tavern", "guildhall" }),
            ("market", new[] { "market_stall", "tavern" }),
            ("counter", new[] { "market_stall" }),
            ("shed", new[] { "tithe_barn", "granary" }),
            ("barn", new[] { "tithe_barn", "granary" }),
            ("tithe", new[] { "tithe_barn" }),
            ("court", new[] { "council_hall", "chapel" }),
            ("rook", new[] { "council_hall", "chapel" }),
            ("chapel", new[] { "chapel" }),
            ("shrine", new[] { "shrine", "stone_circle" }),
            ("penance", new[] { "shrine", "memorial" }),
            ("memorial", new[] { "memorial" }),
            ("memory", new[] { "shrine", "memorial" }),
            ("surrender", new[] { "memorial" }),
            ("duel", new[] { "duel_ring" }),
            ("circle", new[] { "duel_ring", "stone_circle" }),
            ("ring", new[] { "duel_ring" }),
            ("balloon", new[] { "shrine", "stone_circle" }),
            ("grave", new[] { "shrine", "memorial" }),
            ("turnip", new[] { "well", "vineyard_row" }),
            ("lot", new[] { "well", "wheat_patch" }),
            ("yard", new[] { "lumber_yard", "stockyard" }),
            ("under-counter", new[] { "alchemist", "tithe_barn" }),
            ("contraband", new[] { "alchemist" }),
            ("watchtower", new[] { "watchtower" }),
            ("tower", new[] { "watchtower", "bell_tower" }),
            ("guard", new[] { "barracks", "watchtower" }),
            ("garrison", new[] { "barracks" }),
            ("witness", new[] { "tavern", "council_hall" }),
        };

    // Resolve a building PNG path for a given structure. Picks one of
    // the matching kinds + one of its variants deterministically by
    // hashing (worldId, entityId). Returns null if no art exists.
    public static Texture2D GetBuildingTexture(string category, string worldId, string entityId)
    {
        return GetBuildingTexture(category, name: string.Empty, worldId, entityId);
    }

    public static Texture2D GetBuildingTexture(string category, string name, string worldId, string entityId)
    {
        EnsureScanned();
        var kinds = ResolveBuildingKinds(category);
        if (kinds.Count == 0 && !string.IsNullOrEmpty(name))
        {
            kinds = ResolveBuildingKindsFromName(name);
        }
        if (kinds.Count == 0) return null;

        var kindIndex = StableIndex(worldId, entityId, kinds.Count);
        var kind = kinds[kindIndex];
        return GetVariant("buildings", kind, worldId, entityId);
    }

    private static List<string> ResolveBuildingKindsFromName(string name)
    {
        var lowered = name.ToLowerInvariant();
        foreach (var (keyword, kinds) in NameKeywordToKinds)
        {
            if (lowered.Contains(keyword)) return kinds.ToList();
        }
        return new List<string>();
    }

    // Generic variant lookup. category = "buildings" / "structures" /
    // "items" / etc. kind = the file-name prefix
    // (e.g. "smithy" → smithy.png, smithy_a.png, smithy_b.png).
    public static Texture2D GetVariant(string category, string kind, string worldId, string entityId)
    {
        EnsureScanned();
        var key = VariantKey(category, kind);
        if (!_variantsByKindKey.TryGetValue(key, out var variants) || variants.Count == 0)
            return null;
        var variantIndex = StableIndex(worldId + "/v", entityId, variants.Count);
        return LoadTexture(category, variants[variantIndex]);
    }

    public static Texture2D GetExact(string category, string fileNameWithoutExt)
    {
        EnsureScanned();
        return LoadTexture(category, fileNameWithoutExt + ".png");
    }

    public static IReadOnlyList<string> GetAllVariants(string category, string kind)
    {
        EnsureScanned();
        return _variantsByKindKey.TryGetValue(VariantKey(category, kind), out var variants)
            ? variants
            : Array.Empty<string>();
    }

    private static List<string> ResolveBuildingKinds(string category)
    {
        if (string.IsNullOrEmpty(category)) return new List<string>();
        var normalized = category.Trim().ToLowerInvariant();
        if (CategoryToBuildingKinds.TryGetValue(normalized, out var direct)) return direct.ToList();
        // Also accept the kind itself (e.g. "smithy") as a direct lookup.
        if (_variantsByKindKey.ContainsKey(VariantKey("buildings", normalized)))
            return new List<string> { normalized };
        return new List<string>();
    }

    private static string VariantKey(string category, string kind) =>
        category + "/" + kind.ToLowerInvariant();

    private static int StableIndex(string a, string b, int mod)
    {
        if (mod <= 0) return 0;
        return Math.Abs(HashCode.Combine(a, b)) % mod;
    }

    private static Texture2D LoadTexture(string category, string fileName)
    {
        var fullPath = ThemeRoot + category + "/" + fileName;
        if (_textureCache.TryGetValue(fullPath, out var cached)) return cached;
        var image = new Image();
        var globalPath = ProjectSettings.GlobalizePath(fullPath);
        var err = image.Load(globalPath);
        if (err != Error.Ok)
        {
            _textureCache[fullPath] = null;
            return null;
        }
        var tex = ImageTexture.CreateFromImage(image);
        _textureCache[fullPath] = tex;
        return tex;
    }

    private static void EnsureScanned()
    {
        if (_scanned) return;
        _scanned = true;
        foreach (var category in new[] { "buildings", "structures", "items", "banners", "decals", "status_icons", "hud_chrome", "map_icons", "quest_glyphs", "mounts", "environment", "tiles", "npc_portraits" })
        {
            ScanCategory(category);
        }
    }

    private static void ScanCategory(string category)
    {
        var dirPath = ThemeRoot + category + "/";
        using var dir = DirAccess.Open(dirPath);
        if (dir is null) return;
        var grouped = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        dir.ListDirBegin();
        while (true)
        {
            var name = dir.GetNext();
            if (string.IsNullOrEmpty(name)) break;
            if (dir.CurrentIsDir()) continue;
            if (!name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
            var stem = name.Substring(0, name.Length - 4);
            // Strip trailing _<single letter or word> to recover the
            // base "kind" — e.g. smithy_a.png → smithy.
            var kind = StripVariantSuffix(stem);
            if (!grouped.TryGetValue(kind, out var list))
            {
                list = new List<string>();
                grouped[kind] = list;
            }
            list.Add(name);
            // Also register the exact name so callers can ask for
            // "smithy_a" specifically.
            if (!grouped.TryGetValue(stem, out var exact))
            {
                exact = new List<string>();
                grouped[stem] = exact;
            }
            if (!exact.Contains(name)) exact.Add(name);
        }
        dir.ListDirEnd();
        foreach (var (kind, files) in grouped)
        {
            files.Sort(StringComparer.OrdinalIgnoreCase);
            _variantsByKindKey[VariantKey(category, kind)] = files;
        }
    }

    // Remove a trailing _a/_b/_c (single letter) or known semantic
    // suffix so we can group variants by their root "kind". Words
    // longer than one letter at the end are kept (e.g. "stone_pedestal"
    // stays grouped as "stone_pedestal").
    private static string StripVariantSuffix(string stem)
    {
        var underscore = stem.LastIndexOf('_');
        if (underscore < 0) return stem;
        var tail = stem.Substring(underscore + 1);
        if (tail.Length == 1 && char.IsLetter(tail[0])) return stem.Substring(0, underscore);
        return stem;
    }
}
