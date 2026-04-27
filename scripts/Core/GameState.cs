using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Karma.Data;

namespace Karma.Core;

public partial class GameState : Node
{
    public const string LocalPlayerId = "local_player";

    public PlayerKarma LocalKarma => LocalPlayer.Karma;
    public IReadOnlyList<GameItem> Inventory => LocalPlayer.Inventory;
    public int LocalScrip => LocalPlayer.Scrip;
    public IReadOnlyDictionary<string, PlayerState> Players => _players;
    public PlayerState LocalPlayer => _players[LocalPlayerId];
    public IReadOnlyList<KarmaPerk> LocalPerks => PerkCatalog.GetForPlayer(LocalPlayer, GetLeaderboardStanding());
    public RelationshipLedger Relationships { get; } = new();
    public FactionLedger Factions { get; } = new();
    public QuestLedger Quests { get; } = new(StarterQuests.All);
    public EntanglementLedger Entanglements { get; } = new();
    public DuelLedger Duels { get; } = new();
    public WorldEventLog WorldEvents { get; } = new();

    private readonly Dictionary<string, PlayerState> _players = new();
    private bool _prototypeInventorySeeded;

    [Signal]
    public delegate void KarmaChangedEventHandler(int score, string tierName, string path);

    [Signal]
    public delegate void KarmaEventEventHandler(string message);

    [Signal]
    public delegate void InventoryChangedEventHandler(string inventoryText);

    [Signal]
    public delegate void LeaderboardChangedEventHandler(string leaderboardText);

    [Signal]
    public delegate void PerksChangedEventHandler(string perksText);

    [Signal]
    public delegate void RelationshipsChangedEventHandler(string relationshipsText);

    [Signal]
    public delegate void FactionsChangedEventHandler(string factionsText);

    [Signal]
    public delegate void QuestsChangedEventHandler(string questsText);

    [Signal]
    public delegate void CombatChangedEventHandler(string combatText);

    [Signal]
    public delegate void EntanglementsChangedEventHandler(string entanglementsText);

    [Signal]
    public delegate void DuelsChangedEventHandler(string duelsText);

    [Signal]
    public delegate void WorldEventsChangedEventHandler(string worldEventsText);

    public override void _Ready()
    {
        EnsurePrototypePlayers();
        GD.Print($"Karma started. Current tier: {LocalKarma.TierName}");
        EmitKarmaChanged();
        EmitInventoryChanged();
        EmitLeaderboardChanged();
        EmitPerksChanged();
        EmitRelationshipsChanged();
        EmitFactionsChanged();
        EmitQuestsChanged();
        EmitCombatChanged();
        EmitEntanglementsChanged();
        EmitDuelsChanged();
        EmitWorldEventsChanged();
    }

    public KarmaShift ApplyLocalShift(KarmaAction action)
    {
        return ApplyShift(LocalPlayerId, action);
    }

    public KarmaShift ApplyShift(string playerId, KarmaAction action)
    {
        EnsurePrototypePlayers();
        var player = _players[playerId];
        var shift = KarmaRules.CalculateShift(action);
        player.ApplyKarma(shift.Amount);
        ApplyRelationshipDelta(playerId, action);
        ApplyFactionDelta(playerId, action);
        var message = $"{player.DisplayName}: {shift.Direction} {Math.Abs(shift.Amount)} karma: {shift.Reason}";
        WorldEvents.Add(WorldEventType.Karma, message, playerId, action.TargetId);
        GD.Print($"{message} Tier: {player.Karma.TierName}");
        EmitSignal(SignalName.KarmaEvent, message);
        EmitKarmaChanged();
        EmitLeaderboardChanged();
        EmitPerksChanged();
        EmitRelationshipsChanged();
        EmitFactionsChanged();
        EmitWorldEventsChanged();
        return shift;
    }

    public void TriggerKarmaBreak()
    {
        TriggerKarmaBreak(LocalPlayerId);
    }

    public void TriggerKarmaBreak(string playerId)
    {
        EnsurePrototypePlayers();
        var player = _players[playerId];
        Duels.EndForPlayer(playerId);
        player.KarmaBreak();
        GD.Print($"{player.DisplayName} suffered a Karma Break.");
        EmitSignal(SignalName.KarmaEvent, $"{player.DisplayName}: Karma Break reset path to Unmarked.");
        EmitKarmaChanged();
        EmitLeaderboardChanged();
        EmitPerksChanged();
        EmitRelationshipsChanged();
        EmitFactionsChanged();
        EmitDuelsChanged();
    }

    public PlayerState RegisterPlayer(string playerId, string displayName)
    {
        if (_players.TryGetValue(playerId, out var existing))
        {
            return existing;
        }

        var player = new PlayerState(playerId, displayName);
        _players.Add(playerId, player);
        return player;
    }

    public bool SetPlayerPosition(string playerId, TilePosition position)
    {
        EnsurePrototypePlayers();
        if (!_players.TryGetValue(playerId, out var player))
        {
            return false;
        }

        player.SetPosition(position);
        return true;
    }

    public bool SetPlayerAppearance(string playerId, PlayerAppearanceSelection appearance)
    {
        EnsurePrototypePlayers();
        if (!_players.TryGetValue(playerId, out var player))
        {
            return false;
        }

        player.SetAppearance(appearance);
        return true;
    }

    public bool SetPlayerTeam(string playerId, string teamId)
    {
        EnsurePrototypePlayers();
        if (!_players.TryGetValue(playerId, out var player) || string.IsNullOrWhiteSpace(teamId))
        {
            return false;
        }

        player.SetTeam(teamId);
        return true;
    }

    public bool ClearPlayerTeamStatus(string playerId)
    {
        EnsurePrototypePlayers();
        if (!_players.TryGetValue(playerId, out var player))
        {
            return false;
        }

        player.ClearTeamStatus();
        return true;
    }

    public LeaderboardStanding GetLeaderboardStanding()
    {
        EnsurePrototypePlayers();
        var paragon = _players.Values
            .OrderByDescending(player => player.Karma.Score)
            .ThenBy(player => player.DisplayName)
            .First();
        var renegade = _players.Values
            .OrderBy(player => player.Karma.Score)
            .ThenBy(player => player.DisplayName)
            .First();

        return new LeaderboardStanding(
            paragon.Id,
            paragon.DisplayName,
            paragon.Karma.Score,
            renegade.Id,
            renegade.DisplayName,
            renegade.Karma.Score);
    }

    public void AddItem(GameItem item)
    {
        AddItem(LocalPlayerId, item);
    }

    public void AddItem(string playerId, GameItem item)
    {
        EnsurePrototypePlayers();
        _players[playerId].AddItem(item);
        EmitSignal(SignalName.KarmaEvent, $"{_players[playerId].DisplayName} picked up: {item.Name}");
        if (playerId == LocalPlayerId)
        {
            EmitInventoryChanged();
        }
    }

    public bool StartQuest(string questId)
    {
        return StartQuest(LocalPlayerId, questId);
    }

    public bool StartQuest(string playerId, string questId)
    {
        var quest = Quests.Get(questId);
        if (quest.Status != QuestStatus.Available)
        {
            return false;
        }

        quest.Start();
        WorldEvents.Add(WorldEventType.Quest, $"Quest started: {quest.Definition.Title}", playerId, quest.Definition.GiverNpcId);
        EmitSignal(SignalName.KarmaEvent, $"Quest started: {quest.Definition.Title}");
        EmitQuestsChanged();
        EmitWorldEventsChanged();
        return true;
    }

    public bool CompleteQuest(string playerId, string questId)
    {
        var quest = Quests.Get(questId);
        if (quest.Status == QuestStatus.Completed)
        {
            return false;
        }

        foreach (var itemId in quest.Definition.RequiredItemIds)
        {
            if (!ConsumeItem(playerId, itemId))
            {
                EmitSignal(SignalName.KarmaEvent, $"Quest needs item: {StarterItems.GetById(itemId).Name}");
                return false;
            }
        }

        if (!TryResolveQuestCompletionAction(playerId, quest.Definition, out var action))
        {
            return false;
        }

        quest.Complete();
        ApplyShift(playerId, action);
        if (quest.Definition.ScripReward > 0)
        {
            AddScrip(playerId, quest.Definition.ScripReward);
        }

        WorldEvents.Add(WorldEventType.Quest, $"Quest completed: {quest.Definition.Title}", playerId, quest.Definition.GiverNpcId);
        EmitSignal(SignalName.KarmaEvent, $"Quest completed: {quest.Definition.Title}");
        EmitQuestsChanged();
        EmitWorldEventsChanged();
        return true;
    }

    private static bool TryResolveQuestCompletionAction(string playerId, QuestDefinition quest, out KarmaAction action)
    {
        if (PrototypeActions.TryGet(quest.CompletionActionId, out action))
        {
            return true;
        }

        if (quest.CompletionActionId.StartsWith("generated_station_help:"))
        {
            action = new KarmaAction(
                playerId,
                quest.GiverNpcId,
                new[] { "helpful", "generous", "lawful" },
                $"You followed through on {quest.Title}.");
            return true;
        }

        action = null;
        return false;
    }

    public WorldEvent AddWorldEvent(
        WorldEventType type,
        string summary,
        string sourcePlayerId,
        string targetId)
    {
        var worldEvent = WorldEvents.Add(type, summary, sourcePlayerId, targetId);
        EmitSignal(SignalName.KarmaEvent, summary);
        EmitWorldEventsChanged();
        return worldEvent;
    }

    public int ApplyFactionReputation(string factionId, string playerId, int delta)
    {
        EnsurePrototypePlayers();
        var reputation = Factions.Apply(factionId, playerId, delta);
        EmitFactionsChanged();
        return reputation;
    }

    public bool DamagePlayer(string attackerId, string targetId, int amount, string reason)
    {
        EnsurePrototypePlayers();
        var target = _players[targetId];
        var before = target.Health;
        var died = target.ApplyDamage(amount);
        var actualDamage = before - target.Health;
        WorldEvents.Add(WorldEventType.Combat, $"{target.DisplayName} took {actualDamage} damage: {reason}", attackerId, targetId);
        EmitSignal(SignalName.KarmaEvent, $"{target.DisplayName} took {actualDamage} damage: {reason}");

        if (died)
        {
            Duels.EndForPlayer(targetId);
            TriggerKarmaBreak(targetId);
        }

        EmitCombatChanged();
        EmitWorldEventsChanged();
        return died;
    }

    public bool HealPlayer(string healerId, string targetId, int amount, string reason)
    {
        EnsurePrototypePlayers();
        if (!_players.TryGetValue(targetId, out var target))
        {
            return false;
        }

        var before = target.Health;
        target.Heal(amount);
        var actualHealing = target.Health - before;
        if (actualHealing <= 0)
        {
            return false;
        }

        WorldEvents.Add(WorldEventType.Combat, $"{target.DisplayName} recovered {actualHealing} health: {reason}", healerId, targetId);
        EmitSignal(SignalName.KarmaEvent, $"{target.DisplayName} recovered {actualHealing} health: {reason}");
        EmitCombatChanged();
        EmitWorldEventsChanged();
        return true;
    }

    public bool StartEntanglement(
        string playerId,
        string npcId,
        string affectedNpcId,
        EntanglementType type,
        KarmaAction action)
    {
        if (Entanglements.HasActive(playerId, npcId, type))
        {
            EmitSignal(SignalName.KarmaEvent, "That entanglement is already active.");
            return false;
        }

        Entanglements.Add(playerId, npcId, affectedNpcId, type, action.Context);
        ApplyShift(playerId, action);
        Relationships.Apply(affectedNpcId, playerId, -20);
        Factions.Apply(StarterFactions.FreeSettlersId, playerId, -8);
        EmitEntanglementsChanged();
        EmitRelationshipsChanged();
        EmitFactionsChanged();
        return true;
    }

    public bool ExposeEntanglement(string playerId, string entanglementId, KarmaAction action)
    {
        var entanglement = Entanglements.Get(entanglementId);
        if (entanglement.PlayerId != playerId || !Entanglements.Expose(entanglementId))
        {
            return false;
        }

        ApplyShift(playerId, action);
        Relationships.Apply(entanglement.NpcId, playerId, -15);
        Relationships.Apply(entanglement.AffectedNpcId, playerId, -25);
        Factions.Apply(StarterFactions.FreeSettlersId, playerId, -12);
        var hasRumorcraft = HasPerk(playerId, PerkCatalog.RumorcraftId);
        var rumorTargetId = hasRumorcraft ? WorldEvent.GlobalTargetId : entanglement.NpcId;
        var rumorSummary = hasRumorcraft
            ? $"{entanglement.Summary} (amplified by Rumorcraft)"
            : entanglement.Summary;
        WorldEvents.Add(
            WorldEventType.Rumor,
            rumorSummary,
            playerId,
            rumorTargetId);
        EmitSignal(SignalName.KarmaEvent, $"Rumor exposed: {rumorSummary}");
        EmitEntanglementsChanged();
        EmitRelationshipsChanged();
        EmitFactionsChanged();
        EmitWorldEventsChanged();
        return true;
    }

    public bool EquipPlayer(string playerId, string itemId)
    {
        EnsurePrototypePlayers();
        var item = StarterItems.GetById(itemId);
        if (!ConsumeItem(playerId, itemId))
        {
            EmitSignal(SignalName.KarmaEvent, $"Cannot equip missing item: {item.Name}");
            return false;
        }

        var equipped = _players[playerId].Equip(item);
        if (equipped)
        {
            EmitSignal(SignalName.KarmaEvent, $"{_players[playerId].DisplayName} equipped {item.Name}");
            EmitCombatChanged();
            EmitInventoryChanged();
        }

        return equipped;
    }

    public bool HasItem(string itemId)
    {
        return HasItem(LocalPlayerId, itemId);
    }

    public bool HasItem(string playerId, string itemId)
    {
        EnsurePrototypePlayers();
        return _players.TryGetValue(playerId, out var player) && player.HasItem(itemId);
    }

    public bool ConsumeItem(string itemId)
    {
        return ConsumeItem(LocalPlayerId, itemId);
    }

    public bool ConsumeItem(string playerId, string itemId)
    {
        EnsurePrototypePlayers();
        if (!_players.TryGetValue(playerId, out var player) || !player.ConsumeItem(itemId))
        {
            return false;
        }

        if (playerId == LocalPlayerId)
        {
            EmitInventoryChanged();
        }

        return true;
    }

    public bool TransferItem(string fromPlayerId, string toPlayerId, string itemId)
    {
        EnsurePrototypePlayers();
        if (!StarterItems.TryGetById(itemId, out var item) ||
            !_players.ContainsKey(toPlayerId) ||
            !ConsumeItem(fromPlayerId, itemId))
        {
            return false;
        }

        _players[toPlayerId].AddItem(item);
        EmitSignal(SignalName.KarmaEvent, $"{_players[fromPlayerId].DisplayName} gave {item.Name} to {_players[toPlayerId].DisplayName}");
        if (fromPlayerId == LocalPlayerId || toPlayerId == LocalPlayerId)
        {
            EmitInventoryChanged();
        }

        return true;
    }

    public void AddScrip(string playerId, int amount)
    {
        EnsurePrototypePlayers();
        if (!_players.TryGetValue(playerId, out var player))
        {
            return;
        }

        player.AddScrip(amount);
        EmitSignal(SignalName.KarmaEvent, $"{player.DisplayName} gained {amount} scrip.");
        if (playerId == LocalPlayerId)
        {
            EmitInventoryChanged();
        }
    }

    public bool SpendScrip(string playerId, int amount)
    {
        EnsurePrototypePlayers();
        if (!_players.TryGetValue(playerId, out var player) || !player.SpendScrip(amount))
        {
            return false;
        }

        if (playerId == LocalPlayerId)
        {
            EmitInventoryChanged();
        }

        return true;
    }

    public bool PurchaseItem(string playerId, GameItem item, int price)
    {
        EnsurePrototypePlayers();
        if (!SpendScrip(playerId, price))
        {
            return false;
        }

        _players[playerId].AddItem(item);
        EmitSignal(SignalName.KarmaEvent, $"{_players[playerId].DisplayName} bought {item.Name} for {price} scrip.");
        if (playerId == LocalPlayerId)
        {
            EmitInventoryChanged();
        }

        return true;
    }

    public bool TransferScrip(string fromPlayerId, string toPlayerId, int amount)
    {
        EnsurePrototypePlayers();
        if (!_players.ContainsKey(toPlayerId) || !SpendScrip(fromPlayerId, amount))
        {
            return false;
        }

        _players[toPlayerId].AddScrip(amount);
        EmitSignal(SignalName.KarmaEvent, $"{_players[fromPlayerId].DisplayName} transferred {amount} scrip to {_players[toPlayerId].DisplayName}.");
        if (fromPlayerId == LocalPlayerId || toPlayerId == LocalPlayerId)
        {
            EmitInventoryChanged();
        }

        return true;
    }

    public IReadOnlyList<GameItem> DrainInventory(string playerId)
    {
        EnsurePrototypePlayers();
        if (!_players.TryGetValue(playerId, out var player))
        {
            return Array.Empty<GameItem>();
        }

        var items = player.DrainInventory();
        if (playerId == LocalPlayerId)
        {
            EmitInventoryChanged();
        }

        return items;
    }

    public void NotifyDuelsChanged()
    {
        EmitDuelsChanged();
    }

    public GameSnapshot CreateSnapshot()
    {
        EnsurePrototypePlayers();
        var standing = GetLeaderboardStanding();
        return new GameSnapshot(
            SnapshotBuilder.PlayersFrom(_players, standing),
            SnapshotBuilder.LeaderboardFrom(standing),
            Inventory.Select(item => item.Id).ToArray(),
            Quests.Snapshot(),
            Relationships.Snapshot(),
            Factions.Snapshot(),
            Entanglements.All.ToArray(),
            Duels.All.ToArray(),
            WorldEvents.Events.ToArray());
    }

    private void EmitKarmaChanged()
    {
        EmitSignal(SignalName.KarmaChanged, LocalKarma.Score, LocalKarma.TierName, GetPathName());
    }

    private void EmitInventoryChanged()
    {
        var inventoryText = Inventory.Count == 0
            ? $"Scrip: {LocalScrip} | Inventory: empty"
            : $"Scrip: {LocalScrip} | Inventory: {string.Join(", ", Inventory.Select(item => item.Name))}";
        EmitSignal(SignalName.InventoryChanged, inventoryText);
    }

    private string GetPathName()
    {
        return LocalKarma.Path == KarmaDirection.Neutral ? "Unmarked" : LocalKarma.Path.ToString();
    }

    private void EmitLeaderboardChanged()
    {
        EmitSignal(SignalName.LeaderboardChanged, GetLeaderboardStanding().Summary);
    }

    private void EmitPerksChanged()
    {
        EmitSignal(SignalName.PerksChanged, PerkCatalog.Format(LocalPerks));
    }

    private void EmitRelationshipsChanged()
    {
        var maraOpinion = Relationships.GetOpinion(StarterNpcs.Mara.Id, LocalPlayerId);
        var label = RelationshipRules.GetOpinionLabel(maraOpinion);
        EmitSignal(SignalName.RelationshipsChanged, $"Mara: {label} ({maraOpinion:+#;-#;0})");
    }

    private void EmitFactionsChanged()
    {
        var freeSettlers = Factions.GetReputation(StarterFactions.FreeSettlersId, LocalPlayerId);
        var civicRepair = Factions.GetReputation(StarterFactions.CivicRepairGuildId, LocalPlayerId);
        EmitSignal(
            SignalName.FactionsChanged,
            $"Free Settlers Rep: {freeSettlers:+#;-#;0} | Civic Repair Guild Rep: {civicRepair:+#;-#;0}");
    }

    private void EmitQuestsChanged()
    {
        EmitSignal(SignalName.QuestsChanged, Quests.FormatActiveSummary());
    }

    private void EmitCombatChanged()
    {
        var peer = _players.GetValueOrDefault("peer_stand_in");
        var text = peer is null
            ? "Combat: none"
            : $"Stranded Player HP: {peer.Health}/{peer.MaxHealth} DEF:{peer.Defense}";
        EmitSignal(SignalName.CombatChanged, text);
    }

    private void EmitEntanglementsChanged()
    {
        EmitSignal(SignalName.EntanglementsChanged, Entanglements.FormatSummary());
    }

    private void EmitDuelsChanged()
    {
        EmitSignal(SignalName.DuelsChanged, Duels.FormatSummary());
    }

    private void EmitWorldEventsChanged()
    {
        EmitSignal(SignalName.WorldEventsChanged, WorldEvents.FormatLatestSummary());
    }

    private void ApplyRelationshipDelta(string playerId, KarmaAction action)
    {
        var delta = RelationshipRules.CalculateDelta(action);
        delta = ApplyRelationshipPerks(playerId, action, delta);
        if (delta == 0)
        {
            return;
        }

        Relationships.Apply(action.TargetId, playerId, delta);
    }

    private int ApplyRelationshipPerks(string playerId, KarmaAction action, int delta)
    {
        if (delta >= 0 || !_players.TryGetValue(playerId, out var player))
        {
            return delta;
        }

        var perks = PerkCatalog.GetForPlayer(player, GetLeaderboardStanding());
        if (perks.Any(perk => perk.Id == PerkCatalog.CalmingPresenceId))
        {
            return Math.Min(0, (int)Math.Ceiling(delta * 0.5f));
        }

        if (perks.Any(perk => perk.Id == PerkCatalog.DreadReputationId) && IsDreadReactionAction(action))
        {
            return Math.Min(0, (int)Math.Ceiling(delta * 0.75f));
        }

        return delta;
    }

    private bool HasPerk(string playerId, string perkId)
    {
        return _players.TryGetValue(playerId, out var player) &&
               PerkCatalog.GetForPlayer(player, GetLeaderboardStanding()).Any(perk => perk.Id == perkId);
    }

    private static bool IsDreadReactionAction(KarmaAction action)
    {
        return action.Tags.Any(tag => tag is "harmful" or "violent" or "deceptive" or "humiliating");
    }

    private void ApplyFactionDelta(string playerId, KarmaAction action)
    {
        if (action.TargetId == StarterNpcs.Mara.Id || action.TargetId == StarterNpcs.Dallen.Id)
        {
            var delta = KarmaRules.CalculateShift(action).Amount / 2;
            Factions.Apply(StarterFactions.FreeSettlersId, playerId, delta);
        }
    }

    private void EnsurePrototypePlayers()
    {
        RegisterPlayer(LocalPlayerId, "You");
        RegisterPlayer("peer_stand_in", "Stranded Player");
        RegisterPlayer("rival_paragon", "Helpful Rival");
        RegisterPlayer("rival_renegade", "Shady Rival");

        var helpfulRival = _players["rival_paragon"];
        if (helpfulRival.Karma.Score == 0)
        {
            helpfulRival.ApplyKarma(8);
        }

        var shadyRival = _players["rival_renegade"];
        if (shadyRival.Karma.Score == 0)
        {
            shadyRival.ApplyKarma(-8);
        }

        if (!_prototypeInventorySeeded)
        {
            _players["peer_stand_in"].AddItem(StarterItems.RepairKit);
            _players[LocalPlayerId].AddScrip(25);
            _players["peer_stand_in"].AddScrip(10);
            _prototypeInventorySeeded = true;
        }
    }
}
