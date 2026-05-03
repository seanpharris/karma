using System;
using System.Collections.Generic;
using Godot;
using Karma.Audio;

namespace Karma.UI;

public partial class MainMenuController : Control
{
    public const string GameplayScenePath = "res://scenes/Main.tscn";
    public const string EventPrototypeScenePath = "res://scenes/InGameEventPrototype.tscn";
    public const string OptionsPath = "user://options.cfg";
    public const string MenuThemePath = PrototypeMusicPlayer.MusicDirectory + PrototypeMusicPlayer.TravellingOnMedievalFileName;

    private static readonly Vector2I[] CommonResolutions =
    {
        new(1280, 720),
        new(1366, 768),
        new(1600, 900),
        new(1920, 1080),
        new(2560, 1440),
        new(3840, 2160)
    };

    private Button _startButton;
    private Button _eventPrototypeButton;
    private Button _optionsButton;
    private Button _creditsButton;
    private Button _quitButton;
    private Button _applyOptionsButton;
    private Button _detectResolutionButton;
    private Button _closeOptionsButton;
    private Button _closeCreditsButton;
    private CheckButton _fullscreenToggle;
    private CheckButton _vsyncToggle;
    private OptionButton _resolutionOption;
    private HSlider _masterVolumeSlider;
    private HSlider _musicVolumeSlider;
    private HSlider _effectsVolumeSlider;
    private HSlider _ambientVolumeSlider;
    private Label _detectedResolutionLabel;
    private Label _masterVolumeLabel;
    private Label _musicVolumeLabel;
    private Label _effectsVolumeLabel;
    private Label _ambientVolumeLabel;
    private AudioStreamPlayer _menuThemePlayer;
    private AudioStreamGeneratorPlayback _themePlayback;
    private double _themeTime;
    private Control _optionsPanel;
    private Control _creditsPanel;
    private Label _statusLabel;

    public override void _Ready()
    {
        _startButton = GetNode<Button>("Root/MenuPanel/MenuMargin/MenuButtons/StartButton");
        _eventPrototypeButton = GetNode<Button>("Root/MenuPanel/MenuMargin/MenuButtons/EventPrototypeButton");
        _optionsButton = GetNode<Button>("Root/MenuPanel/MenuMargin/MenuButtons/OptionsButton");
        _creditsButton = GetNode<Button>("Root/MenuPanel/MenuMargin/MenuButtons/CreditsButton");
        _quitButton = GetNode<Button>("Root/MenuPanel/MenuMargin/MenuButtons/QuitButton");
        _optionsPanel = GetNode<Control>("Root/OptionsPanel");
        _creditsPanel = GetNode<Control>("Root/CreditsPanel");
        _closeOptionsButton = GetNode<Button>("Root/OptionsPanel/PanelMargin/OptionsContent/OptionsActions/CloseOptionsButton");
        _closeCreditsButton = GetNode<Button>("Root/CreditsPanel/PanelMargin/CreditsContent/CloseCreditsButton");
        _applyOptionsButton = GetNode<Button>("Root/OptionsPanel/PanelMargin/OptionsContent/OptionsActions/ApplyOptionsButton");
        _detectResolutionButton = GetNode<Button>("Root/OptionsPanel/PanelMargin/OptionsContent/VideoGrid/DetectResolutionButton");
        _fullscreenToggle = GetNode<CheckButton>("Root/OptionsPanel/PanelMargin/OptionsContent/VideoGrid/FullscreenToggle");
        _vsyncToggle = GetNode<CheckButton>("Root/OptionsPanel/PanelMargin/OptionsContent/VideoGrid/VsyncToggle");
        _resolutionOption = GetNode<OptionButton>("Root/OptionsPanel/PanelMargin/OptionsContent/VideoGrid/ResolutionOption");
        _masterVolumeSlider = GetNode<HSlider>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/MasterVolumeSlider");
        _musicVolumeSlider = GetNode<HSlider>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/MusicVolumeSlider");
        _effectsVolumeSlider = GetNode<HSlider>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/EffectsVolumeSlider");
        _ambientVolumeSlider = GetNode<HSlider>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/AmbientVolumeSlider");
        _detectedResolutionLabel = GetNode<Label>("Root/OptionsPanel/PanelMargin/OptionsContent/DetectedResolutionLabel");
        _masterVolumeLabel = GetNode<Label>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/MasterVolumeValue");
        _musicVolumeLabel = GetNode<Label>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/MusicVolumeValue");
        _effectsVolumeLabel = GetNode<Label>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/EffectsVolumeValue");
        _ambientVolumeLabel = GetNode<Label>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/AmbientVolumeValue");
        _menuThemePlayer = GetNode<AudioStreamPlayer>("MenuThemePlayer");
        _statusLabel = GetNode<Label>("Root/MenuPanel/MenuMargin/MenuButtons/StatusLabel");

        // Add runtime "View Assets" + "Building Showcase" buttons
        // injected just above the status label.
        var menuButtonsParent = _statusLabel.GetParent() as Container;
        if (menuButtonsParent is not null)
        {
            var showcaseButton = new Button { Text = "Building Showcase" };
            showcaseButton.Pressed += ShowBuildingShowcase;
            menuButtonsParent.AddChild(showcaseButton);
            menuButtonsParent.MoveChild(showcaseButton, _statusLabel.GetIndex());

            var galleryButton = new Button { Text = "View Assets" };
            galleryButton.Pressed += ShowAssetGallery;
            menuButtonsParent.AddChild(galleryButton);
            menuButtonsParent.MoveChild(galleryButton, _statusLabel.GetIndex());
        }

        _startButton.Pressed += StartGame;
        _eventPrototypeButton.Pressed += StartEventPrototypes;
        _optionsButton.Pressed += ShowOptions;
        _creditsButton.Pressed += ShowCredits;
        _quitButton.Pressed += QuitGame;
        _applyOptionsButton.Pressed += ApplyAndSaveOptions;
        _detectResolutionButton.Pressed += DetectAndSelectCurrentResolution;
        _closeOptionsButton.Pressed += HidePanels;
        _closeCreditsButton.Pressed += HidePanels;
        _masterVolumeSlider.ValueChanged += _ => RefreshVolumeLabels();
        _musicVolumeSlider.ValueChanged += _ => RefreshVolumeLabels();
        _effectsVolumeSlider.ValueChanged += _ => RefreshVolumeLabels();
        _ambientVolumeSlider.ValueChanged += _ => RefreshVolumeLabels();
        _musicVolumeSlider.ValueChanged += _ => ApplyMenuThemeVolume();
        _masterVolumeSlider.ValueChanged += _ => ApplyMenuThemeVolume();
        // Live-apply each slider edit through the central AudioSettings so
        // the change is audible immediately and persists into options.cfg.
        _masterVolumeSlider.ValueChanged += _ => PushSlidersToAudioServer();
        _musicVolumeSlider.ValueChanged += _ => PushSlidersToAudioServer();
        _effectsVolumeSlider.ValueChanged += _ => PushSlidersToAudioServer();
        _ambientVolumeSlider.ValueChanged += _ => PushSlidersToAudioServer();
        AudioSettings.EnsureBusesExist();
        HudController.ApplyUiPalette(this, UiPaletteRegistry.MedievalThemeId);
        SetupMenuTheme();
        PopulateResolutions();
        LoadOptions();
        RefreshDetectedResolutionLabel();
        RefreshVolumeLabels();
        PushSlidersToAudioServer();
        ApplyMenuThemeVolume();
        HidePanels();
        _statusLabel.Text = "Prototype entry point: local sandbox match.";
    }

    public void StartGame()
    {
        _statusLabel.Text = "Starting local prototype...";
        GetTree().ChangeSceneToFile(GameplayScenePath);
    }

    public void StartEventPrototypes()
    {
        _statusLabel.Text = "Opening playable event prototypes...";
        GetTree().ChangeSceneToFile(EventPrototypeScenePath);
    }

    public void ShowOptions()
    {
        _creditsPanel.Visible = false;
        _optionsPanel.Visible = true;
        RefreshDetectedResolutionLabel();
        _statusLabel.Text = "Options prototype: video, audio, controls, and accessibility placeholders.";
    }

    public void ShowCredits()
    {
        _optionsPanel.Visible = false;
        _creditsPanel.Visible = true;
        _statusLabel.Text = "Karma prototype credits.";
    }

    public void HidePanels()
    {
        _optionsPanel.Visible = false;
        _creditsPanel.Visible = false;
    }

    public void ShowAssetGallery()
    {
        _statusLabel.Text = "Asset gallery: scanning assets/art/themes/medieval/...";
        AssetGalleryOverlay.Mount(this);
    }

    public void ShowBuildingShowcase()
    {
        _statusLabel.Text = "Loading Building Showcase scene...";
        var tree = GetTree();
        var showcaseRoot = new BuildingShowcaseScene { Name = "BuildingShowcase" };
        // Add the showcase as a sibling of the main-menu scene under
        // the SceneTree root, then free the current scene. The
        // showcase's _Ready / _Process / _Input fire on the next frame.
        tree.Root.AddChild(showcaseRoot);
        tree.CurrentScene?.QueueFree();
        tree.CurrentScene = showcaseRoot;
    }

    public override void _Process(double delta)
    {
        FillMenuThemeBuffer();
    }

    public void QuitGame()
    {
        _statusLabel.Text = "Quitting Karma.";
        GetTree().Quit();
    }

    private void SetupMenuTheme()
    {
        _menuThemePlayer.Bus = AudioSettings.MusicBusName;
        var menuTheme = LoadMenuThemeStream();
        if (menuTheme is not null)
        {
            if (menuTheme is AudioStreamMP3 mp3)
            {
                mp3.Loop = true;
            }

            _menuThemePlayer.Stream = menuTheme;
            _menuThemePlayer.Play();
            _themePlayback = null;
            return;
        }

        _menuThemePlayer.Stream = new AudioStreamGenerator
        {
            MixRate = 44100,
            BufferLength = 2.0f
        };
        _menuThemePlayer.Play();
        _themePlayback = _menuThemePlayer.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        FillMenuThemeBuffer();
    }

    public static AudioStream LoadMenuThemeStream()
    {
        return PrototypeMusicPlayer.LoadPlayableAudio(MenuThemePath);
    }

    private void FillMenuThemeBuffer()
    {
        if (_themePlayback is null)
        {
            return;
        }

        const double sampleRate = 44100.0;
        var frames = _themePlayback.GetFramesAvailable();
        for (var i = 0; i < frames; i++)
        {
            var sample = GenerateMenuThemeSample(_themeTime);
            _themePlayback.PushFrame(new Vector2(sample, sample));
            _themeTime += 1.0 / sampleRate;
        }
    }

    private static float GenerateMenuThemeSample(double time)
    {
        const double bpm = 112.0;
        const int beats = 16;
        var beatLength = 60.0 / bpm;
        var beat = (int)(time / beatLength) % beats;
        var within = time - Math.Floor(time / beatLength) * beatLength;
        int[] melody = { 0, 3, 5, 7, 5, 3, 10, 7, 0, 5, 7, 12, 10, 7, 5, 3 };
        int[] bass = { 0, 0, 7, 7, 10, 10, 5, 5, 0, 0, 7, 7, 10, 10, 5, 5 };
        var lead = Triangle(NoteFrequency(220.0, melody[beat] + 12) * time) * Envelope(within, beatLength * 0.82) * 0.34;
        var bassWave = Math.Sin(2.0 * Math.PI * NoteFrequency(220.0, bass[beat] - 12) * time) * Envelope(within, beatLength * 0.95) * 0.23;
        var tick = within < 0.018
            ? Math.Sin(2.0 * Math.PI * 1800.0 * time) * (1.0 - within / 0.018) * 0.07
            : 0.0;
        return (float)Math.Clamp(lead + bassWave + tick, -1.0, 1.0);
    }

    private static double NoteFrequency(double root, int semitone)
    {
        return root * Math.Pow(2.0, semitone / 12.0);
    }

    private static double Triangle(double phase)
    {
        return 2.0 * Math.Abs(2.0 * (phase - Math.Floor(phase + 0.5))) - 1.0;
    }

    private static double Envelope(double time, double duration)
    {
        var attack = Math.Min(1.0, time / 0.02);
        var release = Math.Min(1.0, Math.Max(0.0, (duration - time) / 0.08));
        return Math.Min(attack, release);
    }

    private void PopulateResolutions()
    {
        _resolutionOption.Clear();
        var resolutions = new List<Vector2I>(CommonResolutions);
        var detected = GetDetectedResolution();
        if (!resolutions.Contains(detected))
        {
            resolutions.Add(detected);
        }

        resolutions.Sort((left, right) => left.X == right.X ? left.Y.CompareTo(right.Y) : left.X.CompareTo(right.X));
        foreach (var resolution in resolutions)
        {
            _resolutionOption.AddItem(FormatResolution(resolution));
        }
    }

    private void LoadOptions()
    {
        var config = new ConfigFile();
        var loaded = config.Load(OptionsPath) == Error.Ok;
        var currentResolution = GetWindow().Size;
        var resolutionText = loaded
            ? config.GetValue("video", "resolution", FormatResolution(currentResolution)).AsString()
            : FormatResolution(currentResolution);

        SelectResolution(resolutionText);
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
        if (resolution.X > 0 && resolution.Y > 0)
        {
            GetWindow().Size = resolution;
        }

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
        // Preserve any other sections that may exist on disk.
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

    private void ApplyMenuThemeVolume()
    {
        // The player sits on the Music bus; AudioSettings applies Master and
        // Music slider gain centrally so menu and gameplay music match.
        _menuThemePlayer.VolumeDb = 0f;
    }

    private void DetectAndSelectCurrentResolution()
    {
        var detected = GetDetectedResolution();
        var detectedText = FormatResolution(detected);
        SelectResolution(detectedText);
        RefreshDetectedResolutionLabel();
        _statusLabel.Text = $"Detected display resolution: {detectedText}.";
    }

    private void SelectResolution(string resolutionText)
    {
        for (var i = 0; i < _resolutionOption.ItemCount; i++)
        {
            if (_resolutionOption.GetItemText(i) == resolutionText)
            {
                _resolutionOption.Select(i);
                return;
            }
        }

        _resolutionOption.Select(0);
    }

    private void RefreshDetectedResolutionLabel()
    {
        _detectedResolutionLabel.Text = $"Detected display: {FormatResolution(GetDetectedResolution())} | Current window: {FormatResolution(GetWindow().Size)}";
    }

    private void RefreshVolumeLabels()
    {
        _masterVolumeLabel.Text = $"{Math.Round(_masterVolumeSlider.Value)}%";
        _musicVolumeLabel.Text = $"{Math.Round(_musicVolumeSlider.Value)}%";
        _effectsVolumeLabel.Text = $"{Math.Round(_effectsVolumeSlider.Value)}%";
        _ambientVolumeLabel.Text = $"{Math.Round(_ambientVolumeSlider.Value)}%";
    }

    private static Vector2I GetDetectedResolution()
    {
        return DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());
    }

    private static string FormatResolution(Vector2I resolution)
    {
        return $"{resolution.X} x {resolution.Y}";
    }

    private static Vector2I ParseResolution(string value)
    {
        var parts = value.Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height)
            ? new Vector2I(width, height)
            : Vector2I.Zero;
    }
}
