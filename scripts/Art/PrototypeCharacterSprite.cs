using Godot;
using Karma.Util;

namespace Karma.Art;

public enum CharacterFacingDirection
{
    Down,
    DownRight,
    Right,
    UpRight,
    Up,
    UpLeft,
    Left,
    DownLeft
}

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
    public const string IdleDownRightAnimation = "idle-down-right";
    public const string IdleUpRightAnimation = "idle-up-right";
    public const string IdleUpLeftAnimation = "idle-up-left";
    public const string IdleDownLeftAnimation = "idle-down-left";
    public const string WalkDownRightAnimation = "walk-down-right";
    public const string WalkUpRightAnimation = "walk-up-right";
    public const string WalkUpLeftAnimation = "walk-up-left";
    public const string WalkDownLeftAnimation = "walk-down-left";

    [Export] public PrototypeSpriteKind Kind { get; set; } = PrototypeSpriteKind.Player;
    [Export] public bool DrawShadow { get; set; } = true;
    [Export] public bool PreferAtlasArt { get; set; } = true;

    private AnimatedSprite2D _sprite;
    private PrototypeSprite _fallback;
    private PrototypeSpriteKind _loadedKind;
    private CharacterFacingDirection _lastFacing = CharacterFacingDirection.Down;

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
            _lastFacing = ToFacingDirection(velocity);
        }

        var animationName = ResolveAvailableAnimation(_sprite.SpriteFrames, ResolveAnimationName(velocity, _lastFacing));
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
        return ResolveAnimationName(velocity, idleDirection switch
        {
            CardinalDirection.Up => CharacterFacingDirection.Up,
            CardinalDirection.Left => CharacterFacingDirection.Left,
            CardinalDirection.Right => CharacterFacingDirection.Right,
            _ => CharacterFacingDirection.Down
        });
    }

    public static string ResolveAnimationName(Vector2 velocity, CharacterFacingDirection idleDirection)
    {
        if (velocity.LengthSquared() <= 0.01f)
        {
            return idleDirection switch
            {
                CharacterFacingDirection.Up => IdleUpAnimation,
                CharacterFacingDirection.Left => IdleLeftAnimation,
                CharacterFacingDirection.Right => IdleRightAnimation,
                CharacterFacingDirection.DownRight => IdleDownRightAnimation,
                CharacterFacingDirection.UpRight => IdleUpRightAnimation,
                CharacterFacingDirection.UpLeft => IdleUpLeftAnimation,
                CharacterFacingDirection.DownLeft => IdleDownLeftAnimation,
                _ => IdleDownAnimation
            };
        }

        return ToFacingDirection(velocity) switch
        {
            CharacterFacingDirection.Up => WalkUpAnimation,
            CharacterFacingDirection.Left => WalkLeftAnimation,
            CharacterFacingDirection.Right => WalkRightAnimation,
            CharacterFacingDirection.DownRight => WalkDownRightAnimation,
            CharacterFacingDirection.UpRight => WalkUpRightAnimation,
            CharacterFacingDirection.UpLeft => WalkUpLeftAnimation,
            CharacterFacingDirection.DownLeft => WalkDownLeftAnimation,
            _ => WalkDownAnimation
        };
    }

    public static CharacterFacingDirection ToFacingDirection(Vector2 velocity)
    {
        if (velocity.LengthSquared() <= 0.01f)
        {
            return CharacterFacingDirection.Down;
        }

        var normalized = velocity.Normalized();
        if (normalized.Y <= -0.5f)
        {
            if (normalized.X >= 0.5f)
            {
                return CharacterFacingDirection.UpRight;
            }

            if (normalized.X <= -0.5f)
            {
                return CharacterFacingDirection.UpLeft;
            }

            return CharacterFacingDirection.Up;
        }

        if (normalized.Y >= 0.5f)
        {
            if (normalized.X >= 0.5f)
            {
                return CharacterFacingDirection.DownRight;
            }

            if (normalized.X <= -0.5f)
            {
                return CharacterFacingDirection.DownLeft;
            }

            return CharacterFacingDirection.Down;
        }

        return normalized.X < 0f
            ? CharacterFacingDirection.Left
            : CharacterFacingDirection.Right;
    }

    public static string ResolveAvailableAnimation(SpriteFrames frames, string requestedAnimation)
    {
        if (frames.HasAnimation(requestedAnimation))
        {
            return requestedAnimation;
        }

        var fallback = requestedAnimation switch
        {
            IdleDownRightAnimation => IdleDownAnimation,
            IdleDownLeftAnimation => IdleDownAnimation,
            IdleUpRightAnimation => IdleUpAnimation,
            IdleUpLeftAnimation => IdleUpAnimation,
            WalkDownRightAnimation => WalkDownAnimation,
            WalkDownLeftAnimation => WalkDownAnimation,
            WalkUpRightAnimation => WalkUpAnimation,
            WalkUpLeftAnimation => WalkUpAnimation,
            _ => IdleDownAnimation
        };

        return frames.HasAnimation(fallback) ? fallback : IdleDownAnimation;
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

        var texture = AtlasTextureLoader.Load(
            definition.AtlasPath,
            removeDarkBackground: true,
            forceImageLoad: definition.AtlasPath == PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath);
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
