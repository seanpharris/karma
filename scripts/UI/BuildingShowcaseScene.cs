using Godot;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;

namespace Karma.UI;

// Standalone scene that lays out every PNG under
// assets/art/themes/medieval/buildings/ in a grid on a green grass
// field. WASD or arrow keys pan a Camera2D across the layout. ESC
// returns to the main menu.
//
// Built entirely from C# (no .tscn) so it can be entered via
// SceneTree.ChangeSceneToPacked(...) without requiring an editor
// import pass.
public partial class BuildingShowcaseScene : Node2D
{
    private const string BuildingsRoot = "res://assets/art/themes/medieval/buildings/";
    private const float CellWidth = 192f;
    private const float CellHeight = 192f;
    private const int Columns = 6;
    private const float PanSpeed = 600f;

    private Camera2D _camera;
    private Vector2 _cameraTarget;
    private Label _hud;

    public override void _Ready()
    {
        // Background colour rect (a soft grass green).
        var bg = new ColorRect
        {
            Color = new Color(0.27f, 0.45f, 0.22f),
            ZIndex = -100
        };
        AddChild(bg);
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        // Lay out buildings in a Columns × N grid.
        var files = ListBuildingFiles();
        for (var i = 0; i < files.Count; i++)
        {
            var col = i % Columns;
            var row = i / Columns;
            var pos = new Vector2(col * CellWidth, row * CellHeight);
            AddBuildingSprite(files[i], pos);
        }

        var totalRows = (files.Count + Columns - 1) / Columns;
        var layoutSize = new Vector2(Columns * CellWidth, totalRows * CellHeight);

        // Camera centered on the grid initially. Player can pan with
        // WASD or arrow keys.
        _camera = new Camera2D
        {
            Position = layoutSize * 0.5f,
            Zoom = new Vector2(0.45f, 0.45f),
            AnchorMode = Camera2D.AnchorModeEnum.DragCenter
        };
        AddChild(_camera);
        _cameraTarget = _camera.Position;

        // HUD overlay.
        var hudLayer = new CanvasLayer();
        AddChild(hudLayer);
        _hud = new Label
        {
            Text = $"Building Showcase — {files.Count} buildings  •  WASD/arrows to pan  •  Esc → main menu",
            Position = new Vector2(16, 16)
        };
        _hud.AddThemeColorOverride("font_color", new Color(1, 1, 0.85f));
        _hud.AddThemeFontSizeOverride("font_size", 18);
        var hudBg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.5f),
            Position = new Vector2(8, 8),
            Size = new Vector2(820, 36)
        };
        hudLayer.AddChild(hudBg);
        hudLayer.AddChild(_hud);
    }

    public override void _Process(double delta)
    {
        var move = Vector2.Zero;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) move.Y -= 1;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) move.Y += 1;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) move.X -= 1;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) move.X += 1;
        if (move != Vector2.Zero)
        {
            _camera.Position += move.Normalized() * PanSpeed * (float)delta;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && k.Keycode == Key.Escape)
        {
            GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
            GetViewport().SetInputAsHandled();
        }
    }

    private void AddBuildingSprite(string fileName, Vector2 position)
    {
        var fullPath = BuildingsRoot + fileName;
        var image = new Image();
        if (image.Load(ProjectSettings.GlobalizePath(fullPath)) != Error.Ok)
            return;
        var tex = ImageTexture.CreateFromImage(image);

        // Tile a 6×6 grid of grass underneath each cell so the
        // building looks "placed" on the field.
        var grassPanel = new ColorRect
        {
            Color = new Color(0.32f, 0.52f, 0.22f),
            Size = new Vector2(CellWidth - 16, CellHeight - 32),
            Position = position + new Vector2(8, 16),
            ZIndex = -50
        };
        AddChild(grassPanel);

        var sprite = new Sprite2D
        {
            Texture = tex,
            Centered = false,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        };
        var size = tex.GetSize();
        var halfW = size.X * 0.5f;
        // Center horizontally within the cell, push down so it sits
        // near the bottom (anchor like a building footprint).
        sprite.Position = position + new Vector2((CellWidth * 0.5f) - halfW, CellHeight - size.Y - 12);
        sprite.ZIndex = (int)(position.Y + size.Y);
        AddChild(sprite);

        var caption = new Label
        {
            Text = fileName.Replace(".png", string.Empty),
            Position = position + new Vector2(8, CellHeight - 18),
            HorizontalAlignment = HorizontalAlignment.Center,
            CustomMinimumSize = new Vector2(CellWidth - 16, 16)
        };
        caption.AddThemeColorOverride("font_color", new Color(1f, 0.95f, 0.8f));
        caption.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0));
        caption.AddThemeConstantOverride("outline_size", 4);
        caption.AddThemeFontSizeOverride("font_size", 12);
        AddChild(caption);
    }

    private static List<string> ListBuildingFiles()
    {
        var results = new List<string>();
        using var dir = DirAccess.Open(BuildingsRoot);
        if (dir is null) return results;
        dir.ListDirBegin();
        while (true)
        {
            var name = dir.GetNext();
            if (string.IsNullOrEmpty(name)) break;
            if (dir.CurrentIsDir()) continue;
            if (!name.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
            results.Add(name);
        }
        dir.ListDirEnd();
        results.Sort(System.StringComparer.OrdinalIgnoreCase);
        return results;
    }
}
