using Godot;

namespace Karma.Art;

public partial class PrototypeAtlasSprite : Node2D
{
    [Export] public PrototypeSpriteKind Kind { get; set; } = PrototypeSpriteKind.WhoopieCushion;
    [Export] public bool DrawShadow { get; set; }
    [Export] public bool PreferAtlasArt { get; set; } = true;

    private Sprite2D _sprite;
    private PrototypeSprite _fallback;
    private PrototypeSpriteKind _loadedKind;

    public override void _Ready()
    {
        BuildVisual();
    }

    public override void _Draw()
    {
        if (!DrawShadow)
        {
            return;
        }

        var definition = PrototypeSpriteCatalog.Get(Kind);
        DrawRect(GetShadowRect(definition), new Color(0f, 0f, 0f, 0.28f));
    }

    public static AtlasTexture CreateAtlasTexture(Texture2D texture, PrototypeSpriteDefinition definition)
    {
        return AtlasFrames.FromPrototype(definition).ToTexture(texture);
    }

    public static Vector2 CalculateScale(PrototypeSpriteDefinition definition)
    {
        return AtlasFrames.FromPrototype(definition).CalculateScale();
    }

    public static Vector2 CalculateOffset(PrototypeSpriteDefinition definition)
    {
        return AtlasFrames.FromPrototype(definition).CalculateOffset();
    }

    public void Rebuild()
    {
        BuildVisual();
    }

    private static Rect2 GetShadowRect(PrototypeSpriteDefinition definition)
    {
        return new Rect2(
            -definition.Size.X * 0.35f,
            definition.Size.Y * 0.2f,
            definition.Size.X * 0.7f,
            6f);
    }

    private void BuildVisual()
    {
        if (_loadedKind == Kind && (_sprite is not null || _fallback is not null))
        {
            return;
        }

        _loadedKind = Kind;
        _sprite?.QueueFree();
        _fallback?.QueueFree();
        _sprite = null;
        _fallback = null;

        var definition = PrototypeSpriteCatalog.Get(Kind);
        if (!PreferAtlasArt || !definition.HasAtlasRegion)
        {
            AddFallback();
            QueueRedraw();
            return;
        }

        var texture = AtlasTextureLoader.Load(definition.AtlasPath, removeDarkBackground: true);
        if (texture is null)
        {
            AddFallback();
            QueueRedraw();
            return;
        }

        _sprite = new Sprite2D
        {
            Name = "Sprite2D",
            Texture = CreateAtlasTexture(texture, definition),
            Centered = true,
            Offset = CalculateOffset(definition),
            Scale = CalculateScale(definition)
        };
        AddChild(_sprite);
        QueueRedraw();
    }

    private void AddFallback()
    {
        _fallback = new PrototypeSprite
        {
            Name = "FallbackPrototypeSprite",
            Kind = Kind,
            DrawShadow = DrawShadow,
            PreferAtlasArt = false
        };
        AddChild(_fallback);
    }
}
