using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Core;
using Karma.Net;

namespace Karma.UI;

public partial class HudController : CanvasLayer
{
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
    private PanelContainer _promptPanel = new();
    private Label _promptLabel = new();
    private string _lastCombatText = "Combat: none";
    private IReadOnlyList<string> _lastStatusEffects = System.Array.Empty<string>();

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
        SetHealth(gameState.LocalPlayer.Health, gameState.LocalPlayer.MaxHealth);
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

    public void ShowStamina(string staminaText)
    {
        _staminaLabel.Text = staminaText;
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
            OffsetBottom = 510,
            Text = "Sync: waiting",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(_syncLabel);

        _matchLabel = new Label
        {
            OffsetLeft = 16,
            OffsetTop = 512,
            OffsetRight = 1000,
            OffsetBottom = 542,
            Text = "Match: 30:00 remaining"
        };
        root.AddChild(_matchLabel);

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
            _matchLabel.Text = snapshot.Match.Summary;
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
            var saint = ReadEventData(latest, "saintWinnerId", "none");
            var scourge = ReadEventData(latest, "scourgeWinnerId", "none");
            var saintReward = ReadEventData(latest, "saintScripReward", "0");
            var scourgeReward = ReadEventData(latest, "scourgeScripReward", "0");
            return $"Match complete. Saint: {saint} (+{saintReward} scrip). Scourge: {scourge} (+{scourgeReward} scrip).";
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
            var result = ReadEventData(latest, "result", "Nothing unusual happens.");
            return $"{player} inspected {structureName}: {result}";
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

        if (latest.EventId.Contains("item_used") &&
            ReadEventData(latest, "itemId", string.Empty) == Karma.Data.StarterItems.RepairKitId)
        {
            var player = ReadEventData(latest, "playerId", "Someone");
            var target = ReadEventData(latest, "targetId", "someone");
            var healing = ReadEventData(latest, "healing", "?");
            var health = ReadEventData(latest, "targetHealth", "?");
            var maxHealth = ReadEventData(latest, "targetMaxHealth", "?");
            return $"{player} repaired {target} for {healing}. {target} HP: {health}/{maxHealth}.";
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
                var karmaAmount = ReadEventData(latest, "karmaAmount", "0");
                return $"{player} claimed {itemName} from {dropOwnerId}'s drop. Karma {karmaAmount}.";
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
            var karmaAmount = ReadEventData(latest, "karmaAmount", "0");
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
            var karmaAmount = ReadEventData(latest, "karmaAmount", "0");
            return $"{player} gave {amount} {currency} to {target}. Karma {karmaAmount}.";
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
}
