using Godot;
using Karma.Util;

namespace Karma.Art;

public partial class PrototypeCharacterSprite : Node2D
{
    public const string IdleDownAnimation = "idle-down";
    public const string IdleUpAnimation = "idle-up";
    public const string IdleLeftAnimation = "idle-left";
    public const string IdleRightAnimation = "idle-right";
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
    private CardinalDirection _lastFacing = CardinalDirection.Down;

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

        var velocity = GetParentVelocity();
        if (velocity.LengthSquared() > 0.01f)
        {
            _lastFacing = DirectionHelper.ToCardinalDirection(velocity);
        }

        var animationName = ResolveAnimationName(velocity, _lastFacing);
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
        return ResolveAnimationName(velocity, CardinalDirection.Down);
    }

    public static string ResolveAnimationName(Vector2 velocity, CardinalDirection idleDirection)
    {
        if (velocity.LengthSquared() <= 0.01f)
        {
            return idleDirection switch
            {
                CardinalDirection.Up => IdleUpAnimation,
                CardinalDirection.Left => IdleLeftAnimation,
                CardinalDirection.Right => IdleRightAnimation,
                _ => IdleDownAnimation
            };
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

        var animations = definition.Animations is { Count: > 0 }
            ? definition.Animations
            : PrototypeSpriteCatalog.StillCharacterAnimations(definition.AtlasRegion);

        foreach (var animation in animations)
        {
            AddLoopingAnimation(frames, animation, texture);
        }

        return frames;
    }

    public void Rebuild()
    {
        BuildVisual();
    }

    private static void AddLoopingAnimation(SpriteFrames frames, PrototypeSpriteAnimation animation, Texture2D texture)
    {
        frames.AddAnimation(animation.Name);
        frames.SetAnimationLoop(animation.Name, true);
        frames.SetAnimationSpeed(animation.Name, animation.Speed);
        foreach (var region in animation.Frames)
        {
            frames.AddFrame(
                animation.Name,
                new AtlasTexture
                {
                    Atlas = texture,
                    Region = region
                });
        }
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
        return AtlasFrames.FromPrototype(definition).CalculateScale();
    }

    private static Vector2 CalculateFrameOffset(PrototypeSpriteDefinition definition)
    {
        return AtlasFrames.FromPrototype(definition).CalculateOffset();
    }
}
