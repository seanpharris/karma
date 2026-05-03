using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Karma.UI;

// Full-screen overlay that grids every PNG under
// assets/art/themes/medieval/<category>/. Mounted by the main menu
// when the user clicks "View Assets" so we can verify generated art
// without wiring each registry into the world. ESC dismisses.
public partial class AssetGalleryOverlay : PanelContainer
{
    private const string ThemeRoot = "res://assets/art/themes/medieval/";
    private static readonly string[] Categories =
    {
        "items",
        "buildings",
        "structures",
        "npc_portraits",
        "banners",
        "decals",
        "status_icons",
        "hud_chrome",
        "map_icons",
        "quest_glyphs",
        "mounts",
        "environment",
        "tiles"
    };

    public static AssetGalleryOverlay Mount(Node parent)
    {
        var overlay = new AssetGalleryOverlay
        {
            Name = "AssetGalleryOverlay",
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = MouseFilterEnum.Stop
        };
        parent.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        overlay.Build();
        return overlay;
    }

    private void Build()
    {
        var bg = new ColorRect
        {
            Color = new Color(0.05f, 0.06f, 0.08f, 0.97f)
        };
        AddChild(bg);
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var margin = new MarginContainer { CustomMinimumSize = new Vector2(0, 0) };
        margin.AddThemeConstantOverride("margin_left", 24);
        margin.AddThemeConstantOverride("margin_right", 24);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        var header = new HBoxContainer();
        var title = new Label { Text = "Medieval Asset Gallery", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.AddThemeColorOverride("font_color", new Color(0.95f, 0.86f, 0.7f));
        header.AddChild(title);
        var totalsLabel = new Label { Text = $"loading…" };
        totalsLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.75f));
        header.AddChild(totalsLabel);
        var closeBtn = new Button { Text = "Close (Esc)", CustomMinimumSize = new Vector2(140, 32) };
        closeBtn.Pressed += QueueFree;
        header.AddChild(closeBtn);
        root.AddChild(header);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddChild(scroll);

        var content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        content.AddThemeConstantOverride("separation", 16);
        scroll.AddChild(content);

        var totalCount = 0;
        foreach (var category in Categories)
        {
            var (added, _) = AddCategorySection(content, category);
            totalCount += added;
        }
        totalsLabel.Text = $"{totalCount} PNGs across {Categories.Length} categories";
    }

    private (int count, IReadOnlyList<string> files) AddCategorySection(Container parent, string category)
    {
        var dirPath = ThemeRoot + category + "/";
        var files = ListPngs(dirPath);
        if (files.Count == 0) return (0, files);

        var section = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        section.AddThemeConstantOverride("separation", 4);
        parent.AddChild(section);

        var heading = new Label { Text = $"{category}  —  {files.Count} files" };
        heading.AddThemeFontSizeOverride("font_size", 18);
        heading.AddThemeColorOverride("font_color", new Color(0.9f, 0.78f, 0.55f));
        section.AddChild(heading);

        var grid = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        section.AddChild(grid);

        foreach (var file in files)
        {
            var cell = BuildCell(dirPath + file, file);
            grid.AddChild(cell);
        }

        return (files.Count, files);
    }

    private static Control BuildCell(string fullPath, string fileName)
    {
        var cell = new VBoxContainer { CustomMinimumSize = new Vector2(96, 112) };
        cell.AddThemeConstantOverride("separation", 2);

        var frame = new PanelContainer { CustomMinimumSize = new Vector2(96, 96) };
        var frameStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.13f, 0.16f),
            BorderColor = new Color(0.25f, 0.27f, 0.3f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1
        };
        frame.AddThemeStyleboxOverride("panel", frameStyle);
        cell.AddChild(frame);

        // Bypass the Godot import system so freshly-generated PNGs
        // that don't yet have .import sidecars still display.
        var img = new Image();
        var loadErr = img.Load(ProjectSettings.GlobalizePath(fullPath));
        Texture2D tex = loadErr == Error.Ok ? ImageTexture.CreateFromImage(img) : null;
        if (tex is not null)
        {
            var image = new TextureRect
            {
                Texture = tex,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                CustomMinimumSize = new Vector2(96, 96),
                MouseFilter = MouseFilterEnum.Ignore
            };
            frame.AddChild(image);
        }
        else
        {
            var failed = new Label { Text = "load fail", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            failed.AddThemeColorOverride("font_color", new Color(0.8f, 0.4f, 0.4f));
            frame.AddChild(failed);
        }

        var caption = new Label
        {
            Text = fileName.Replace(".png", string.Empty),
            HorizontalAlignment = HorizontalAlignment.Center,
            ClipText = true,
            AutowrapMode = TextServer.AutowrapMode.Off,
            CustomMinimumSize = new Vector2(96, 0)
        };
        caption.AddThemeFontSizeOverride("font_size", 10);
        caption.AddThemeColorOverride("font_color", new Color(0.78f, 0.78f, 0.82f));
        cell.AddChild(caption);

        return cell;
    }

    private static IReadOnlyList<string> ListPngs(string dir)
    {
        var results = new List<string>();
        using var d = DirAccess.Open(dir);
        if (d is null) return results;
        d.ListDirBegin();
        while (true)
        {
            var name = d.GetNext();
            if (string.IsNullOrEmpty(name)) break;
            if (d.CurrentIsDir()) continue;
            if (!name.EndsWith(".png")) continue;
            results.Add(name);
        }
        d.ListDirEnd();
        results.Sort(System.StringComparer.OrdinalIgnoreCase);
        return results;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && k.Keycode == Key.Escape)
        {
            QueueFree();
            GetViewport().SetInputAsHandled();
        }
    }
}
