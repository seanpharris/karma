using Godot;
using Karma.Art;

namespace Karma.UI;

// Shared visual palette + styling helpers for the Karma main menu and
// its overlays. Pulls colors from the karma_menu_mockup.png splash
// (deep navy panel, metallic gold accents, soft cream body text) so
// the options/credits panels feel like dialog windows from the same
// world rather than generic Godot defaults.
internal static class MenuTheme
{
    // Pack asset paths. The in-game HUD and the pause menu adopt the
    // etahoshi pack; the main menu Options + Credits overlays keep
    // their procedural karma-duality look so they read against the
    // painted splash without competing with it.
    private const string HudPanelTexturePath = "res://assets/art/third_party/Fantasy Minimal Pixel Art GUI by eta-commercial-free/UI/RectangleBox_96x96.png";
    private const string HudButtonTexturePath = "res://assets/art/third_party/Fantasy Minimal Pixel Art GUI by eta-commercial-free/UI/Button_52x14.png";

    // Palette
    public static readonly Color PanelBg       = new(0.06f, 0.09f, 0.14f, 0.96f);
    public static readonly Color PanelBorder   = new(0.85f, 0.68f, 0.32f, 1.00f);
    public static readonly Color TitleGold     = new(1.00f, 0.86f, 0.45f, 1.00f);
    public static readonly Color HeadingGold   = new(0.92f, 0.78f, 0.42f, 1.00f);
    public static readonly Color BodyCream     = new(0.95f, 0.92f, 0.82f, 1.00f);
    public static readonly Color SubtleCream   = new(0.78f, 0.72f, 0.58f, 1.00f);
    public static readonly Color DividerGold   = new(0.75f, 0.60f, 0.30f, 0.45f);

    public static readonly Color BtnBg         = new(0.08f, 0.13f, 0.20f, 1.00f);
    public static readonly Color BtnBgHover    = new(0.14f, 0.22f, 0.32f, 1.00f);
    public static readonly Color BtnBgPressed  = new(0.20f, 0.30f, 0.42f, 1.00f);
    public static readonly Color BtnBorder     = new(0.65f, 0.52f, 0.26f, 1.00f);
    public static readonly Color BtnBorderHov  = new(1.00f, 0.86f, 0.45f, 1.00f);
    public static readonly Color BtnText       = new(0.95f, 0.88f, 0.65f, 1.00f);
    public static readonly Color BtnTextHover  = new(1.00f, 0.96f, 0.78f, 1.00f);

    public static readonly Color SliderTrack   = new(0.04f, 0.07f, 0.11f, 1.00f);
    public static readonly Color SliderFill    = new(0.85f, 0.68f, 0.32f, 1.00f);

    // Panel — used by the main menu Options / Credits overlays and the
    // in-game pause menu. Procedural deep-navy box with a gold border;
    // matches the karma duality splash language.
    public static StyleBoxFlat MakePanelStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = PanelBg,
            BorderColor = PanelBorder,
            BorderWidthLeft = 3, BorderWidthRight = 3,
            BorderWidthTop = 3, BorderWidthBottom = 3,
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            ContentMarginLeft = 28, ContentMarginRight = 28,
            ContentMarginTop = 22, ContentMarginBottom = 22,
            ShadowColor = new Color(0, 0, 0, 0.55f),
            ShadowSize = 14
        };
    }

    // In-game HUD panel — uses the etahoshi pack's 96×96 frame texture
    // 9-sliced. Restricted to HUD surfaces (vitals, etc.) so the menus
    // keep their cleaner procedural look while the diegetic in-game UI
    // gets the pixel-art frame.
    public static StyleBox MakeHudPanelStyle()
    {
        // Routed through AtlasTextureLoader so the panel works pre-import.
        var texture = AtlasTextureLoader.Load(HudPanelTexturePath, forceImageLoad: true);
        if (texture is not null)
        {
            return new StyleBoxTexture
            {
                Texture = texture,
                TextureMarginLeft = 10, TextureMarginRight = 10,
                TextureMarginTop = 10, TextureMarginBottom = 10,
                ContentMarginLeft = 18, ContentMarginRight = 18,
                ContentMarginTop = 14, ContentMarginBottom = 14,
            };
        }
        return MakePanelStyle();
    }

    // Buttons — applied to every Button inside an overlay panel so they
    // read as part of the same world as the splash buttons.
    public static void StyleButton(Button button)
    {
        button.AddThemeStyleboxOverride("normal", MakeButtonStyle(BtnBg, BtnBorder));
        button.AddThemeStyleboxOverride("hover", MakeButtonStyle(BtnBgHover, BtnBorderHov));
        button.AddThemeStyleboxOverride("pressed", MakeButtonStyle(BtnBgPressed, BtnBorderHov));
        button.AddThemeStyleboxOverride("focus", MakeButtonStyle(BtnBgHover, BtnBorderHov));
        button.AddThemeStyleboxOverride("disabled", MakeButtonStyle(BtnBg, BtnBorder));
        button.AddThemeColorOverride("font_color", BtnText);
        button.AddThemeColorOverride("font_hover_color", BtnTextHover);
        button.AddThemeColorOverride("font_pressed_color", BtnTextHover);
        button.AddThemeColorOverride("font_focus_color", BtnTextHover);
    }

    // Pack-textured button — used by the pause menu for visual
    // consistency with MakeHudPanelStyle. Modulate variants per state
    // since the pack's HighlightButton bracket overlay can't compose
    // into a single stylebox; font color carries the rest of the punch.
    public static void StyleHudButton(Button button)
    {
        button.AddThemeStyleboxOverride("normal", MakeHudButtonStyle(HudBtnNormal));
        button.AddThemeStyleboxOverride("hover", MakeHudButtonStyle(HudBtnHover));
        button.AddThemeStyleboxOverride("pressed", MakeHudButtonStyle(HudBtnPressed));
        button.AddThemeStyleboxOverride("focus", MakeHudButtonStyle(HudBtnHover));
        button.AddThemeStyleboxOverride("disabled", MakeHudButtonStyle(HudBtnDisabled));
        button.AddThemeColorOverride("font_color", BtnText);
        button.AddThemeColorOverride("font_hover_color", BtnTextHover);
        button.AddThemeColorOverride("font_pressed_color", BtnTextHover);
        button.AddThemeColorOverride("font_focus_color", BtnTextHover);
    }

    private static StyleBox MakeHudButtonStyle(Color modulate)
    {
        var texture = AtlasTextureLoader.Load(HudButtonTexturePath, forceImageLoad: true);
        if (texture is not null)
        {
            return new StyleBoxTexture
            {
                Texture = texture,
                TextureMarginLeft = 7, TextureMarginRight = 7,
                TextureMarginTop = 3, TextureMarginBottom = 3,
                ContentMarginLeft = 14, ContentMarginRight = 14,
                ContentMarginTop = 4, ContentMarginBottom = 4,
                ModulateColor = modulate
            };
        }
        return MakeButtonStyle(BtnBg, BtnBorder);
    }

    private static readonly Color HudBtnNormal = new(1.00f, 1.00f, 1.00f);
    private static readonly Color HudBtnHover = new(1.10f, 1.05f, 0.92f);
    private static readonly Color HudBtnPressed = new(0.85f, 0.82f, 0.75f);
    private static readonly Color HudBtnDisabled = new(0.65f, 0.65f, 0.65f);

    private static StyleBoxFlat MakeButtonStyle(Color bg, Color border)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = 2, BorderWidthRight = 2,
            BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            ContentMarginLeft = 14, ContentMarginRight = 14,
            ContentMarginTop = 6, ContentMarginBottom = 6
        };
    }

    // OptionButton (resolution dropdown) reuses the button stylebox; its
    // popup is themed separately so it matches when expanded.
    public static void StyleOptionButton(OptionButton optionButton)
    {
        optionButton.AddThemeStyleboxOverride("normal", MakeButtonStyle(BtnBg, BtnBorder));
        optionButton.AddThemeStyleboxOverride("hover", MakeButtonStyle(BtnBgHover, BtnBorderHov));
        optionButton.AddThemeStyleboxOverride("pressed", MakeButtonStyle(BtnBgPressed, BtnBorderHov));
        optionButton.AddThemeStyleboxOverride("focus", MakeButtonStyle(BtnBgHover, BtnBorderHov));
        optionButton.AddThemeColorOverride("font_color", BtnText);
        optionButton.AddThemeColorOverride("font_hover_color", BtnTextHover);
        optionButton.AddThemeColorOverride("font_pressed_color", BtnTextHover);
        optionButton.AddThemeColorOverride("font_focus_color", BtnTextHover);

        // Theme the popup so the dropdown list reads the same.
        var popup = optionButton.GetPopup();
        popup.AddThemeStyleboxOverride("panel", MakeButtonStyle(BtnBg, BtnBorder));
        popup.AddThemeStyleboxOverride("hover", MakeButtonStyle(BtnBgHover, BtnBorderHov));
        popup.AddThemeColorOverride("font_color", BtnText);
        popup.AddThemeColorOverride("font_hover_color", BtnTextHover);
        popup.AddThemeColorOverride("font_separator_color", HeadingGold);
    }

    // HSlider — dark navy track, gold fill, gold grabber.
    public static void StyleSlider(HSlider slider)
    {
        var track = new StyleBoxFlat
        {
            BgColor = SliderTrack,
            BorderColor = BtnBorder,
            BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
            ContentMarginTop = 4, ContentMarginBottom = 4
        };
        var fill = new StyleBoxFlat
        {
            BgColor = SliderFill,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3
        };
        slider.AddThemeStyleboxOverride("slider", track);
        slider.AddThemeStyleboxOverride("grabber_area", fill);
        slider.AddThemeStyleboxOverride("grabber_area_highlight", fill);
    }

    public static void StyleCheckButton(CheckButton check)
    {
        check.AddThemeColorOverride("font_color", BodyCream);
        check.AddThemeColorOverride("font_hover_color", TitleGold);
        check.AddThemeColorOverride("font_pressed_color", TitleGold);
        check.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        check.AddThemeStyleboxOverride("hover", new StyleBoxEmpty());
        check.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
        check.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
    }

    // Label helpers — keeps overlay text legible against the dark panel.
    public static Label MakeTitle(string text)
    {
        var label = new Label
        {
            Text = text.ToUpper(),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 32);
        label.AddThemeColorOverride("font_color", TitleGold);
        return label;
    }

    public static Label MakeSectionHeading(string text)
    {
        var label = new Label { Text = text.ToUpper() };
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", HeadingGold);
        return label;
    }

    public static Label MakeBodyLabel(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 15);
        label.AddThemeColorOverride("font_color", BodyCream);
        return label;
    }

    public static Label MakeSubtleLabel(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", SubtleCream);
        return label;
    }

    public static ColorRect MakeDivider()
    {
        return new ColorRect
        {
            Color = DividerGold,
            CustomMinimumSize = new Vector2(0, 2)
        };
    }
}
