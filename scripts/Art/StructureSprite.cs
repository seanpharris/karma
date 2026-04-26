using Godot;

namespace Karma.Art;

public partial class StructureSprite : Node2D
{
    [Export] public StructureSpriteKind Kind { get; set; } = StructureSpriteKind.GreenhouseStandard;
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
        var definition = StructureArtCatalog.Get(Kind);
        DrawShadow(definition.Size);

        if (PreferAtlasArt && definition.HasAtlasRegion && _atlasTexture is not null)
        {
            DrawTextureRectRegion(
                _atlasTexture,
                new Rect2(-definition.Size.X * 0.5f, -definition.Size.Y, definition.Size.X, definition.Size.Y),
                definition.AtlasRegion);
            return;
        }

        DrawFallbackGreenhouse(definition);
    }

    private void LoadAtlas()
    {
        var definition = StructureArtCatalog.Get(Kind);
        if (!definition.HasAtlasRegion || _atlasPath == definition.AtlasPath)
        {
            return;
        }

        _atlasPath = definition.AtlasPath;
        _atlasTexture = ResourceLoader.Exists(_atlasPath)
            ? ResourceLoader.Load<Texture2D>(_atlasPath)
            : null;
    }

    private void DrawShadow(Vector2 size)
    {
        DrawRect(
            new Rect2(-size.X * 0.48f, -8f, size.X * 0.96f, 12f),
            new Color(0f, 0f, 0f, 0.32f));
    }

    private void DrawFallbackGreenhouse(StructureSpriteDefinition definition)
    {
        var size = definition.Size;
        var left = -size.X * 0.5f;
        var top = -size.Y;
        var baseHeight = size.Y * 0.34f;
        var domeHeight = size.Y * 0.66f;
        var baseRect = new Rect2(left + 6f, top + domeHeight - 2f, size.X - 12f, baseHeight);

        var baseColor = definition.Kind switch
        {
            StructureSpriteKind.GreenhouseDamaged => new Color(0.18f, 0.17f, 0.18f),
            StructureSpriteKind.GreenhousePoweredOff => new Color(0.13f, 0.13f, 0.15f),
            _ => new Color(0.22f, 0.24f, 0.26f)
        };
        var glassColor = definition.Kind switch
        {
            StructureSpriteKind.GreenhouseOvergrown => new Color(0.18f, 0.58f, 0.38f, 0.86f),
            StructureSpriteKind.GreenhouseDamaged => new Color(0.14f, 0.22f, 0.28f, 0.7f),
            StructureSpriteKind.GreenhousePoweredOff => new Color(0.11f, 0.16f, 0.2f, 0.72f),
            _ => new Color(0.18f, 0.72f, 0.78f, 0.78f)
        };

        DrawRect(baseRect, new Color(0.08f, 0.09f, 0.1f));
        DrawRect(baseRect.Grow(-3f), baseColor);
        DrawCircle(new Vector2(0f, top + domeHeight), size.X * 0.42f, glassColor);
        DrawRect(new Rect2(left + 4f, top + domeHeight, size.X - 8f, size.Y - domeHeight), baseColor);

        for (var i = -3; i <= 3; i++)
        {
            var x = i * (size.X / 8f);
            DrawLine(new Vector2(x, top + 6f), new Vector2(x * 0.55f, top + domeHeight + 4f), new Color(0.08f, 0.1f, 0.11f), 2f);
        }

        DrawCircle(new Vector2(0f, top + 8f), 8f, new Color(0.34f, 0.36f, 0.38f));
        DrawRect(new Rect2(-13f, top + domeHeight + 8f, 26f, baseHeight - 4f), new Color(0.08f, 0.09f, 0.1f));
        DrawRect(new Rect2(-9f, top + domeHeight + 12f, 18f, baseHeight - 10f), new Color(0.18f, 0.2f, 0.22f));

        if (definition.Kind == StructureSpriteKind.GreenhouseOvergrown)
        {
            DrawCircle(new Vector2(left + 12f, top + domeHeight + 18f), 10f, new Color(0.18f, 0.6f, 0.22f));
            DrawCircle(new Vector2(size.X * 0.34f, top + domeHeight + 8f), 8f, new Color(0.26f, 0.68f, 0.28f));
        }

        if (definition.Kind == StructureSpriteKind.GreenhouseDamaged)
        {
            DrawLine(new Vector2(-16f, top + 18f), new Vector2(4f, top + 38f), new Color(0.02f, 0.03f, 0.04f), 4f);
            DrawLine(new Vector2(14f, top + 26f), new Vector2(28f, top + 12f), new Color(0.02f, 0.03f, 0.04f), 3f);
        }
    }
}
