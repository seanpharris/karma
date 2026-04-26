using Godot;

namespace Karma.UI;

public partial class MainMenuController : Control
{
    public const string GameplayScenePath = "res://scenes/Main.tscn";

    private Button _startButton;
    private Button _optionsButton;
    private Button _creditsButton;
    private Button _quitButton;
    private Button _closeOptionsButton;
    private Button _closeCreditsButton;
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
        _closeOptionsButton = GetNode<Button>("Root/OptionsPanel/PanelMargin/OptionsContent/CloseOptionsButton");
        _closeCreditsButton = GetNode<Button>("Root/CreditsPanel/PanelMargin/CreditsContent/CloseCreditsButton");
        _statusLabel = GetNode<Label>("Root/MenuPanel/MenuMargin/MenuButtons/StatusLabel");

        _startButton.Pressed += StartGame;
        _optionsButton.Pressed += ShowOptions;
        _creditsButton.Pressed += ShowCredits;
        _quitButton.Pressed += QuitGame;
        _closeOptionsButton.Pressed += HidePanels;
        _closeCreditsButton.Pressed += HidePanels;

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
        _statusLabel.Text = "Options are prototype-only for now.";
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
}
