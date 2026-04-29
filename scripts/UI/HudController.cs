using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Core;
using Karma.Data;
using Karma.Net;
using Karma.Player;

namespace Karma.UI;

public partial class HudController : CanvasLayer
{
    public const string MainMenuScenePath = "res://scenes/MainMenu.tscn";
    private GameState _gameState = null!;
    private Label _karmaLabel = new();
    private Label _eventLabel = new();
    private Label _chatLabel = new();
    private PanelContainer _chatInputPanel = new();
    private LineEdit _chatInput = new();
    private Label _staminaLabel = new();
    private Label _healthLabel = new();
    private ProgressBar _healthBar = new();
    private Label _inventoryLabel = new();
    private Label _leaderboardLabel = new();
    private Label _perksLabel = new();
    private Label _relationshipsLabel = new();
    private Label _factionsLabel = new();
    private Label _questsLabel = new();
    private Label _combatLabel = new();
    private Label _entanglementsLabel = new();
    private Label _duelsLabel = new();
    private Label _worldEventsLabel = new();
    private Label _matchLabel = new();
    private Label _syncLabel = new();
    private Label _perfLabel = new();
    private PrototypeServerSession _serverSession;
    private double _perfAccumulator;
    private int _perfFrameCount;
    private int _lastSnapshotCount;
    private PanelContainer _promptPanel = new();
    private Label _promptLabel = new();
    private PanelContainer _inventoryPanel = new();
    private Label _inventoryOverlayLabel = new();
    private PanelContainer _developerPanel = new();
    private Label _developerOverlayLabel = new();
    private int _developerPageIndex;
    private ClientInterestSnapshot _lastSnapshot;
    private PanelContainer _escapeMenuPanel = new();
    private Control _escapeOptionsPanel = new();
    private Label _escapeMenuStatusLabel = new();
    private Button _resumeButton = new();
    private Button _escapeOptionsButton = new();
    private Button _appearanceButton = new();
    private Button _backToMenuButton = new();
    private Button _quitButton = new();
    private Button _closeEscapeOptionsButton = new();
    private Control _appearancePanel = new();
    private Label _appearanceSummaryLabel = new();
    private Label _appearanceSkinLabel = new();
    private Label _appearanceHairLabel = new();
    private Label _appearanceOutfitLabel = new();
    private Label _appearanceToolLabel = new();
    private Label _appearancePreviewLabel = new();
    private Button _cycleSkinButton = new();
    private Button _cycleHairButton = new();
    private Button _cycleOutfitButton = new();
    private Button _closeAppearanceButton = new();
    private Label _posseLabel = new();
    private PanelContainer _possePanel = new();
    private string _lastCombatText = "Combat: none";
    private IReadOnlyList<string> _lastStatusEffects = System.Array.Empty<string>();

    public override void _Ready()
    {
        BuildUi();

        _gameState = GetNode<GameState>("/root/GameState");
        _gameState.KarmaChanged += OnKarmaChanged;
        _gameState.KarmaEvent += OnKarmaEvent;
        _gameState.InventoryChanged += OnInventoryChanged;
        _gameState.LeaderboardChanged += OnLeaderboardChanged;
        _gameState.PerksChanged += OnPerksChanged;
        _gameState.RelationshipsChanged += OnRelationshipsChanged;
        _gameState.FactionsChanged += OnFactionsChanged;
        _gameState.QuestsChanged += OnQuestsChanged;
        _gameState.CombatChanged += OnCombatChanged;
        _gameState.EntanglementsChanged += OnEntanglementsChanged;
        _gameState.DuelsChanged += OnDuelsChanged;
        _gameState.WorldEventsChanged += OnWorldEventsChanged;
        _resumeButton.Pressed += HideEscapeMenu;
        _escapeOptionsButton.Pressed += ShowEscapeOptions;
        _appearanceButton.Pressed += ShowAppearancePanel;
        _cycleSkinButton.Pressed += () => CycleAppearanceLayer("skin");
        _cycleHairButton.Pressed += () => CycleAppearanceLayer("hair");
        _cycleOutfitButton.Pressed += () => CycleAppearanceLayer("outfit");
        _closeAppearanceButton.Pressed += HideAppearancePanel;
        _backToMenuButton.Pressed += ReturnToMainMenu;
        _quitButton.Pressed += () => GetTree().Quit();
        _closeEscapeOptionsButton.Pressed += HideEscapeOptions;
        _chatInput.TextSubmitted += OnChatInputSubmitted;
        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        if (_serverSession is not null)
        {
            _serverSession.LocalSnapshotChanged += OnLocalSnapshotChanged;
            OnLocalSnapshotChanged(_serverSession.LastLocalSnapshot?.Summary ?? "Sync: waiting");
        }

        var pathName = _gameState.LocalKarma.Path == KarmaDirection.Neutral
            ? "Unmarked"
            : _gameState.LocalKarma.Path.ToString();
        OnKarmaChanged(_gameState.LocalKarma.Score, _gameState.LocalKarma.TierName, pathName);
        SetHealth(_gameState.LocalPlayer.Health, _gameState.LocalPlayer.MaxHealth);
        OnInventoryChanged(_gameState.Inventory.Count == 0 ? "Inventory: empty" : "Inventory: loaded");
        OnLeaderboardChanged(_gameState.GetLeaderboardStanding().Summary);
        OnPerksChanged(PerkCatalog.Format(_gameState.LocalPerks));
        OnRelationshipsChanged("Mara: Neutral (0)");
        OnFactionsChanged("Free Settlers Rep: 0");
        OnQuestsChanged(_gameState.Quests.FormatActiveSummary());
        OnCombatChanged("Combat: none");
        OnEntanglementsChanged(_gameState.Entanglements.FormatSummary());
        OnDuelsChanged(_gameState.Duels.FormatSummary());
        OnWorldEventsChanged(_gameState.WorldEvents.FormatLatestSummary());
    }

    public override void _ExitTree()
    {
        if (_gameState is not null)
        {
            _gameState.KarmaChanged -= OnKarmaChanged;
            _gameState.KarmaEvent -= OnKarmaEvent;
            _gameState.InventoryChanged -= OnInventoryChanged;
            _gameState.LeaderboardChanged -= OnLeaderboardChanged;
            _gameState.PerksChanged -= OnPerksChanged;
            _gameState.RelationshipsChanged -= OnRelationshipsChanged;
            _gameState.FactionsChanged -= OnFactionsChanged;
            _gameState.QuestsChanged -= OnQuestsChanged;
            _gameState.CombatChanged -= OnCombatChanged;
            _gameState.EntanglementsChanged -= OnEntanglementsChanged;
            _gameState.DuelsChanged -= OnDuelsChanged;
            _gameState.WorldEventsChanged -= OnWorldEventsChanged;
        }

        if (_serverSession is not null)
        {
            _serverSession.LocalSnapshotChanged -= OnLocalSnapshotChanged;
        }
    }

    public override void _Process(double delta)
    {
        _perfAccumulator += delta;
        _perfFrameCount++;
        if (_perfAccumulator < 0.5)
        {
            return;
        }

        var fps = _perfFrameCount / _perfAccumulator;
        var snapshotCount = _serverSession?.SnapshotsRefreshed ?? 0;
        var snapshotsPerSecond = (snapshotCount - _lastSnapshotCount) / _perfAccumulator;
        _lastSnapshotCount = snapshotCount;
        _perfLabel.Text = $"Perf: {fps:0} FPS | snapshots {snapshotsPerSecond:0.0}/s | visible chunks {_serverSession?.LastLocalSnapshot?.MapChunks.Count ?? 0}";
        if (_developerPanel.Visible)
        {
            RefreshDeveloperOverlay();
        }
        _perfAccumulator = 0;
        _perfFrameCount = 0;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key)
        {
            return;
        }

        if (_chatInputPanel.Visible)
        {
            if (key.Keycode == Key.Escape)
            {
                CloseLocalChatInput();
                GetViewport().SetInputAsHandled();
            }

            return;
        }

        if (key.Keycode == Key.Escape)
        {
            ToggleEscapeMenu();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.Slash || key.Keycode == Key.T)
        {
            OpenLocalChatInput();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.Quoteleft || key.Keycode == Key.Asciitilde)
        {
            ToggleDeveloperOverlay();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_developerPanel.Visible && key.Keycode == Key.Tab)
        {
            CycleDeveloperOverlayPage(key.ShiftPressed ? -1 : 1);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.I)
        {
            ToggleInventoryOverlay();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.P)
        {
            TogglePossePanel();
            GetViewport().SetInputAsHandled();
        }
    }

    public void ShowPrompt(string text)
    {
        _promptLabel.Text = text;
        _promptPanel.Visible = true;
    }

    public void HidePrompt()
    {
        _promptPanel.Visible = false;
    }

    public void ToggleInventoryOverlay()
    {
        SetInventoryOverlayVisible(!_inventoryPanel.Visible);
    }

    public void SetInventoryOverlayVisible(bool visible)
    {
        _inventoryPanel.Visible = visible;
        if (visible)
        {
            RefreshInventoryOverlay();
        }
    }

    public void TogglePossePanel()
    {
        SetPossePanelVisible(!_possePanel.Visible);
    }

    public void SetPossePanelVisible(bool visible)
    {
        _possePanel.Visible = visible;
        if (visible)
        {
            RefreshPossePanel();
        }
    }

    private void RefreshPossePanel()
    {
        if (_lastSnapshot is null)
        {
            _posseLabel.Text = "Posse: no snapshot";
            return;
        }

        _posseLabel.Text = FormatPossePanel(_lastSnapshot.Players, _lastSnapshot.PlayerId);
    }

    public void ShowStamina(string staminaText)
    {
        _staminaLabel.Text = staminaText;
    }

    public void OpenLocalChatInput()
    {
        _chatInputPanel.Visible = true;
        _chatInput.Text = string.Empty;
        _chatInput.PlaceholderText = "Local chat — Enter to send, Esc to cancel";
        _chatInput.GrabFocus();
    }

    public void CloseLocalChatInput()
    {
        _chatInputPanel.Visible = false;
        _chatInput.Text = string.Empty;
        if (GetViewport().GuiGetFocusOwner() == _chatInput)
        {
            _chatInput.ReleaseFocus();
        }
    }

    public bool TrySubmitLocalChatText(string rawText)
    {
        var text = NormalizeLocalChatInput(rawText);
        if (string.IsNullOrWhiteSpace(text) || _serverSession is null)
        {
            CloseLocalChatInput();
            return false;
        }

        var result = _serverSession.SendLocal(
            IntentType.SendLocalChat,
            new Dictionary<string, string>
            {
                ["text"] = text
            });
        _chatLabel.Text = result.WasAccepted
            ? $"Local chat: You: {text}"
            : $"Local chat failed: {result.RejectionReason}";
        CloseLocalChatInput();
        return result.WasAccepted;
    }

    public static string NormalizeLocalChatInput(string rawText)
    {
        return string.Join(" ", (rawText ?? string.Empty)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private void OnChatInputSubmitted(string text)
    {
        TrySubmitLocalChatText(text);
    }

    public void ToggleDeveloperOverlay()
    {
        SetDeveloperOverlayVisible(!_developerPanel.Visible);
    }

    public void SetDeveloperOverlayVisible(bool visible)
    {
        _developerPanel.Visible = visible;
        if (visible)
        {
            RefreshDeveloperOverlay();
        }
    }

    public void CycleDeveloperOverlayPage(int delta)
    {
        _developerPageIndex = WrapDeveloperPageIndex(_developerPageIndex + delta);
        RefreshDeveloperOverlay();
    }

    public static int WrapDeveloperPageIndex(int index)
    {
        const int pageCount = 4;
        return ((index % pageCount) + pageCount) % pageCount;
    }

    public void ToggleEscapeMenu()
    {
        SetEscapeMenuVisible(!_escapeMenuPanel.Visible);
    }

    public void HideEscapeMenu()
    {
        SetEscapeMenuVisible(false);
    }

    public void SetEscapeMenuVisible(bool visible)
    {
        _escapeMenuPanel.Visible = visible;
        if (!visible)
        {
            HideEscapeOptions();
        }
    }

    private void ShowEscapeOptions()
    {
        HideAppearancePanel();
        _escapeOptionsPanel.Visible = true;
        _escapeMenuStatusLabel.Text = "Options are live-menu placeholders; gameplay keeps running.";
    }

    private void HideEscapeOptions()
    {
        _escapeOptionsPanel.Visible = false;
        HideAppearancePanel();
        _escapeMenuStatusLabel.Text = "Menu open. Prototype world is still running.";
    }

    public void ShowAppearancePanel()
    {
        _escapeOptionsPanel.Visible = false;
        _appearancePanel.Visible = true;
        RefreshAppearancePanel();
        _escapeMenuStatusLabel.Text = "Appearance changes are routed through the authoritative prototype server.";
    }

    public void HideAppearancePanel()
    {
        _appearancePanel.Visible = false;
    }

    public bool CycleAppearanceLayer(string slot)
    {
        if (_serverSession is null)
        {
            _appearanceSummaryLabel.Text = "Appearance unavailable: prototype server is not running.";
            return false;
        }

        var current = GetLocalAppearanceSelection(_serverSession.LastLocalSnapshot) ?? PlayerAppearanceSelection.Default;
        var payload = BuildAppearanceCyclePayload(slot, current);
        if (payload.Count == 0)
        {
            _appearanceSummaryLabel.Text = $"Unknown appearance slot: {slot}";
            return false;
        }

        var result = _serverSession.SendLocal(IntentType.SetAppearance, payload);
        RefreshAppearancePanel();
        if (!result.WasAccepted)
        {
            _appearanceSummaryLabel.Text = $"Appearance change failed: {result.RejectionReason}";
        }

        return result.WasAccepted;
    }

    private void RefreshAppearancePanel()
    {
        var appearance = GetLocalAppearanceSelection(_serverSession?.LastLocalSnapshot) ?? PlayerAppearanceSelection.Default;
        _appearanceSummaryLabel.Text = FormatAppearanceSummary(appearance);
        _appearanceSkinLabel.Text = FormatAppearanceDetailLine("Skin", appearance.SkinLayerId);
        _appearanceHairLabel.Text = FormatAppearanceDetailLine("Hair", appearance.HairLayerId);
        _appearanceOutfitLabel.Text = FormatAppearanceDetailLine("Outfit", appearance.OutfitLayerId);
        _appearanceToolLabel.Text = FormatAppearanceDetailLine("Held tool", appearance.HeldToolLayerId);
        _appearancePreviewLabel.Text = "Preview: live on your character; thumbnails will plug into this panel as variants grow.";
    }

    private void ReturnToMainMenu()
    {
        GetTree().ChangeSceneToFile(MainMenuScenePath);
    }

    private void BuildUi()
    {
        var root = new Control
        {
            Name = "HudRoot",
            AnchorRight = 1,
            AnchorBottom = 1
        };
        AddChild(root);

        var statusPanel = new PanelContainer
        {
            OffsetLeft = 16,
            OffsetTop = 16,
            OffsetRight = 290,
            OffsetBottom = 104
        };
        root.AddChild(statusPanel);

        _karmaLabel = new Label
        {
            Text = "Karma: 0\nTier: Unmarked\nPath: Unmarked\nProgress: 0/10 toward Trusted",
            VerticalAlignment = VerticalAlignment.Center
        };
        statusPanel.AddChild(_karmaLabel);

        _eventLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 96,
            OffsetRight = 700,
            OffsetBottom = 126,
            Text = "Find someone to help, prank, or betray."
        };
        root.AddChild(_eventLabel);

        _chatLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 126,
            OffsetRight = 700,
            OffsetBottom = 156,
            Text = "Local chat: quiet"
        };
        root.AddChild(_chatLabel);

        _chatInputPanel = new PanelContainer
        {
            Name = "ChatInputPanel",
            OffsetLeft = 16,
            OffsetTop = 188,
            OffsetRight = 560,
            OffsetBottom = 230,
            Visible = false
        };
        root.AddChild(_chatInputPanel);

        _chatInput = new LineEdit
        {
            Name = "LocalChatInput",
            PlaceholderText = "Local chat — Enter to send, Esc to cancel",
            MaxLength = AuthoritativeWorldServer.LocalChatMaxMessageLength
        };
        _chatInputPanel.AddChild(_chatInput);

        _staminaLabel = new Label
        {
            OffsetLeft = 300,
            OffsetTop = 16,
            OffsetRight = 520,
            OffsetBottom = 46,
            Text = "Stamina: 100/100"
        };
        root.AddChild(_staminaLabel);

        _healthLabel = new Label
        {
            OffsetLeft = 300,
            OffsetTop = 48,
            OffsetRight = 520,
            OffsetBottom = 70,
            Text = "Health: 100/100"
        };
        root.AddChild(_healthLabel);

        _healthBar = new ProgressBar
        {
            OffsetLeft = 300,
            OffsetTop = 72,
            OffsetRight = 520,
            OffsetBottom = 90,
            MinValue = 0,
            MaxValue = 100,
            Value = 100,
            ShowPercentage = false
        };
        root.AddChild(_healthBar);

        _inventoryLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 158,
            OffsetRight = 700,
            OffsetBottom = 188,
            Text = "Inventory: empty"
        };
        root.AddChild(_inventoryLabel);

        _leaderboardLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 160,
            OffsetRight = 760,
            OffsetBottom = 190,
            Text = "Saint: -- | Scourge: --"
        };
        root.AddChild(_leaderboardLabel);

        _perksLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 192,
            OffsetRight = 900,
            OffsetBottom = 222,
            Text = "Perks: none"
        };
        root.AddChild(_perksLabel);

        _relationshipsLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 224,
            OffsetRight = 900,
            OffsetBottom = 254,
            Text = "Mara: Neutral (0)",
            Visible = false
        };
        root.AddChild(_relationshipsLabel);

        _factionsLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 256,
            OffsetRight = 900,
            OffsetBottom = 286,
            Text = "Free Settlers Rep: 0",
            Visible = false
        };
        root.AddChild(_factionsLabel);

        _questsLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 288,
            OffsetRight = 900,
            OffsetBottom = 318,
            Text = "Quests: none",
            Visible = false
        };
        root.AddChild(_questsLabel);

        _combatLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 320,
            OffsetRight = 900,
            OffsetBottom = 350,
            Text = "Combat: none",
            Visible = false
        };
        root.AddChild(_combatLabel);

        _entanglementsLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 352,
            OffsetRight = 900,
            OffsetBottom = 382,
            Text = "Entanglements: none",
            Visible = false
        };
        root.AddChild(_entanglementsLabel);

        _worldEventsLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 384,
            OffsetRight = 1000,
            OffsetBottom = 414,
            Text = "Rumors: quiet",
            Visible = false
        };
        root.AddChild(_worldEventsLabel);

        _duelsLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 416,
            OffsetRight = 1000,
            OffsetBottom = 446,
            Text = "Duels: none",
            Visible = false
        };
        root.AddChild(_duelsLabel);

        _syncLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 448,
            OffsetRight = 1000,
            OffsetBottom = 510,
            Text = "Sync: waiting",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Visible = false
        };
        root.AddChild(_syncLabel);

        _perfLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 512,
            OffsetRight = 1000,
            OffsetBottom = 540,
            Text = "Perf: waiting",
            Visible = false
        };
        root.AddChild(_perfLabel);

        _matchLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 544,
            OffsetRight = 1000,
            OffsetBottom = 588,
            Text = "Match: 30:00 remaining",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(_matchLabel);

        _promptPanel = new PanelContainer
        {
            OffsetLeft = 16,
            OffsetTop = 592,
            OffsetRight = 580,
            OffsetBottom = 704,
            Visible = false
        };
        root.AddChild(_promptPanel);

        _promptLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _promptPanel.AddChild(_promptLabel);

        _inventoryPanel = new PanelContainer
        {
            OffsetLeft = 880,
            OffsetTop = 16,
            OffsetRight = 1264,
            OffsetBottom = 420,
            Visible = false
        };
        root.AddChild(_inventoryPanel);

        _inventoryOverlayLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = "Inventory"
        };
        _inventoryPanel.AddChild(_inventoryOverlayLabel);

        BuildPossePanel(root);
        BuildDeveloperOverlay(root);
        BuildEscapeMenu(root);
    }

    private void BuildPossePanel(Control root)
    {
        _possePanel = new PanelContainer
        {
            Name = "PossePanel",
            OffsetLeft = 530,
            OffsetTop = 16,
            OffsetRight = 780,
            OffsetBottom = 168,
            Visible = false
        };
        root.AddChild(_possePanel);

        var margin = new MarginContainer { Name = "PossePanelMargin" };
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        _possePanel.AddChild(margin);

        _posseLabel = new Label
        {
            Name = "PossePanelLabel",
            Text = "Posse: not in a posse",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        margin.AddChild(_posseLabel);
    }

    private void BuildDeveloperOverlay(Control root)
    {
        _developerPanel = new PanelContainer
        {
            Name = "DeveloperPanel",
            OffsetLeft = 760,
            OffsetTop = 16,
            OffsetRight = 1264,
            OffsetBottom = 704,
            Visible = false
        };
        root.AddChild(_developerPanel);

        var margin = new MarginContainer { Name = "DeveloperMargin" };
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        _developerPanel.AddChild(margin);

        _developerOverlayLabel = new Label
        {
            Name = "DeveloperOverlayLabel",
            Text = "Developer overlay: waiting for snapshot",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        margin.AddChild(_developerOverlayLabel);
    }

    private void BuildEscapeMenu(Control root)
    {
        _escapeMenuPanel = new PanelContainer
        {
            Name = "EscapeMenuPanel",
            OffsetLeft = 390,
            OffsetTop = 120,
            OffsetRight = 890,
            OffsetBottom = 620,
            Visible = false
        };
        root.AddChild(_escapeMenuPanel);

        var margin = new MarginContainer
        {
            Name = "EscapeMenuMargin"
        };
        margin.AddThemeConstantOverride("margin_left", 28);
        margin.AddThemeConstantOverride("margin_top", 24);
        margin.AddThemeConstantOverride("margin_right", 28);
        margin.AddThemeConstantOverride("margin_bottom", 24);
        _escapeMenuPanel.AddChild(margin);

        var content = new VBoxContainer
        {
            Name = "EscapeMenuContent"
        };
        content.AddThemeConstantOverride("separation", 12);
        margin.AddChild(content);

        content.AddChild(new Label
        {
            Name = "EscapeMenuTitle",
            Text = "Karma Menu",
            HorizontalAlignment = HorizontalAlignment.Center
        });

        _escapeMenuStatusLabel = new Label
        {
            Name = "EscapeMenuStatusLabel",
            Text = "Menu open. Prototype world is still running.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        content.AddChild(_escapeMenuStatusLabel);

        _resumeButton = new Button { Name = "ResumeButton", Text = "Resume" };
        _escapeOptionsButton = new Button { Name = "OptionsButton", Text = "Options" };
        _appearanceButton = new Button { Name = "AppearanceButton", Text = "Appearance" };
        _backToMenuButton = new Button { Name = "MainMenuButton", Text = "Main Menu" };
        _quitButton = new Button { Name = "QuitButton", Text = "Quit" };
        content.AddChild(_resumeButton);
        content.AddChild(_escapeOptionsButton);
        content.AddChild(_appearanceButton);
        content.AddChild(_backToMenuButton);
        content.AddChild(_quitButton);

        _escapeOptionsPanel = new PanelContainer
        {
            Name = "EscapeOptionsPanel",
            Visible = false
        };
        content.AddChild(_escapeOptionsPanel);

        var optionsMargin = new MarginContainer { Name = "EscapeOptionsMargin" };
        optionsMargin.AddThemeConstantOverride("margin_left", 16);
        optionsMargin.AddThemeConstantOverride("margin_top", 12);
        optionsMargin.AddThemeConstantOverride("margin_right", 16);
        optionsMargin.AddThemeConstantOverride("margin_bottom", 12);
        _escapeOptionsPanel.AddChild(optionsMargin);

        var optionsContent = new VBoxContainer { Name = "EscapeOptionsContent" };
        optionsContent.AddThemeConstantOverride("separation", 8);
        optionsMargin.AddChild(optionsContent);
        optionsContent.AddChild(new Label
        {
            Text = "Options quick panel",
            HorizontalAlignment = HorizontalAlignment.Center
        });
        optionsContent.AddChild(new Label
        {
            Text = "Audio/video/control settings will reuse the main menu options model here. This overlay does not pause the match timer.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        _closeEscapeOptionsButton = new Button
        {
            Name = "CloseOptionsButton",
            Text = "Back"
        };
        optionsContent.AddChild(_closeEscapeOptionsButton);

        _appearancePanel = new PanelContainer
        {
            Name = "AppearancePanel",
            Visible = false
        };
        content.AddChild(_appearancePanel);

        var appearanceMargin = new MarginContainer { Name = "AppearanceMargin" };
        appearanceMargin.AddThemeConstantOverride("margin_left", 16);
        appearanceMargin.AddThemeConstantOverride("margin_top", 12);
        appearanceMargin.AddThemeConstantOverride("margin_right", 16);
        appearanceMargin.AddThemeConstantOverride("margin_bottom", 12);
        _appearancePanel.AddChild(appearanceMargin);

        var appearanceContent = new VBoxContainer { Name = "AppearanceContent" };
        appearanceContent.AddThemeConstantOverride("separation", 8);
        appearanceMargin.AddChild(appearanceContent);
        appearanceContent.AddChild(new Label
        {
            Text = "Prototype appearance",
            HorizontalAlignment = HorizontalAlignment.Center
        });
        _appearanceSummaryLabel = new Label
        {
            Name = "AppearanceSummaryLabel",
            Text = "Appearance: default",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        appearanceContent.AddChild(_appearanceSummaryLabel);
        _appearanceSkinLabel = new Label { Name = "AppearanceSkinLabel", Text = "Skin: default" };
        _appearanceHairLabel = new Label { Name = "AppearanceHairLabel", Text = "Hair: default" };
        _appearanceOutfitLabel = new Label { Name = "AppearanceOutfitLabel", Text = "Outfit: default" };
        _appearanceToolLabel = new Label { Name = "AppearanceToolLabel", Text = "Held tool: none" };
        _appearancePreviewLabel = new Label
        {
            Name = "AppearancePreviewLabel",
            Text = "Preview: live on your character",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        appearanceContent.AddChild(_appearanceSkinLabel);
        appearanceContent.AddChild(_appearanceHairLabel);
        appearanceContent.AddChild(_appearanceOutfitLabel);
        appearanceContent.AddChild(_appearanceToolLabel);
        appearanceContent.AddChild(_appearancePreviewLabel);
        _cycleSkinButton = new Button { Name = "CycleSkinButton", Text = "Cycle skin (V)" };
        _cycleHairButton = new Button { Name = "CycleHairButton", Text = "Cycle hair (B)" };
        _cycleOutfitButton = new Button { Name = "CycleOutfitButton", Text = "Cycle outfit (N)" };
        _closeAppearanceButton = new Button { Name = "CloseAppearanceButton", Text = "Back" };
        appearanceContent.AddChild(_cycleSkinButton);
        appearanceContent.AddChild(_cycleHairButton);
        appearanceContent.AddChild(_cycleOutfitButton);
        appearanceContent.AddChild(_closeAppearanceButton);
    }

    private void OnKarmaChanged(int score, string tierName, string pathName)
    {
        var progress = Karma.Data.KarmaTiers.GetRankProgress(score);
        _karmaLabel.Text = $"Karma: {score:+#;-#;0}\nTier: {tierName}\nPath: {pathName}\n{progress.Summary}";
    }

    private void OnKarmaEvent(string message)
    {
        _eventLabel.Text = message;
    }

    private void OnInventoryChanged(string inventoryText)
    {
        _inventoryLabel.Text = inventoryText;
        if (_inventoryPanel.Visible)
        {
            RefreshInventoryOverlay();
        }
    }

    private void OnLeaderboardChanged(string leaderboardText)
    {
        _leaderboardLabel.Text = leaderboardText;
    }

    private void OnPerksChanged(string perksText)
    {
        _perksLabel.Text = perksText;
    }

    private void OnRelationshipsChanged(string relationshipsText)
    {
        _relationshipsLabel.Text = relationshipsText;
    }

    private void OnFactionsChanged(string factionsText)
    {
        _factionsLabel.Text = factionsText;
    }

    private void OnQuestsChanged(string questsText)
    {
        _questsLabel.Text = questsText;
    }

    private void OnCombatChanged(string combatText)
    {
        _lastCombatText = combatText;
        var gameState = GetNode<GameState>("/root/GameState");
        RenderCombatLine(gameState);
    }

    private void OnEntanglementsChanged(string entanglementsText)
    {
        _entanglementsLabel.Text = entanglementsText;
    }

    private void OnDuelsChanged(string duelsText)
    {
        _duelsLabel.Text = duelsText;
    }

    private void OnWorldEventsChanged(string worldEventsText)
    {
        _worldEventsLabel.Text = worldEventsText;
    }

    private void OnLocalSnapshotChanged(string snapshotSummary)
    {
        var serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        if (serverSession?.LastLocalSnapshot is not null)
        {
            var snapshot = serverSession.LastLocalSnapshot;
            _lastSnapshot = snapshot;
            _matchLabel.Text = FormatMatchStatus(snapshot.Match);
            var localPlayer = snapshot.Players.FirstOrDefault(player => player.Id == snapshot.PlayerId);
            if (localPlayer is not null)
            {
                SetHealth(localPlayer.Health, localPlayer.MaxHealth);
                _lastStatusEffects = localPlayer.StatusEffects;
                RenderCombatLine(GetNode<GameState>("/root/GameState"));
            }

            _eventLabel.Text = FormatLatestServerEvent(snapshot.ServerEvents);
            _chatLabel.Text = FormatLocalChatSummary(snapshot.LocalChatMessages);
            if (_appearancePanel.Visible)
            {
                RefreshAppearancePanel();
            }

            if (_possePanel.Visible)
            {
                RefreshPossePanel();
            }
        }

        _syncLabel.Text = $"Sync: {snapshotSummary}";
        if (_developerPanel.Visible)
        {
            RefreshDeveloperOverlay();
        }
    }

    private void RefreshDeveloperOverlay()
    {
        _developerOverlayLabel.Text = FormatDeveloperOverlay(_lastSnapshot, _perfLabel.Text, _developerPageIndex);
    }

    private void SetHealth(int health, int maxHealth)
    {
        _healthLabel.Text = FormatHealth(health, maxHealth);
        _healthBar.Value = CalculateHealthPercent(health, maxHealth);
    }

    public static string FormatHealth(int health, int maxHealth)
    {
        var safeMax = Mathf.Max(1, maxHealth);
        var clampedHealth = Mathf.Clamp(health, 0, safeMax);
        return $"Health: {clampedHealth}/{safeMax}";
    }

    public static float CalculateHealthPercent(int health, int maxHealth)
    {
        var safeMax = Mathf.Max(1, maxHealth);
        var clampedHealth = Mathf.Clamp(health, 0, safeMax);
        return clampedHealth * 100f / safeMax;
    }

    public static string FormatStatusEffects(IReadOnlyList<string> statusEffects)
    {
        return statusEffects is null || statusEffects.Count == 0
            ? "Status: none"
            : $"Status: {string.Join(", ", statusEffects.Take(3))}";
    }

    public static string FormatCombatLine(
        string combatText,
        int attackPower,
        int defense,
        IReadOnlyList<string> statusEffects)
    {
        var safeCombatText = string.IsNullOrWhiteSpace(combatText) ? "Combat: none" : combatText;
        return $"{safeCombatText} | You ATK:{attackPower} DEF:{defense} | {FormatStatusEffects(statusEffects)}";
    }

    public static string FormatMatchStatus(MatchSnapshot match)
    {
        if (match.Status == MatchStatus.Running)
        {
            return match.Summary;
        }

        return $"RESULTS LOCKED — Saint: {match.SaintWinnerName} ({match.SaintWinnerScore:+#;-#;0}) | Scourge: {match.ScourgeWinnerName} ({match.ScourgeWinnerScore:+#;-#;0})\nPost-match free roam: movement/dialogue only. Winners paid +{ServerConfig.DefaultMatchWinnerScripReward} scrip.";
    }

    public static string FormatLatestServerEvent(IReadOnlyList<ServerEvent> serverEvents)
    {
        if (serverEvents is null || serverEvents.Count == 0)
        {
            return "Events: quiet";
        }

        var latest = serverEvents[^1];
        if (latest.EventId.Contains("player_joined"))
        {
            var displayName = ReadEventData(latest, "displayName", ReadEventData(latest, "playerId", "Someone"));
            var connected = ReadEventData(latest, "connectedPlayers", "?");
            var maxPlayers = ReadEventData(latest, "maxPlayers", "?");
            return $"{displayName} joined the world. Players: {connected}/{maxPlayers}.";
        }

        if (latest.EventId.Contains("match_finished"))
        {
            var saint = ReadEventData(latest, "saintWinnerName", ReadEventData(latest, "saintWinnerId", "none"));
            var scourge = ReadEventData(latest, "scourgeWinnerName", ReadEventData(latest, "scourgeWinnerId", "none"));
            var saintScore = ReadEventData(latest, "saintWinnerScore", "0");
            var scourgeScore = ReadEventData(latest, "scourgeWinnerScore", "0");
            var saintReward = ReadEventData(latest, "saintScripReward", "0");
            var scourgeReward = ReadEventData(latest, "scourgeScripReward", "0");
            return $"Match complete — results locked. Saint: {saint} ({FormatSignedScore(saintScore)}, +{saintReward} scrip). Scourge: {scourge} ({FormatSignedScore(scourgeScore)}, +{scourgeReward} scrip).";
        }

        if (latest.EventId.Contains("local_chat"))
        {
            var displayName = ReadEventData(latest, "displayName", ReadEventData(latest, "playerId", "Someone"));
            var text = ReadEventData(latest, "text", "...");
            return $"{displayName} says: {text}";
        }

        if (latest.EventId.Contains("player_appearance_changed"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var skin = FormatAppearanceLayerName(ReadEventData(latest, "skinLayerId", string.Empty));
            var hair = FormatAppearanceLayerName(ReadEventData(latest, "hairLayerId", string.Empty));
            var outfit = FormatAppearanceLayerName(ReadEventData(latest, "outfitLayerId", string.Empty));
            return $"{player} changed appearance: {skin} skin, {hair} hair, {outfit} outfit.";
        }

        if (latest.EventId.Contains("player_moved"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var x = ReadEventData(latest, "x", "?");
            var y = ReadEventData(latest, "y", "?");
            return $"{player} moved to {x},{y}.";
        }

        if (latest.EventId.Contains("karma_shift"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var action = FormatActionName(ReadEventData(latest, "action", string.Empty));
            var amountText = ReadEventData(latest, "amount", "0");
            var amount = int.TryParse(amountText, out var parsedAmount) ? Math.Abs(parsedAmount) : 0;
            var direction = ReadEventData(latest, "direction", "karma");
            var target = FormatNpcName(ReadEventData(latest, "targetId", string.Empty));
            return $"{player} chose {action}: {direction} {amount} karma toward {target}.";
        }

        if (latest.EventId.Contains("player_downed"))
        {
            var attacker = ReadEventData(latest, "playerId", "Someone");
            var target = ReadEventData(latest, "targetId", "someone");
            var damage = ReadEventData(latest, "rawDamage", "?");
            return $"{attacker} downed {target} for {damage}. Countdown started.";
        }

        if (latest.EventId.Contains("player_respawned"))
        {
            var target = ReadEventData(latest, "playerId", "Someone");
            var drops = ReadEventData(latest, "droppedItemCount", "0");
            var x = ReadEventData(latest, "respawnX", "?");
            var y = ReadEventData(latest, "respawnY", "?");
            return $"{target} broke, dropped {drops}, and respawned at {x},{y}.";
        }

        if (latest.EventId.Contains("player_attacked"))
        {
            var attacker = ReadEventData(latest, "playerId", "Someone");
            var target = ReadEventData(latest, "targetId", "someone");
            var damage = ReadEventData(latest, "rawDamage", "?");
            var health = ReadEventData(latest, "targetHealth", "?");
            var maxHealth = ReadEventData(latest, "targetMaxHealth", "?");
            return $"{attacker} hit {target} for {damage}. {target} HP: {health}/{maxHealth}.";
        }

        if (latest.EventId.Contains("karma_break"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var drops = ReadEventData(latest, "droppedItemCount", "0");
            var x = ReadEventData(latest, "respawnX", "?");
            var y = ReadEventData(latest, "respawnY", "?");
            return $"{player} suffered a Karma Break, dropped {drops}, and respawned at {x},{y}.";
        }

        if (latest.EventId.Contains("structure_interacted"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var structureName = FormatStructureName(ReadEventData(latest, "structureId", string.Empty));
            var action = ReadEventData(latest, "action", "inspect");
            var result = ReadEventData(latest, "result", "Nothing unusual happens.");
            var integrity = ReadEventData(latest, "integrity", "?");
            var condition = ReadEventData(latest, "condition", "unknown");
            var scripReward = ReadEventData(latest, "scripReward", "0");
            var factionDelta = ReadEventData(latest, "factionDelta", "0");
            var verb = action switch
            {
                "repair" => "repaired",
                "sabotage" => "sabotaged",
                _ => "inspected"
            };
            var rewardText = scripReward != "0" ? $" +{scripReward} scrip." : string.Empty;
            var factionText = factionDelta != "0" ? $" Civic Repair Guild {factionDelta}." : string.Empty;
            return $"{player} {verb} {structureName}: {result} Integrity {integrity}% ({condition}).{rewardText}{factionText}";
        }

        if (latest.EventId.Contains("dialogue_started"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var npc = FormatNpcName(ReadEventData(latest, "npcId", string.Empty));
            var choices = ReadEventData(latest, "choiceIds", string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Length;
            return $"{player} started talking with {npc}. Choices: {choices}.";
        }

        if (latest.EventId.Contains("dialogue_choice_selected"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var npc = FormatNpcName(ReadEventData(latest, "npcId", string.Empty));
            var choice = FormatDialogueChoice(ReadEventData(latest, "choiceId", string.Empty));
            var amountText = ReadEventData(latest, "amount", "0");
            var amount = int.TryParse(amountText, out var parsedAmount) ? parsedAmount : 0;
            return $"{player} chose \"{choice}\" with {npc}. Karma {amount:+#;-#;0}.";
        }

        if (latest.EventId.Contains("item_used"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var target = ReadEventData(latest, "targetId", "someone");
            var itemName = FormatItemName(ReadEventData(latest, "itemId", string.Empty));
            var healing = ReadEventData(latest, "healing", "?");
            var health = ReadEventData(latest, "targetHealth", "?");
            var maxHealth = ReadEventData(latest, "targetMaxHealth", "?");
            return $"{player} used {itemName} on {target} for {healing}. {target} HP: {health}/{maxHealth}.";
        }

        if (latest.EventId.Contains("item_purchased"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var itemName = FormatItemName(ReadEventData(latest, "itemId", string.Empty));
            var price = ReadEventData(latest, "price", "?");
            var basePrice = ReadEventData(latest, "basePrice", price);
            var currency = ReadEventData(latest, "currency", "scrip");
            if (basePrice != price)
            {
                return $"{player} bought {itemName} for {price} {currency} (base {basePrice}).";
            }

            return $"{player} bought {itemName} for {price} {currency}.";
        }

        if (latest.EventId.Contains("item_equipped"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var itemName = FormatItemName(ReadEventData(latest, "itemId", string.Empty));
            var slot = ReadEventData(latest, "slot", "slot");
            return $"{player} equipped {itemName} in {slot}.";
        }

        if (latest.EventId.Contains("item_placed"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var itemName = FormatItemName(ReadEventData(latest, "itemId", string.Empty));
            var x = ReadEventData(latest, "x", "?");
            var y = ReadEventData(latest, "y", "?");
            return $"{player} placed {itemName} at {x},{y}.";
        }

        if (latest.EventId.Contains("item_picked_up"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var itemName = FormatItemName(ReadEventData(latest, "itemId", string.Empty));
            var dropOwnerId = ReadEventData(latest, "dropOwnerId", string.Empty);
            if (!string.IsNullOrWhiteSpace(dropOwnerId))
            {
                var dropOwnerName = ReadEventData(latest, "dropOwnerName", dropOwnerId);
                var karmaAmount = ReadEventData(latest, "karmaAmount", "0");
                return $"{player} claimed {itemName} from {dropOwnerName}'s Karma Break drop. Karma {karmaAmount}.";
            }

            return $"{player} picked up {itemName}.";
        }

        if (latest.EventId.Contains("duel_requested"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var target = ReadEventData(latest, "targetId", "someone");
            var duelId = ReadEventData(latest, "duelId", "duel");
            return $"{player} requested {duelId} with {target}.";
        }

        if (latest.EventId.Contains("duel_accepted"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var target = ReadEventData(latest, "targetId", "someone");
            var duelId = ReadEventData(latest, "duelId", "duel");
            var status = ReadEventData(latest, "status", "Active");
            return $"{player} accepted {duelId} with {target}. Status: {status}.";
        }

        if (latest.EventId.Contains("quest_started"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var questName = FormatQuestName(ReadEventData(latest, "questId", string.Empty));
            var giver = ReadEventData(latest, "targetId", "quest giver");
            return $"{player} started {questName} with {giver}.";
        }

        if (latest.EventId.Contains("quest_completed"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var questName = FormatQuestName(ReadEventData(latest, "questId", string.Empty));
            var reward = ReadEventData(latest, "scripReward", "0");
            return $"{player} completed {questName} and earned {reward} scrip.";
        }

        if (latest.EventId.Contains("entanglement_started"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var npc = FormatNpcName(ReadEventData(latest, "npcId", string.Empty));
            var affectedNpc = FormatNpcName(ReadEventData(latest, "affectedNpcId", string.Empty));
            var type = ReadEventData(latest, "type", "Entanglement");
            return $"{player} started a {type} entanglement with {npc}, affecting {affectedNpc}.";
        }

        if (latest.EventId.Contains("entanglement_exposed"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var npc = FormatNpcName(ReadEventData(latest, "npcId", string.Empty));
            var affectedNpc = FormatNpcName(ReadEventData(latest, "affectedNpcId", string.Empty));
            var type = ReadEventData(latest, "type", "Entanglement");
            return $"{player} exposed a {type} entanglement between {npc} and {affectedNpc}.";
        }

        if (latest.EventId.Contains("item_transferred"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var target = ReadEventData(latest, "targetId", "someone");
            var itemName = FormatItemName(ReadEventData(latest, "itemId", string.Empty));
            var mode = ReadEventData(latest, "mode", "gift");
            var returnedDrop = ReadEventData(latest, "returnedDrop", "False") == "True";
            var karmaAmount = ReadEventData(latest, "karmaAmount", "0");
            if (returnedDrop)
            {
                var dropOwnerName = ReadEventData(latest, "dropOwnerName", target);
                return $"{player} returned {itemName} from {dropOwnerName}'s Karma Break drop. Karma {karmaAmount}.";
            }

            return mode == "steal"
                ? $"{player} stole {itemName} from {target}. Karma {karmaAmount}."
                : $"{player} gave {itemName} to {target}. Karma {karmaAmount}.";
        }

        if (latest.EventId.Contains("currency_transferred"))
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var target = ReadEventData(latest, "targetId", "someone");
            var amount = ReadEventData(latest, "amount", "?");
            var currency = ReadEventData(latest, "currency", "currency");
            var mode = ReadEventData(latest, "mode", "gift");
            var karmaAmount = ReadEventData(latest, "karmaAmount", "0");
            return mode == "steal"
                ? $"{player} stole {amount} {currency} from {target}. Karma {karmaAmount}."
                : $"{player} gave {amount} {currency} to {target}. Karma {karmaAmount}.";
        }

        if (latest.EventId.Contains("intent_rejected"))
        {
            var intentType = ReadEventData(latest, "intentType", "Intent");
            var reason = ReadEventData(latest, "reason", latest.Description);
            return $"{intentType} rejected: {reason}";
        }

        if (latest.EventId.Contains("posse_invite_sent"))
        {
            var inviterId = ReadEventData(latest, "inviterId", "Someone");
            var targetId = ReadEventData(latest, "targetId", "someone");
            return $"{inviterId} invited {targetId} to a posse. (P to open panel)";
        }

        if (latest.EventId.Contains("posse_accepted"))
        {
            var playerId = ReadEventData(latest, "playerId", "Someone");
            var count = ReadEventData(latest, "memberCount", "?");
            return $"{playerId} joined the posse. Members: {count}.";
        }

        if (latest.EventId.Contains("posse_member_left"))
        {
            var playerId = ReadEventData(latest, "playerId", "Someone");
            var remaining = ReadEventData(latest, "remainingMembers", "?");
            return $"{playerId} left the posse. Remaining: {remaining}.";
        }

        if (latest.EventId.Contains("posse_disbanded"))
        {
            return "Posse disbanded — last member left.";
        }

        return $"Events: {latest.Description}";
    }

    private static string ReadEventData(ServerEvent serverEvent, string key, string fallback)
    {
        return serverEvent.Data.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static string FormatSignedScore(string scoreText)
    {
        return int.TryParse(scoreText, out var score)
            ? score.ToString("+#;-#;0")
            : scoreText;
    }

    private static string FormatItemName(string itemId)
    {
        return Karma.Data.StarterItems.TryGetById(itemId, out var item)
            ? item.Name
            : string.IsNullOrWhiteSpace(itemId) ? "item" : itemId;
    }

    private static string FormatAppearanceLayerName(string layerId)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            return "default";
        }

        return string.Join(" ", layerId
            .Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => part != "skin" && part != "hair" && part != "outfit" && part != "tool" && part != "32x64")
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    public static string FormatAppearanceSummary(PlayerAppearanceSelection appearance)
    {
        return $"Appearance: {FormatAppearanceLayerName(appearance.SkinLayerId)} skin | {FormatAppearanceLayerName(appearance.HairLayerId)} hair | {FormatAppearanceLayerName(appearance.OutfitLayerId)} outfit | {FormatAppearanceLayerName(appearance.HeldToolLayerId)} tool";
    }

    public static string FormatAppearanceDetailLine(string label, string layerId)
    {
        var value = string.IsNullOrWhiteSpace(layerId) ? "none" : FormatAppearanceLayerName(layerId);
        return $"{label}: {value}";
    }

    public static IReadOnlyDictionary<string, string> BuildAppearanceCyclePayload(string slot, PlayerAppearanceSelection current)
    {
        return slot switch
        {
            "skin" => new Dictionary<string, string> { ["skinLayerId"] = PlayerController.CycleSkinLayerId(current.SkinLayerId) },
            "hair" => new Dictionary<string, string> { ["hairLayerId"] = PlayerController.CycleHairLayerId(current.HairLayerId) },
            "outfit" => new Dictionary<string, string> { ["outfitLayerId"] = PlayerController.CycleOutfitLayerId(current.OutfitLayerId) },
            _ => new Dictionary<string, string>()
        };
    }

    private static PlayerAppearanceSelection GetLocalAppearanceSelection(ClientInterestSnapshot snapshot)
    {
        return snapshot?.Players.FirstOrDefault(player => player.Id == snapshot.PlayerId)?.Appearance;
    }

    private static string FormatQuestName(string questId)
    {
        return questId == Karma.Data.StarterQuests.MaraClinicFiltersId
            ? Karma.Data.StarterQuests.MaraClinicFilters.Title
            : string.IsNullOrWhiteSpace(questId) ? "quest" : questId;
    }

    private static string FormatActionName(string actionId)
    {
        return actionId switch
        {
            PrototypeActions.HelpMaraId => "Help Mara",
            PrototypeActions.WhoopieCushionMaraId => "Clinic Prank",
            PrototypeActions.StealFromMaraId => "Steal Spare Parts",
            PrototypeActions.GiftBalloonToMaraId => "Gift Balloon",
            PrototypeActions.MockMaraWithBalloonId => "Mock Mara",
            PrototypeActions.HelpPeerId => "Help Peer",
            PrototypeActions.AttackPeerId => "Attack Peer",
            PrototypeActions.RobPeerId => "Rob Peer",
            PrototypeActions.ReturnPeerItemId => "Return Peer Item",
            PrototypeActions.StartMaraEntanglementId => "Start Entanglement",
            PrototypeActions.ExposeMaraEntanglementId => "Expose Entanglement",
            _ => string.IsNullOrWhiteSpace(actionId) ? "an action" : actionId
        };
    }

    private static string FormatDialogueChoice(string choiceId)
    {
        return choiceId switch
        {
            "help_filters" => "Repair the filters",
            "prank_stool" => "Plant a whoopie cushion",
            "steal_parts" => "Steal spare parts",
            "gift_balloon" => "Offer a deflated balloon",
            "mock_balloon" => "Mock with a deflated balloon",
            _ => string.IsNullOrWhiteSpace(choiceId) ? "a choice" : choiceId
        };
    }

    private static string FormatNpcName(string npcId)
    {
        if (npcId == Karma.Data.StarterNpcs.Mara.Id)
        {
            return Karma.Data.StarterNpcs.Mara.Name;
        }

        if (npcId == Karma.Data.StarterNpcs.Dallen.Id)
        {
            return Karma.Data.StarterNpcs.Dallen.Name;
        }

        return string.IsNullOrWhiteSpace(npcId) ? "someone" : npcId;
    }

    private static string FormatStructureName(string structureId)
    {
        return StructureArtCatalog.TryGetById(structureId, out var structure)
            ? structure.DisplayName
            : string.IsNullOrWhiteSpace(structureId) ? "structure" : structureId;
    }

    private void RenderCombatLine(GameState gameState)
    {
        _combatLabel.Text = FormatCombatLine(
            _lastCombatText,
            gameState.LocalPlayer.AttackPower,
            gameState.LocalPlayer.Defense,
            _lastStatusEffects);
    }

    private void RefreshInventoryOverlay()
    {
        if (_gameState is null)
        {
            return;
        }

        _inventoryOverlayLabel.Text = FormatInventoryOverlay(
            _gameState.Inventory,
            _gameState.LocalScrip,
            _gameState.LocalPlayer.Equipment);
    }

    public static string FormatInventoryOverlay(
        IReadOnlyList<GameItem> items,
        int scrip,
        IReadOnlyDictionary<EquipmentSlot, GameItem> equipment)
    {
        var lines = new List<string>
        {
            "Inventory",
            $"Scrip: {scrip}",
            string.Empty,
            "Equipment:"
        };

        lines.Add($"Main Hand: {FormatEquipped(equipment, EquipmentSlot.MainHand)}");
        lines.Add($"Body: {FormatEquipped(equipment, EquipmentSlot.Body)}");
        lines.Add($"Trinket: {FormatEquipped(equipment, EquipmentSlot.Trinket)}");
        lines.Add(string.Empty);
        lines.Add("Items:");

        if (items is null || items.Count == 0)
        {
            lines.Add("empty");
            lines.Add(string.Empty);
            lines.Add("I - Close");
            return string.Join("\n", lines);
        }

        foreach (var group in items
            .GroupBy(item => item.Id)
            .OrderBy(group => group.First().Category)
            .ThenBy(group => group.First().Name))
        {
            var item = group.First();
            lines.Add($"{item.Name} x{group.Count()} [{ItemText.FormatSummary(item)}]");
        }

        lines.Add(string.Empty);
        lines.Add("I - Close | Z/X equip basics | C place loose item");
        return string.Join("\n", lines);
    }

    public static string FormatPossePanel(IReadOnlyList<PlayerSnapshot> players, string localPlayerId)
    {
        var localPlayer = players?.FirstOrDefault(player => player.Id == localPlayerId);
        if (localPlayer is null || string.IsNullOrEmpty(localPlayer.PosseId))
        {
            return "Posse: not in a posse\nP — close";
        }

        var posseId = localPlayer.PosseId;
        var members = players.Where(player => player.PosseId == posseId).OrderBy(player => player.DisplayName).ToArray();
        var label = posseId.StartsWith("posse_") ? posseId[6..] : posseId;

        var lines = new List<string> { $"Posse [{label}] — {members.Length} member{(members.Length == 1 ? "" : "s")}" };
        foreach (var member in members)
        {
            var self = member.Id == localPlayerId ? " (you)" : string.Empty;
            var karmaSign = member.Karma >= 0 ? "+" : string.Empty;
            lines.Add($"{member.DisplayName}{self}: {karmaSign}{member.Karma} | HP {member.Health}/{member.MaxHealth}");
        }

        lines.Add("P — close");
        return string.Join("\n", lines);
    }

    public static string FormatLocalChatSummary(IReadOnlyList<LocalChatMessageSnapshot> messages)
    {
        if (messages is null || messages.Count == 0)
        {
            return "Local chat: quiet";
        }

        var latest = messages[^1];
        if (latest.Channel == "posse")
        {
            return $"[Posse] {latest.SpeakerName}: {latest.Text}";
        }

        var volumePercent = Mathf.RoundToInt(latest.Volume * 100f);
        return $"Local chat: {latest.SpeakerName} ({latest.DistanceTiles} tiles, {volumePercent}%): {latest.Text}";
    }

    public static string FormatDeveloperOverlay(ClientInterestSnapshot snapshot, string perfLine)
    {
        return FormatDeveloperOverlay(snapshot, perfLine, 0);
    }

    public static string FormatDeveloperOverlay(ClientInterestSnapshot snapshot, string perfLine, int pageIndex)
    {
        if (snapshot is null)
        {
            return "Developer Overlay (~)\nWaiting for local snapshot. Tab cycles pages.";
        }

        pageIndex = WrapDeveloperPageIndex(pageIndex);
        var lines = new List<string>
        {
            $"Developer Overlay (~) | Page {pageIndex + 1}/4: {FormatDeveloperPageName(pageIndex)}",
            "Tab next | Shift+Tab previous | ~ close",
            string.Empty
        };

        switch (pageIndex)
        {
            case 0:
                AppendDeveloperLocalPage(lines, snapshot, perfLine);
                break;
            case 1:
                AppendDeveloperCharactersPage(lines, snapshot);
                break;
            case 2:
                AppendDeveloperWorldPage(lines, snapshot);
                break;
            default:
                AppendDeveloperEventsPage(lines, snapshot);
                break;
        }

        return string.Join("\n", lines);
    }

    private static string FormatDeveloperPageName(int pageIndex)
    {
        return pageIndex switch
        {
            0 => "Local",
            1 => "Characters",
            2 => "World",
            _ => "Events"
        };
    }

    private static void AppendDeveloperLocalPage(List<string> lines, ClientInterestSnapshot snapshot, string perfLine)
    {
        var local = snapshot.Players.FirstOrDefault(player => player.Id == snapshot.PlayerId) ?? snapshot.Players.FirstOrDefault();
        lines.Add(perfLine);
        lines.Add($"World: {snapshot.WorldId} | Tick: {snapshot.Tick} | Radius: {snapshot.InterestRadiusTiles}");
        lines.Add($"Match: {snapshot.Match.Summary}");
        lines.Add(string.Empty);
        lines.Add("Local player:");
        if (local is null)
        {
            lines.Add("missing");
            return;
        }

        lines.Add($"{local.DisplayName} ({local.Id}) @ {local.TileX},{local.TileY}");
        lines.Add($"Karma {local.Karma:+#;-#;0} | {local.Tier} rank {local.KarmaRank} | {local.KarmaProgress}");
        lines.Add($"HP {local.Health}/{local.MaxHealth} | Scrip {local.Scrip} | Standing {local.Standing}");
        lines.Add($"Inventory: {local.InventoryItemIds.Count} | Equipment: {string.Join(", ", local.EquipmentItemIds.Select(pair => $"{pair.Key}:{pair.Value}"))}");
        lines.Add($"Status: {(local.StatusEffects.Count == 0 ? "none" : string.Join(", ", local.StatusEffects))}");
    }

    private static void AppendDeveloperCharactersPage(List<string> lines, ClientInterestSnapshot snapshot)
    {
        lines.Add("Nearby players:");
        var nearbyPlayers = snapshot.Players.Where(player => player.Id != snapshot.PlayerId).Take(8).ToArray();
        foreach (var player in nearbyPlayers)
        {
            lines.Add($"- {player.DisplayName} ({player.Id}) @ {player.TileX},{player.TileY} HP {player.Health}/{player.MaxHealth} Karma {player.Karma:+#;-#;0} {player.Tier}");
        }

        if (nearbyPlayers.Length == 0)
        {
            lines.Add("none");
        }

        lines.Add(string.Empty);
        lines.Add("Nearby NPCs:");
        foreach (var npc in snapshot.Npcs.Take(10))
        {
            var dialogue = snapshot.Dialogues.FirstOrDefault(candidate => candidate.NpcId == npc.Id);
            var choices = dialogue is null ? "no dialogue" : $"{dialogue.Choices.Count} choices";
            lines.Add($"- {npc.Name} ({npc.Id}) @ {npc.TileX},{npc.TileY} {npc.Role}/{npc.Faction} | {choices}");
        }

        if (snapshot.Npcs.Count == 0)
        {
            lines.Add("none");
        }
    }

    private static void AppendDeveloperWorldPage(List<string> lines, ClientInterestSnapshot snapshot)
    {
        lines.Add($"Map chunks: {snapshot.MapChunks.Count} | Items: {snapshot.WorldItems.Count} | Structures: {snapshot.Structures.Count}");
        lines.Add($"Sync: after {snapshot.SyncHint.AfterTick} | delta {snapshot.SyncHint.IsDelta} | map rev {snapshot.SyncHint.VisibleMapRevision}");
        lines.Add(string.Empty);
        lines.Add("Visible items:");
        foreach (var item in snapshot.WorldItems.Take(10))
        {
            lines.Add($"- {item.ItemId} ({item.EntityId}) @ {item.TileX},{item.TileY}");
        }

        if (snapshot.WorldItems.Count == 0)
        {
            lines.Add("none");
        }

        lines.Add(string.Empty);
        lines.Add("Visible structures:");
        foreach (var structure in snapshot.Structures.Take(8))
        {
            lines.Add($"- {structure.Name} ({structure.EntityId}) @ {structure.TileX},{structure.TileY} {structure.Condition}/{structure.Integrity}%");
        }
    }

    private static void AppendDeveloperEventsPage(List<string> lines, ClientInterestSnapshot snapshot)
    {
        lines.Add($"Events: server {snapshot.SyncHint.ServerEventCount}, world {snapshot.SyncHint.WorldEventCount}, local chat {snapshot.LocalChatMessages.Count}");
        lines.Add(string.Empty);
        lines.Add("Recent local chat:");
        foreach (var chat in snapshot.LocalChatMessages.TakeLast(6))
        {
            var volumePercent = Mathf.RoundToInt(chat.Volume * 100f);
            lines.Add($"- [{chat.Tick}] {chat.SpeakerName} ({chat.DistanceTiles} tiles/{volumePercent}%): {chat.Text}");
        }

        if (snapshot.LocalChatMessages.Count == 0)
        {
            lines.Add("none");
        }

        lines.Add(string.Empty);
        lines.Add("Recent server events:");
        foreach (var serverEvent in snapshot.ServerEvents.TakeLast(8))
        {
            lines.Add($"- [{serverEvent.Tick}] {serverEvent.EventId}: {serverEvent.Description}");
        }

        if (snapshot.ServerEvents.Count == 0)
        {
            lines.Add("none");
        }

        lines.Add(string.Empty);
        lines.Add("Recent world events:");
        foreach (var worldEvent in snapshot.WorldEvents.TakeLast(8))
        {
            lines.Add($"- {worldEvent.Type}: {worldEvent.Summary}");
        }

        if (snapshot.WorldEvents.Count == 0)
        {
            lines.Add("none");
        }
    }

    private static string FormatEquipped(
        IReadOnlyDictionary<EquipmentSlot, GameItem> equipment,
        EquipmentSlot slot)
    {
        return equipment is not null && equipment.TryGetValue(slot, out var item)
            ? $"{item.Name} [{ItemText.FormatSummary(item)}]"
            : "empty";
    }
}
