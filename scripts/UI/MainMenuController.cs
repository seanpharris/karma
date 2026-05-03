using System;
using System.Collections.Generic;
using Godot;
using Karma.Audio;

namespace Karma.UI;

// Karma main menu — programmatic construction.
//
// Renders the karma_menu_mockup.png splash and lays transparent click-
// through buttons exactly over the painted PLAY / MULTIVERSE / OPTIONS
// / CREDITS / QUIT artwork so the visible art stays the same but
// inputs are real. Adds ambient animation (gold motes left, red embers
// right, periodic lightning on the right half, title shimmer pulse,
// button hover bloom).
//
// Options + Credits are rendered as parchment overlays that hide the
// splash when shown; closing returns to the splash.
public partial class MainMenuController : Control
{
    public const string GameplayScenePath = "res://scenes/Main.tscn";
    public const string EventPrototypeScenePath = "res://scenes/InGameEventPrototype.tscn";
    public const string OptionsPath = "user://options.cfg";
    public const string MenuThemePath = PrototypeMusicPlayer.MusicDirectory + PrototypeMusicPlayer.TravellingOnMedievalFileName;
    public const string SplashTexturePath = "res://assets/art/main_menu/karma_menu_mockup.png";

    // Debug flag: when true, button hover glows render at idle so
    // their placement is visible without mousing over each. Flip
    // back to false before merging.
    private const bool AlwaysShowHoverGlows = true;

    // Image is 1536x1024. These rects are normalized to that aspect:
    // (left, top, right, bottom) in 0..1 over the image bounds. Tuned
    // by eye to overlap the painted buttons. Adjust if the splash
    // re-renders or moves the buttons.
    // Tuned for the 4-button splash variant of karma_menu_mockup.png
    // (1536×1024, MULTIVERSE removed). Buttons are stacked closer than
    // the 5-button version: ~10% center-to-center, 8% tall.
    private static readonly (string Id, string Label, Rect2 Bounds)[] ButtonLayout =
    {
        // PLAY pushed down, OPTIONS pulled up, so the gap between
        // PLAY → OPTIONS equals the gap between CREDITS → QUIT
        // (≈ 9.5% top-to-top). Top tier (PLAY/OPTIONS) and bottom tier
        // (CREDITS/QUIT) read as paired stacks.
        ("play",    "PLAY",    new Rect2(0.39f, 0.505f, 0.22f, 0.082f)),
        ("options", "OPTIONS", new Rect2(0.39f, 0.600f, 0.22f, 0.082f)),
        ("credits", "CREDITS", new Rect2(0.39f, 0.708f, 0.22f, 0.082f)),
        ("quit",    "QUIT",    new Rect2(0.39f, 0.793f, 0.22f, 0.082f))
    };

    // Title shimmer rect (over the painted "KARMA" letters).
    private static readonly Rect2 TitleRect = new(0.20f, 0.04f, 0.60f, 0.21f);

    // Right-half cover for lightning flash. Anchored to right 50% of
    // the splash so the flash only kisses the Renegade side.
    private static readonly Rect2 LightningRect = new(0.50f, 0.0f, 0.50f, 1.0f);

    private static readonly Vector2I[] CommonResolutions =
    {
        new(1280, 720),
        new(1366, 768),
        new(1600, 900),
        new(1920, 1080),
        new(2560, 1440),
        new(3840, 2160)
    };

    private AudioStreamPlayer _menuThemePlayer;
    private Control _stageFrame;
    private TextureRect _splash;
    private Control _imageFrame;
    private GpuParticles2D _goldMotes;
    private GpuParticles2D _redEmbers;
    private ColorRect _lightningFlash;
    private TextureRect _titleShimmer;
    private Timer _lightningTimer;
    private readonly Dictionary<string, Button> _buttons = new();
    private Control _overlayLayer;
    private PanelContainer _optionsOverlay;
    private PanelContainer _creditsOverlay;
    private Label _statusLabel;

    // Options form widgets (built lazily when the panel is opened the
    // first time so launching to splash is fast).
    private OptionButton _resolutionOption;
    private CheckButton _fullscreenToggle;
    private CheckButton _vsyncToggle;
    private HSlider _masterVolumeSlider;
    private HSlider _musicVolumeSlider;
    private HSlider _effectsVolumeSlider;
    private HSlider _ambientVolumeSlider;
    private Label _detectedResolutionLabel;
    private Label _masterVolumeLabel;
    private Label _musicVolumeLabel;
    private Label _effectsVolumeLabel;
    private Label _ambientVolumeLabel;
    private bool _optionsBuilt;

    public override void _Ready()
    {
        _menuThemePlayer = GetNodeOrNull<AudioStreamPlayer>("MenuThemePlayer") ?? new AudioStreamPlayer { Name = "MenuThemePlayer", VolumeDb = -6f };
        if (_menuThemePlayer.GetParent() is null) AddChild(_menuThemePlayer);

        AudioSettings.EnsureBusesExist();

        BuildSplashStage();
        BuildAnimations();
        BuildButtons();
        BuildOverlayLayer();
        BuildStatusBar();

        SetupMenuTheme();
        ApplyMenuThemeVolume();
    }

    // ───── view construction ─────────────────────────────────────────

    private void BuildSplashStage()
    {
        // Outer frame fills the window; the splash inside fills + crops
        // (KeepAspectCovered) so it's distortion-free on any aspect.
        // ImageFrame is a sibling Control sized to track the visible
        // image rect — every overlay (animations, buttons, title
        // shimmer, lightning) lives inside ImageFrame and uses anchor
        // percentages relative to it, so they always align with the
        // painted art regardless of window aspect.
        _stageFrame = new Control
        {
            Name = "StageFrame",
            MouseFilter = MouseFilterEnum.Pass
        };
        _stageFrame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_stageFrame);

        var splashImage = new Image();
        var loadErr = splashImage.Load(ProjectSettings.GlobalizePath(SplashTexturePath));
        if (loadErr != Error.Ok)
        {
            var bg = new ColorRect { Color = new Color(0.05f, 0.05f, 0.08f) };
            bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            _stageFrame.AddChild(bg);
            return;
        }
        var splashTex = ImageTexture.CreateFromImage(splashImage);

        _splash = new TextureRect
        {
            Name = "Splash",
            Texture = splashTex,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _splash.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _stageFrame.AddChild(_splash);

        _imageFrame = new Control
        {
            Name = "ImageFrame",
            MouseFilter = MouseFilterEnum.Pass
        };
        _stageFrame.AddChild(_imageFrame);
        _stageFrame.Resized += LayoutImageFrame;
        LayoutImageFrame();
    }

    // Recompute the visible image rect after KeepAspectCovered scaling
    // and snap _imageFrame to it. Called whenever the stage frame
    // resizes (window resize / fullscreen toggle).
    private void LayoutImageFrame()
    {
        if (_imageFrame is null || _splash?.Texture is null || _stageFrame is null) return;
        var stageSize = _stageFrame.Size;
        if (stageSize.X <= 0 || stageSize.Y <= 0) return;
        var texSize = _splash.Texture.GetSize();
        if (texSize.X <= 0 || texSize.Y <= 0) return;

        // KeepAspectCovered scale = max so the image fills both axes.
        var scale = Mathf.Max(stageSize.X / texSize.X, stageSize.Y / texSize.Y);
        var imgSize = texSize * scale;
        var imgPos = (stageSize - imgSize) * 0.5f;

        _imageFrame.AnchorLeft = 0; _imageFrame.AnchorTop = 0;
        _imageFrame.AnchorRight = 0; _imageFrame.AnchorBottom = 0;
        _imageFrame.OffsetLeft = imgPos.X;
        _imageFrame.OffsetTop = imgPos.Y;
        _imageFrame.OffsetRight = imgPos.X + imgSize.X;
        _imageFrame.OffsetBottom = imgPos.Y + imgSize.Y;
    }

    private void BuildAnimations()
    {
        if (_imageFrame is null) return;

        _titleShimmer = new TextureRect
        {
            Name = "TitleShimmer",
            Texture = MakeRadialDot(64, 64, new Color(1, 0.95f, 0.65f), softness: 0.85f),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Modulate = new Color(1, 1, 1, 0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AnchorRect(_titleShimmer, TitleRect);
        _imageFrame.AddChild(_titleShimmer);
        StartTitleShimmer();

        _lightningFlash = new ColorRect
        {
            Name = "LightningFlash",
            Color = new Color(1f, 0.85f, 0.95f, 0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AnchorRect(_lightningFlash, LightningRect);
        _imageFrame.AddChild(_lightningFlash);
        _lightningTimer = new Timer { OneShot = true, WaitTime = NextLightningInterval() };
        _lightningTimer.Timeout += OnLightning;
        AddChild(_lightningTimer);
        _lightningTimer.Start();

        // Particles drift up across the painted halves. Children of
        // _imageFrame so the emission box tracks the visible image
        // rect (not the full window) — keeps motes on-image even when
        // KeepAspectCovered crops part of the splash off the window.
        _goldMotes = MakeAmbientParticles("GoldMotes", new Color(1f, 0.85f, 0.45f, 0.9f), 28, 6.5);
        _imageFrame.AddChild(_goldMotes);
        _redEmbers = MakeAmbientParticles("RedEmbers", new Color(1f, 0.4f, 0.18f, 0.9f), 32, 4.5);
        _imageFrame.AddChild(_redEmbers);
        _imageFrame.Resized += LayoutParticles;
        LayoutParticles();
    }

    private void LayoutParticles()
    {
        if (_imageFrame is null || _goldMotes is null || _redEmbers is null) return;
        var size = _imageFrame.Size;
        if (size.X <= 0 || size.Y <= 0) return;

        var halfWidth = size.X * 0.5f;
        var halfX = halfWidth * 0.5f;
        var bottomY = size.Y * 0.95f;
        var emissionExtents = new Vector3(halfWidth * 0.5f, size.Y * 0.05f, 1f);

        _goldMotes.Position = new Vector2(halfX, bottomY);
        if (_goldMotes.ProcessMaterial is ParticleProcessMaterial goldMat)
            goldMat.EmissionBoxExtents = emissionExtents;

        _redEmbers.Position = new Vector2(halfWidth + halfX, bottomY);
        if (_redEmbers.ProcessMaterial is ParticleProcessMaterial redMat)
            redMat.EmissionBoxExtents = emissionExtents;
    }

    private GpuParticles2D MakeAmbientParticles(string name, Color tint, int amount, double lifetime)
    {
        var material = new ParticleProcessMaterial
        {
            EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
            EmissionBoxExtents = new Vector3(384f, 32f, 1f), // half the image width on each side, short band along bottom; offset adjusts vertical spawn
            Direction = new Vector3(0f, -1f, 0f),
            Spread = 18f,
            InitialVelocityMin = 18f,
            InitialVelocityMax = 56f,
            Gravity = Vector3.Zero,
            ScaleMin = 0.6f,
            ScaleMax = 1.4f,
            Color = tint,
        };
        var particles = new GpuParticles2D
        {
            Name = name,
            Amount = amount,
            Lifetime = lifetime,
            Preprocess = lifetime,
            ProcessMaterial = material,
            Texture = MakeRadialDot(8, 8, Colors.White, softness: 0.6f),
            SpeedScale = 0.8f
        };
        return particles;
    }

    private static Texture2D MakeRadialDot(int width, int height, Color tint, float softness)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        var cx = (width - 1) * 0.5f;
        var cy = (height - 1) * 0.5f;
        var maxR = Math.Min(width, height) * 0.5f;
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var dx = x - cx;
                var dy = y - cy;
                var d = MathF.Sqrt(dx * dx + dy * dy) / maxR;
                var a = MathF.Max(0f, 1f - d * softness);
                image.SetPixel(x, y, new Color(tint.R, tint.G, tint.B, a));
            }
        return ImageTexture.CreateFromImage(image);
    }

    private void StartTitleShimmer()
    {
        if (_titleShimmer is null) return;
        var tween = CreateTween();
        tween.SetLoops();
        tween.TweenProperty(_titleShimmer, "modulate:a", 0.32f, 1.4f).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_titleShimmer, "modulate:a", 0.0f, 1.4f).SetEase(Tween.EaseType.InOut);
        tween.TweenInterval(2.6);
    }

    private void OnLightning()
    {
        if (_lightningFlash is null) return;
        var tween = CreateTween();
        tween.TweenProperty(_lightningFlash, "color:a", 0.36f, 0.06f);
        tween.TweenProperty(_lightningFlash, "color:a", 0.0f, 0.05f);
        tween.TweenProperty(_lightningFlash, "color:a", 0.22f, 0.04f);
        tween.TweenProperty(_lightningFlash, "color:a", 0.0f, 0.18f);
        tween.Finished += () =>
        {
            if (_lightningTimer is null || !IsInstanceValid(_lightningTimer)) return;
            _lightningTimer.WaitTime = NextLightningInterval();
            _lightningTimer.Start();
        };
    }

    private static double NextLightningInterval()
    {
        return 8.0 + GD.RandRange(0.0, 5.0);
    }

    private void BuildButtons()
    {
        if (_imageFrame is null) return;

        var transparent = new StyleBoxEmpty();
        var pillTexture = MakePillGlow(256, 80, softness: 1.4f);
        foreach (var (id, label, bounds) in ButtonLayout)
        {
            // Glow sits below the button as a separate stage-frame child
            // (under the click target's draw order). Both share the
            // same normalised rect and are tweened together on hover.
            var glow = new TextureRect
            {
                Name = $"Glow_{id}",
                Texture = pillTexture,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                // Debug: glow visible at idle so the click rect
                // placement shows without hovering. Reset to 0f to
                // restore the hover-only behaviour.
                Modulate = new Color(GlowTint(id), AlwaysShowHoverGlows ? 0.55f : 0f),
                MouseFilter = MouseFilterEnum.Ignore
            };
            // Anchor the glow slightly larger than the button rect so
            // the halo bleeds around the painted edges. Vertical
            // padding kept tight (10% per side) so the glow doesn't
            // bleed visibly into the dark cliff area below QUIT.
            var glowBounds = new Rect2(
                bounds.Position.X - bounds.Size.X * 0.08f,
                bounds.Position.Y - bounds.Size.Y * 0.10f,
                bounds.Size.X * 1.16f,
                bounds.Size.Y * 1.20f);
            AnchorRect(glow, glowBounds);
            _imageFrame.AddChild(glow);
            glow.PivotOffset = glow.Size * 0.5f;
            glow.Resized += () => glow.PivotOffset = glow.Size * 0.5f;

            var button = new Button
            {
                Name = $"Button_{id}",
                Text = string.Empty,
                FocusMode = FocusModeEnum.None,
                MouseFilter = MouseFilterEnum.Stop,
                TooltipText = label
            };
            button.AddThemeStyleboxOverride("normal", transparent);
            button.AddThemeStyleboxOverride("focus", transparent);
            button.AddThemeStyleboxOverride("disabled", transparent);
            button.AddThemeStyleboxOverride("hover", transparent);
            button.AddThemeStyleboxOverride("pressed", transparent);
            AnchorRect(button, bounds);
            _imageFrame.AddChild(button);
            button.PivotOffset = button.Size * 0.5f;
            button.Resized += () => button.PivotOffset = button.Size * 0.5f;

            // Hover animation: glow fades in, button scales up slightly.
            // No vertical lift — keeps the click rect anchored where
            // the painted button sits so hover stays sticky.
            button.MouseEntered += () => AnimateButtonHover(button, glow, hovered: true);
            button.MouseExited += () => AnimateButtonHover(button, glow, hovered: false);
            button.ButtonDown += () => AnimateButtonPress(button, glow, pressed: true);
            button.ButtonUp += () => AnimateButtonPress(button, glow, pressed: false);

            _buttons[id] = button;
        }

        _buttons["play"].Pressed += StartGame;
        _buttons["options"].Pressed += ShowOptions;
        _buttons["credits"].Pressed += ShowCredits;
        _buttons["quit"].Pressed += QuitGame;
    }

    private static Color GlowTint(string buttonId) => buttonId switch
    {
        "play" => new Color(1.00f, 0.88f, 0.45f),   // gold (Paragon)
        "quit" => new Color(0.95f, 0.30f, 0.20f),   // red (Renegade)
        _ => new Color(0.96f, 0.88f, 0.65f)         // parchment (neutral)
    };

    private static void AnimateButtonHover(Control button, Control glow, bool hovered)
    {
        // Animate scale on the glow (visual emphasis) and alpha on the
        // glow's modulate. Button itself stays put so the click rect
        // doesn't drift out from under the cursor on hover-in.
        var tween = button.CreateTween().SetParallel(true).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(glow, "scale", hovered ? new Vector2(1.06f, 1.06f) : Vector2.One, 0.18);
        var idleAlpha = AlwaysShowHoverGlows ? 0.55f : 0f;
        tween.TweenProperty(glow, "modulate:a", hovered ? 0.65f : idleAlpha, 0.22);
    }

    private static void AnimateButtonPress(Control button, Control glow, bool pressed)
    {
        // Subtle "press" pulse on the glow only — the painted button
        // doesn't need to physically dip since it's part of the splash.
        var tween = glow.CreateTween().SetEase(Tween.EaseType.Out);
        tween.TweenProperty(glow, "scale", pressed ? new Vector2(0.98f, 0.98f) : new Vector2(1.06f, 1.06f), 0.08);
    }

    // Soft elliptical-pill glow texture. Computes one image at startup
    // and shares it across every button — colorisation is per-button
    // via Modulate.
    private static Texture2D MakePillGlow(int width, int height, float softness)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        var cx = (width - 1) * 0.5f;
        var cy = (height - 1) * 0.5f;
        // Treat the rectangle as an ellipse for radial falloff so the
        // glow has soft rounded ends (pill shape).
        var rx = width * 0.5f;
        var ry = height * 0.5f;
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var dx = (x - cx) / rx;
                var dy = (y - cy) / ry;
                var d = MathF.Sqrt(dx * dx + dy * dy);
                var alpha = MathF.Max(0f, 1f - d * softness);
                // Smoothstep the edge so it feathers nicely.
                alpha = alpha * alpha * (3f - 2f * alpha);
                image.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        return ImageTexture.CreateFromImage(image);
    }

    private static void AnchorRect(Control node, Rect2 normalized)
    {
        node.AnchorLeft = normalized.Position.X;
        node.AnchorTop = normalized.Position.Y;
        node.AnchorRight = normalized.Position.X + normalized.Size.X;
        node.AnchorBottom = normalized.Position.Y + normalized.Size.Y;
        node.OffsetLeft = 0;
        node.OffsetTop = 0;
        node.OffsetRight = 0;
        node.OffsetBottom = 0;
    }

    private void BuildOverlayLayer()
    {
        _overlayLayer = new Control
        {
            Name = "OverlayLayer",
            AnchorRight = 1f,
            AnchorBottom = 1f,
            MouseFilter = MouseFilterEnum.Pass,
            Visible = false
        };
        AddChild(_overlayLayer);

        // Dark scrim over the splash when an overlay is open.
        var scrim = new ColorRect { Color = new Color(0, 0, 0, 0.55f), Name = "Scrim" };
        scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        scrim.MouseFilter = MouseFilterEnum.Stop;
        _overlayLayer.AddChild(scrim);
    }

    private void BuildStatusBar()
    {
        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AnchorTop = 0.96f,
            AnchorBottom = 1f,
            AnchorRight = 1f,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.86f, 0.6f, 0.7f));
        _statusLabel.AddThemeFontSizeOverride("font_size", 13);
        AddChild(_statusLabel);
    }

    // ───── routing ───────────────────────────────────────────────────

    public void StartGame()
    {
        _statusLabel.Text = "Loading prototype...";
        GetTree().ChangeSceneToFile(GameplayScenePath);
    }


    public void ShowOptions()
    {
        if (!_optionsBuilt) BuildOptionsOverlay();
        _overlayLayer.Visible = true;
        _optionsOverlay.Visible = true;
        if (_creditsOverlay is not null) _creditsOverlay.Visible = false;
        RefreshDetectedResolutionLabel();
        _statusLabel.Text = "Options.";
    }

    public void ShowCredits()
    {
        if (_creditsOverlay is null) BuildCreditsOverlay();
        _overlayLayer.Visible = true;
        _creditsOverlay.Visible = true;
        if (_optionsOverlay is not null) _optionsOverlay.Visible = false;
        _statusLabel.Text = "Credits.";
    }

    public void HideOverlays()
    {
        _overlayLayer.Visible = false;
        if (_optionsOverlay is not null) _optionsOverlay.Visible = false;
        if (_creditsOverlay is not null) _creditsOverlay.Visible = false;
        _statusLabel.Text = string.Empty;
    }

    public override void _Process(double delta) { }

    public void QuitGame()
    {
        _statusLabel.Text = "Goodbye.";
        GetTree().Quit();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey k && k.Pressed && k.Keycode == Key.Escape && _overlayLayer.Visible)
        {
            HideOverlays();
            GetViewport().SetInputAsHandled();
        }
    }

    // ───── Options + Credits panels ──────────────────────────────────

    private void BuildOptionsOverlay()
    {
        _optionsOverlay = MakeParchmentPanel("OptionsOverlay", "Options");
        _overlayLayer.AddChild(_optionsOverlay);
        var content = (VBoxContainer)_optionsOverlay.GetMeta("content");

        var videoGrid = new GridContainer { Columns = 2 };
        videoGrid.AddThemeConstantOverride("h_separation", 16);
        videoGrid.AddThemeConstantOverride("v_separation", 8);
        content.AddChild(videoGrid);

        videoGrid.AddChild(new Label { Text = "Resolution" });
        _resolutionOption = new OptionButton();
        videoGrid.AddChild(_resolutionOption);

        videoGrid.AddChild(new Label { Text = "Fullscreen" });
        _fullscreenToggle = new CheckButton();
        videoGrid.AddChild(_fullscreenToggle);

        videoGrid.AddChild(new Label { Text = "VSync" });
        _vsyncToggle = new CheckButton();
        videoGrid.AddChild(_vsyncToggle);

        var detectRow = new HBoxContainer();
        var detectButton = new Button { Text = "Detect display" };
        detectButton.Pressed += DetectAndSelectCurrentResolution;
        detectRow.AddChild(detectButton);
        _detectedResolutionLabel = new Label { Text = string.Empty };
        _detectedResolutionLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.6f, 0.4f));
        detectRow.AddChild(_detectedResolutionLabel);
        content.AddChild(detectRow);

        content.AddChild(MakeDivider());

        var audioGrid = new GridContainer { Columns = 3 };
        audioGrid.AddThemeConstantOverride("h_separation", 16);
        audioGrid.AddThemeConstantOverride("v_separation", 8);
        content.AddChild(audioGrid);

        (_masterVolumeSlider, _masterVolumeLabel) = AddVolumeRow(audioGrid, "Master");
        (_musicVolumeSlider, _musicVolumeLabel) = AddVolumeRow(audioGrid, "Music");
        (_effectsVolumeSlider, _effectsVolumeLabel) = AddVolumeRow(audioGrid, "Effects");
        (_ambientVolumeSlider, _ambientVolumeLabel) = AddVolumeRow(audioGrid, "Ambient");

        foreach (var slider in new[] { _masterVolumeSlider, _musicVolumeSlider, _effectsVolumeSlider, _ambientVolumeSlider })
        {
            slider.ValueChanged += _ =>
            {
                RefreshVolumeLabels();
                PushSlidersToAudioServer();
            };
        }

        content.AddChild(MakeDivider());

        var actions = new HBoxContainer();
        actions.AddThemeConstantOverride("separation", 12);
        var apply = new Button { Text = "Apply" };
        apply.Pressed += ApplyAndSaveOptions;
        actions.AddChild(apply);
        var close = new Button { Text = "Close (Esc)" };
        close.Pressed += HideOverlays;
        actions.AddChild(close);
        content.AddChild(actions);

        PopulateResolutions();
        LoadOptions();
        RefreshVolumeLabels();
        PushSlidersToAudioServer();
        _optionsBuilt = true;
    }

    private static (HSlider slider, Label label) AddVolumeRow(GridContainer grid, string name)
    {
        grid.AddChild(new Label { Text = name });
        var slider = new HSlider
        {
            MinValue = 0, MaxValue = 100, Step = 1, Value = 80,
            CustomMinimumSize = new Vector2(220, 24),
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        grid.AddChild(slider);
        var label = new Label { Text = "80%", CustomMinimumSize = new Vector2(56, 0) };
        grid.AddChild(label);
        return (slider, label);
    }

    private static Control MakeDivider()
    {
        var sep = new ColorRect { Color = new Color(0.6f, 0.5f, 0.3f, 0.4f), CustomMinimumSize = new Vector2(0, 1) };
        return sep;
    }

    private void BuildCreditsOverlay()
    {
        _creditsOverlay = MakeParchmentPanel("CreditsOverlay", "Credits");
        _overlayLayer.AddChild(_creditsOverlay);
        var content = (VBoxContainer)_creditsOverlay.GetMeta("content");

        foreach (var line in new[]
        {
            "KARMA — pixel-art prototype",
            "",
            "Built on Godot 4 .NET",
            "Art: PixelLab + Liberated Pixel Cup + Cainos top-down basics",
            "Music: Pixabay-licensed medieval tracks",
            "Code: Sean Pharris",
            "",
            "Press Esc to return."
        })
        {
            var label = new Label { Text = line, HorizontalAlignment = HorizontalAlignment.Center };
            label.AddThemeFontSizeOverride("font_size", 16);
            content.AddChild(label);
        }

        var close = new Button { Text = "Close (Esc)" };
        close.Pressed += HideOverlays;
        content.AddChild(close);
    }

    private static PanelContainer MakeParchmentPanel(string name, string title)
    {
        var panel = new PanelContainer
        {
            Name = name,
            AnchorLeft = 0.5f, AnchorTop = 0.5f, AnchorRight = 0.5f, AnchorBottom = 0.5f,
            OffsetLeft = -360, OffsetRight = 360, OffsetTop = -260, OffsetBottom = 260,
            Visible = false
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.93f, 0.86f, 0.7f, 0.96f),
            BorderColor = new Color(0.45f, 0.32f, 0.18f),
            BorderWidthLeft = 3, BorderWidthRight = 3, BorderWidthTop = 3, BorderWidthBottom = 3,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 24, ContentMarginRight = 24, ContentMarginTop = 18, ContentMarginBottom = 18
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var content = new VBoxContainer { Name = "Content" };
        content.AddThemeConstantOverride("separation", 10);
        panel.AddChild(content);

        var heading = new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center };
        heading.AddThemeFontSizeOverride("font_size", 24);
        heading.AddThemeColorOverride("font_color", new Color(0.32f, 0.18f, 0.08f));
        content.AddChild(heading);

        panel.SetMeta("content", content);
        return panel;
    }

    // ───── audio + options helpers (preserved from prior controller) ─

    public static AudioStream LoadMenuThemeStream()
    {
        return PrototypeMusicPlayer.LoadPlayableAudio(MenuThemePath);
    }

    private void SetupMenuTheme()
    {
        var menuTheme = LoadMenuThemeStream();
        if (menuTheme is null) return;
        if (menuTheme is AudioStreamMP3 mp3) mp3.Loop = true;
        _menuThemePlayer.Stream = menuTheme;
        _menuThemePlayer.Play();
    }

    private void ApplyMenuThemeVolume()
    {
        // Player rides the Music bus; AudioSettings handles per-bus
        // gain so menu and gameplay match.
        _menuThemePlayer.VolumeDb = 0f;
    }

    private void PopulateResolutions()
    {
        _resolutionOption.Clear();
        var resolutions = new List<Vector2I>(CommonResolutions);
        var detected = GetDetectedResolution();
        if (!resolutions.Contains(detected)) resolutions.Add(detected);
        resolutions.Sort((a, b) => a.X == b.X ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
        foreach (var res in resolutions) _resolutionOption.AddItem(FormatResolution(res));
    }

    private void LoadOptions()
    {
        var config = new ConfigFile();
        var loaded = config.Load(OptionsPath) == Error.Ok;
        var current = GetWindow().Size;
        var resText = loaded
            ? config.GetValue("video", "resolution", FormatResolution(current)).AsString()
            : FormatResolution(current);
        SelectResolution(resText);
        _fullscreenToggle.ButtonPressed = loaded && config.GetValue("video", "fullscreen", false).AsBool();
        _vsyncToggle.ButtonPressed = !loaded || config.GetValue("video", "vsync", true).AsBool();
        if (loaded) AudioSettings.LoadFromConfig(config);
        _masterVolumeSlider.Value = AudioSettings.MasterVolume;
        _musicVolumeSlider.Value = AudioSettings.MusicVolume;
        _effectsVolumeSlider.Value = AudioSettings.SfxVolume;
        _ambientVolumeSlider.Value = AudioSettings.AmbientVolume;
    }

    private void ApplyAndSaveOptions()
    {
        var resolution = ParseResolution(_resolutionOption.GetItemText(_resolutionOption.Selected));
        if (resolution.X > 0 && resolution.Y > 0) GetWindow().Size = resolution;
        GetWindow().Mode = _fullscreenToggle.ButtonPressed ? Window.ModeEnum.Fullscreen : Window.ModeEnum.Windowed;
        DisplayServer.WindowSetVsyncMode(_vsyncToggle.ButtonPressed
            ? DisplayServer.VSyncMode.Enabled
            : DisplayServer.VSyncMode.Disabled);
        SaveOptions(resolution);
        RefreshDetectedResolutionLabel();
        _statusLabel.Text = $"Options applied: {FormatResolution(resolution)}, {(_fullscreenToggle.ButtonPressed ? "fullscreen" : "windowed")}.";
    }

    private void SaveOptions(Vector2I resolution)
    {
        var config = new ConfigFile();
        config.Load(OptionsPath);
        config.SetValue("video", "resolution", FormatResolution(resolution));
        config.SetValue("video", "fullscreen", _fullscreenToggle.ButtonPressed);
        config.SetValue("video", "vsync", _vsyncToggle.ButtonPressed);
        SyncSlidersToAudioSettings();
        AudioSettings.SaveToConfig(config);
        config.Save(OptionsPath);
    }

    private void SyncSlidersToAudioSettings()
    {
        AudioSettings.MasterVolume = _masterVolumeSlider.Value;
        AudioSettings.MusicVolume = _musicVolumeSlider.Value;
        AudioSettings.SfxVolume = _effectsVolumeSlider.Value;
        AudioSettings.AmbientVolume = _ambientVolumeSlider.Value;
    }

    private void PushSlidersToAudioServer()
    {
        if (_masterVolumeSlider is null) return;
        SyncSlidersToAudioSettings();
        AudioSettings.ApplyToAudioServer();
    }

    private void DetectAndSelectCurrentResolution()
    {
        var detected = GetDetectedResolution();
        SelectResolution(FormatResolution(detected));
        RefreshDetectedResolutionLabel();
        _statusLabel.Text = $"Detected: {FormatResolution(detected)}.";
    }

    private void SelectResolution(string text)
    {
        for (var i = 0; i < _resolutionOption.ItemCount; i++)
        {
            if (_resolutionOption.GetItemText(i) == text)
            {
                _resolutionOption.Select(i);
                return;
            }
        }
        _resolutionOption.Select(0);
    }

    private void RefreshDetectedResolutionLabel()
    {
        if (_detectedResolutionLabel is null) return;
        _detectedResolutionLabel.Text = $"Detected: {FormatResolution(GetDetectedResolution())}  •  Window: {FormatResolution(GetWindow().Size)}";
    }

    private void RefreshVolumeLabels()
    {
        if (_masterVolumeLabel is null) return;
        _masterVolumeLabel.Text = $"{Math.Round(_masterVolumeSlider.Value)}%";
        _musicVolumeLabel.Text = $"{Math.Round(_musicVolumeSlider.Value)}%";
        _effectsVolumeLabel.Text = $"{Math.Round(_effectsVolumeSlider.Value)}%";
        _ambientVolumeLabel.Text = $"{Math.Round(_ambientVolumeSlider.Value)}%";
    }

    private static Vector2I GetDetectedResolution() =>
        DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());

    private static string FormatResolution(Vector2I resolution) =>
        $"{resolution.X} x {resolution.Y}";

    private static Vector2I ParseResolution(string value)
    {
        var parts = value.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height)
            ? new Vector2I(width, height)
            : Vector2I.Zero;
    }
}
