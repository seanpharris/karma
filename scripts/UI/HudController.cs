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
    public const string FirstRunTutorialPath = "user://first_run.json";
    public const string MedievalPanelFramePath = "res://assets/art/third_party/kenney_fantasy_ui_borders/preview/panel_frame_000.png";
    public sealed record StatusStripEntry(string Status, string Glyph, Color Color);
    public sealed record UiArtPreviewAsset(string Label, string TexturePath, Vector2 PreviewSize);
    private static readonly UiArtPreviewAsset[] MedievalUiArtPreviewAssets =
    {
        new("Panel", MedievalPanelFramePath, new Vector2(64, 64)),
        new("Border", "res://assets/art/third_party/kenney_fantasy_ui_borders/preview/panel_border_020.png", new Vector2(64, 64)),
        new("Divider", "res://assets/art/third_party/kenney_fantasy_ui_borders/preview/divider_004.png", new Vector2(128, 48))
    };
    private GameState _gameState = null!;
    private Label _karmaLabel = new();
    private KarmaTierBadge _karmaBadge;
    private KarmaDualityBar _karmaDualityBar;
    private Label _eventLabel = new();
    private TextureRect _eventIcon = new();
    private Label _chatLabel = new();
    private PanelContainer _chatInputPanel = new();
    private LineEdit _chatInput = new();
    private Label _staminaLabel = new();
    private Label _healthLabel = new();
    private ProgressBar _healthBar = new();
    private ProgressBar _staminaBar = new();
    private ProgressBar _ammoBar = new();
    private ProgressBar _hungerBar = new();
    private Control _staminaRow;
    private Control _healthRow;
    private Control _ammoRow;
    private Control _hungerRow;
    private HBoxContainer _statusStrip = new();
    private Label _ammoLabel = new();
    private Label _hungerLabel = new();
    private HSlider _pauseMasterVolumeSlider;
    private HSlider _pauseMusicVolumeSlider;
    private HSlider _pauseEffectsVolumeSlider;
    private HSlider _pauseAmbientVolumeSlider;
    private Label _pauseMasterVolumeLabel;
    private Label _pauseMusicVolumeLabel;
    private Label _pauseEffectsVolumeLabel;
    private Label _pauseAmbientVolumeLabel;
    private ColorRect _karmaBreakFlash = new();
    private long _lastKarmaBreakFlashTick = -1;
    private ColorRect _contrabandFlash = new();
    private long _lastContrabandFlashTick = -1;
    private long _lastVoiceBarkTick = -1;
    private string _lastEventStingerKey = string.Empty;
    private AudioStreamPlayer _eventStingerPlayer;
    private Label _inventoryLabel = new();
    private Label _leaderboardLabel = new();
    private Label _perksLabel = new();
    private Label _relationshipsLabel = new();
    private Label _factionsLabel = new();
    private Label _questsLabel = new();
    private Label _combatLabel = new();
    private Label _targetLabel = new();
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
    private PanelContainer _npcTooltipPanel = new();
    private Label _npcTooltipLabel = new();
    private PanelContainer _inventoryPanel = new();
    private Label _inventoryOverlayLabel = new();
    private VBoxContainer _inventoryRowsContainer = new();
    private PanelContainer _shopPanel = new();
    private VBoxContainer _shopContainer = new();
    private Label _shopHeaderLabel = new();
    private VBoxContainer _shopRowsContainer = new();
    private string _shopVendorNpcId = string.Empty;
    private bool _shopSellMode;
    private PanelContainer _dialoguePanel = new();
    private VBoxContainer _dialogueContainer = new();
    private Label _dialoguePromptLabel = new();
    private VBoxContainer _dialogueChoicesContainer = new();
    private string _dialogueNpcId = string.Empty;
    private NpcDialogueSnapshot _lastDialogueSnapshot;
    private PanelContainer _hotbarPanel = new();
    private Label _hotbarLabel = new();
    private HBoxContainer _hotbarSlotsContainer = new();
    private readonly Dictionary<int, string> _hotbarBindings = new();
    private PanelContainer _bountyPanel = new();
    private Label _bountyLabel = new();
    private PanelContainer _bountyBoardPanel = new();
    private Label _bountyBoardLabel = new();
    private PanelContainer _factionPanel = new();
    private Label _factionPanelLabel = new();
    private PanelContainer _questLogPanel = new();
    private Label _questLogLabel = new();
    private PanelContainer _matchSummaryPanel = new();
    private Label _matchSummaryLabel = new();
    private PanelContainer _combatLogPanel = new();
    private ScrollContainer _combatLogScroll = new();
    private Label _combatLogLabel = new();
    private PanelContainer _mountBagPanel = new();
    private Label _mountBagLabel = new();
    private PanelContainer _developerPanel = new();
    private Label _developerOverlayLabel = new();
    private int _developerPageIndex;
    private ClientInterestSnapshot _lastSnapshot;
    private PanelContainer _escapeMenuPanel = new();
    private Control _escapeOptionsPanel = new();
    private Label _escapeMenuStatusLabel = new();
    private Button _resumeButton = new();
    private Button _escapeOptionsButton = new();
    private Button _backToMenuButton = new();
    private Button _quitButton = new();
    private Button _closeEscapeOptionsButton = new();
    private Label _posseLabel = new();
    private PanelContainer _possePanel = new();
    private PanelContainer _tutorialOverlay = new();
    private Label _tutorialLabel = new();
    private string _firstRunTutorialMarkerPath = FirstRunTutorialPath;
    private string _lastCombatText = "Combat: none";
    private IReadOnlyList<string> _lastStatusEffects = System.Array.Empty<string>();

    public override void _Ready()
    {
        BuildUi();
        ShowFirstRunTutorial();

        // Boot at the saved volume so opening the pause options later
        // doesn't jump the AudioServer to a different value.
        Karma.Audio.AudioSettings.EnsureBusesExist();
        Karma.Audio.AudioSettings.LoadFromDisk(MainMenuController.OptionsPath);
        Karma.Audio.AudioSettings.ApplyToAudioServer();

        _gameState = GetNode<GameState>("/root/GameState");
        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        ApplyUiPalette(ResolveActiveThemeId());
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
        _chatInput.TextSubmitted += OnChatInputSubmitted;
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

        if (_tutorialOverlay.Visible &&
            (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter || key.Keycode == Key.Escape))
        {
            DismissFirstRunTutorial();
            GetViewport().SetInputAsHandled();
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
            return;
        }

        if (key.Keycode == Key.J)
        {
            ToggleQuestLogPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.L)
        {
            ToggleCombatLogPanel();
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

    public void ShowFirstRunTutorial(string markerPath = FirstRunTutorialPath)
    {
        _firstRunTutorialMarkerPath = string.IsNullOrWhiteSpace(markerPath) ? FirstRunTutorialPath : markerPath;
        _tutorialLabel.Text = FormatFirstRunTutorialText();
        _tutorialOverlay.Visible = !HasSeenFirstRunTutorial(_firstRunTutorialMarkerPath);
    }

    public void DismissFirstRunTutorial()
    {
        _tutorialOverlay.Visible = false;
        MarkFirstRunTutorialSeen(_firstRunTutorialMarkerPath);
    }

    public static string FormatFirstRunTutorialText()
    {
        return "Welcome to Karma. WASD/arrows to move, E to interact, T to chat, J for quest log, Esc for menu.";
    }

    public static bool HasSeenFirstRunTutorial(string markerPath = FirstRunTutorialPath)
    {
        return !string.IsNullOrWhiteSpace(markerPath) && FileAccess.FileExists(markerPath);
    }

    public static bool MarkFirstRunTutorialSeen(string markerPath = FirstRunTutorialPath)
    {
        if (string.IsNullOrWhiteSpace(markerPath)) return false;
        using var marker = FileAccess.Open(markerPath, FileAccess.ModeFlags.Write);
        if (marker is null) return false;
        marker.StoreString("{\"seen\":true}");
        return true;
    }

    public void ToggleInventoryOverlay()
    {
        SetInventoryOverlayVisible(!_inventoryPanel.Visible);
    }

    public void OpenShopForVendor(string vendorNpcId, bool sellMode = false)
    {
        _shopVendorNpcId = vendorNpcId;
        _shopSellMode = sellMode;
        _shopPanel.Visible = true;
        RefreshShopOverlay();
    }

    public void CloseShop()
    {
        _shopVendorNpcId = string.Empty;
        _shopPanel.Visible = false;
    }

    public bool IsShopOpen => _shopPanel.Visible;
    public string ShopVendorNpcId => _shopVendorNpcId;
    public bool ShopSellMode => _shopSellMode;

    public void OpenMountBag(MountSnapshot mount)
    {
        _mountBagLabel.Text = FormatMountBag(mount);
        _mountBagPanel.Visible = true;
    }

    public void CloseMountBag()
    {
        _mountBagPanel.Visible = false;
    }

    public void OpenBountyBoard()
    {
        _bountyBoardLabel.Text = FormatBountyBoard(_lastSnapshot?.Players ?? System.Array.Empty<PlayerSnapshot>());
        _bountyBoardPanel.Visible = true;
    }

    public void CloseBountyBoard()
    {
        _bountyBoardPanel.Visible = false;
    }

    private void RefreshShopOverlay()
    {
        if (!_shopPanel.Visible || _gameState is null) return;
        var activeThemeId = ResolveActiveThemeId();
        foreach (var child in _shopRowsContainer.GetChildren())
            child.QueueFree();

        if (_shopSellMode)
        {
            var inventory = _gameState.Inventory;
            _shopHeaderLabel.Text = inventory.Count == 0
                ? $"-- Nothing to sell --   Scrip: {_gameState.LocalScrip}"
                : $"-- Sell ({inventory.Count})   Scrip: {_gameState.LocalScrip} --";
            for (var i = 0; i < inventory.Count; i++)
            {
                var capturedIndex = i;
                var item = inventory[i];
                var button = new Button { CustomMinimumSize = new Vector2(240, 38) };
                AddItemButtonContent(
                    button,
                    activeThemeId,
                    item.Id,
                    item.Name,
                    new Vector2(32f, 32f),
                    InventoryTintForRarity(item.Rarity));
                button.Pressed += () => SellInventoryRow(capturedIndex);
                _shopRowsContainer.AddChild(button);
            }
        }
        else
        {
            var allOffers = _lastSnapshot?.ShopOffers ?? System.Array.Empty<ShopOfferSnapshot>();
            var vendorOffers = allOffers.Where(o => o.VendorNpcId == _shopVendorNpcId).ToList();
            _shopHeaderLabel.Text = vendorOffers.Count == 0
                ? $"-- No wares available --   Scrip: {_gameState.LocalScrip}"
                : $"-- Wares ({vendorOffers.Count})   Scrip: {_gameState.LocalScrip} --";
            foreach (var offer in vendorOffers)
            {
                var capturedOfferId = offer.OfferId;
                var canAfford = _gameState.LocalScrip >= offer.Price;
                var factionReputation = GetFactionReputation(
                    _lastSnapshot?.Factions,
                    _lastSnapshot?.PlayerId ?? string.Empty,
                    offer.RequiredFactionId);
                var factionLocked = IsShopOfferFactionLocked(offer, factionReputation);
                var rep = offer.MinReputation > 0 ? $" [req {offer.MinReputation}]" : "";
                var lockPrefix = factionLocked ? "🔒 " : string.Empty;
                var label = $"{lockPrefix}{offer.ItemName}  {offer.Price} {offer.Currency}{rep}";
                if (!canAfford && !factionLocked) label += " — insufficient";
                var button = new Button { Disabled = !canAfford && !factionLocked, CustomMinimumSize = new Vector2(260, 38) };
                var pricingTooltip = FormatShopPricingTooltip(offer);
                if (factionLocked)
                {
                    button.TooltipText = FormatFactionStoreDenial(
                        offer.RequiredFactionId,
                        offer.MinReputation,
                        factionReputation) + (string.IsNullOrWhiteSpace(pricingTooltip) ? string.Empty : $"\n{pricingTooltip}");
                }
                else
                {
                    button.TooltipText = pricingTooltip;
                }
                AddItemButtonContent(button, activeThemeId, offer.ItemId, label, new Vector2(32f, 32f));
                button.Pressed += () => BuyShopOffer(capturedOfferId);
                _shopRowsContainer.AddChild(button);
            }
        }

        var closeButton = new Button { Text = "Close" };
        closeButton.Pressed += CloseShop;
        _shopRowsContainer.AddChild(closeButton);
    }

    public void BuyShopOffer(string offerId)
    {
        var serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        if (serverSession is null) return;
        var result = serverSession.PurchaseOffer(offerId);
        if (!result.WasAccepted)
        {
            ShowPrompt(result.RejectionReason);
            return;
        }
        if (_shopPanel.Visible) RefreshShopOverlay();
    }

    public void SellInventoryRow(int index)
    {
        var serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        if (serverSession is null || _gameState is null) return;
        var inventory = _gameState.Inventory;
        if (index < 0 || index >= inventory.Count) return;
        var itemId = inventory[index].Id;
        var result = serverSession.SendLocal(IntentType.SellItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = itemId,
                ["vendorNpcId"] = _shopVendorNpcId
            });
        if (!result.WasAccepted)
        {
            ShowPrompt(result.RejectionReason);
            return;
        }
        if (_shopPanel.Visible) RefreshShopOverlay();
    }

    public int ShopRowCount => _shopRowsContainer.GetChildCount();

    public bool IsDialogueOpen => _dialoguePanel.Visible;
    public string DialogueNpcId => _dialogueNpcId;
    public NpcDialogueSnapshot LastDialogueSnapshot => _lastDialogueSnapshot;

    public void OpenDialogue(string playerId, string npcId)
    {
        var serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        var server = serverSession?.Server;
        if (server is null) return;
        OpenDialogue(server.GetDialogueFor(playerId, npcId), npcId);
    }

    public void OpenDialogue(NpcDialogueSnapshot snapshot, string npcId)
    {
        _dialogueNpcId = npcId;
        _lastDialogueSnapshot = snapshot;
        _dialoguePromptLabel.Text = $"{snapshot.NpcName}\n\n{snapshot.Prompt}";
        foreach (var child in _dialogueChoicesContainer.GetChildren())
            child.QueueFree();

        // Surface a 64×64 portrait at the top of the choice container
        // when the NPC has an `LpcBundleId` that maps to a generated
        // portrait under assets/art/themes/medieval/npc_portraits/.
        var bundleId = _lastSnapshot?.Npcs.FirstOrDefault(n => n.Id == npcId)?.LpcBundleId;
        if (!string.IsNullOrWhiteSpace(bundleId))
        {
            var portrait = ThemedArtRegistry.GetExact("npc_portraits", bundleId);
            if (portrait is not null)
            {
                var portraitRect = new TextureRect
                {
                    Texture = portrait,
                    CustomMinimumSize = new Vector2(64, 64),
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                    SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
                };
                _dialogueChoicesContainer.AddChild(portraitRect);
            }
        }
        for (var i = 0; i < snapshot.Choices.Count; i++)
        {
            var choice = snapshot.Choices[i];
            var button = new Button { Text = $"{i + 1}. {choice.Label}" };
            var capturedId = choice.Id;
            button.Pressed += () => SelectDialogueChoice(capturedId);
            _dialogueChoicesContainer.AddChild(button);
        }
        var closeButton = new Button { Text = "Cancel" };
        closeButton.Pressed += CloseDialogue;
        _dialogueChoicesContainer.AddChild(closeButton);
        _dialoguePanel.Visible = true;
    }

    public void CloseDialogue()
    {
        _dialogueNpcId = string.Empty;
        _dialoguePanel.Visible = false;
    }

    public void SelectDialogueChoice(string choiceId)
    {
        if (string.IsNullOrEmpty(_dialogueNpcId)) return;
        var npcId = _dialogueNpcId;
        var choice = _lastDialogueSnapshot?.Choices.FirstOrDefault(c => c.Id == choiceId);
        if (choice is null) return;

        if (choice.ActionId == "open_shop_browse")
        {
            CloseDialogue();
            OpenShopForVendor(npcId, sellMode: false);
            return;
        }
        if (choice.ActionId == "open_shop_sell")
        {
            CloseDialogue();
            OpenShopForVendor(npcId, sellMode: true);
            return;
        }

        var serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        if (serverSession is not null)
        {
            serverSession.SendLocal(IntentType.SelectDialogueChoice,
                new System.Collections.Generic.Dictionary<string, string>
                {
                    ["npcId"] = npcId,
                    ["choiceId"] = choiceId
                });
        }
        CloseDialogue();
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

    public void ToggleQuestLogPanel()
    {
        SetQuestLogPanelVisible(!_questLogPanel.Visible);
    }

    public void ToggleCombatLogPanel()
    {
        SetCombatLogPanelVisible(!_combatLogPanel.Visible);
    }

    public void SetCombatLogPanelVisible(bool visible)
    {
        _combatLogPanel.Visible = visible;
        if (visible)
        {
            RefreshCombatLogPanel(_lastSnapshot?.ServerEvents ?? System.Array.Empty<ServerEvent>());
        }
    }

    public void SetQuestLogPanelVisible(bool visible)
    {
        _questLogPanel.Visible = visible;
        if (visible)
        {
            RefreshQuestLogPanel();
        }
    }

    private void RefreshQuestLogPanel()
    {
        _questLogLabel.Text = FormatQuestLog(_lastSnapshot?.Quests ?? System.Array.Empty<QuestSnapshot>());
    }

    private void RefreshCombatLogPanel(IReadOnlyList<ServerEvent> serverEvents)
    {
        _combatLogLabel.Text = FormatCombatLog(serverEvents);
        CallDeferred(nameof(ScrollCombatLogToBottom));
    }

    private void ScrollCombatLogToBottom()
    {
        if (_combatLogScroll is null) return;
        _combatLogScroll.ScrollVertical = (int)_combatLogScroll.GetVScrollBar().MaxValue;
    }

    private void RefreshStatusStrip(IReadOnlyList<string> statusEffects)
    {
        foreach (var child in _statusStrip.GetChildren())
            child.QueueFree();

        foreach (var entry in FormatStatusStrip(statusEffects, UiPaletteRegistry.Get(ResolveActiveThemeId())))
        {
            // Try the medieval status_icons PNG first; fall back to
            // the placeholder colored glyph if no art exists for the
            // status id.
            var iconKey = "status_" + entry.Status.ToLowerInvariant();
            var iconTex = ThemedArtRegistry.GetExact("status_icons", iconKey);
            if (iconTex is not null)
            {
                var rect = new TextureRect
                {
                    Texture = iconTex,
                    TooltipText = entry.Status,
                    CustomMinimumSize = new Vector2(24, 24),
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                };
                _statusStrip.AddChild(rect);
                continue;
            }

            var label = new Label
            {
                Text = entry.Glyph,
                TooltipText = entry.Status,
                CustomMinimumSize = new Vector2(22, 22),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            label.AddThemeColorOverride("font_color", entry.Color);
            _statusStrip.AddChild(label);
        }
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

    private void RefreshFactionPanel(ClientInterestSnapshot snapshot)
    {
        _factionPanelLabel.Text = FormatFactionPanel(snapshot?.Factions, snapshot?.PlayerId ?? string.Empty);
    }

    private void RefreshNpcTooltip(ClientInterestSnapshot snapshot)
    {
        var text = FormatNpcApproachTooltip(snapshot, 2);
        _npcTooltipLabel.Text = text;
        _npcTooltipPanel.Visible = !string.IsNullOrWhiteSpace(text);
    }

    private void RefreshDeathPileOwnershipPrompt(ClientInterestSnapshot snapshot)
    {
        var prompt = FormatDeathPileOwnershipPrompt(snapshot);
        if (!string.IsNullOrWhiteSpace(prompt))
        {
            ShowPrompt(prompt);
        }
    }

    public void ShowStamina(string staminaText)
    {
        _staminaLabel.Text = staminaText;
    }

    public void ShowStamina(float stamina, float maxStamina, bool isExhausted)
    {
        _staminaLabel.Text = FormatMovementStamina(stamina, maxStamina, isExhausted);
        var safeMax = MathF.Max(1f, maxStamina);
        var clamped = Math.Clamp(stamina, 0f, safeMax);
        _staminaBar.Value = clamped / safeMax * 100.0;
    }

    public static string FormatMovementStamina(float stamina, float maxStamina, bool isExhausted)
    {
        var roundedStamina = Mathf.RoundToInt(stamina);
        var roundedMax = Mathf.RoundToInt(MathF.Max(1f, maxStamina));
        if (isExhausted) return $"Stamina  {roundedStamina} / {roundedMax}  (winded)";
        return maxStamina > 0f && stamina / maxStamina <= 0.25f
            ? $"Stamina  {roundedStamina} / {roundedMax}  (low)"
            : $"Stamina  {roundedStamina} / {roundedMax}";
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

    public static IReadOnlyList<UiArtPreviewAsset> GetMedievalUiArtPreviewAssets()
    {
        return MedievalUiArtPreviewAssets;
    }

    public void ToggleEscapeMenu()
    {
        var anyOpen = _escapeMenuPanel.Visible || _escapeOptionsPanel.Visible;
        SetEscapeMenuVisible(!anyOpen);
    }

    public void HideEscapeMenu()
    {
        SetEscapeMenuVisible(false);
    }

    public void SetEscapeMenuVisible(bool visible)
    {
        if (visible)
        {
            // Always open onto the pause main view; sub-panels stay hidden.
            _escapeOptionsPanel.Visible = false;
            _escapeMenuPanel.Visible = true;
        }
        else
        {
            _escapeMenuPanel.Visible = false;
            _escapeOptionsPanel.Visible = false;
        }
    }

    private void ShowEscapeOptions()
    {
        LoadPauseAudioSettings();
        ApplyPauseAudioSettings();
        _escapeMenuPanel.Visible = false;
        _escapeOptionsPanel.Visible = true;
    }

    private void BuildPauseVolumeRow(VBoxContainer parent, string title, out HSlider slider, out Label valueLabel)
    {
        var row = new HBoxContainer { Name = $"{title}VolumeRow" };
        row.AddThemeConstantOverride("separation", 12);
        var titleLabel = MenuTheme.MakeBodyLabel($"{title} volume");
        titleLabel.CustomMinimumSize = new Vector2(140, 0);
        slider = new HSlider
        {
            Name = $"{title}VolumeSlider",
            MinValue = 0,
            MaxValue = 100,
            Step = 1,
            Value = 80,
            CustomMinimumSize = new Vector2(220, 20),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        MenuTheme.StyleSlider(slider);
        valueLabel = MenuTheme.MakeSubtleLabel("80%");
        valueLabel.CustomMinimumSize = new Vector2(56, 0);
        valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(titleLabel);
        row.AddChild(slider);
        row.AddChild(valueLabel);
        parent.AddChild(row);
        slider.ValueChanged += _ =>
        {
            RefreshPauseVolumeLabels();
            ApplyPauseAudioSettings();
            SavePauseAudioSettings();
        };
    }

    private void RefreshPauseVolumeLabels()
    {
        if (_pauseMasterVolumeLabel is null) return;
        _pauseMasterVolumeLabel.Text = $"{Math.Round(_pauseMasterVolumeSlider.Value)}%";
        _pauseMusicVolumeLabel.Text = $"{Math.Round(_pauseMusicVolumeSlider.Value)}%";
        _pauseEffectsVolumeLabel.Text = $"{Math.Round(_pauseEffectsVolumeSlider.Value)}%";
        _pauseAmbientVolumeLabel.Text = $"{Math.Round(_pauseAmbientVolumeSlider.Value)}%";
    }

    private void LoadPauseAudioSettings()
    {
        if (_pauseMasterVolumeSlider is null) return;
        Karma.Audio.AudioSettings.LoadFromDisk(MainMenuController.OptionsPath);
        _pauseMasterVolumeSlider.Value = Karma.Audio.AudioSettings.MasterVolume;
        _pauseMusicVolumeSlider.Value = Karma.Audio.AudioSettings.MusicVolume;
        _pauseEffectsVolumeSlider.Value = Karma.Audio.AudioSettings.SfxVolume;
        _pauseAmbientVolumeSlider.Value = Karma.Audio.AudioSettings.AmbientVolume;
        RefreshPauseVolumeLabels();
    }

    private void SavePauseAudioSettings()
    {
        if (_pauseMasterVolumeSlider is null) return;
        SyncPauseSlidersToAudioSettings();
        Karma.Audio.AudioSettings.SaveToDisk(MainMenuController.OptionsPath);
    }

    private void ApplyPauseAudioSettings()
    {
        if (_pauseMasterVolumeSlider is null) return;
        SyncPauseSlidersToAudioSettings();
        Karma.Audio.AudioSettings.EnsureBusesExist();
        Karma.Audio.AudioSettings.ApplyToAudioServer();

        // MusicPlayer sits on the Music bus, so AudioSettings owns
        // the live slider gain for every gameplay music source.
    }

    private void SyncPauseSlidersToAudioSettings()
    {
        Karma.Audio.AudioSettings.MasterVolume = _pauseMasterVolumeSlider.Value;
        Karma.Audio.AudioSettings.MusicVolume = _pauseMusicVolumeSlider.Value;
        Karma.Audio.AudioSettings.SfxVolume = _pauseEffectsVolumeSlider.Value;
        Karma.Audio.AudioSettings.AmbientVolume = _pauseAmbientVolumeSlider.Value;
    }

    public static float PercentToDb(double percent) => Karma.Audio.AudioSettings.PercentToDb(percent);

    public static float LinearToDb(double linear)
    {
        var clamped = Math.Clamp(linear, 0.0, 1.0);
        if (clamped <= 0.001) return -80f;
        return (float)(20.0 * Math.Log10(clamped));
    }

    private void HideEscapeOptions()
    {
        // "Back" returns to the pause main view rather than closing pause.
        _escapeOptionsPanel.Visible = false;
        _escapeMenuPanel.Visible = true;
    }

    // Removed: appearance panel UI (cycle buttons + labels) and the
    // instance methods that drove it. Keyboard shortcuts in
    // PlayerController still cycle layers through the server intent
    // path; only the pause-menu surface is gone.

    private void ReturnToMainMenu()
    {
        // Wipe per-match state on the GameState autoload so the next
        // match the player starts begins fresh, not inheriting this
        // round's scrip / inventory / quests / karma.
        var gameState = GetNodeOrNull<GameState>("/root/GameState");
        gameState?.ResetForNewMatch();
        var serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        serverSession?.RestartForNewMatch();
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

        _eventIcon = new TextureRect
        {
            OffsetLeft = 16,
            OffsetTop = 88,
            OffsetRight = 48,
            OffsetBottom = 120,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        root.AddChild(_eventIcon);

        _eventLabel = new Label
        {
            OffsetLeft = 56,
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

        BuildVitalsPanel(root);

        _statusStrip = new HBoxContainer
        {
            Name = "StatusStrip",
            OffsetLeft = 560,
            OffsetTop = 24,
            OffsetRight = 860,
            OffsetBottom = 56
        };
        root.AddChild(_statusStrip);

        _karmaBreakFlash = new ColorRect
        {
            Name = "KarmaBreakFlash",
            Color = new Color(1f, 1f, 1f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorRight = 1f,
            AnchorBottom = 1f
        };
        root.AddChild(_karmaBreakFlash);

        _contrabandFlash = new ColorRect
        {
            Name = "ContrabandFlash",
            Color = new Color(1f, 0.15f, 0.15f, 0f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorRight = 1f,
            AnchorBottom = 1f
        };
        root.AddChild(_contrabandFlash);

        _eventStingerPlayer = new AudioStreamPlayer
        {
            Name = "EventStingerPlayer",
            Bus = "Master"
        };
        root.AddChild(_eventStingerPlayer);

        _npcTooltipPanel = new PanelContainer
        {
            Name = "NpcTooltipPanel",
            OffsetLeft = 420,
            OffsetTop = 16,
            OffsetRight = 860,
            OffsetBottom = 52,
            Visible = false
        };
        root.AddChild(_npcTooltipPanel);

        _npcTooltipLabel = new Label
        {
            Name = "NpcTooltipLabel",
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _npcTooltipPanel.AddChild(_npcTooltipLabel);

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

        _targetLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 350,
            OffsetRight = 900,
            OffsetBottom = 380,
            Text = "Target: none in range"
        };
        root.AddChild(_targetLabel);

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

        var inventoryContent = new VBoxContainer();
        _inventoryPanel.AddChild(inventoryContent);

        _inventoryOverlayLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = "Inventory"
        };
        inventoryContent.AddChild(_inventoryOverlayLabel);

        _inventoryRowsContainer = new VBoxContainer();
        inventoryContent.AddChild(_inventoryRowsContainer);

        _shopPanel = new PanelContainer
        {
            OffsetLeft = 480,
            OffsetTop = 200,
            OffsetRight = 880,
            OffsetBottom = 540,
            Visible = false
        };
        root.AddChild(_shopPanel);

        _shopContainer = new VBoxContainer();
        _shopPanel.AddChild(_shopContainer);

        _shopHeaderLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = "Shop"
        };
        _shopContainer.AddChild(_shopHeaderLabel);

        _shopRowsContainer = new VBoxContainer();
        _shopContainer.AddChild(_shopRowsContainer);

        _dialoguePanel = new PanelContainer
        {
            OffsetLeft = 240,
            OffsetTop = 220,
            OffsetRight = 720,
            OffsetBottom = 540,
            Visible = false
        };
        root.AddChild(_dialoguePanel);

        _dialogueContainer = new VBoxContainer();
        _dialoguePanel.AddChild(_dialogueContainer);

        _dialoguePromptLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Text = "Dialogue"
        };
        _dialogueContainer.AddChild(_dialoguePromptLabel);

        _dialogueChoicesContainer = new VBoxContainer();
        _dialogueContainer.AddChild(_dialogueChoicesContainer);

        _hotbarPanel = new PanelContainer
        {
            OffsetLeft = 16,
            OffsetTop = 640,
            OffsetRight = 760,
            OffsetBottom = 680
        };
        root.AddChild(_hotbarPanel);

        var hotbarContent = new VBoxContainer();
        _hotbarPanel.AddChild(hotbarContent);

        _hotbarLabel = new Label
        {
            Text = FormatHotbar(System.Array.Empty<GameItem>(), -1)
        };
        hotbarContent.AddChild(_hotbarLabel);

        _hotbarSlotsContainer = new HBoxContainer();
        hotbarContent.AddChild(_hotbarSlotsContainer);

        // Karma tier badge replaces the minimap — top-down view means
        // the player always knows orientation, so a tier crest with
        // progress ring is more useful in this corner.
        _karmaBadge = new KarmaTierBadge
        {
            Name = "KarmaTierBadge",
            OffsetLeft = 1110,
            OffsetTop = 16,
            OffsetRight = 1110 + 140,
            OffsetBottom = 16 + 140 + 56
        };
        _karmaBadge.SetMeta(PaletteOptOutMeta, true);
        root.AddChild(_karmaBadge);

        // Karma duality spectrum bar: top-center, anchored so it
        // stays centered as the window resizes. Shows the player's
        // position on the Paragon ↔ Renegade spectrum.
        _karmaDualityBar = new KarmaDualityBar
        {
            Name = "KarmaDualityBar",
            AnchorLeft = 0.5f,
            AnchorRight = 0.5f,
            OffsetLeft = -KarmaDualityBar.BarWidth * 0.5f,
            OffsetRight = KarmaDualityBar.BarWidth * 0.5f,
            OffsetTop = 18,
            OffsetBottom = 18 + KarmaDualityBar.BarHeight + 22
        };
        _karmaDualityBar.SetMeta(PaletteOptOutMeta, true);
        root.AddChild(_karmaDualityBar);

        _bountyPanel = new PanelContainer
        {
            OffsetLeft = 1100,
            OffsetTop = 220,
            OffsetRight = 1280,
            OffsetBottom = 360
        };
        root.AddChild(_bountyPanel);

        _bountyLabel = new Label
        {
            Text = "Bounties: --",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _bountyPanel.AddChild(_bountyLabel);

        _bountyBoardPanel = new PanelContainer
        {
            Name = "BountyBoardPanel",
            OffsetLeft = 360,
            OffsetTop = 180,
            OffsetRight = 860,
            OffsetBottom = 420,
            Visible = false
        };
        root.AddChild(_bountyBoardPanel);

        _bountyBoardLabel = new Label
        {
            Name = "BountyBoardLabel",
            Text = "Bounty board: none active",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _bountyBoardPanel.AddChild(_bountyBoardLabel);

        _factionPanel = new PanelContainer
        {
            Name = "FactionPanel",
            OffsetLeft = 1100,
            OffsetTop = 376,
            OffsetRight = 1280,
            OffsetBottom = 520
        };
        root.AddChild(_factionPanel);

        _factionPanelLabel = new Label
        {
            Name = "FactionPanelLabel",
            Text = "Factions: --",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _factionPanel.AddChild(_factionPanelLabel);

        _questLogPanel = new PanelContainer
        {
            Name = "QuestLogPanel",
            OffsetLeft = 530,
            OffsetTop = 184,
            OffsetRight = 900,
            OffsetBottom = 356,
            Visible = false
        };
        root.AddChild(_questLogPanel);

        var questLogMargin = new MarginContainer { Name = "QuestLogPanelMargin" };
        questLogMargin.AddThemeConstantOverride("margin_left", 12);
        questLogMargin.AddThemeConstantOverride("margin_top", 8);
        questLogMargin.AddThemeConstantOverride("margin_right", 12);
        questLogMargin.AddThemeConstantOverride("margin_bottom", 8);
        _questLogPanel.AddChild(questLogMargin);

        _questLogLabel = new Label
        {
            Name = "QuestLogPanelLabel",
            Text = "Quest log: no active quests",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        questLogMargin.AddChild(_questLogLabel);

        _matchSummaryPanel = new PanelContainer
        {
            Name = "MatchSummaryPanel",
            OffsetLeft = 200,
            OffsetTop = 220,
            OffsetRight = 1080,
            OffsetBottom = 540,
            Visible = false
        };
        root.AddChild(_matchSummaryPanel);

        var matchSummaryContent = new VBoxContainer
        {
            Name = "MatchSummaryContent"
        };
        matchSummaryContent.AddThemeConstantOverride("separation", 8);
        _matchSummaryPanel.AddChild(matchSummaryContent);

        _matchSummaryLabel = new Label
        {
            Name = "MatchSummaryLabel",
            Text = "Match in progress.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        matchSummaryContent.AddChild(_matchSummaryLabel);

        var returnToMenuButton = new Button
        {
            Name = "ReturnToMainMenuButton",
            Text = "Return to Main Menu",
            CustomMinimumSize = new Vector2(220, 34)
        };
        returnToMenuButton.Pressed += ReturnToMainMenu;
        matchSummaryContent.AddChild(returnToMenuButton);

        BuildCombatLogPanel(root);
        BuildMountBagPanel(root);
        BuildPossePanel(root);
        BuildDeveloperOverlay(root);
        BuildEscapeMenu(root);
        BuildFirstRunTutorialOverlay(root);
    }

    // Themed vitals readout — pack icon + name/value label + slim
    // gold-fill progress bar per row. The panel uses MenuTheme so it
    // matches the karma duality main + pause menus, and opts out of the
    // medieval palette walker so the styling sticks.
    private static readonly Color HealthBarColor = new(0.86f, 0.22f, 0.22f);
    private static readonly Color StaminaBarColor = new(0.95f, 0.80f, 0.32f);
    private static readonly Color AmmoBarColor = new(0.78f, 0.84f, 0.95f);
    private static readonly Color HungerBarColor = new(0.95f, 0.55f, 0.18f);

    // 4-icon strip from the etahoshi pack — 68×17 = 4 icons of 17×17.
    // Slot 0 is the heart; remaining slots are weapon-ish glyphs +
    // gold/coin. Each vital row picks one slot.
    private const string AttributesIconsPath = "res://assets/art/third_party/Fantasy Minimal Pixel Art GUI by eta-commercial-free/UI/AttributesIcons_17x17.png";
    private const int AttributesIconSize = 17;

    private void BuildVitalsPanel(Control root)
    {
        var panel = new PanelContainer
        {
            Name = "VitalsPanel",
            OffsetLeft = 300,
            OffsetTop = 16,
            OffsetRight = 540,
            OffsetBottom = 220
        };
        panel.AddThemeStyleboxOverride("panel", MenuTheme.MakePanelStyle());
        panel.SetMeta(PaletteOptOutMeta, true);
        root.AddChild(panel);

        var content = new VBoxContainer { Name = "VitalsContent" };
        content.AddThemeConstantOverride("separation", 8);
        panel.AddChild(content);

        // Pack icon assignments — slot 0 is the heart, so Health gets
        // it. Remaining slots (1-3) are mapped by visual fit.
        _healthRow = BuildVitalRow(content, "Health", HealthBarColor, iconSlot: 0, out _healthLabel, out _healthBar);
        _healthLabel.Text = "Health 100 / 100";
        _healthBar.Value = 100;

        _staminaRow = BuildVitalRow(content, "Stamina", StaminaBarColor, iconSlot: 2, out _staminaLabel, out _staminaBar);
        _staminaLabel.Text = "Stamina 100 / 100";
        _staminaBar.Value = 100;

        _ammoRow = BuildVitalRow(content, "Ammo", AmmoBarColor, iconSlot: 1, out _ammoLabel, out _ammoBar);
        _ammoRow.Visible = false;

        _hungerRow = BuildVitalRow(content, "Hunger", HungerBarColor, iconSlot: 3, out _hungerLabel, out _hungerBar);
        _hungerRow.Visible = false;
    }

    private static Control BuildVitalRow(VBoxContainer parent, string name, Color tint, int iconSlot, out Label valueLabel, out ProgressBar bar)
    {
        var row = new VBoxContainer { Name = $"{name}VitalRow" };
        row.AddThemeConstantOverride("separation", 2);
        parent.AddChild(row);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        row.AddChild(header);

        // Icon: 17×17 region from the AttributesIcons strip, scaled 2×
        // (34×34 on screen) so the pixel art reads at HUD distance.
        // Falls back to a colored dot if the texture didn't load.
        var iconTexture = MakeAttributesIcon(iconSlot, tint);
        var icon = new TextureRect
        {
            Texture = iconTexture,
            CustomMinimumSize = new Vector2(AttributesIconSize * 2, AttributesIconSize * 2),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        header.AddChild(icon);

        valueLabel = MenuTheme.MakeBodyLabel(name);
        valueLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        header.AddChild(valueLabel);

        bar = new ProgressBar
        {
            Name = $"{name}Bar",
            MinValue = 0,
            MaxValue = 100,
            Value = 0,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 6)
        };
        StyleVitalBar(bar, tint);
        row.AddChild(bar);

        return row;
    }

    private static void StyleVitalBar(ProgressBar bar, Color fillTint)
    {
        var track = new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.07f, 0.11f, 1f),
            BorderColor = new Color(0.55f, 0.45f, 0.22f, 0.7f),
            BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderWidthTop = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3
        };
        var fill = new StyleBoxFlat
        {
            BgColor = fillTint,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3
        };
        bar.AddThemeStyleboxOverride("background", track);
        bar.AddThemeStyleboxOverride("fill", fill);
    }

    // Slices one 17×17 icon out of AttributesIcons_17x17.png (a 4-icon
    // horizontal strip). Falls back to a procedural colored dot if the
    // pack texture can't be loaded so the HUD still functions.
    private static Texture2D MakeAttributesIcon(int slot, Color fallbackTint)
    {
        var atlas = ResourceLoader.Load<Texture2D>(AttributesIconsPath);
        if (atlas is null) return MakeCircleIcon(14, fallbackTint);
        var clampedSlot = Math.Max(0, slot);
        return new AtlasTexture
        {
            Atlas = atlas,
            Region = new Rect2(clampedSlot * AttributesIconSize, 0, AttributesIconSize, AttributesIconSize)
        };
    }

    // Procedurally-drawn antialiased circle, used as a fallback vitals
    // icon and anywhere else we need a small colored dot without an asset.
    private static Texture2D MakeCircleIcon(int size, Color tint)
    {
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        var c = (size - 1) * 0.5f;
        var r = size * 0.5f;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - c;
                var dy = y - c;
                var d = MathF.Sqrt(dx * dx + dy * dy);
                var alpha = MathF.Max(0f, MathF.Min(1f, r - d));
                img.SetPixel(x, y, new Color(tint.R, tint.G, tint.B, alpha));
            }
        }
        return ImageTexture.CreateFromImage(img);
    }

    private void BuildCombatLogPanel(Control root)
    {
        _combatLogPanel = new PanelContainer
        {
            Name = "CombatLogPanel",
            OffsetLeft = 700,
            OffsetTop = 420,
            OffsetRight = 1264,
            OffsetBottom = 704,
            Visible = false
        };
        root.AddChild(_combatLogPanel);

        _combatLogScroll = new ScrollContainer
        {
            Name = "CombatLogScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        _combatLogPanel.AddChild(_combatLogScroll);

        _combatLogLabel = new Label
        {
            Name = "CombatLogLabel",
            Text = "Combat log: quiet",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _combatLogScroll.AddChild(_combatLogLabel);
    }

    private void BuildMountBagPanel(Control root)
    {
        _mountBagPanel = new PanelContainer
        {
            Name = "MountBagPanel",
            OffsetLeft = 760,
            OffsetTop = 200,
            OffsetRight = 1060,
            OffsetBottom = 380,
            Visible = false
        };
        root.AddChild(_mountBagPanel);

        _mountBagLabel = new Label
        {
            Name = "MountBagLabel",
            Text = "Mount bag: empty",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _mountBagPanel.AddChild(_mountBagLabel);
    }

    private string ResolveActiveThemeId()
    {
        return string.IsNullOrWhiteSpace(_serverSession?.ActiveThemeId)
            ? UiPaletteRegistry.MedievalThemeId
            : _serverSession.ActiveThemeId;
    }

    private void ApplyUiPalette(string themeId)
    {
        var root = GetNodeOrNull<Control>("HudRoot");
        if (root is null) return;
        ApplyUiPalette(root, themeId);
    }

    public static void ApplyUiPalette(Control root, string themeId)
    {
        if (root is null) return;
        ApplyUiPaletteRecursive(root, UiPaletteRegistry.Get(themeId), themeId);
    }

    public static void ApplyUiPalette(Control root, UiPalette palette)
    {
        if (root is null || palette is null) return;
        ApplyUiPaletteRecursive(root, palette, string.Empty);
    }

    // Subtrees that opt out (e.g. the pause menu, which uses MenuTheme's
    // karma duality styling) are skipped entirely — neither the control
    // itself nor any of its descendants get repainted.
    public const string PaletteOptOutMeta = "opt_out_ui_palette";

    private static void ApplyUiPaletteRecursive(Control control, UiPalette palette, string themeId)
    {
        if (control.HasMeta(PaletteOptOutMeta)) return;

        switch (control)
        {
            case PanelContainer panel:
                panel.AddThemeStyleboxOverride("panel", CreatePanelStyle(palette, themeId));
                break;
            case Button button:
                ApplyButtonPalette(button, palette);
                break;
            case Label label:
                label.AddThemeColorOverride("font_color", palette.Text);
                label.AddThemeColorOverride("font_shadow_color", palette.DimText with { A = 0.35f });
                break;
            case LineEdit lineEdit:
                lineEdit.AddThemeColorOverride("font_color", palette.Text);
                lineEdit.AddThemeColorOverride("font_placeholder_color", palette.DimText);
                break;
            case ProgressBar progress:
                progress.AddThemeColorOverride("font_color", palette.Text);
                break;
        }

        foreach (var child in control.GetChildren())
        {
            if (child is Control childControl)
                ApplyUiPaletteRecursive(childControl, palette, themeId);
        }
    }

    private static StyleBox CreatePanelStyle(UiPalette palette, string themeId = "")
    {
        if (UsesMedievalPanelFrameStyle(themeId))
        {
            var medievalStyle = CreateMedievalPanelFrameStyle();
            if (medievalStyle is not null)
                return medievalStyle;
        }

        return CreateFlatPanelStyle(palette);
    }

    public static bool UsesMedievalPanelFrameStyle(string themeId)
    {
        return string.Equals(themeId, UiPaletteRegistry.MedievalThemeId, StringComparison.OrdinalIgnoreCase);
    }

    private static StyleBoxTexture CreateMedievalPanelFrameStyle()
    {
        var texture = AtlasTextureLoader.Load(MedievalPanelFramePath, forceImageLoad: true);
        if (texture is null)
            return null;

        return new StyleBoxTexture
        {
            Texture = texture,
            TextureMarginLeft = 9,
            TextureMarginTop = 9,
            TextureMarginRight = 9,
            TextureMarginBottom = 9,
            ContentMarginLeft = 14,
            ContentMarginTop = 12,
            ContentMarginRight = 14,
            ContentMarginBottom = 12,
            DrawCenter = true
        };
    }

    private static StyleBoxFlat CreateFlatPanelStyle(UiPalette palette)
    {
        var style = new StyleBoxFlat
        {
            BgColor = palette.PanelBackground,
            BorderColor = palette.PanelBorder,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomRight = 6,
            CornerRadiusBottomLeft = 6,
            ContentMarginLeft = 8,
            ContentMarginTop = 6,
            ContentMarginRight = 8,
            ContentMarginBottom = 6
        };
        return style;
    }

    private static void ApplyButtonPalette(Button button, UiPalette palette)
    {
        button.AddThemeColorOverride("font_color", palette.Text);
        button.AddThemeColorOverride("font_hover_color", palette.Text);
        button.AddThemeColorOverride("font_pressed_color", palette.PanelBackground);
        button.AddThemeStyleboxOverride("normal", CreateButtonStyle(palette.PanelBackground, palette.PanelBorder));
        button.AddThemeStyleboxOverride("hover", CreateButtonStyle(palette.Accent, palette.PanelBorder));
        button.AddThemeStyleboxOverride("pressed", CreateButtonStyle(palette.Accent, palette.Accent));
    }

    private static StyleBoxFlat CreateButtonStyle(Color background, Color border)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusBottomLeft = 4,
            ContentMarginLeft = 8,
            ContentMarginTop = 5,
            ContentMarginRight = 8,
            ContentMarginBottom = 5
        };
    }

    private static void AddItemButtonContent(
        Button button,
        string themeId,
        string itemId,
        string labelText,
        Vector2 iconSize,
        Color? labelTint = null)
    {
        if (button is null) return;
        button.Text = string.Empty;

        var content = new HBoxContainer
        {
            Name = "ItemButtonContent",
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 6f,
            OffsetTop = 2f,
            OffsetRight = -6f,
            OffsetBottom = -2f,
            Alignment = BoxContainer.AlignmentMode.Begin,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        content.AddThemeConstantOverride("separation", 6);

        content.AddChild(CreateItemIconRect(themeId, itemId, iconSize));

        var label = new Label
        {
            Name = "ItemButtonLabel",
            Text = string.IsNullOrWhiteSpace(labelText) ? "--" : labelText,
            VerticalAlignment = VerticalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        if (labelTint.HasValue)
        {
            label.AddThemeColorOverride("font_color", labelTint.Value);
            label.AddThemeColorOverride("font_hover_color", labelTint.Value);
        }

        content.AddChild(label);
        button.AddChild(content);
    }

    private static TextureRect CreateItemIconRect(string themeId, string itemId, Vector2 size)
    {
        return new TextureRect
        {
            Name = "ItemIcon",
            Texture = CreateItemIconTexture(themeId, itemId),
            CustomMinimumSize = size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
    }

    private static Texture2D CreateItemIconTexture(string themeId, string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        var themedArt = ItemArtRegistry.Get(themeId, itemId);
        if (themedArt.HasIcon)
        {
            var iconTexture = AtlasTextureLoader.Load(themedArt.IconPath, removeDarkBackground: true);
            if (iconTexture is not null)
            {
                return iconTexture;
            }
        }

        var definition = PrototypeSpriteCatalog.Get(PrototypeSpriteCatalog.GetKindForItem(itemId));
        var texture = AtlasTextureLoader.Load(definition.AtlasPath, removeDarkBackground: true);
        if (texture is null)
        {
            return null;
        }

        return definition.HasAtlasRegion
            ? PrototypeAtlasSprite.CreateAtlasTexture(texture, definition)
            : texture;
    }

    private void BuildFirstRunTutorialOverlay(Control root)
    {
        _tutorialOverlay = new PanelContainer
        {
            Name = "FirstRunTutorialOverlay",
            AnchorLeft = 0.5f,
            AnchorTop = 0.5f,
            AnchorRight = 0.5f,
            AnchorBottom = 0.5f,
            OffsetLeft = -260,
            OffsetTop = -72,
            OffsetRight = 260,
            OffsetBottom = 72,
            Visible = false
        };
        root.AddChild(_tutorialOverlay);

        var margin = new MarginContainer { Name = "FirstRunTutorialMargin" };
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_top", 12);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        _tutorialOverlay.AddChild(margin);

        _tutorialLabel = new Label
        {
            Name = "FirstRunTutorialLabel",
            Text = FormatFirstRunTutorialText(),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        margin.AddChild(_tutorialLabel);
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

        var content = new VBoxContainer
        {
            Name = "DeveloperOverlayContent"
        };
        content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(content);

        content.AddChild(BuildMedievalUiArtPreviewPanel());

        _developerOverlayLabel = new Label
        {
            Name = "DeveloperOverlayLabel",
            Text = "Developer overlay: waiting for snapshot",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        content.AddChild(_developerOverlayLabel);
    }

    private static PanelContainer BuildMedievalUiArtPreviewPanel()
    {
        var previewPanel = new PanelContainer
        {
            Name = "MedievalUiArtPreviewPanel",
            CustomMinimumSize = new Vector2(0, 126)
        };

        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.075f, 0.065f, 0.94f),
            BorderColor = new Color(0.42f, 0.36f, 0.25f, 1f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomRight = 4,
            CornerRadiusBottomLeft = 4
        };
        previewPanel.AddThemeStyleboxOverride("panel", panelStyle);

        var margin = new MarginContainer { Name = "MedievalUiArtPreviewMargin" };
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        previewPanel.AddChild(margin);

        var content = new VBoxContainer { Name = "MedievalUiArtPreviewContent" };
        content.AddThemeConstantOverride("separation", 6);
        margin.AddChild(content);

        var title = new Label
        {
            Name = "MedievalUiArtPreviewLabel",
            Text = "Medieval UI candidates"
        };
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.82f, 0.62f, 1f));
        content.AddChild(title);

        var row = new HBoxContainer { Name = "MedievalUiArtPreviewRow" };
        row.AddThemeConstantOverride("separation", 10);
        content.AddChild(row);

        foreach (var asset in MedievalUiArtPreviewAssets)
        {
            row.AddChild(BuildMedievalUiArtPreviewSample(asset));
        }

        return previewPanel;
    }

    private static VBoxContainer BuildMedievalUiArtPreviewSample(UiArtPreviewAsset asset)
    {
        var sample = new VBoxContainer
        {
            Name = $"{asset.Label}Preview",
            CustomMinimumSize = new Vector2(92, 0)
        };
        sample.AddThemeConstantOverride("separation", 4);

        var frame = new PanelContainer
        {
            Name = $"{asset.Label}PreviewFrame",
            CustomMinimumSize = new Vector2(92, 64)
        };
        var frameStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.015f, 0.014f, 0.012f, 1f),
            BorderColor = new Color(0.22f, 0.19f, 0.15f, 1f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomRight = 3,
            CornerRadiusBottomLeft = 3,
            ContentMarginLeft = 6,
            ContentMarginTop = 6,
            ContentMarginRight = 6,
            ContentMarginBottom = 6
        };
        frame.AddThemeStyleboxOverride("panel", frameStyle);
        sample.AddChild(frame);

        frame.AddChild(new TextureRect
        {
            Name = $"{asset.Label}Texture",
            Texture = AtlasTextureLoader.Load(asset.TexturePath, forceImageLoad: true),
            CustomMinimumSize = asset.PreviewSize,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });

        var label = new Label
        {
            Name = $"{asset.Label}Label",
            Text = asset.Label,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeColorOverride("font_color", new Color(0.82f, 0.76f, 0.62f, 1f));
        sample.AddChild(label);

        return sample;
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
        _escapeMenuPanel.AddThemeStyleboxOverride("panel", MenuTheme.MakePanelStyle());
        // Opt this entire subtree (and its sub-panels) out of the
        // medieval UI palette walker so MenuTheme styling sticks.
        _escapeMenuPanel.SetMeta(PaletteOptOutMeta, true);
        root.AddChild(_escapeMenuPanel);

        var content = new VBoxContainer
        {
            Name = "EscapeMenuContent"
        };
        content.AddThemeConstantOverride("separation", 12);
        _escapeMenuPanel.AddChild(content);

        var title = MenuTheme.MakeTitle("Pause");
        title.Name = "EscapeMenuTitle";
        content.AddChild(title);
        content.AddChild(MenuTheme.MakeDivider());

        _escapeMenuStatusLabel = MenuTheme.MakeSubtleLabel("Menu open. World is still running.");
        _escapeMenuStatusLabel.Name = "EscapeMenuStatusLabel";
        _escapeMenuStatusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _escapeMenuStatusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        content.AddChild(_escapeMenuStatusLabel);

        _resumeButton = new Button { Name = "ResumeButton", Text = "Resume" };
        _escapeOptionsButton = new Button { Name = "OptionsButton", Text = "Options" };
        _backToMenuButton = new Button { Name = "MainMenuButton", Text = "Main Menu" };
        _quitButton = new Button { Name = "QuitButton", Text = "Quit" };
        foreach (var b in new[] { _resumeButton, _escapeOptionsButton, _backToMenuButton, _quitButton })
            MenuTheme.StyleButton(b);
        content.AddChild(_resumeButton);
        content.AddChild(_escapeOptionsButton);
        content.AddChild(_backToMenuButton);
        content.AddChild(_quitButton);

        _escapeOptionsPanel = new PanelContainer
        {
            Name = "EscapeOptionsPanel",
            OffsetLeft = 390,
            OffsetTop = 120,
            OffsetRight = 890,
            OffsetBottom = 620,
            Visible = false
        };
        _escapeOptionsPanel.AddThemeStyleboxOverride("panel", MenuTheme.MakePanelStyle());
        _escapeOptionsPanel.SetMeta(PaletteOptOutMeta, true);
        // Sibling of the pause panel, not a child — clicking Options
        // swaps views (hides pause, shows this) instead of expanding it.
        root.AddChild(_escapeOptionsPanel);

        var optionsContent = new VBoxContainer { Name = "EscapeOptionsContent" };
        optionsContent.AddThemeConstantOverride("separation", 10);
        _escapeOptionsPanel.AddChild(optionsContent);

        optionsContent.AddChild(MenuTheme.MakeSectionHeading("Audio"));
        var optionsHint = MenuTheme.MakeSubtleLabel(
            "Adjustments persist via the same options.cfg the main menu writes. This overlay does not pause the match timer.");
        optionsHint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        optionsContent.AddChild(optionsHint);

        BuildPauseVolumeRow(optionsContent, "Master", out _pauseMasterVolumeSlider, out _pauseMasterVolumeLabel);
        BuildPauseVolumeRow(optionsContent, "Music", out _pauseMusicVolumeSlider, out _pauseMusicVolumeLabel);
        BuildPauseVolumeRow(optionsContent, "Effects", out _pauseEffectsVolumeSlider, out _pauseEffectsVolumeLabel);
        BuildPauseVolumeRow(optionsContent, "Ambient", out _pauseAmbientVolumeSlider, out _pauseAmbientVolumeLabel);

        optionsContent.AddChild(MenuTheme.MakeDivider());

        _closeEscapeOptionsButton = new Button
        {
            Name = "CloseOptionsButton",
            Text = "Back"
        };
        MenuTheme.StyleButton(_closeEscapeOptionsButton);
        optionsContent.AddChild(_closeEscapeOptionsButton);
    }

    private void OnKarmaChanged(int score, string tierName, string pathName)
    {
        var progress = Karma.Data.KarmaTiers.GetRankProgress(score);
        _karmaLabel.Text = $"Karma: {score:+#;-#;0}\nTier: {tierName}\nPath: {pathName}\n{progress.Summary}";
        _karmaBadge?.SetKarma(score, progress, _gameState?.LocalKarma.Path ?? Karma.Data.KarmaDirection.Neutral);
        _karmaDualityBar?.SetScore(score);
    }

    private void OnKarmaEvent(string message)
    {
        _eventLabel.Text = message;
    }

    private void OnInventoryChanged(string inventoryText)
    {
        _inventoryLabel.Text = inventoryText;
        RefreshHotbar();
        if (_inventoryPanel.Visible)
        {
            RefreshInventoryOverlay();
        }
    }

    private void RefreshHotbar()
    {
        if (_gameState is null || _hotbarLabel is null) return;
        var equipped = FindEquippedHotbarIndex(_gameState.Inventory, _gameState.LocalPlayer?.Equipment);
        _hotbarLabel.Text = FormatHotbar(_gameState.Inventory, equipped, _hotbarBindings);
        if (_hotbarSlotsContainer is null) return;
        foreach (var child in _hotbarSlotsContainer.GetChildren())
            child.QueueFree();
        var activeThemeId = ResolveActiveThemeId();
        for (var i = 0; i < HotbarSlots; i++)
        {
            var item = ResolveHotbarSlotItem(_gameState.Inventory, _hotbarBindings, i);
            var slot = new HotbarDropSlot(this, i, item, activeThemeId)
            {
                CustomMinimumSize = new Vector2(82, 34)
            };
            _hotbarSlotsContainer.AddChild(slot);
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
                SetAmmoFromSnapshot(localPlayer);
                SetHungerFromSnapshot(localPlayer);
                _lastStatusEffects = localPlayer.StatusEffects;
                RenderCombatLine(GetNode<GameState>("/root/GameState"));
                RefreshStatusStrip(localPlayer.StatusEffects);
            }

            _eventLabel.Text = FormatLatestServerEvent(snapshot.ServerEvents);
            UpdateEventIcon(snapshot.ServerEvents);
            MaybeTriggerKarmaBreakFlash(snapshot.ServerEvents, snapshot.PlayerId);
            MaybeTriggerContrabandFlash(snapshot.ServerEvents, snapshot.PlayerId);
            MaybeTriggerEventStinger(snapshot.ServerEvents);
            MaybeTriggerVoiceBarks(snapshot.ServerEvents, snapshot.PlayerId);
            _chatLabel.Text = FormatLocalChatSummary(snapshot.LocalChatMessages);
            if (_possePanel.Visible)
            {
                RefreshPossePanel();
            }

            if (_questLogPanel.Visible)
            {
                RefreshQuestLogPanel();
            }

            if (_combatLogPanel.Visible)
            {
                RefreshCombatLogPanel(snapshot.ServerEvents);
            }

            if (_mountBagPanel.Visible)
            {
                var mounted = snapshot.Mounts.FirstOrDefault(m => m.OccupantPlayerId == snapshot.PlayerId);
                if (mounted is not null)
                    _mountBagLabel.Text = FormatMountBag(mounted);
            }

            if (_shopPanel.Visible)
            {
                RefreshShopOverlay();
            }

            if (_bountyBoardPanel.Visible)
            {
                _bountyBoardLabel.Text = FormatBountyBoard(snapshot.Players);
            }

            var combatRange = serverSession.Server.Config.CombatRangeTiles;
            _targetLabel.Text = FormatAttackTargetLine(snapshot, snapshot.PlayerId, combatRange);
            _bountyLabel.Text = FormatBountyLeaderboard(snapshot);
            RefreshFactionPanel(snapshot);
            RefreshNpcTooltip(snapshot);
            RefreshDeathPileOwnershipPrompt(snapshot);

            // Match summary appears when the match has finished and the server
            // has populated MatchSummarySnapshot (which CreateInterestSnapshot
            // does in the Finished state).
            var summaryVisible = snapshot.Match.Status == MatchStatus.Finished
                                 && snapshot.MatchSummary is not null;
            _matchSummaryPanel.Visible = summaryVisible;
            if (summaryVisible)
                _matchSummaryLabel.Text = FormatMatchSummary(snapshot.MatchSummary);
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
        return $"Health  {clampedHealth} / {safeMax}";
    }

    private void SetAmmoFromSnapshot(PlayerSnapshot localPlayer)
    {
        if (localPlayer.EquippedWeaponKind == WeaponKind.Ranged)
        {
            var safeMax = Mathf.Max(0, localPlayer.MaxAmmo);
            var clamped = Mathf.Clamp(localPlayer.CurrentAmmo, 0, safeMax);
            _ammoLabel.Text = FormatAmmo(localPlayer.CurrentAmmo, localPlayer.MaxAmmo);
            _ammoBar.Value = safeMax == 0 ? 0 : clamped / (double)safeMax * 100.0;
            if (_ammoRow is not null) _ammoRow.Visible = true;
        }
        else
        {
            if (_ammoRow is not null) _ammoRow.Visible = false;
        }
    }


    public static string FormatAmmo(int currentAmmo, int maxAmmo)
    {
        var safeMax = Mathf.Max(0, maxAmmo);
        var clamped = Mathf.Clamp(currentAmmo, 0, safeMax);
        return safeMax == 0
            ? "Ammo  --"
            : clamped == 0
                ? $"Ammo  {clamped} / {safeMax}  (reload)"
                : $"Ammo  {clamped} / {safeMax}";
    }

    private void SetHungerFromSnapshot(PlayerSnapshot localPlayer)
    {
        if (localPlayer.MaxHunger > 0)
        {
            var safeMax = Mathf.Max(1, localPlayer.MaxHunger);
            var clamped = Mathf.Clamp(localPlayer.Hunger, 0, safeMax);
            _hungerLabel.Text = FormatHunger(localPlayer.Hunger, localPlayer.MaxHunger);
            _hungerBar.Value = clamped / (double)safeMax * 100.0;
            if (_hungerRow is not null) _hungerRow.Visible = true;
        }
        else
        {
            if (_hungerRow is not null) _hungerRow.Visible = false;
        }
    }

    public static string FormatHunger(int hunger, int maxHunger)
    {
        var safeMax = Mathf.Max(1, maxHunger);
        var clamped = Mathf.Clamp(hunger, 0, safeMax);
        var ratio = clamped / (float)safeMax;
        var label = ratio <= 0.0f ? "  (starving)"
            : ratio <= 0.25f ? "  (hungry)"
            : ratio <= 0.5f ? "  (peckish)"
            : string.Empty;
        return $"Hunger  {clamped} / {safeMax}{label}";
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

    public static IReadOnlyList<StatusStripEntry> FormatStatusStrip(IReadOnlyList<string> statusEffects)
    {
        return FormatStatusStrip(statusEffects, UiPaletteRegistry.Get(UiPaletteRegistry.WesternSciFiThemeId));
    }

    public static IReadOnlyList<StatusStripEntry> FormatStatusStrip(IReadOnlyList<string> statusEffects, UiPalette palette)
    {
        return (statusEffects ?? System.Array.Empty<string>())
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Distinct()
            .OrderBy(NormalizeStatusId)
            .Select(status => new StatusStripEntry(status, "●", StatusColorFor(status, palette)))
            .ToArray();
    }

    private static string NormalizeStatusId(string status)
    {
        var index = status.IndexOf('(');
        return (index >= 0 ? status[..index] : status).Trim().ToLowerInvariant();
    }

    private static Color StatusColorFor(string status, UiPalette palette)
    {
        palette ??= UiPaletteRegistry.Get(UiPaletteRegistry.WesternSciFiThemeId);
        return NormalizeStatusId(status) switch
        {
            "burning" => palette.Danger,
            "chilled" => palette.Accent,
            "poisoned" => palette.Success,
            "silenced" => palette.DimText,
            "hungry" or "starving" => palette.Accent,
            "dirty" or "filthy" => palette.DimText,
            "crashing" or "twitchy" or "sluggish" => palette.Accent,
            _ => palette.Text
        };
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

    public static int ParseBountyAmount(string statusEffect)
    {
        if (string.IsNullOrEmpty(statusEffect) || !statusEffect.StartsWith("Bounty: ")) return 0;
        var span = statusEffect.AsSpan("Bounty: ".Length);
        return int.TryParse(span, out var amount) ? amount : 0;
    }

    public static string FormatBountyLeaderboard(ClientInterestSnapshot snapshot, int topN = 5) =>
        FormatBountyLeaderboard(snapshot?.Players ?? (IEnumerable<PlayerSnapshot>)System.Array.Empty<PlayerSnapshot>(), topN);

    public static string FormatFactionPanel(IReadOnlyList<FactionSnapshot> factions, string playerId)
    {
        if (factions is null || factions.Count == 0) return "Factions: none";

        var displayNames = StarterFactions.All.ToDictionary(faction => faction.Id, faction => faction.Name);
        var rows = factions
            .Where(faction => string.IsNullOrWhiteSpace(playerId) || faction.PlayerId == playerId)
            .GroupBy(faction => faction.FactionId)
            .Select(group => group.OrderByDescending(faction => Math.Abs(faction.Reputation)).First())
            .OrderBy(faction => displayNames.TryGetValue(faction.FactionId, out var name) ? name : faction.FactionId)
            .ToList();
        if (rows.Count == 0) return "Factions: none";

        var lines = new List<string> { "-- Factions --" };
        foreach (var faction in rows)
        {
            var name = displayNames.TryGetValue(faction.FactionId, out var displayName)
                ? displayName
                : faction.FactionId;
            lines.Add($"{name} {faction.Reputation:+#;-#;0} {FormatFactionMood(faction.Reputation)}");
        }

        return string.Join('\n', lines);
    }

    public static string FormatFactionMood(int reputation) => reputation switch
    {
        <= -50 => "Hostile",
        < 0 => "Wary",
        >= 50 => "Loyal",
        > 0 => "Friendly",
        _ => "Neutral"
    };

    public static bool IsShopOfferFactionLocked(ShopOfferSnapshot offer, int currentReputation)
    {
        if (offer is null) return false;
        if (offer.MinReputation <= 0 || string.IsNullOrWhiteSpace(offer.RequiredFactionId)) return false;
        return currentReputation < offer.MinReputation;
    }

    public static string FormatFactionStoreDenial(string factionId, int minReputation, int currentReputation)
    {
        var factionName = FormatFactionDisplayName(factionId);
        return $"{factionName} won't sell to you yet (need rep ≥ {minReputation}, you're at {currentReputation})";
    }

    public static string FormatFactionDisplayName(string factionId)
    {
        if (string.IsNullOrWhiteSpace(factionId)) return "This faction";
        var faction = StarterFactions.All.FirstOrDefault(candidate => candidate.Id == factionId);
        return faction?.Name ?? factionId;
    }

    public static string FormatShopPricingTooltip(ShopOfferSnapshot offer)
    {
        if (offer is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(offer.PricingBreakdown))
        {
            return offer.PricingBreakdown;
        }

        var basePrice = offer.BasePrice > 0 ? offer.BasePrice : offer.Price;
        return $"Net price: {offer.Price} {offer.Currency} (base {basePrice}).";
    }

    private static int GetFactionReputation(IReadOnlyList<FactionSnapshot> factions, string playerId, string factionId)
    {
        if (string.IsNullOrWhiteSpace(factionId) || factions is null)
        {
            return 0;
        }

        return factions
            .Where(faction => faction.FactionId == factionId)
            .Where(faction => string.IsNullOrWhiteSpace(playerId) || faction.PlayerId == playerId)
            .Select(faction => faction.Reputation)
            .DefaultIfEmpty(0)
            .First();
    }

    public static string FormatQuestLog(IReadOnlyList<QuestSnapshot> quests)
    {
        var active = (quests ?? System.Array.Empty<QuestSnapshot>())
            .Where(quest => quest.Status == QuestStatus.Active)
            .OrderBy(quest => FormatQuestName(quest.Id))
            .ToList();
        if (active.Count == 0)
            return "Quest log: no active quests";

        var lines = new List<string> { "-- Quest Log --" };
        foreach (var quest in active)
        {
            var title = FormatQuestName(quest.Id);
            var counter = quest.TotalSteps > 0
                ? $"{Math.Clamp(quest.CurrentStep + 1, 1, quest.TotalSteps)}/{quest.TotalSteps}"
                : "1/1";
            var step = string.IsNullOrWhiteSpace(quest.CurrentStepDescription)
                ? "Ready to complete"
                : quest.CurrentStepDescription;
            lines.Add($"{title} [{counter}]");
            lines.Add($"  {step}");
        }

        return string.Join('\n', lines);
    }

    public static string FormatBountyLeaderboard(IEnumerable<PlayerSnapshot> players, int topN = 5)
    {
        if (players is null) return "Bounties: --";
        var entries = players
            .Select(p => (player: p, bounty: p.StatusEffects.Sum(s => ParseBountyAmount(s))))
            .Where(e => e.bounty > 0)
            .OrderByDescending(e => e.bounty)
            .Take(topN)
            .ToList();
        if (entries.Count == 0) return "Bounties: none active";
        var lines = new List<string> { "-- Top Bounties --" };
        foreach (var (player, bounty) in entries)
            lines.Add($"  {player.DisplayName,-16} {bounty} scrip");
        return string.Join('\n', lines);
    }

    public static string FormatBountyBoard(IEnumerable<PlayerSnapshot> players, int topN = 8)
    {
        if (players is null) return "Bounty board: none active";
        var entries = players
            .Select(player => (
                player,
                bounty: player.StatusEffects.Sum(ParseBountyAmount),
                wanted: player.StatusEffects.Any(status => status == "Wanted")))
            .Where(entry => entry.wanted || entry.bounty > 0)
            .OrderByDescending(entry => entry.bounty)
            .ThenBy(entry => entry.player.DisplayName)
            .Take(topN)
            .ToList();
        if (entries.Count == 0) return "Bounty board: none active";

        var lines = new List<string> { "-- Bounty Board --" };
        foreach (var entry in entries)
        {
            var warrant = entry.wanted ? "Wanted" : "Bounty";
            lines.Add($"{entry.player.DisplayName}: {warrant} {entry.bounty} scrip");
        }

        return string.Join('\n', lines);
    }

    public static PlayerSnapshot FindAttackTarget(ClientInterestSnapshot snapshot, string localPlayerId, int combatRangeTiles)
    {
        if (snapshot is null) return null;
        var local = snapshot.Players.FirstOrDefault(p => p.Id == localPlayerId);
        if (local is null) return null;
        var rangeSq = combatRangeTiles * combatRangeTiles;
        return snapshot.Players
            .Where(p => p.Id != localPlayerId)
            .Select(p => (p, dx: p.TileX - local.TileX, dy: p.TileY - local.TileY))
            .Where(t => t.dx * t.dx + t.dy * t.dy <= rangeSq)
            .OrderBy(t => t.dx * t.dx + t.dy * t.dy)
            .Select(t => t.p)
            .FirstOrDefault();
    }

    public static string FormatAttackTargetLine(ClientInterestSnapshot snapshot, string localPlayerId, int combatRangeTiles)
    {
        var target = FindAttackTarget(snapshot, localPlayerId, combatRangeTiles);
        if (target is null) return "Target: none in range";
        var local = snapshot?.Players.FirstOrDefault(p => p.Id == localPlayerId);
        if (local is null) return "Target: none in range";
        var dx = target.TileX - local.TileX;
        var dy = target.TileY - local.TileY;
        var dist = (int)System.Math.Sqrt(dx * dx + dy * dy);
        return $"Target: {target.DisplayName} ({target.Health}/{target.MaxHealth} HP, {dist}t)";
    }

    public static string FormatNpcApproachTooltip(ClientInterestSnapshot snapshot, int rangeTiles = 2)
    {
        if (snapshot is null) return string.Empty;
        var local = snapshot.Players.FirstOrDefault(player => player.Id == snapshot.PlayerId);
        if (local is null) return string.Empty;

        var rangeSq = rangeTiles * rangeTiles;
        var npc = snapshot.Npcs
            .Select(candidate => (
                candidate,
                distanceSq: ((candidate.TileX - local.TileX) * (candidate.TileX - local.TileX)) +
                            ((candidate.TileY - local.TileY) * (candidate.TileY - local.TileY))))
            .Where(entry => entry.distanceSq <= rangeSq)
            .OrderBy(entry => entry.distanceSq)
            .ThenBy(entry => entry.candidate.Id)
            .Select(entry => entry.candidate)
            .FirstOrDefault();
        return npc is null ? string.Empty : FormatNpcTooltip(npc.Name, npc.Role, npc.Faction);
    }

    public static string FormatNpcTooltip(string name, string role, string faction)
    {
        var safeName = string.IsNullOrWhiteSpace(name) ? "Unknown" : name.Trim();
        var safeRole = string.IsNullOrWhiteSpace(role) ? "Unknown" : role.Trim();
        var safeFaction = string.IsNullOrWhiteSpace(faction) ? "Unaffiliated" : faction.Trim();
        return $"{safeName} • {safeRole} • {safeFaction}";
    }

    public static string FormatDeathPileOwnershipPrompt(ClientInterestSnapshot snapshot)
    {
        if (snapshot is null)
        {
            return string.Empty;
        }

        var local = snapshot.Players.FirstOrDefault(player => player.Id == snapshot.PlayerId);
        if (local is null)
        {
            return string.Empty;
        }

        var pile = snapshot.WorldItems
            .Where(item => item.TileX == local.TileX && item.TileY == local.TileY)
            .Where(item => !string.IsNullOrWhiteSpace(item.DropOwnerId))
            .Where(item => item.DropOwnerExpiresTick <= 0 || item.DropOwnerExpiresTick > snapshot.Tick)
            .OrderBy(item => item.DropOwnerId == snapshot.PlayerId ? 0 : 1)
            .ThenBy(item => item.EntityId)
            .FirstOrDefault();
        if (pile is null)
        {
            return string.Empty;
        }

        var remainingTicks = pile.DropOwnerExpiresTick <= 0
            ? 0
            : Math.Max(0, pile.DropOwnerExpiresTick - snapshot.Tick);
        return pile.DropOwnerId == snapshot.PlayerId
            ? $"Drop ownership expires in {remainingTicks} ticks."
            : $"{pile.DropOwnerName}'s drop ownership expires in {remainingTicks} ticks.";
    }

    public const int HotbarSlots = 9;

    private sealed partial class InventoryDragRow : Button
    {
        private readonly string _itemId;
        private readonly string _dragLabel;

        public InventoryDragRow(GameItem item, string themeId)
        {
            _itemId = item?.Id ?? string.Empty;
            _dragLabel = string.IsNullOrWhiteSpace(item?.Name) ? "Item" : item.Name;
            TooltipText = item is null ? "Drag item" : $"Drag {item.Name}";
            if (item is not null)
            {
                var tint = InventoryTintForRarity(item.Rarity);
                AddThemeColorOverride("font_color", tint);
                AddThemeColorOverride("font_hover_color", tint);
                AddItemButtonContent(this, themeId, item.Id, item.Name, new Vector2(32f, 32f), tint);
            }
        }

        public override Variant _GetDragData(Vector2 atPosition)
        {
            if (string.IsNullOrWhiteSpace(_itemId))
                return default;

            SetDragPreview(new Label { Text = _dragLabel });
            return _itemId;
        }
    }

    private sealed partial class HotbarDropSlot : Button
    {
        private readonly HudController _hud;
        private readonly int _slotIndex;

        public HotbarDropSlot(HudController hud, int slotIndex, GameItem item, string themeId)
        {
            _hud = hud;
            _slotIndex = slotIndex;
            var label = item is null ? $"{slotIndex + 1}: --" : $"{slotIndex + 1}: {Trim(item.Name, 8)}";
            TooltipText = item is null
                ? $"Drop an inventory item to bind slot {slotIndex + 1}"
                : $"Slot {slotIndex + 1}: {item.Name}";
            AddItemButtonContent(this, themeId, item?.Id ?? string.Empty, label, new Vector2(24f, 24f));
        }

        public override bool _CanDropData(Vector2 atPosition, Variant data)
        {
            return data.VariantType == Variant.Type.String && !string.IsNullOrWhiteSpace(data.AsString());
        }

        public override void _DropData(Vector2 atPosition, Variant data)
        {
            _hud?.BindHotbarSlot(_slotIndex, data.AsString());
        }
    }

    public static string FormatHotbar(IReadOnlyList<GameItem> inventory, int equippedIndex)
    {
        return FormatHotbar(inventory, equippedIndex, null);
    }

    public static string FormatHotbar(
        IReadOnlyList<GameItem> inventory,
        int equippedIndex,
        IReadOnlyDictionary<int, string> bindings)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < HotbarSlots; i++)
        {
            if (i > 0) sb.Append("  ");
            var item = ResolveHotbarSlotItem(inventory, bindings, i);
            var name = item is not null ? Trim(item.Name, 8) : "—";
            var marker = i == equippedIndex ? "*" : " ";
            sb.Append($"[{i + 1}{marker}{name}]");
        }
        return sb.ToString();
    }

    private static string Trim(string text, int max)
    {
        if (string.IsNullOrEmpty(text)) return "—";
        return text.Length <= max ? text : text.Substring(0, max);
    }

    public static int FindEquippedHotbarIndex(IReadOnlyList<GameItem> inventory, IReadOnlyDictionary<EquipmentSlot, GameItem> equipment)
    {
        if (equipment is null) return -1;
        if (!equipment.TryGetValue(EquipmentSlot.MainHand, out var equipped)) return -1;
        for (var i = 0; i < inventory.Count; i++)
            if (inventory[i].Id == equipped.Id) return i;
        return -1;
    }

    public IReadOnlyDictionary<int, string> HotbarBindings => _hotbarBindings;

    public void BindHotbarSlot(int slotIndex, string itemId)
    {
        BindHotbarSlot(_hotbarBindings, slotIndex, itemId);
        RefreshHotbar();
    }

    public string ResolveHotbarSlotItemId(int slotIndex, IReadOnlyList<GameItem> inventory)
    {
        return ResolveHotbarSlotItemId(_hotbarBindings, slotIndex, inventory);
    }

    public static void BindHotbarSlot(IDictionary<int, string> bindings, int slotIndex, string itemId)
    {
        if (bindings is null) return;
        if (slotIndex < 0 || slotIndex >= HotbarSlots) return;
        bindings[slotIndex] = itemId ?? string.Empty;
    }

    public static string ResolveHotbarSlotItemId(
        IReadOnlyDictionary<int, string> bindings,
        int slotIndex,
        IReadOnlyList<GameItem> inventory)
    {
        var item = ResolveHotbarSlotItem(inventory, bindings, slotIndex);
        return item?.Id ?? string.Empty;
    }

    private static GameItem ResolveHotbarSlotItem(
        IReadOnlyList<GameItem> inventory,
        IReadOnlyDictionary<int, string> bindings,
        int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= HotbarSlots || inventory is null)
            return null;

        if (bindings is not null && bindings.TryGetValue(slotIndex, out var boundItemId))
        {
            if (string.IsNullOrWhiteSpace(boundItemId))
                return null;

            var bound = inventory.FirstOrDefault(item => item.Id == boundItemId);
            if (bound is not null)
                return bound;
        }

        return slotIndex < inventory.Count ? inventory[slotIndex] : null;
    }

    public static string FormatShopBubble(IReadOnlyList<ShopOfferSnapshot> offers, string vendorNpcId, int playerScrip)
    {
        var vendorOffers = offers.Where(o => o.VendorNpcId == vendorNpcId).ToList();
        if (vendorOffers.Count == 0) return $"-- No wares available --\nYour scrip: {playerScrip}";
        var lines = new List<string> { $"-- Wares ({vendorOffers.Count}) | Scrip: {playerScrip} --" };
        foreach (var offer in vendorOffers)
        {
            var canAfford = playerScrip >= offer.Price ? "" : "  [insufficient scrip]";
            var repNote = offer.MinReputation > 0 ? $"  [req {offer.RequiredFactionId} {offer.MinReputation}]" : "";
            lines.Add($"  {offer.ItemName,-20} {offer.Price,4} {offer.Currency}{repNote}{canAfford}");
        }
        return string.Join('\n', lines);
    }

    public static string FormatSellBubble(IReadOnlyList<GameItem> inventory, int playerScrip)
    {
        if (inventory.Count == 0) return $"-- Nothing to sell --\nYour scrip: {playerScrip}";
        var lines = new List<string> { $"-- Sell ({inventory.Count}) | Scrip: {playerScrip} --" };
        foreach (var item in inventory)
            lines.Add($"  {item.Name}");
        return string.Join('\n', lines);
    }

    public static string FormatMinimap(ClientInterestSnapshot snapshot, string localPlayerId, int radiusTiles = 8)
    {
        var local = snapshot.Players.FirstOrDefault(p => p.Id == localPlayerId);
        if (local is null) return "[minimap unavailable]";

        var size = radiusTiles * 2 + 1;
        var grid = new char[size, size];
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                grid[x, y] = '.';

        void Place(int worldX, int worldY, char marker)
        {
            var dx = worldX - local.TileX + radiusTiles;
            var dy = worldY - local.TileY + radiusTiles;
            if (dx >= 0 && dx < size && dy >= 0 && dy < size)
                grid[dx, dy] = marker;
        }

        foreach (var structure in snapshot.Structures)
            Place(structure.TileX, structure.TileY, 'S');
        foreach (var npc in snapshot.Npcs)
            Place(npc.TileX, npc.TileY, 'N');
        foreach (var player in snapshot.Players)
            if (player.Id != localPlayerId)
                Place(player.TileX, player.TileY, 'P');
        Place(local.TileX, local.TileY, '@');

        var sb = new System.Text.StringBuilder();
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
                sb.Append(grid[x, y]);
            if (y < size - 1) sb.Append('\n');
        }
        return sb.ToString();
    }

    public static string FormatMatchStatus(MatchSnapshot match)
    {
        if (match.Status == MatchStatus.Running)
        {
            return match.Summary;
        }

        return $"RESULTS LOCKED — Saint: {match.SaintWinnerName} ({match.SaintWinnerScore:+#;-#;0}) | Scourge: {match.ScourgeWinnerName} ({match.ScourgeWinnerScore:+#;-#;0})\nPost-match free roam: movement/dialogue only. Winners paid +{ServerConfig.DefaultMatchWinnerScripReward} scrip.";
    }

    public static string FormatMatchSummary(MatchSummarySnapshot summary)
    {
        if (summary is null)
        {
            return "Match in progress.";
        }

        var saint = string.IsNullOrEmpty(summary.Winners.SaintPlayerId) ? "none" : summary.Winners.SaintName;
        var scourge = string.IsNullOrEmpty(summary.Winners.ScourgePlayerId) ? "none" : summary.Winners.ScourgeName;
        var header = $"Match Over — Saint: {saint} | Scourge: {scourge}";
        var rows = summary.Players
            .Select(p =>
            {
                var highlight = summary.Highlights is not null && summary.Highlights.TryGetValue(p.Id, out var h)
                    ? $" | highlights +{h.MostKarmaGained}/-{h.MostKarmaLost} spree {h.LongestSpree} bounty {h.BountyClaimed} rescues {h.RescuesPerformed}"
                    : string.Empty;
                return $"  {p.DisplayName}: karma {p.FinalKarma:+#;-#;0} (peak {p.KarmaPeak:+#;-#;0} / floor {p.KarmaFloor:+#;-#;0}) quests {p.QuestsCompleted} kills {p.Kills}{highlight}";
            })
            .ToArray();
        return rows.Length == 0 ? header : $"{header}\n{string.Join("\n", rows)}";
    }

    public static string FormatCombatLog(IReadOnlyList<ServerEvent> serverEvents, int maxRows = 20)
    {
        var events = (serverEvents ?? System.Array.Empty<ServerEvent>())
            .TakeLast(Math.Max(1, maxRows))
            .ToArray();
        if (events.Length == 0)
            return "Combat log: quiet";

        var lines = new List<string> { "-- Combat Log --" };
        foreach (var serverEvent in events)
        {
            var iconName = ResolveEventIconName(serverEvent.EventId);
            var icon = string.IsNullOrWhiteSpace(iconName) ? "event" : iconName;
            lines.Add($"[{serverEvent.Tick}] {icon}: {FormatCombatLogSummary(serverEvent)}");
        }

        return string.Join("\n", lines);
    }

    private static string FormatCombatLogSummary(ServerEvent serverEvent)
    {
        if (serverEvent is null) return "event";
        if (!string.IsNullOrWhiteSpace(serverEvent.Description))
            return serverEvent.Description;
        return string.IsNullOrWhiteSpace(serverEvent.EventId) ? "event" : serverEvent.EventId;
    }

    public static string FormatMountBag(MountSnapshot mount)
    {
        if (mount is null)
            return "Mount bag: no mount";

        var itemIds = mount.BagItemIds ?? System.Array.Empty<string>();
        var lines = new List<string>
        {
            $"{mount.Name} Bag ({itemIds.Count}/8)"
        };
        if (itemIds.Count == 0)
        {
            lines.Add("empty");
            return string.Join("\n", lines);
        }

        foreach (var group in itemIds.GroupBy(id => id).OrderBy(group => StarterItems.GetById(group.Key).Name))
        {
            var item = StarterItems.GetById(group.Key);
            lines.Add($"{item.Name} x{group.Count()}");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Maps a server event id (e.g. "world1:42:supply_drop_spawned") to the
    /// resource path of its sliced UI icon, or empty string if no mapping exists.
    /// The sliced atlas lives at res://assets/art/generated/sliced/prototype_ui_icons/
    /// and provides 36 icons keyed by event-name fragments.
    /// </summary>
    private void MaybeTriggerKarmaBreakFlash(IReadOnlyList<ServerEvent> serverEvents, string localPlayerId)
    {
        var triggerTick = FindKarmaBreakTriggerTick(serverEvents, localPlayerId, _lastKarmaBreakFlashTick);
        if (triggerTick < 0) return;

        _lastKarmaBreakFlashTick = triggerTick;
        _karmaBreakFlash.Color = new Color(1f, 1f, 1f, 0.6f);
        var tween = CreateTween();
        tween.TweenProperty(_karmaBreakFlash, "color:a", 0f, 0.9f);
        PlayEventStinger("karma_break");
    }

    // Resolve an audio clip via AudioEventCatalog and play it through the
    // shared stinger AudioStreamPlayer. Silently no-ops when the resolved
    // path is empty or the file isn't on disk yet — the catalog is the seam,
    // not the asset. Once TASKS.md (Sound Needed section) deliveries land, drop the clips
    // at the registered paths and they play automatically.
    public void PlayEventStinger(string eventId)
    {
        if (_eventStingerPlayer is null) return;
        var path = Karma.Audio.AudioEventCatalog.Resolve(eventId);
        if (string.IsNullOrEmpty(path) || !FileAccess.FileExists(path)) return;
        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream is null) return;
        _eventStingerPlayer.Stream = stream;
        _eventStingerPlayer.Play();
    }

    private void MaybeTriggerEventStinger(IReadOnlyList<ServerEvent> serverEvents)
    {
        if (_eventStingerPlayer is null || serverEvents is null || serverEvents.Count == 0)
            return;

        var latest = serverEvents[^1];
        var key = $"{latest.Tick}:{latest.EventId}";
        if (_lastEventStingerKey == key)
            return;

        if (latest.EventId.Contains("karma_break") || latest.EventId.Contains("contraband_detected"))
            return;

        var cueId = ReadEventData(latest, "audioCue", string.Empty);
        if (string.IsNullOrWhiteSpace(cueId) &&
            latest.EventId.Contains("item_equipped") &&
            latest.Data is not null &&
            latest.Data.TryGetValue("itemId", out var itemId))
        {
            cueId = Karma.Audio.AudioEventCatalog.EquipmentCueIdForItemId(itemId);
        }

        if (string.IsNullOrWhiteSpace(cueId))
            cueId = latest.EventId;

        var path = Karma.Audio.AudioEventCatalog.Resolve(cueId);
        if (string.IsNullOrWhiteSpace(path) || !FileAccess.FileExists(path))
            return;

        _lastEventStingerKey = key;
        PlayEventStinger(cueId);
    }

    private void MaybeTriggerVoiceBarks(IReadOnlyList<ServerEvent> serverEvents, string localPlayerId)
    {
        if (_eventStingerPlayer is null || serverEvents is null || serverEvents.Count == 0)
        {
            return;
        }

        for (var i = serverEvents.Count - 1; i >= 0; i--)
        {
            var ev = serverEvents[i];
            if (ev.Tick <= _lastVoiceBarkTick) continue;
            if (!EventConcernsLocalPlayer(ev, localPlayerId)) continue;

            var barkId = Karma.Audio.VoiceBarkCatalog.BarkForEventId(ev.EventId);
            if (string.IsNullOrEmpty(barkId)) continue;

            _lastVoiceBarkTick = ev.Tick;
            PlayVoiceBark(Karma.Audio.VoiceSlot.Voice1, barkId);
            return;
        }
    }

    private void PlayVoiceBark(Karma.Audio.VoiceSlot slot, string barkId)
    {
        var path = Karma.Audio.VoiceBarkCatalog.Resolve(slot, barkId);
        if (string.IsNullOrEmpty(path) || !FileAccess.FileExists(path)) return;
        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream is null) return;
        _eventStingerPlayer.Stream = stream;
        _eventStingerPlayer.Play();
    }

    private static bool EventConcernsLocalPlayer(ServerEvent serverEvent, string localPlayerId)
    {
        if (string.IsNullOrEmpty(localPlayerId)) return true;
        if (serverEvent.Witnesses is not null && serverEvent.Witnesses.Contains(localPlayerId)) return true;
        if (serverEvent.Data is null || serverEvent.Data.Count == 0) return true;
        return serverEvent.Data.Values.Any(value => value == localPlayerId);
    }

    public static long FindKarmaBreakTriggerTick(
        IReadOnlyList<ServerEvent> serverEvents,
        string localPlayerId,
        long lastTriggerTick)
    {
        if (serverEvents is null || serverEvents.Count == 0 || string.IsNullOrEmpty(localPlayerId))
            return -1;
        for (var i = serverEvents.Count - 1; i >= 0; i--)
        {
            var ev = serverEvents[i];
            if (ev.Tick <= lastTriggerTick) continue;
            if (!ev.EventId.Contains("karma_break") && !ev.EventId.Contains("player_respawned")) continue;
            var subjectId = ev.Data.TryGetValue("playerId", out var pid) ? pid : string.Empty;
            if (subjectId != localPlayerId) continue;
            return ev.Tick;
        }
        return -1;
    }

    private void MaybeTriggerContrabandFlash(IReadOnlyList<ServerEvent> serverEvents, string localPlayerId)
    {
        var triggerTick = FindContrabandFlashTriggerTick(serverEvents, localPlayerId, _lastContrabandFlashTick);
        if (triggerTick < 0) return;

        _lastContrabandFlashTick = triggerTick;
        _contrabandFlash.Color = new Color(1f, 0.15f, 0.15f, 0.45f);
        var tween = CreateTween();
        tween.TweenProperty(_contrabandFlash, "color:a", 0f, 0.6f);
        PlayEventStinger("contraband_detected");
    }

    public static long FindContrabandFlashTriggerTick(
        IReadOnlyList<ServerEvent> serverEvents,
        string localPlayerId,
        long lastTriggerTick)
    {
        if (serverEvents is null || serverEvents.Count == 0 || string.IsNullOrEmpty(localPlayerId))
            return -1;
        for (var i = serverEvents.Count - 1; i >= 0; i--)
        {
            var ev = serverEvents[i];
            if (ev.Tick <= lastTriggerTick) continue;
            if (!ev.EventId.Contains("contraband_detected")) continue;
            var subjectId = ev.Data.TryGetValue("playerId", out var pid) ? pid : string.Empty;
            if (subjectId != localPlayerId) continue;
            return ev.Tick;
        }
        return -1;
    }

    private void UpdateEventIcon(IReadOnlyList<ServerEvent> serverEvents)
    {
        if (_eventIcon is null) return;
        if (serverEvents is null || serverEvents.Count == 0)
        {
            _eventIcon.Texture = null;
            return;
        }
        var path = ResolveEventIconPath(serverEvents[^1].EventId);
        if (string.IsNullOrEmpty(path))
        {
            _eventIcon.Texture = null;
            return;
        }
        _eventIcon.Texture = AtlasTextureLoader.Load(path);
    }

    public static string ResolveEventIconPath(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return string.Empty;
        var name = ResolveEventIconName(eventId);
        return string.IsNullOrEmpty(name)
            ? string.Empty
            : $"res://assets/art/generated/sliced/prototype_ui_icons/{name}.png";
    }

    public static string ResolveEventIconName(string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return string.Empty;
        // Order matters: longer / more specific matches first.
        if (eventId.Contains("supply_drop_spawned")) return "supply_spawned";
        if (eventId.Contains("supply_drop_claimed")) return "supply_claimed";
        if (eventId.Contains("supply_drop_expired")) return "supply_spawned";
        if (eventId.Contains("clinic_revive")) return "clinic_revive";
        if (eventId.Contains("player_downed")) return "player_downed";
        if (eventId.Contains("player_rescued") || eventId.Contains("rescue")) return "player_rescued";
        if (eventId.Contains("karma_break")) return "karma_break";
        if (eventId.Contains("bounty_claimed")) return "bounty_claimed";
        if (eventId.Contains("wanted_bounty_claimed") || eventId.Contains("player_wanted") || eventId.Contains("issue_wanted")) return "wanted";
        if (eventId.Contains("contraband_detected") || eventId.Contains("contraband")) return "contraband_detected";
        if (eventId.Contains("ready_up") || eventId.Contains("player_ready")) return "ready_up";
        if (eventId.Contains("match_started")) return "match_started";
        if (eventId.Contains("match_finished")) return "match_summary";
        if (eventId.Contains("duel_requested")) return "duel_requested";
        if (eventId.Contains("duel_accepted")) return "duel_accepted";
        if (eventId.Contains("item_purchased")) return "item_purchased";
        if (eventId.Contains("item_sold")) return "item_purchased";
        if (eventId.Contains("item_used")) return "item_used";
        if (eventId.Contains("item_crafted")) return "item_used";
        if (eventId.Contains("structure_repaired") || eventId.Contains("structure_sabotaged") || eventId.Contains("structure_interacted")) return "structure_interacted";
        if (eventId.Contains("posse_invited") || eventId.Contains("posse_invite")) return "posse_invite";
        if (eventId.Contains("posse_accepted") || eventId.Contains("posse_formed")) return "posse_accepted";
        if (eventId.Contains("local_chat") || eventId.Contains("posse_chat")) return "local_chat";
        if (eventId.Contains("mounted")) return "mount";
        if (eventId.Contains("dismounted")) return "dismount";
        if (eventId.Contains("quest_started") || eventId.Contains("posse_quest_started")) return "quest_started";
        if (eventId.Contains("quest_completed") || eventId.Contains("posse_quest_completed")) return "quest_completed";
        if (eventId.Contains("dialogue")) return "dialogue";
        if (eventId.Contains("entanglement")) return "entanglement";
        if (eventId.Contains("rumor")) return "rumor";
        if (eventId.Contains("witness")) return "witness";
        if (eventId.Contains("evidence")) return "evidence";
        if (eventId.Contains("entered_lawless") || eventId.Contains("left_lawless")) return "danger_heat";
        if (eventId.Contains("trophy_drop")) return "evidence";
        if (eventId.Contains("station_claimed")) return "objective_arrow";
        if (eventId.Contains("door_opened")) return "interact_key_prompt";
        if (eventId.Contains("player_started_posse_quest")) return "quest_started";
        return string.Empty;
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

        if (latest.EventId.Contains("saint_title_changed"))
        {
            var newHolder = ReadEventData(latest, "newHolderName", "none");
            var prev = ReadEventData(latest, "previousHolderName", "none");
            return newHolder == "none"
                ? $"{prev} is no longer Saint. The title is vacant."
                : $"{newHolder} is now Saint! (was: {prev})";
        }

        if (latest.EventId.Contains("scourge_title_changed"))
        {
            var newHolder = ReadEventData(latest, "newHolderName", "none");
            var prev = ReadEventData(latest, "previousHolderName", "none");
            return newHolder == "none"
                ? $"{prev} is no longer Scourge. The title is vacant."
                : $"{newHolder} is now Scourge! (was: {prev})";
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

        if (latest.EventId.Contains("player_rescued"))
        {
            var rescuer = ReadEventData(latest, "rescuerId", "Someone");
            var target = ReadEventData(latest, "targetId", "someone");
            var heal = ReadEventData(latest, "healAmount", "?");
            return $"{rescuer} rescued {target} (+{heal} HP).";
        }

        if (latest.EventId.Contains("clinic_revive"))
        {
            var target = ReadEventData(latest, "playerId", "Someone");
            var heal = ReadEventData(latest, "healAmount", "?");
            var cost = ReadEventData(latest, "scripCost", "?");
            return $"{target} was revived by the clinic (+{heal} HP, -{cost} scrip).";
        }

        if (latest.EventId.Contains("player_mounted"))
        {
            var rider = ReadEventData(latest, "playerName", "Someone");
            var mountName = ReadEventData(latest, "mountName", "a mount");
            var speed = ReadEventData(latest, "speedModifier", "?");
            return $"{rider} mounted {mountName} ({speed}x speed).";
        }

        if (latest.EventId.Contains("player_dismounted"))
        {
            var rider = ReadEventData(latest, "playerName", "Someone");
            var mountName = ReadEventData(latest, "mountName", "a mount");
            var nearStation = ReadEventData(latest, "nearStation", "False");
            var suffix = nearStation == "True" ? " Parked near a station." : string.Empty;
            return $"{rider} dismounted {mountName}.{suffix}";
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
            .Where(part => part != "skin" && part != "hair" && part != "outfit" && part != "pants" && part != "shirt" && part != "tool" && part != "32x64")
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    public static string FormatAppearanceSummary(PlayerAppearanceSelection appearance)
    {
        return $"Appearance: {FormatAppearanceLayerName(appearance.SkinLayerId)} skin | {FormatAppearanceLayerName(appearance.HairLayerId)} hair | {FormatAppearanceLayerName(appearance.OutfitLayerId)} outfit | {FormatAppearanceLayerName(appearance.PantsLayerId)} pants | {FormatAppearanceLayerName(appearance.ShirtLayerId)} shirt | {FormatAppearanceLayerName(appearance.HeldToolLayerId)} tool";
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
            "pants" => new Dictionary<string, string> { ["pantsLayerId"] = PlayerController.CyclePantsLayerId(current.PantsLayerId) },
            "shirt" => new Dictionary<string, string> { ["shirtLayerId"] = PlayerController.CycleShirtLayerId(current.ShirtLayerId) },
            _ => new Dictionary<string, string>()
        };
    }

    private static PlayerAppearanceSelection GetLocalAppearanceSelection(ClientInterestSnapshot snapshot)
    {
        return snapshot?.Players.FirstOrDefault(player => player.Id == snapshot.PlayerId)?.Appearance;
    }

    private static string FormatQuestName(string questId)
    {
        var quest = StarterQuests.All.FirstOrDefault(candidate => candidate.Id == questId);
        return quest is not null
            ? quest.Title
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
        if (_inventoryRowsContainer is null) return;
        foreach (var child in _inventoryRowsContainer.GetChildren())
            child.QueueFree();
        var activeThemeId = ResolveActiveThemeId();
        for (var i = 0; i < _gameState.Inventory.Count; i++)
        {
            var item = _gameState.Inventory[i];
            _inventoryRowsContainer.AddChild(new InventoryDragRow(item, activeThemeId)
            {
                CustomMinimumSize = new Vector2(220, 38)
            });
        }
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
        lines.Add($"Weapon Durability: {FormatDurability(equipment, EquipmentSlot.MainHand)}");
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

    public static Color InventoryTintForRarity(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Uncommon => new Color(0.36f, 0.86f, 0.45f),
            ItemRarity.Rare => new Color(0.38f, 0.64f, 1f),
            ItemRarity.Contraband => new Color(1f, 0.36f, 0.34f),
            _ => new Color(0.78f, 0.78f, 0.78f)
        };
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
        var label = string.IsNullOrWhiteSpace(localPlayer.PosseName)
            ? (posseId.StartsWith("posse_") ? posseId[6..] : posseId)
            : localPlayer.PosseName;
        var leaderId = string.IsNullOrWhiteSpace(localPlayer.PosseLeaderId)
            ? string.Empty
            : localPlayer.PosseLeaderId;

        var lines = new List<string> { $"Posse [{label}] — {members.Length} member{(members.Length == 1 ? "" : "s")}" };
        foreach (var member in members)
        {
            var self = member.Id == localPlayerId ? " (you)" : string.Empty;
            var leader = member.Id == leaderId ? " (leader)" : string.Empty;
            var karmaSign = member.Karma >= 0 ? "+" : string.Empty;
            lines.Add($"{member.DisplayName}{self}{leader}: {karmaSign}{member.Karma} | HP {member.Health}/{member.MaxHealth}");
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

    public static string FormatDurability(
        IReadOnlyDictionary<EquipmentSlot, GameItem> equipment,
        EquipmentSlot slot)
    {
        if (equipment is null || !equipment.TryGetValue(slot, out var item))
            return "empty";
        if (item.MaxDurability <= 0)
            return "n/a";
        var marker = item.IsBroken ? " broken" : string.Empty;
        return $"{item.Durability}/{item.MaxDurability}{marker}";
    }
}
