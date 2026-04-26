using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Core;
using Karma.Data;
using Karma.Net;

namespace Karma.UI;

public partial class HudController : CanvasLayer
{
    public const string MainMenuScenePath = "res://scenes/MainMenu.tscn";
    private GameState _gameState = null!;
    private Label _karmaLabel = new();
    private Label _eventLabel = new();
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
    private ClientInterestSnapshot _lastSnapshot;
    private PanelContainer _escapeMenuPanel = new();
    private Control _escapeOptionsPanel = new();
    private Label _escapeMenuStatusLabel = new();
    private Button _resumeButton = new();
    private Button _escapeOptionsButton = new();
    private Button _backToMenuButton = new();
    private Button _quitButton = new();
    private Button _closeEscapeOptionsButton = new();
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
        _backToMenuButton.Pressed += ReturnToMainMenu;
        _quitButton.Pressed += () => GetTree().Quit();
        _closeEscapeOptionsButton.Pressed += HideEscapeOptions;
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

        if (key.Keycode == Key.Escape)
        {
            ToggleEscapeMenu();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.Quoteleft || key.Keycode == Key.Asciitilde)
        {
            ToggleDeveloperOverlay();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.I)
        {
            ToggleInventoryOverlay();
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

    public void ShowStamina(string staminaText)
    {
        _staminaLabel.Text = staminaText;
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
        _escapeOptionsPanel.Visible = true;
        _escapeMenuStatusLabel.Text = "Options are live-menu placeholders; gameplay keeps running.";
    }

    private void HideEscapeOptions()
    {
        _escapeOptionsPanel.Visible = false;
        _escapeMenuStatusLabel.Text = "Menu open. Prototype world is still running.";
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
            OffsetTop = 128,
            OffsetRight = 700,
            OffsetBottom = 158,
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

        BuildDeveloperOverlay(root);
        BuildEscapeMenu(root);
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
        _backToMenuButton = new Button { Name = "MainMenuButton", Text = "Main Menu" };
        _quitButton = new Button { Name = "QuitButton", Text = "Quit" };
        content.AddChild(_resumeButton);
        content.AddChild(_escapeOptionsButton);
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
        }

        _syncLabel.Text = $"Sync: {snapshotSummary}";
        if (_developerPanel.Visible)
        {
            RefreshDeveloperOverlay();
        }
    }

    private void RefreshDeveloperOverlay()
    {
        _developerOverlayLabel.Text = FormatDeveloperOverlay(_lastSnapshot, _perfLabel.Text);
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

        if (latest.EventId.Contains("player_attacked"))
        {
            var attacker = ReadEventData(latest, "playerId", "Someone");
            var target = ReadEventData(latest, "targetId", "someone");
            var damage = ReadEventData(latest, "rawDamage", "?");
            var health = ReadEventData(latest, "targetHealth", "?");
            var maxHealth = ReadEventData(latest, "targetMaxHealth", "?");
            if (ReadEventData(latest, "died", "False") == "True")
            {
                var drops = ReadEventData(latest, "droppedItemCount", "0");
                var x = ReadEventData(latest, "respawnX", "?");
                var y = ReadEventData(latest, "respawnY", "?");
                return $"{attacker} hit {target} for {damage}. {target} broke, dropped {drops}, and respawned at {x},{y}.";
            }

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

    public static string FormatDeveloperOverlay(ClientInterestSnapshot snapshot, string perfLine)
    {
        if (snapshot is null)
        {
            return "Developer Overlay (~)\nWaiting for local snapshot.";
        }

        var local = snapshot.Players.FirstOrDefault(player => player.Id == snapshot.PlayerId) ?? snapshot.Players.FirstOrDefault();
        var lines = new List<string>
        {
            "Developer Overlay (~)",
            perfLine,
            $"World: {snapshot.WorldId} | Tick: {snapshot.Tick} | Radius: {snapshot.InterestRadiusTiles}",
            $"Map chunks: {snapshot.MapChunks.Count} | Items: {snapshot.WorldItems.Count} | Structures: {snapshot.Structures.Count}",
            $"Events: server {snapshot.SyncHint.ServerEventCount}, world {snapshot.SyncHint.WorldEventCount}",
            string.Empty,
            "Local player:"
        };

        if (local is null)
        {
            lines.Add("missing");
        }
        else
        {
            lines.Add($"{local.DisplayName} ({local.Id}) @ {local.TileX},{local.TileY}");
            lines.Add($"Karma {local.Karma:+#;-#;0} | {local.Tier} rank {local.KarmaRank} | {local.KarmaProgress}");
            lines.Add($"HP {local.Health}/{local.MaxHealth} | Scrip {local.Scrip} | Standing {local.Standing}");
            lines.Add($"Inventory: {local.InventoryItemIds.Count} | Equipment: {string.Join(", ", local.EquipmentItemIds.Select(pair => $"{pair.Key}:{pair.Value}"))}");
            lines.Add($"Status: {(local.StatusEffects.Count == 0 ? "none" : string.Join(", ", local.StatusEffects))}");
        }

        lines.Add(string.Empty);
        lines.Add("Nearby players:");
        foreach (var player in snapshot.Players.Where(player => player.Id != snapshot.PlayerId).Take(8))
        {
            lines.Add($"- {player.DisplayName} ({player.Id}) @ {player.TileX},{player.TileY} HP {player.Health}/{player.MaxHealth} Karma {player.Karma:+#;-#;0} {player.Tier}");
        }

        if (snapshot.Players.Count <= 1)
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

        lines.Add(string.Empty);
        lines.Add("Tilde closes this overlay. Esc opens the non-pausing menu.");
        return string.Join("\n", lines);
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
