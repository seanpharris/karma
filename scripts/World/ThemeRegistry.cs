using System;
using System.Collections.Generic;
using System.Linq;

namespace Karma.World;

// Module+registry index of supported world themes. Each ThemeDefinition
// declares everything the engine needs to switch worlds between rounds:
// art factory, tile-map style, optional theme.json data path, and a set
// of capability flags that let prototype-only features (e.g. boarding
// school props showcase) opt in by name instead of having callers
// branch on the raw theme string.
//
// Anywhere code currently checks `theme is "boarding_school"` or the
// like, ask the registry instead — that keeps theme dispatch in one
// place so adding a new theme is a one-row change here, not a hunt
// through every consumer.
public enum ThemeTileMapStyle
{
    // Default coarse-noise tile map used by every gameplay theme.
    Generic,
    // Hand-laid boarding-school tile map (special prototype layout).
    BoardingSchool
}

public sealed record ThemeDefinition(
    string Id,
    string DisplayName,
    Func<ThemeArtSet> CreateArtSet,
    ThemeTileMapStyle TileMapStyle = ThemeTileMapStyle.Generic,
    string ThemeJsonPath = "",
    IReadOnlySet<string> Capabilities = null)
{
    public bool HasCapability(string capability) =>
        Capabilities is not null && Capabilities.Contains(capability);
}

public static class ThemeCapabilities
{
    // Prototype-only: render the boarding-school showcase props
    // (buildings/trees/flowers) authored against a specific theme.
    public const string BoardingSchoolPrototypeProps = "boarding_school_prototype_props";
}

public static class ThemeRegistry
{
    // Production default — what CreatePrototype builds and the
    // between-rounds picker pre-selects. Never returned as a fallback
    // for unrecognised theme strings; that goes to FallbackThemeId so
    // unknown ids preserve the prior "WesternSciFi art" behaviour.
    public const string DefaultThemeId = "medieval";
    public const string FallbackThemeId = "western_sci_fi";

    private static readonly Dictionary<string, ThemeDefinition> _themes = BuildDefaults();

    public static IReadOnlyDictionary<string, ThemeDefinition> All => _themes;

    public static IReadOnlyList<string> AllIds =>
        _themes.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

    public static ThemeDefinition Get(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId))
            return _themes[DefaultThemeId];
        return _themes.TryGetValue(NormalizeId(themeId), out var def)
            ? def
            : _themes[FallbackThemeId];
    }

    public static bool TryGet(string themeId, out ThemeDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(themeId))
        {
            definition = null;
            return false;
        }
        return _themes.TryGetValue(NormalizeId(themeId), out definition);
    }

    // Pick a theme id at random from the supported set. Used by
    // WorldGenerator when a config arrives with a blank theme.
    public static string PickRandomId(Random random)
    {
        var ids = AllIds;
        return ids[random.Next(ids.Count)];
    }

    // Test/dev seam: register a theme at runtime. Mirrors the
    // AudioEventCatalog override pattern so future plugin-style
    // themes can drop in without touching this file.
    public static void Register(ThemeDefinition definition)
    {
        if (definition is null) return;
        _themes[NormalizeId(definition.Id)] = definition;
    }

    private static string NormalizeId(string themeId) =>
        themeId.Replace('-', '_').ToLowerInvariant();

    private static Dictionary<string, ThemeDefinition> BuildDefaults()
    {
        var medieval = new ThemeDefinition(
            Id: "medieval",
            DisplayName: "Medieval",
            CreateArtSet: () => ThemeArtRegistry.BuildMedieval("medieval"),
            TileMapStyle: ThemeTileMapStyle.Generic,
            ThemeJsonPath: "res://assets/themes/medieval/theme.json",
            Capabilities: new HashSet<string>());

        var boardingSchool = new ThemeDefinition(
            Id: "boarding_school",
            DisplayName: "Boarding School",
            CreateArtSet: () => ThemeArtRegistry.BuildBoardingSchool("boarding_school"),
            TileMapStyle: ThemeTileMapStyle.BoardingSchool,
            ThemeJsonPath: "res://assets/themes/boarding_school/theme.json",
            Capabilities: new HashSet<string>
            {
                ThemeCapabilities.BoardingSchoolPrototypeProps
            });

        var westernSciFi = new ThemeDefinition(
            Id: "western_sci_fi",
            DisplayName: "Western Sci-Fi",
            CreateArtSet: () => ThemeArtRegistry.BuildWesternSciFi("western-sci-fi"),
            TileMapStyle: ThemeTileMapStyle.Generic,
            ThemeJsonPath: "res://assets/themes/western/theme.json",
            Capabilities: new HashSet<string>());

        return new Dictionary<string, ThemeDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            { medieval.Id, medieval },
            { boardingSchool.Id, boardingSchool },
            { westernSciFi.Id, westernSciFi }
        };
    }
}
