using System.Collections.Generic;
using Godot;

namespace Karma.UI;

public sealed record UiPalette(
    Color PanelBackground,
    Color PanelBorder,
    Color Accent,
    Color Text,
    Color DimText,
    Color Danger,
    Color Success);

public static class UiPaletteRegistry
{
    public const string MedievalThemeId = "medieval";
    public const string BoardingSchoolThemeId = "boarding_school";
    public const string WesternSciFiThemeId = "western_sci_fi";

    private static readonly UiPalette WesternSciFi = new(
        FromHex("#2b2f36"),
        FromHex("#4a5563"),
        FromHex("#c98847"),
        FromHex("#d8d8d8"),
        FromHex("#7a8290"),
        FromHex("#e05f5f"),
        FromHex("#78c27a"));

    private static readonly IReadOnlyDictionary<string, UiPalette> Palettes = new Dictionary<string, UiPalette>
    {
        [MedievalThemeId] = new UiPalette(
            FromHex("#f4e6c7"),
            FromHex("#8b6f47"),
            FromHex("#5d2a1f"),
            FromHex("#2a1810"),
            FromHex("#b68b65"),
            FromHex("#9d2f26"),
            FromHex("#3f7f43")),

        [BoardingSchoolThemeId] = new UiPalette(
            FromHex("#1f3a2b"),
            FromHex("#5c2a1e"),
            FromHex("#c9a64a"),
            FromHex("#f0e9d6"),
            FromHex("#95a486"),
            FromHex("#d66a58"),
            FromHex("#88b86a")),

        [WesternSciFiThemeId] = WesternSciFi
    };

    public static UiPalette Get(string themeId)
    {
        if (!string.IsNullOrWhiteSpace(themeId) &&
            Palettes.TryGetValue(themeId.Trim(), out var palette))
        {
            return palette;
        }

        return WesternSciFi;
    }

    private static Color FromHex(string hex) => Color.FromHtml(hex);
}
