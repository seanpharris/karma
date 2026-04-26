using Godot;

namespace Karma.Art;

public partial class PrototypeSprite : Node2D
{
    [Export] public PrototypeSpriteKind Kind { get; set; } = PrototypeSpriteKind.Player;
    [Export] public bool DrawShadow { get; set; } = true;
    [Export] public bool PreferAtlasArt { get; set; } = true;

    private Texture2D _atlasTexture;
    private string _atlasPath = string.Empty;

    public override void _Ready()
    {
        LoadAtlas();
        QueueRedraw();
    }

    public override void _Draw()
    {
        var definition = PrototypeSpriteCatalog.Get(Kind);
        if (DrawShadow)
        {
            var shadowRect = new Rect2(
                -definition.Size.X * 0.35f,
                definition.Size.Y * 0.2f,
                definition.Size.X * 0.7f,
                6f);
            DrawRect(shadowRect, new Color(0f, 0f, 0f, 0.28f));
        }

        if (PreferAtlasArt && definition.HasAtlasRegion && _atlasTexture is not null)
        {
            DrawTextureRectRegion(
                _atlasTexture,
                new Rect2(-definition.Size.X * 0.5f, -definition.Size.Y * 0.8f, definition.Size.X, definition.Size.Y),
                definition.AtlasRegion);
            return;
        }

        foreach (var layer in definition.Layers)
        {
            switch (layer.Shape)
            {
                case PrototypeSpriteShape.Rect:
                    DrawRect(layer.Rect, layer.Color);
                    break;
                case PrototypeSpriteShape.Circle:
                    DrawCircle(layer.From, layer.Radius, layer.Color);
                    break;
                case PrototypeSpriteShape.Line:
                    DrawLine(layer.From, layer.To, layer.Color, layer.Width);
                    break;
            }
        }
    }

    private void LoadAtlas()
    {
        var definition = PrototypeSpriteCatalog.Get(Kind);
        if (!definition.HasAtlasRegion || _atlasPath == definition.AtlasPath)
        {
            return;
        }

        _atlasPath = definition.AtlasPath;
        _atlasTexture = ResourceLoader.Exists(_atlasPath)
            ? ResourceLoader.Load<Texture2D>(_atlasPath)
            : null;
    }
}
