using Godot;
using Karma.Data;
using Karma.Util;
using System.Linq;

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
    [Export] public string AtlasPathOverride { get; set; } = string.Empty;
    [Export] public bool DrawShadow { get; set; } = false;
    [Export] public bool PreferAtlasArt { get; set; } = true;

    private AnimatedSprite2D _sprite;
    private PrototypeSprite _fallback;
    private PrototypeSpriteKind _loadedKind;
    private string _loadedAtlasPathOverride = string.Empty;
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
                -3f,
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
        // Diagonal motion shows the character in profile (east/west) rather than
        // facing the camera — keeps the silhouette readable when both axes are active.
        if (Mathf.Abs(normalized.X) >= 0.5f && Mathf.Abs(normalized.Y) >= 0.5f)
        {
            return normalized.X < 0f
                ? CharacterFacingDirection.Left
                : CharacterFacingDirection.Right;
        }

        if (normalized.Y <= -0.5f)
        {
            return CharacterFacingDirection.Up;
        }

        if (normalized.Y >= 0.5f)
        {
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
        _loadedAtlasPathOverride = "__force_rebuild__";
        BuildVisual();
    }

    public void SetAtlasPathOverride(string atlasPath)
    {
        var normalized = atlasPath?.Trim() ?? string.Empty;
        if (AtlasPathOverride == normalized)
        {
            return;
        }

        AtlasPathOverride = normalized;
        Rebuild();
    }

    // Resolve and apply an LPC theme bundle by id (e.g. "blacksmith_male").
    // Looks up the materialized atlas at
    // assets/art/sprites/themes/medieval/generated/<id>_32x64_8dir_4row.png and overrides
    // this sprite to render from it. Falls back to the catalog default for
    // the current Kind if the atlas is missing on disk.
    public bool ApplyLpcBundle(string bundleId)
    {
        if (string.IsNullOrEmpty(bundleId)) return false;
        var atlasPath = LpcPlayerAppearanceRegistry.BuildAtlasPath(bundleId);
        if (string.IsNullOrEmpty(atlasPath) || !FileAccess.FileExists(atlasPath))
        {
            return false;
        }
        SetAtlasPathOverride(atlasPath);
        return true;
    }

    public bool ApplyLpcBundle(
        string bundleId,
        System.Collections.Generic.IReadOnlyDictionary<EquipmentSlot, string> equipmentItemIds)
    {
        if (string.IsNullOrEmpty(bundleId)) return false;
        var atlasPath = LpcPlayerEquipmentComposer.ComposeEquippedAtlas(bundleId, equipmentItemIds);
        if (string.IsNullOrEmpty(atlasPath) || !FileAccess.FileExists(atlasPath))
        {
            return false;
        }

        SetAtlasPathOverride(atlasPath);
        return true;
    }

    public string ApplyPlayerAppearanceSelection(
        PlayerAppearanceSelection selection,
        string cacheRoot = "user://player_v2/composites")
    {
        Kind = PrototypeSpriteKind.Player;
        // Prefer the LPC composed character if the random-pick run wrote one
        // — gives the prototype an actual walking sprite without depending on
        // PixelLab credits. SET (don't clear) the AtlasPathOverride so the
        // renderer actually pulls the new texture instead of falling back to
        // the catalog's default Kind atlas.
        if (FileAccess.FileExists(PrototypeSpriteCatalog.LpcRandomCharacterAtlasPath))
        {
            SetAtlasPathOverride(PrototypeSpriteCatalog.LpcRandomCharacterAtlasPath);
            return PrototypeSpriteCatalog.LpcRandomCharacterAtlasPath;
        }

        // When the player has pants_blue / shirt_black set, blend them on top
        // of the prebuilt black-boots atlas so the layers are actually visible.
        // The PlayerV2LayerManifest composer can't help here because most other
        // referenced layers (hair_short_*, outfit_*) don't exist as files yet.
        if (HasPantsOrShirt(selection)
            && FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath))
        {
            var composedPath = ComposePantsShirtOntoBase(
                PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath,
                selection,
                cacheRoot);
            if (!string.IsNullOrEmpty(composedPath))
            {
                SetAtlasPathOverride(composedPath);
                return composedPath;
            }
        }

        if (IsDefaultSelection(selection) && FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath))
        {
            SetAtlasPathOverride(string.Empty);
            return PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath;
        }

        if (IsDefaultSelection(selection) && FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2TrialImportedAtlasPath))
        {
            SetAtlasPathOverride(string.Empty);
            return PrototypeSpriteCatalog.PlayerV2TrialImportedAtlasPath;
        }

        if (IsDefaultSelection(selection) && FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath))
        {
            SetAtlasPathOverride(string.Empty);
            return PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath;
        }

        if (IsDefaultSelection(selection) && FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2Model32x64AtlasPath))
        {
            SetAtlasPathOverride(string.Empty);
            return PrototypeSpriteCatalog.PlayerV2Model32x64AtlasPath;
        }

        if (IsDefaultSelection(selection) && FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath))
        {
            SetAtlasPathOverride(string.Empty);
            return PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath;
        }

        if (IsDefaultSelection(selection) && FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath))
        {
            SetAtlasPathOverride(string.Empty);
            return PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath;
        }

        if (IsDefaultSelection(selection) && FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath))
        {
            SetAtlasPathOverride(string.Empty);
            return PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath;
        }

        var atlasPath = ExportPlayerAppearanceAtlas(selection, cacheRoot);
        SetAtlasPathOverride(atlasPath);
        return atlasPath;
    }

    private static bool IsDefaultSelection(PlayerAppearanceSelection selection)
    {
        return selection == PlayerAppearanceSelection.Default;
    }

    public static bool HasPantsOrShirt(PlayerAppearanceSelection selection)
    {
        return !string.IsNullOrWhiteSpace(selection.PantsLayerId)
            || !string.IsNullOrWhiteSpace(selection.ShirtLayerId);
    }

    private const string PlayerV2LayersRoot = "res://assets/art/sprites/player_v2/layers_32x64/";

    public static string ComposePantsShirtOntoBase(
        string basePath,
        PlayerAppearanceSelection selection,
        string cacheRoot)
    {
        var pantsId = selection.PantsLayerId;
        var shirtId = selection.ShirtLayerId;
        var cacheKey = $"{System.IO.Path.GetFileNameWithoutExtension(basePath)}__{Sanitize(pantsId)}__{Sanitize(shirtId)}";
        var outputPath = $"{cacheRoot.TrimEnd('/')}/{cacheKey}.png";
        if (FileAccess.FileExists(outputPath))
        {
            return outputPath;
        }

        var baseImage = Image.LoadFromFile(basePath);
        if (baseImage is null || baseImage.IsEmpty())
        {
            return string.Empty;
        }

        BlendOptionalLayer(baseImage, pantsId);
        BlendOptionalLayer(baseImage, shirtId);

        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(cacheRoot));
        var save = baseImage.SavePng(outputPath);
        return save == Error.Ok ? outputPath : string.Empty;
    }

    private static void BlendOptionalLayer(Image target, string layerId)
    {
        if (string.IsNullOrWhiteSpace(layerId)) return;
        var layerPath = PlayerV2LayersRoot + layerId + ".png";
        if (!FileAccess.FileExists(layerPath)) return;
        var layer = Image.LoadFromFile(layerPath);
        if (layer is null || layer.IsEmpty()) return;
        if (layer.GetWidth() != target.GetWidth() || layer.GetHeight() != target.GetHeight()) return;
        target.BlendRect(layer, new Rect2I(Vector2I.Zero, layer.GetSize()), Vector2I.Zero);
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "none";
        var chars = value.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray();
        return chars.Length == 0 ? "layer" : new string(chars);
    }

    public static string ExportPlayerAppearanceAtlas(
        PlayerAppearanceSelection selection,
        string cacheRoot = "user://player_v2/composites")
    {
        return PlayerV2LayerManifest
            .LoadDefault()
            .ExportAppearanceComposite(selection, cacheRoot);
    }

    public static PrototypeSpriteDefinition WithAtlasPath(PrototypeSpriteDefinition definition, string atlasPath)
    {
        if (string.IsNullOrWhiteSpace(atlasPath))
        {
            return definition;
        }

        return definition with
        {
            AtlasPath = atlasPath,
            HasAtlasRegion = true
        };
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
        var normalizedAtlasOverride = AtlasPathOverride?.Trim() ?? string.Empty;
        if (_loadedKind == Kind &&
            _loadedAtlasPathOverride == normalizedAtlasOverride &&
            (_sprite is not null || _fallback is not null))
        {
            return;
        }

        _loadedKind = Kind;
        _loadedAtlasPathOverride = normalizedAtlasOverride;
        _sprite?.QueueFree();
        _fallback?.QueueFree();
        _sprite = null;
        _fallback = null;

        var definition = WithAtlasPath(PrototypeSpriteCatalog.Get(Kind), normalizedAtlasOverride);
        if (!PreferAtlasArt || !definition.HasAtlasRegion)
        {
            AddFallback();
            QueueRedraw();
            return;
        }

        var texture = AtlasTextureLoader.Load(
            definition.AtlasPath,
            removeDarkBackground: true,
            forceImageLoad: definition.AtlasPath == PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath ||
                            definition.AtlasPath == PrototypeSpriteCatalog.LayeredPlayerPreviewEightDirectionAtlasPath ||
                            definition.AtlasPath == PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath ||
                            definition.AtlasPath == PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath ||
                            definition.AtlasPath == PrototypeSpriteCatalog.PixellabTrialNpcRuntimeAtlasPath ||
                            !string.IsNullOrWhiteSpace(normalizedAtlasOverride));
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
