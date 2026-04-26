using Godot;
using Karma.Core;
using Karma.Net;

namespace Karma.UI;

public partial class HudController : CanvasLayer
{
    private Label _karmaLabel = new();
    private Label _eventLabel = new();
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
    private Label _syncLabel = new();
    private PanelContainer _promptPanel = new();
    private Label _promptLabel = new();

    public override void _Ready()
    {
        BuildUi();

        var gameState = GetNode<GameState>("/root/GameState");
        gameState.KarmaChanged += OnKarmaChanged;
        gameState.KarmaEvent += OnKarmaEvent;
        gameState.InventoryChanged += OnInventoryChanged;
        gameState.LeaderboardChanged += OnLeaderboardChanged;
        gameState.PerksChanged += OnPerksChanged;
        gameState.RelationshipsChanged += OnRelationshipsChanged;
        gameState.FactionsChanged += OnFactionsChanged;
        gameState.QuestsChanged += OnQuestsChanged;
        gameState.CombatChanged += OnCombatChanged;
        gameState.EntanglementsChanged += OnEntanglementsChanged;
        gameState.DuelsChanged += OnDuelsChanged;
        gameState.WorldEventsChanged += OnWorldEventsChanged;
        var serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        if (serverSession is not null)
        {
            serverSession.LocalSnapshotChanged += OnLocalSnapshotChanged;
            OnLocalSnapshotChanged(serverSession.LastLocalSnapshot?.Summary ?? "Sync: waiting");
        }

        var pathName = gameState.LocalKarma.Path == Data.KarmaDirection.Neutral
            ? "Unmarked"
            : gameState.LocalKarma.Path.ToString();
        OnKarmaChanged(gameState.LocalKarma.Score, gameState.LocalKarma.TierName, pathName);
        OnInventoryChanged(gameState.Inventory.Count == 0 ? "Inventory: empty" : "Inventory: loaded");
        OnLeaderboardChanged(gameState.GetLeaderboardStanding().Summary);
        OnPerksChanged(Data.PerkCatalog.Format(gameState.LocalPerks));
        OnRelationshipsChanged("Mara: Neutral (0)");
        OnFactionsChanged("Free Settlers Rep: 0");
        OnQuestsChanged(gameState.Quests.FormatActiveSummary());
        OnCombatChanged("Combat: none");
        OnEntanglementsChanged(gameState.Entanglements.FormatSummary());
        OnDuelsChanged(gameState.Duels.FormatSummary());
        OnWorldEventsChanged(gameState.WorldEvents.FormatLatestSummary());
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

    private void BuildUi()
    {
        var root = new Control
        {
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
            Text = "Mara: Neutral (0)"
        };
        root.AddChild(_relationshipsLabel);

        _factionsLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 256,
            OffsetRight = 900,
            OffsetBottom = 286,
            Text = "Free Settlers Rep: 0"
        };
        root.AddChild(_factionsLabel);

        _questsLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 288,
            OffsetRight = 900,
            OffsetBottom = 318,
            Text = "Quests: none"
        };
        root.AddChild(_questsLabel);

        _combatLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 320,
            OffsetRight = 900,
            OffsetBottom = 350,
            Text = "Combat: none"
        };
        root.AddChild(_combatLabel);

        _entanglementsLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 352,
            OffsetRight = 900,
            OffsetBottom = 382,
            Text = "Entanglements: none"
        };
        root.AddChild(_entanglementsLabel);

        _worldEventsLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 384,
            OffsetRight = 1000,
            OffsetBottom = 414,
            Text = "Rumors: quiet"
        };
        root.AddChild(_worldEventsLabel);

        _duelsLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 416,
            OffsetRight = 1000,
            OffsetBottom = 446,
            Text = "Duels: none"
        };
        root.AddChild(_duelsLabel);

        _syncLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 448,
            OffsetRight = 1000,
            OffsetBottom = 540,
            Text = "Sync: waiting",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(_syncLabel);

        _promptPanel = new PanelContainer
        {
            OffsetLeft = 16,
            OffsetTop = 560,
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
        var gameState = GetNode<GameState>("/root/GameState");
        _combatLabel.Text = $"{combatText} | You ATK:{gameState.LocalPlayer.AttackPower} DEF:{gameState.LocalPlayer.Defense}";
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
        _syncLabel.Text = $"Sync: {snapshotSummary}";
    }
}
