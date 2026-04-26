using Godot;
using Karma.Util;

namespace Karma.Art;

public partial class PrototypeCharacterSprite : Node2D
{
    public const string IdleDownAnimation = "idle-down";
    public const string WalkDownAnimation = "walk-down";
    public const string WalkUpAnimation = "walk-up";
    public const string WalkLeftAnimation = "walk-left";
    public const string WalkRightAnimation = "walk-right";

    [Export] public PrototypeSpriteKind Kind { get; set; } = PrototypeSpriteKind.Player;
    [Export] public bool DrawShadow { get; set; } = true;
    [Export] public bool PreferAtlasArt { get; set; } = true;

    private AnimatedSprite2D _sprite;
    private PrototypeSprite _fallback;
    private PrototypeSpriteKind _loadedKind;

    public override void _Ready()
    {
        BuildVisual();
    }

    public override void _Process(double delta)
    {
        if (_sprite is null)
        {
            return;
        }

        var animationName = ResolveAnimationName(GetParentVelocity());
        if (_sprite.Animation != animationName)
        {
            _sprite.Play(animationName);
        }
    }

    public override void _Draw()
    {
        if (!DrawShadow)
        {
            return;
        }

        var definition = PrototypeSpriteCatalog.Get(Kind);
        DrawRect(
            new Rect2(
                -definition.Size.X * 0.35f,
                definition.Size.Y * 0.2f,
                definition.Size.X * 0.7f,
                6f),
            new Color(0f, 0f, 0f, 0.28f));
    }

    public static string ResolveAnimationName(Vector2 velocity)
    {
        if (velocity.LengthSquared() <= 0.01f)
        {
            return IdleDownAnimation;
        }

        return DirectionHelper.ToCardinalDirection(velocity) switch
        {
            CardinalDirection.Up => WalkUpAnimation,
            CardinalDirection.Left => WalkLeftAnimation,
            CardinalDirection.Right => WalkRightAnimation,
            _ => WalkDownAnimation
        };
    }

    public static SpriteFrames CreateSpriteFrames(Texture2D texture, PrototypeSpriteDefinition definition)
    {
        var frames = new SpriteFrames();
        frames.RemoveAnimation("default");

        var frame = new AtlasTexture
        {
            Atlas = texture,
            Region = definition.AtlasRegion
        };

        AddLoopingAnimation(frames, IdleDownAnimation, frame, 1f);
        AddLoopingAnimation(frames, WalkDownAnimation, frame, 5f);
        AddLoopingAnimation(frames, WalkUpAnimation, frame, 5f);
        AddLoopingAnimation(frames, WalkLeftAnimation, frame, 5f);
        AddLoopingAnimation(frames, WalkRightAnimation, frame, 5f);
        return frames;
    }

    public void Rebuild()
    {
        BuildVisual();
    }

    private static void AddLoopingAnimation(SpriteFrames frames, string name, Texture2D texture, float speed)
    {
        frames.AddAnimation(name);
        frames.SetAnimationLoop(name, true);
        frames.SetAnimationSpeed(name, speed);
        frames.AddFrame(name, texture);
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

        _sprite = new AnimatedSprite2D
        {
            Name = "AnimatedSprite2D",
            SpriteFrames = CreateSpriteFrames(texture, definition),
            Animation = IdleDownAnimation,
            Centered = true,
            Offset = CalculateFrameOffset(definition),
            Scale = CalculateFrameScale(definition)
        };
        _sprite.Play(IdleDownAnimation);
        AddChild(_sprite);
        QueueRedraw();
    }

    private void AddFallback()
    {
        _fallback = new PrototypeSprite
        {
            Name = "FallbackPrototypeSprite",
            Kind = Kind,
            DrawShadow = false,
            PreferAtlasArt = false
        };
        AddChild(_fallback);
    }

    private Vector2 GetParentVelocity()
    {
        return GetParent() switch
        {
            CharacterBody2D body => body.Velocity,
            _ => Vector2.Zero
        };
    }

    private static Vector2 CalculateFrameScale(PrototypeSpriteDefinition definition)
    {
        if (definition.AtlasRegion.Size.X <= 0f || definition.AtlasRegion.Size.Y <= 0f)
        {
            return Vector2.One;
        }

        return new Vector2(
            definition.Size.X / definition.AtlasRegion.Size.X,
            definition.Size.Y / definition.AtlasRegion.Size.Y);
    }

    private static Vector2 CalculateFrameOffset(PrototypeSpriteDefinition definition)
    {
        return new Vector2(0f, -definition.Size.Y * 0.31f);
    }
}
