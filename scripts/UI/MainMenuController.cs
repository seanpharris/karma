using System;
using System.Collections.Generic;
using Godot;

namespace Karma.UI;

public partial class MainMenuController : Control
{
    public const string GameplayScenePath = "res://scenes/Main.tscn";
    private const string OptionsPath = "user://options.cfg";

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
    private Label _detectedResolutionLabel;
    private Label _masterVolumeLabel;
    private Label _musicVolumeLabel;
    private Label _effectsVolumeLabel;
    private Control _optionsPanel;
    private Control _creditsPanel;
    private Label _statusLabel;

    public override void _Ready()
    {
        _startButton = GetNode<Button>("Root/MenuPanel/MenuMargin/MenuButtons/StartButton");
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
        _detectedResolutionLabel = GetNode<Label>("Root/OptionsPanel/PanelMargin/OptionsContent/DetectedResolutionLabel");
        _masterVolumeLabel = GetNode<Label>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/MasterVolumeValue");
        _musicVolumeLabel = GetNode<Label>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/MusicVolumeValue");
        _effectsVolumeLabel = GetNode<Label>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/EffectsVolumeValue");
        _statusLabel = GetNode<Label>("Root/MenuPanel/MenuMargin/MenuButtons/StatusLabel");

        _startButton.Pressed += StartGame;
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

        PopulateResolutions();
        LoadOptions();
        RefreshDetectedResolutionLabel();
        RefreshVolumeLabels();
        HidePanels();
        _statusLabel.Text = "Prototype entry point: local sandbox match.";
    }

    public void StartGame()
    {
        _statusLabel.Text = "Starting local prototype...";
        GetTree().ChangeSceneToFile(GameplayScenePath);
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

    public void QuitGame()
    {
        _statusLabel.Text = "Quitting Karma.";
        GetTree().Quit();
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
        _masterVolumeSlider.Value = loaded ? config.GetValue("audio", "master_volume", 80).AsDouble() : 80;
        _musicVolumeSlider.Value = loaded ? config.GetValue("audio", "music_volume", 70).AsDouble() : 70;
        _effectsVolumeSlider.Value = loaded ? config.GetValue("audio", "effects_volume", 80).AsDouble() : 80;
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
        config.SetValue("video", "resolution", FormatResolution(resolution));
        config.SetValue("video", "fullscreen", _fullscreenToggle.ButtonPressed);
        config.SetValue("video", "vsync", _vsyncToggle.ButtonPressed);
        config.SetValue("audio", "master_volume", _masterVolumeSlider.Value);
        config.SetValue("audio", "music_volume", _musicVolumeSlider.Value);
        config.SetValue("audio", "effects_volume", _effectsVolumeSlider.Value);
        config.Save(OptionsPath);
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
