using System;
using System.Collections.Generic;
using System.Linq;
using Karma.Core;
using Karma.Data;
using Karma.World;

namespace Karma.Net;

public sealed class AuthoritativeWorldServer
{
    private readonly GameState _state;
    private readonly Dictionary<string, int> _lastSequenceByPlayer = new();
    private readonly HashSet<string> _connectedPlayerIds = new();
    private readonly Dictionary<string, WorldItemEntity> _worldItems = new();
    private readonly Dictionary<string, NpcEntity> _npcs = new();
    private readonly List<ServerEvent> _eventLog = new();
    private readonly MatchState _match;
    private GeneratedTileMap _tileMap;
    private long _tick;
    private bool _matchRewardsPaid;

    public AuthoritativeWorldServer(GameState state, string worldId, ServerConfig config = null)
    {
        _state = state;
        WorldId = worldId;
        Config = config ?? ServerConfig.Prototype4Player;
        Config.Validate();
        _match = new MatchState(Config.MatchDurationSeconds);
        SeedConnectedPlayers();
        SeedStarterNpcs();
    }

    public string WorldId { get; }
    public ServerConfig Config { get; }
    public long Tick => _tick;
    public IReadOnlyList<ServerEvent> EventLog => _eventLog;
    public IReadOnlyCollection<string> ConnectedPlayerIds => _connectedPlayerIds;
    public IReadOnlyDictionary<string, WorldItemEntity> WorldItems => _worldItems;
    public IReadOnlyDictionary<string, NpcEntity> Npcs => _npcs;
    public MatchSnapshot Match => _match.Snapshot(_state.GetLeaderboardStanding());

    public void SetTileMap(GeneratedTileMap tileMap)
    {
        _tileMap = tileMap;
    }

    public void AdvanceMatchTime(int seconds)
    {
        var previousStatus = _match.Status;
        _match.Advance(seconds, _state.GetLeaderboardStanding());
        if (previousStatus != MatchStatus.Finished && _match.Status == MatchStatus.Finished)
        {
            var snapshot = Match;
            PayMatchRewards(snapshot);
            AppendEvent(
                "match_finished",
                snapshot.Summary,
                new Dictionary<string, string>
                {
                    ["saintWinnerId"] = snapshot.SaintWinnerId,
                    ["saintWinnerScore"] = snapshot.SaintWinnerScore.ToString(),
                    ["saintScripReward"] = GetWinnerReward(snapshot.SaintWinnerId).ToString(),
                    ["scourgeWinnerId"] = snapshot.ScourgeWinnerId,
                    ["scourgeWinnerScore"] = snapshot.ScourgeWinnerScore.ToString(),
                    ["scourgeScripReward"] = GetWinnerReward(snapshot.ScourgeWinnerId).ToString()
                });
        }
    }

    private void PayMatchRewards(MatchSnapshot snapshot)
    {
        if (_matchRewardsPaid)
        {
            return;
        }

        PayWinner(snapshot.SaintWinnerId);
        PayWinner(snapshot.ScourgeWinnerId);
        _matchRewardsPaid = true;
    }

    private void PayWinner(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
        {
            return;
        }

        _state.AddScrip(playerId, ServerConfig.DefaultMatchWinnerScripReward);
    }

    private static int GetWinnerReward(string playerId)
    {
        return string.IsNullOrWhiteSpace(playerId) ? 0 : ServerConfig.DefaultMatchWinnerScripReward;
    }

    public void SeedWorldItem(string entityId, GameItem item, TilePosition position)
    {
        _worldItems[entityId] = new WorldItemEntity(entityId, item, position, IsAvailable: true);
    }

    public ServerJoinResult JoinPlayer(string playerId, string displayName)
    {
        if (_connectedPlayerIds.Contains(playerId))
        {
            return ServerJoinResult.Accepted(playerId, "Player already connected.");
        }

        if (_connectedPlayerIds.Count >= Config.MaxPlayers)
        {
            return ServerJoinResult.Rejected(
                playerId,
                $"World is full ({Config.MaxPlayers}/{Config.MaxPlayers}).");
        }

        _state.RegisterPlayer(playerId, displayName);
        _connectedPlayerIds.Add(playerId);
        AppendEvent(
            "player_joined",
            $"{displayName} joined {WorldId}",
            new Dictionary<string, string>
            {
                ["playerId"] = playerId,
                ["displayName"] = displayName,
                ["connectedPlayers"] = _connectedPlayerIds.Count.ToString(),
                ["maxPlayers"] = Config.MaxPlayers.ToString()
            });

        return ServerJoinResult.Accepted(playerId, "Player joined.");
    }

    public ServerProcessResult ProcessIntent(ServerIntent intent)
    {
        _tick++;

        if (!_connectedPlayerIds.Contains(intent.PlayerId))
        {
            return Reject(intent, $"Unknown or disconnected player: {intent.PlayerId}.");
        }

        if (_lastSequenceByPlayer.TryGetValue(intent.PlayerId, out var lastSequence) &&
            intent.Sequence <= lastSequence)
        {
            return Reject(intent, $"Rejected stale intent {intent.Sequence}; last accepted was {lastSequence}.");
        }

        if (_match.Status == MatchStatus.Finished && !IsPostMatchIntentAllowed(intent.Type))
        {
            return Reject(intent, $"Match is finished; {intent.Type} is no longer accepted.");
        }

        _lastSequenceByPlayer[intent.PlayerId] = intent.Sequence;

        return intent.Type switch
        {
            IntentType.Move => ProcessMove(intent),
            IntentType.Interact => ProcessInteract(intent),
            IntentType.RequestDuel => ProcessRequestDuel(intent),
            IntentType.AcceptDuel => ProcessAcceptDuel(intent),
            IntentType.Attack => ProcessAttack(intent),
            IntentType.UseItem => ProcessUseItem(intent),
            IntentType.PurchaseItem => ProcessPurchaseItem(intent),
            IntentType.TransferItem => ProcessTransferItem(intent),
            IntentType.TransferCurrency => ProcessTransferCurrency(intent),
            IntentType.PlaceObject => ProcessPlaceObject(intent),
            IntentType.StartDialogue => ProcessStartDialogue(intent),
            IntentType.SelectDialogueChoice => ProcessSelectDialogueChoice(intent),
            IntentType.StartQuest => ProcessStartQuest(intent),
            IntentType.CompleteQuest => ProcessCompleteQuest(intent),
            IntentType.StartEntanglement => ProcessStartEntanglement(intent),
            IntentType.ExposeEntanglement => ProcessExposeEntanglement(intent),
            IntentType.KarmaAction => ProcessKarmaAction(intent),
            IntentType.KarmaBreak => ProcessKarmaBreak(intent),
            _ => Reject(intent, $"Unsupported intent type: {intent.Type}")
        };
    }

    private static bool IsPostMatchIntentAllowed(IntentType type)
    {
        return type is IntentType.Move or IntentType.StartDialogue;
    }

    public PlayerInterest GetInterestFor(string playerId)
    {
        if (!_state.Players.TryGetValue(playerId, out var player))
        {
            return new PlayerInterest(playerId, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
        }

        var radiusSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        var visiblePlayers = _state.Players.Values
            .Where(candidate => candidate.Id != playerId)
            .Where(candidate => player.Position.DistanceSquaredTo(candidate.Position) <= radiusSquared)
            .OrderBy(candidate => candidate.Id)
            .Select(candidate => candidate.Id)
            .ToArray();
        var visibleEntities = _worldItems.Values
            .Where(entity => entity.IsAvailable)
            .Where(entity => player.Position.DistanceSquaredTo(entity.Position) <= radiusSquared)
            .OrderBy(entity => entity.EntityId)
            .Select(entity => entity.EntityId)
            .ToArray();
        var visibleNpcs = _npcs.Values
            .Where(entity => player.Position.DistanceSquaredTo(entity.Position) <= radiusSquared)
            .OrderBy(entity => entity.Profile.Id)
            .Select(entity => entity.Profile.Id)
            .ToArray();

        return new PlayerInterest(playerId, visiblePlayers, visibleEntities, visibleNpcs);
    }

    public ClientInterestSnapshot CreateInterestSnapshot(string playerId, long afterTick = 0)
    {
        var interest = GetInterestFor(playerId);
        var visiblePlayerIds = new HashSet<string>(interest.VisiblePlayerIds)
        {
            playerId
        };
        var standing = _state.GetLeaderboardStanding();
        var visiblePlayers = _state.Players.Values
            .Where(player => visiblePlayerIds.Contains(player.Id))
            .ToArray();
        var visibleNpcs = _npcs.Values
            .Where(entity => interest.VisibleNpcIds.Contains(entity.Profile.Id))
            .OrderBy(entity => entity.Profile.Id)
            .Select(ToSnapshot)
            .ToArray();
        var visibleDialogues = interest.VisibleNpcIds
            .OrderBy(npcId => npcId)
            .Select(npcId => GetDialogueFor(playerId, npcId))
            .Where(dialogue => dialogue.Choices.Count > 0)
            .ToArray();
        var visibleQuests = _state.Quests.Quests.Values
            .Where(quest => interest.VisibleNpcIds.Contains(quest.Definition.GiverNpcId))
            .OrderBy(quest => quest.Definition.Id)
            .Select(quest => new QuestSnapshot(quest.Definition.Id, quest.Status, quest.Definition.ScripReward))
            .ToArray();
        var visibleMapChunks = CreateVisibleMapChunks(playerId);
        var visibleWorldItems = _worldItems.Values
            .Where(entity => interest.VisibleEntityIds.Contains(entity.EntityId))
            .OrderBy(entity => entity.EntityId)
            .Select(ToSnapshot)
            .ToArray();
        var visibleShopOffers = CreateVisibleShopOffers(playerId, interest.VisibleNpcIds, standing);
        var visibleDuels = _state.Duels.All
            .Where(duel => IsDuelVisibleTo(duel, visiblePlayerIds))
            .OrderBy(duel => duel.Id)
            .ToArray();
        var events = _eventLog
            .Where(serverEvent => serverEvent.Tick > afterTick)
            .Where(serverEvent => IsEventVisibleTo(serverEvent, visiblePlayerIds))
            .OrderBy(serverEvent => serverEvent.Tick)
            .ToArray();
        var worldEvents = _state.WorldEvents.Events
            .Where(worldEvent => IsWorldEventVisibleTo(worldEvent, visiblePlayerIds))
            .ToArray();

        return new ClientInterestSnapshot(
            WorldId,
            Tick,
            playerId,
            Config.InterestRadiusTiles,
            interest,
            SnapshotBuilder.PlayersFrom(visiblePlayers, standing),
            visibleNpcs,
            visibleDialogues,
            visibleQuests,
            visibleMapChunks,
            visibleWorldItems,
            visibleShopOffers,
            SnapshotBuilder.LeaderboardFrom(standing),
            _match.Snapshot(standing),
            visibleDuels,
            CreateSyncHint(afterTick, visibleMapChunks, events, worldEvents),
            events,
            worldEvents);
    }

    private static InterestSnapshotSyncHint CreateSyncHint(
        long afterTick,
        IReadOnlyList<MapChunkSnapshot> visibleMapChunks,
        IReadOnlyList<ServerEvent> events,
        IReadOnlyList<WorldEvent> worldEvents)
    {
        return new InterestSnapshotSyncHint(
            afterTick,
            afterTick > 0,
            events.Count,
            worldEvents.Count,
            visibleMapChunks.Count,
            CalculateVisibleMapRevision(visibleMapChunks));
    }

    private static int CalculateVisibleMapRevision(IReadOnlyList<MapChunkSnapshot> visibleMapChunks)
    {
        unchecked
        {
            var hash = 19;
            foreach (var chunk in visibleMapChunks.OrderBy(chunk => chunk.ChunkKey))
            {
                hash = (hash * 31) + StableStringHash(chunk.ChunkKey);
                hash = (hash * 31) + chunk.Revision;
            }

            return hash;
        }
    }

    private IReadOnlyList<MapChunkSnapshot> CreateVisibleMapChunks(string playerId)
    {
        if (_tileMap is null || !_state.Players.TryGetValue(playerId, out var player))
        {
            return Array.Empty<MapChunkSnapshot>();
        }

        return _tileMap
            .GetChunksAround(player.Position.X, player.Position.Y, Config.InterestRadiusChunks)
            .Select(ToSnapshot)
            .ToArray();
    }

    private IReadOnlyList<ShopOfferSnapshot> CreateVisibleShopOffers(
        string playerId,
        IReadOnlyCollection<string> visibleNpcIds,
        LeaderboardStanding standing)
    {
        if (!_state.Players.TryGetValue(playerId, out var player))
        {
            return Array.Empty<ShopOfferSnapshot>();
        }

        return StarterShopCatalog.Offers
            .Where(offer => visibleNpcIds.Contains(offer.VendorNpcId))
            .OrderBy(offer => offer.Id)
            .Select(offer => ToSnapshot(offer, player, standing))
            .ToArray();
    }

    private ServerProcessResult ProcessMove(ServerIntent intent)
    {
        if (!TryReadInt(intent.Payload, "x", out var x) || !TryReadInt(intent.Payload, "y", out var y))
        {
            return Reject(intent, "Move intent requires integer x and y payload values.");
        }

        _state.SetPlayerPosition(intent.PlayerId, new TilePosition(x, y));
        var serverEvent = AppendEvent(
            "player_moved",
            $"{intent.PlayerId} moved to {x},{y}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["x"] = x.ToString(),
                ["y"] = y.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessKarmaAction(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("action", out var actionId))
        {
            return Reject(intent, "Missing karma action id.");
        }

        if (!PrototypeActions.TryGet(actionId, out var action))
        {
            return Reject(intent, $"Unknown karma action id: {actionId}");
        }

        if (!CanReachPlayerActionTarget(intent.PlayerId, action.TargetId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        var shift = _state.ApplyShift(intent.PlayerId, action);
        var serverEvent = AppendEvent(
            "karma_shift",
            $"{intent.PlayerId} {shift.Direction} {Math.Abs(shift.Amount)} karma",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["action"] = actionId,
                ["amount"] = shift.Amount.ToString(),
                ["direction"] = shift.Direction.ToString(),
                ["targetId"] = action.TargetId
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessInteract(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("entityId", out var entityId))
        {
            return Reject(intent, "Interact intent requires entityId.");
        }

        if (!_state.Players.TryGetValue(intent.PlayerId, out var player) ||
            !_worldItems.TryGetValue(entityId, out var entity))
        {
            return Reject(intent, $"Unknown interact entity: {entityId}.");
        }

        if (!entity.IsAvailable)
        {
            return Reject(intent, $"Interact entity is no longer available: {entityId}.");
        }

        var radiusSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        if (player.Position.DistanceSquaredTo(entity.Position) > radiusSquared)
        {
            return Reject(intent, $"Interact entity is out of range: {entityId}.");
        }

        var dropOwnerId = GetDropOwnerId(entity.EntityId);
        var karmaAmount = 0;
        if (!string.IsNullOrWhiteSpace(dropOwnerId) && dropOwnerId != intent.PlayerId)
        {
            var shift = _state.ApplyShift(
                intent.PlayerId,
                new KarmaAction(
                    intent.PlayerId,
                    dropOwnerId,
                    new[] { "harmful", "selfish" },
                    $"{_state.Players[intent.PlayerId].DisplayName} claimed {entity.Item.Name} from {dropOwnerId}'s Karma Break drop."));
            karmaAmount = shift.Amount;
        }

        _state.AddItem(intent.PlayerId, entity.Item);
        _worldItems[entityId] = entity with { IsAvailable = false };
        var serverEvent = AppendEvent(
            "item_picked_up",
            $"{intent.PlayerId} picked up {entity.Item.Id}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["entityId"] = entity.EntityId,
                ["itemId"] = entity.Item.Id,
                ["dropOwnerId"] = dropOwnerId,
                ["karmaAmount"] = karmaAmount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessRequestDuel(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("targetId", out var targetId))
        {
            return Reject(intent, "RequestDuel intent requires targetId.");
        }

        if (!CanReachPlayer(intent.PlayerId, targetId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        var duel = _state.Duels.Request(intent.PlayerId, targetId);
        _state.NotifyDuelsChanged();
        var serverEvent = AppendEvent(
            "duel_requested",
            $"{intent.PlayerId} requested a duel with {targetId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["targetId"] = targetId,
                ["duelId"] = duel.Id,
                ["status"] = duel.Status.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessAcceptDuel(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("challengerId", out var challengerId))
        {
            return Reject(intent, "AcceptDuel intent requires challengerId.");
        }

        if (!CanReachPlayer(intent.PlayerId, challengerId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        if (!_state.Duels.Accept(intent.PlayerId, challengerId, out var duel))
        {
            return Reject(intent, $"No requested duel from player: {challengerId}.");
        }

        _state.NotifyDuelsChanged();
        var serverEvent = AppendEvent(
            "duel_accepted",
            $"{intent.PlayerId} accepted a duel with {challengerId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["targetId"] = challengerId,
                ["duelId"] = duel.Id,
                ["status"] = duel.Status.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessAttack(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("targetId", out var targetId))
        {
            return Reject(intent, "Attack intent requires targetId.");
        }

        if (targetId == intent.PlayerId)
        {
            return Reject(intent, "Players cannot attack themselves.");
        }

        if (!_connectedPlayerIds.Contains(targetId) ||
            !_state.Players.TryGetValue(intent.PlayerId, out var attacker) ||
            !_state.Players.TryGetValue(targetId, out var target))
        {
            return Reject(intent, $"Unknown attack target: {targetId}.");
        }

        var rangeSquared = Config.CombatRangeTiles * Config.CombatRangeTiles;
        if (attacker.Position.DistanceSquaredTo(target.Position) > rangeSquared)
        {
            return Reject(intent, $"Attack target is out of range: {targetId}.");
        }

        var isDuelAttack = _state.Duels.IsActive(intent.PlayerId, targetId);
        var shift = isDuelAttack
            ? new KarmaShift(0, KarmaDirection.Neutral, $"{attacker.DisplayName} struck {target.DisplayName} during an accepted duel.")
            : _state.ApplyShift(
                intent.PlayerId,
                new KarmaAction(
                    intent.PlayerId,
                    targetId,
                    new[] { "violent", "harmful", "chaotic" },
                    $"{attacker.DisplayName} attacked {target.DisplayName} outside a duel."));
        var damage = 30 + attacker.AttackPower;
        var died = _state.DamagePlayer(intent.PlayerId, targetId, damage, isDuelAttack ? "accepted duel strike" : "server-authorized attack");
        var droppedItemCount = died ? DropInventory(targetId).Count : 0;
        var serverEvent = AppendEvent(
            "player_attacked",
            $"{intent.PlayerId} attacked {targetId} for {damage} raw damage",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["targetId"] = targetId,
                ["rawDamage"] = damage.ToString(),
                ["died"] = died.ToString(),
                ["duel"] = isDuelAttack.ToString(),
                ["droppedItemCount"] = droppedItemCount.ToString(),
                ["karmaAmount"] = shift.Amount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    public NpcDialogueSnapshot GetDialogueFor(string playerId, string npcId)
    {
        if (!_state.Players.TryGetValue(playerId, out var player) ||
            !_npcs.TryGetValue(npcId, out var npc))
        {
            return new NpcDialogueSnapshot(npcId, "Unknown", "No dialogue available.", Array.Empty<NpcDialogueChoice>());
        }

        var radiusSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        if (player.Position.DistanceSquaredTo(npc.Position) > radiusSquared)
        {
            return new NpcDialogueSnapshot(npcId, npc.Profile.Name, "Too far away.", Array.Empty<NpcDialogueChoice>());
        }

        return new NpcDialogueSnapshot(
            npc.Profile.Id,
            npc.Profile.Name,
            $"{npc.Profile.Name} needs {npc.Profile.Need}.",
            GetChoicesFor(npc.Profile.Id));
    }

    private ServerProcessResult ProcessStartDialogue(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("npcId", out var npcId))
        {
            return Reject(intent, "StartDialogue intent requires npcId.");
        }

        var dialogue = GetDialogueFor(intent.PlayerId, npcId);
        if (dialogue.Choices.Count == 0)
        {
            return Reject(intent, $"Dialogue unavailable for NPC: {npcId}.");
        }

        var serverEvent = AppendEvent(
            "dialogue_started",
            $"{intent.PlayerId} started dialogue with {npcId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["npcId"] = npcId,
                ["choiceIds"] = string.Join(",", dialogue.Choices.Select(choice => choice.Id))
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessSelectDialogueChoice(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("npcId", out var npcId) ||
            !intent.Payload.TryGetValue("choiceId", out var choiceId))
        {
            return Reject(intent, "SelectDialogueChoice intent requires npcId and choiceId.");
        }

        var dialogue = GetDialogueFor(intent.PlayerId, npcId);
        var choice = dialogue.Choices.FirstOrDefault(candidate => candidate.Id == choiceId);
        if (choice is null)
        {
            return Reject(intent, $"Dialogue choice unavailable: {choiceId}.");
        }

        if (!string.IsNullOrWhiteSpace(choice.RequiredItemId) &&
            !_state.ConsumeItem(intent.PlayerId, choice.RequiredItemId))
        {
            return Reject(intent, $"Dialogue choice requires missing item: {choice.RequiredItemId}.");
        }

        if (!PrototypeActions.TryGet(choice.ActionId, out var action))
        {
            return Reject(intent, $"Dialogue choice action is unknown: {choice.ActionId}.");
        }

        var shift = _state.ApplyShift(intent.PlayerId, action);
        var serverEvent = AppendEvent(
            "dialogue_choice_selected",
            $"{intent.PlayerId} selected {choiceId} with {npcId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["npcId"] = npcId,
                ["choiceId"] = choice.Id,
                ["action"] = choice.ActionId,
                ["amount"] = shift.Amount.ToString(),
                ["targetId"] = action.TargetId
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessStartQuest(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("questId", out var questId))
        {
            return Reject(intent, "StartQuest intent requires questId.");
        }

        if (!CanReachQuestGiver(intent.PlayerId, questId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        if (!_state.StartQuest(intent.PlayerId, questId))
        {
            return Reject(intent, $"Quest cannot be started: {questId}.");
        }

        var quest = _state.Quests.Get(questId);
        var serverEvent = AppendEvent(
            "quest_started",
            $"{intent.PlayerId} started quest {questId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["questId"] = questId,
                ["targetId"] = quest.Definition.GiverNpcId
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessCompleteQuest(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("questId", out var questId))
        {
            return Reject(intent, "CompleteQuest intent requires questId.");
        }

        if (!CanReachQuestGiver(intent.PlayerId, questId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        if (!_state.CompleteQuest(intent.PlayerId, questId))
        {
            return Reject(intent, $"Quest cannot be completed: {questId}.");
        }

        var quest = _state.Quests.Get(questId);
        var serverEvent = AppendEvent(
            "quest_completed",
            $"{intent.PlayerId} completed quest {questId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["questId"] = questId,
                ["targetId"] = quest.Definition.GiverNpcId,
                ["scripReward"] = quest.Definition.ScripReward.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessStartEntanglement(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("npcId", out var npcId) ||
            !intent.Payload.TryGetValue("affectedNpcId", out var affectedNpcId))
        {
            return Reject(intent, "StartEntanglement intent requires npcId and affectedNpcId.");
        }

        var type = EntanglementType.Romantic;
        if (intent.Payload.TryGetValue("type", out var typeText) &&
            !Enum.TryParse(typeText, ignoreCase: true, out type))
        {
            return Reject(intent, $"Unknown entanglement type: {typeText}.");
        }

        var actionId = intent.Payload.TryGetValue("action", out var payloadActionId)
            ? payloadActionId
            : PrototypeActions.StartMaraEntanglementId;
        if (!PrototypeActions.TryGet(actionId, out var action))
        {
            return Reject(intent, $"Unknown entanglement action id: {actionId}.");
        }

        if (!CanReachNpc(intent.PlayerId, npcId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        if (!_npcs.ContainsKey(affectedNpcId))
        {
            return Reject(intent, $"Affected NPC unavailable: {affectedNpcId}.");
        }

        if (!_state.StartEntanglement(intent.PlayerId, npcId, affectedNpcId, type, action))
        {
            return Reject(intent, $"Entanglement cannot be started with NPC: {npcId}.");
        }

        _state.Entanglements.TryGetActive(intent.PlayerId, npcId, type, out var entanglement);
        var serverEvent = AppendEvent(
            "entanglement_started",
            $"{intent.PlayerId} started a {type} entanglement with {npcId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["npcId"] = npcId,
                ["affectedNpcId"] = affectedNpcId,
                ["type"] = type.ToString(),
                ["action"] = actionId,
                ["entanglementId"] = entanglement?.Id ?? string.Empty
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessExposeEntanglement(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("entanglementId", out var entanglementId))
        {
            return Reject(intent, "ExposeEntanglement intent requires entanglementId.");
        }

        var entanglement = _state.Entanglements.All.FirstOrDefault(candidate => candidate.Id == entanglementId);
        if (entanglement is null)
        {
            return Reject(intent, $"Unknown entanglement id: {entanglementId}.");
        }

        if (!CanReachNpc(intent.PlayerId, entanglement.NpcId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        var actionId = intent.Payload.TryGetValue("action", out var payloadActionId)
            ? payloadActionId
            : PrototypeActions.ExposeMaraEntanglementId;
        if (!PrototypeActions.TryGet(actionId, out var action))
        {
            return Reject(intent, $"Unknown entanglement action id: {actionId}.");
        }

        if (!_state.ExposeEntanglement(intent.PlayerId, entanglementId, action))
        {
            return Reject(intent, $"Entanglement cannot be exposed: {entanglementId}.");
        }

        var serverEvent = AppendEvent(
            "entanglement_exposed",
            $"{intent.PlayerId} exposed entanglement {entanglementId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["npcId"] = entanglement.NpcId,
                ["affectedNpcId"] = entanglement.AffectedNpcId,
                ["type"] = entanglement.Type.ToString(),
                ["action"] = actionId,
                ["entanglementId"] = entanglementId
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessUseItem(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("itemId", out var itemId))
        {
            return Reject(intent, "UseItem intent requires itemId.");
        }

        if (!StarterItems.TryGetById(itemId, out var item))
        {
            return Reject(intent, $"Unknown item id: {itemId}.");
        }

        if (item.Slot == EquipmentSlot.None)
        {
            return Reject(intent, $"Item cannot be equipped: {item.Name}.");
        }

        if (!_state.EquipPlayer(intent.PlayerId, itemId))
        {
            return Reject(intent, $"Player cannot equip item: {item.Name}.");
        }

        var serverEvent = AppendEvent(
            "item_equipped",
            $"{intent.PlayerId} equipped {item.Id}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["itemId"] = item.Id,
                ["slot"] = item.Slot.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessPurchaseItem(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("offerId", out var offerId))
        {
            return Reject(intent, "PurchaseItem intent requires offerId.");
        }

        if (!StarterShopCatalog.TryGet(offerId, out var offer))
        {
            return Reject(intent, $"Unknown shop offer id: {offerId}.");
        }

        if (!CanReachNpc(intent.PlayerId, offer.VendorNpcId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        if (!StarterItems.TryGetById(offer.ItemId, out var item))
        {
            return Reject(intent, $"Shop offer points to unknown item id: {offer.ItemId}.");
        }

        var price = ShopPricing.CalculatePrice(offer, _state.Players[intent.PlayerId], _state.GetLeaderboardStanding());
        if (!_state.PurchaseItem(intent.PlayerId, item, price))
        {
            return Reject(intent, $"Not enough scrip for offer: {offer.Id}.");
        }

        var serverEvent = AppendEvent(
            "item_purchased",
            $"{intent.PlayerId} purchased {item.Id} for {price} scrip",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["vendorNpcId"] = offer.VendorNpcId,
                ["offerId"] = offer.Id,
                ["itemId"] = item.Id,
                ["basePrice"] = offer.Price.ToString(),
                ["price"] = price.ToString(),
                ["currency"] = offer.Currency
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessTransferItem(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("targetId", out var targetId) ||
            !intent.Payload.TryGetValue("itemId", out var itemId))
        {
            return Reject(intent, "TransferItem intent requires targetId and itemId.");
        }

        var mode = intent.Payload.TryGetValue("mode", out var payloadMode)
            ? payloadMode
            : "gift";
        if (mode != "gift" && mode != "steal")
        {
            return Reject(intent, $"Unknown transfer mode: {mode}.");
        }

        if (!CanReachPlayer(intent.PlayerId, targetId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        if (!StarterItems.TryGetById(itemId, out var item))
        {
            return Reject(intent, $"Unknown item id: {itemId}.");
        }

        var fromPlayerId = mode == "steal" ? targetId : intent.PlayerId;
        var toPlayerId = mode == "steal" ? intent.PlayerId : targetId;
        if (!_state.TransferItem(fromPlayerId, toPlayerId, itemId))
        {
            return Reject(intent, $"Transfer failed for item: {item.Name}.");
        }

        var karmaAction = mode == "steal"
            ? new KarmaAction(
                intent.PlayerId,
                targetId,
                new[] { "harmful", "selfish", "deceptive" },
                $"{_state.Players[intent.PlayerId].DisplayName} stole {item.Name} from {_state.Players[targetId].DisplayName}.")
            : new KarmaAction(
                intent.PlayerId,
                targetId,
                new[] { "helpful", "generous" },
                $"{_state.Players[intent.PlayerId].DisplayName} gave {item.Name} to {_state.Players[targetId].DisplayName}.");
        var shift = _state.ApplyShift(intent.PlayerId, karmaAction);
        var serverEvent = AppendEvent(
            "item_transferred",
            $"{intent.PlayerId} {mode} transferred {item.Id} with {targetId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["targetId"] = targetId,
                ["fromPlayerId"] = fromPlayerId,
                ["toPlayerId"] = toPlayerId,
                ["itemId"] = item.Id,
                ["mode"] = mode,
                ["karmaAmount"] = shift.Amount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessTransferCurrency(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("targetId", out var targetId) ||
            !TryReadInt(intent.Payload, "amount", out var amount))
        {
            return Reject(intent, "TransferCurrency intent requires targetId and integer amount.");
        }

        if (amount <= 0)
        {
            return Reject(intent, "TransferCurrency amount must be positive.");
        }

        if (!CanReachPlayer(intent.PlayerId, targetId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        if (!_state.TransferScrip(intent.PlayerId, targetId, amount))
        {
            return Reject(intent, $"Transfer failed for {amount} scrip.");
        }

        var karmaAction = new KarmaAction(
            intent.PlayerId,
            targetId,
            new[] { "helpful", "generous" },
            $"{_state.Players[intent.PlayerId].DisplayName} gave {amount} scrip to {_state.Players[targetId].DisplayName}.");
        var shift = _state.ApplyShift(intent.PlayerId, karmaAction);
        var serverEvent = AppendEvent(
            "currency_transferred",
            $"{intent.PlayerId} transferred {amount} scrip to {targetId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["targetId"] = targetId,
                ["amount"] = amount.ToString(),
                ["currency"] = "scrip",
                ["karmaAmount"] = shift.Amount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessPlaceObject(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("itemId", out var itemId))
        {
            return Reject(intent, "PlaceObject intent requires itemId.");
        }

        if (!TryReadInt(intent.Payload, "x", out var x) || !TryReadInt(intent.Payload, "y", out var y))
        {
            return Reject(intent, "PlaceObject intent requires integer x and y payload values.");
        }

        if (!StarterItems.TryGetById(itemId, out var item))
        {
            return Reject(intent, $"Unknown item id: {itemId}.");
        }

        if (!_state.Players.TryGetValue(intent.PlayerId, out var player))
        {
            return Reject(intent, $"Unknown player: {intent.PlayerId}.");
        }

        var position = new TilePosition(x, y);
        var rangeSquared = Config.CombatRangeTiles * Config.CombatRangeTiles;
        if (player.Position.DistanceSquaredTo(position) > rangeSquared)
        {
            return Reject(intent, $"PlaceObject target is out of range: {x},{y}.");
        }

        if (!_state.ConsumeItem(intent.PlayerId, itemId))
        {
            return Reject(intent, $"Player cannot place missing item: {item.Name}.");
        }

        var entityId = $"placed_{intent.PlayerId}_{_tick}_{item.Id}";
        SeedWorldItem(entityId, item, position);
        var serverEvent = AppendEvent(
            "item_placed",
            $"{intent.PlayerId} placed {item.Id}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["entityId"] = entityId,
                ["itemId"] = item.Id,
                ["x"] = x.ToString(),
                ["y"] = y.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessKarmaBreak(ServerIntent intent)
    {
        var droppedItemCount = DropInventory(intent.PlayerId).Count;
        _state.TriggerKarmaBreak(intent.PlayerId);
        var serverEvent = AppendEvent(
            "karma_break",
            $"{intent.PlayerId} suffered a Karma Break",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["droppedItemCount"] = droppedItemCount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private IReadOnlyList<string> DropInventory(string playerId)
    {
        if (!_state.Players.TryGetValue(playerId, out var player))
        {
            return Array.Empty<string>();
        }

        var items = _state.DrainInventory(playerId);
        var droppedEntityIds = new List<string>();
        var index = 0;
        foreach (var item in items)
        {
            var offset = GetDropOffset(index);
            var position = new TilePosition(player.Position.X + offset.X, player.Position.Y + offset.Y);
            var entityId = $"drop_{playerId}_{_tick}_{index}_{item.Id}";
            SeedWorldItem(entityId, item, position);
            droppedEntityIds.Add(entityId);
            index++;
        }

        return droppedEntityIds;
    }

    private ServerProcessResult Reject(ServerIntent intent, string reason)
    {
        var serverEvent = AppendEvent(
            "intent_rejected",
            reason,
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["sequence"] = intent.Sequence.ToString(),
                ["intentType"] = intent.Type.ToString()
            });

        return ServerProcessResult.Rejected(serverEvent, reason);
    }

    private ServerEvent AppendEvent(
        string eventType,
        string description,
        IReadOnlyDictionary<string, string> data)
    {
        var serverEvent = new ServerEvent(
            $"{WorldId}:{_tick}:{eventType}",
            WorldId,
            _tick,
            description,
            data);
        _eventLog.Add(serverEvent);
        return serverEvent;
    }

    private static bool TryReadInt(
        IReadOnlyDictionary<string, string> payload,
        string key,
        out int value)
    {
        value = 0;
        return payload.TryGetValue(key, out var text) && int.TryParse(text, out value);
    }

    private static TilePosition GetDropOffset(int index)
    {
        return (index % 8) switch
        {
            0 => new TilePosition(0, 0),
            1 => new TilePosition(1, 0),
            2 => new TilePosition(-1, 0),
            3 => new TilePosition(0, 1),
            4 => new TilePosition(0, -1),
            5 => new TilePosition(1, 1),
            6 => new TilePosition(-1, 1),
            _ => new TilePosition(1, -1)
        };
    }

    private string GetDropOwnerId(string entityId)
    {
        return _connectedPlayerIds
            .OrderByDescending(playerId => playerId.Length)
            .FirstOrDefault(playerId => entityId.StartsWith($"drop_{playerId}_")) ?? string.Empty;
    }

    private static bool IsEventVisibleTo(ServerEvent serverEvent, IReadOnlySet<string> visiblePlayerIds)
    {
        if (!serverEvent.Data.TryGetValue("playerId", out var playerId))
        {
            return true;
        }

        return visiblePlayerIds.Contains(playerId) ||
            (serverEvent.Data.TryGetValue("targetId", out var targetId) && visiblePlayerIds.Contains(targetId));
    }

    private static bool IsDuelVisibleTo(Duel duel, IReadOnlySet<string> visiblePlayerIds)
    {
        return visiblePlayerIds.Contains(duel.ChallengerId) ||
            visiblePlayerIds.Contains(duel.TargetId);
    }

    private static bool IsWorldEventVisibleTo(WorldEvent worldEvent, IReadOnlySet<string> visiblePlayerIds)
    {
        if (worldEvent.Type == WorldEventType.System)
        {
            return true;
        }

        return visiblePlayerIds.Contains(worldEvent.SourcePlayerId) ||
            visiblePlayerIds.Contains(worldEvent.TargetId);
    }

    private static WorldItemSnapshot ToSnapshot(WorldItemEntity entity)
    {
        return new WorldItemSnapshot(
            entity.EntityId,
            entity.Item.Id,
            entity.Item.Name,
            entity.Item.Category,
            entity.Position.X,
            entity.Position.Y);
    }

    private static ShopOfferSnapshot ToSnapshot(ShopOffer offer, PlayerState player, LeaderboardStanding standing)
    {
        var item = StarterItems.GetById(offer.ItemId);
        var price = ShopPricing.CalculatePrice(offer, player, standing);
        return new ShopOfferSnapshot(
            offer.Id,
            offer.VendorNpcId,
            item.Id,
            item.Name,
            item.Category,
            price,
            offer.Currency);
    }

    private static MapChunkSnapshot ToSnapshot(GeneratedTileChunk chunk)
    {
        return new MapChunkSnapshot(
            CreateChunkKey(chunk.Coordinate),
            CalculateChunkRevision(chunk),
            chunk.Coordinate.X,
            chunk.Coordinate.Y,
            chunk.Left,
            chunk.Top,
            chunk.Width,
            chunk.Height,
            chunk.Tiles
                .Select(tile => new MapTileSnapshot(
                    tile.X,
                    tile.Y,
                    tile.FloorId,
                    tile.StructureId,
                    tile.ZoneId))
                .ToArray());
    }

    private static string CreateChunkKey(GeneratedChunkCoordinate coordinate)
    {
        return $"{coordinate.X}:{coordinate.Y}";
    }

    private static int CalculateChunkRevision(GeneratedTileChunk chunk)
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + chunk.Coordinate.X;
            hash = (hash * 31) + chunk.Coordinate.Y;
            hash = (hash * 31) + chunk.Left;
            hash = (hash * 31) + chunk.Top;
            hash = (hash * 31) + chunk.Width;
            hash = (hash * 31) + chunk.Height;

            foreach (var tile in chunk.Tiles)
            {
                hash = (hash * 31) + tile.X;
                hash = (hash * 31) + tile.Y;
                hash = (hash * 31) + StableStringHash(tile.FloorId);
                hash = (hash * 31) + StableStringHash(tile.StructureId);
                hash = (hash * 31) + StableStringHash(tile.ZoneId);
            }

            return hash;
        }
    }

    private static int StableStringHash(string value)
    {
        unchecked
        {
            var hash = 23;
            foreach (var character in value ?? string.Empty)
            {
                hash = (hash * 31) + character;
            }

            return hash;
        }
    }

    private static NpcSnapshot ToSnapshot(NpcEntity entity)
    {
        return new NpcSnapshot(
            entity.Profile.Id,
            entity.Profile.Name,
            entity.Profile.Role,
            entity.Profile.Faction,
            entity.Position.X,
            entity.Position.Y);
    }

    private bool CanReachQuestGiver(string playerId, string questId, out string rejectionReason)
    {
        rejectionReason = string.Empty;
        if (!_state.Quests.Quests.TryGetValue(questId, out var quest))
        {
            rejectionReason = $"Unknown quest id: {questId}.";
            return false;
        }

        if (!_state.Players.TryGetValue(playerId, out var player) ||
            !_npcs.TryGetValue(quest.Definition.GiverNpcId, out var giver))
        {
            rejectionReason = $"Quest giver unavailable for quest: {questId}.";
            return false;
        }

        var radiusSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        if (player.Position.DistanceSquaredTo(giver.Position) > radiusSquared)
        {
            rejectionReason = $"Quest giver is out of range: {quest.Definition.GiverNpcId}.";
            return false;
        }

        return true;
    }

    private bool CanReachNpc(string playerId, string npcId, out string rejectionReason)
    {
        rejectionReason = string.Empty;
        if (!_state.Players.TryGetValue(playerId, out var player) ||
            !_npcs.TryGetValue(npcId, out var npc))
        {
            rejectionReason = $"NPC unavailable: {npcId}.";
            return false;
        }

        var radiusSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        if (player.Position.DistanceSquaredTo(npc.Position) > radiusSquared)
        {
            rejectionReason = $"NPC is out of range: {npcId}.";
            return false;
        }

        return true;
    }

    private bool CanReachPlayerActionTarget(string playerId, string targetId, out string rejectionReason)
    {
        rejectionReason = string.Empty;
        if (!_connectedPlayerIds.Contains(targetId))
        {
            return true;
        }

        if (targetId == playerId)
        {
            rejectionReason = "Players cannot target themselves with this action.";
            return false;
        }

        if (!_state.Players.TryGetValue(playerId, out var player) ||
            !_state.Players.TryGetValue(targetId, out var target))
        {
            rejectionReason = $"Player action target unavailable: {targetId}.";
            return false;
        }

        var rangeSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        if (player.Position.DistanceSquaredTo(target.Position) > rangeSquared)
        {
            rejectionReason = $"Player action target is out of range: {targetId}.";
            return false;
        }

        return true;
    }

    private bool CanReachPlayer(string playerId, string targetId, out string rejectionReason)
    {
        rejectionReason = string.Empty;
        if (targetId == playerId)
        {
            rejectionReason = "Players cannot target themselves with this action.";
            return false;
        }

        if (!_connectedPlayerIds.Contains(targetId) ||
            !_state.Players.TryGetValue(playerId, out var player) ||
            !_state.Players.TryGetValue(targetId, out var target))
        {
            rejectionReason = $"Player target unavailable: {targetId}.";
            return false;
        }

        var rangeSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        if (player.Position.DistanceSquaredTo(target.Position) > rangeSquared)
        {
            rejectionReason = $"Player target is out of range: {targetId}.";
            return false;
        }

        return true;
    }

    private static IReadOnlyList<NpcDialogueChoice> GetChoicesFor(string npcId)
    {
        if (npcId != StarterNpcs.Mara.Id)
        {
            return Array.Empty<NpcDialogueChoice>();
        }

        return new[]
        {
            new NpcDialogueChoice("help_filters", "Repair the filters", PrototypeActions.HelpMaraId),
            new NpcDialogueChoice("prank_stool", "Plant a whoopie cushion", PrototypeActions.WhoopieCushionMaraId, StarterItems.WhoopieCushionId),
            new NpcDialogueChoice("steal_parts", "Steal spare parts", PrototypeActions.StealFromMaraId),
            new NpcDialogueChoice("gift_balloon", "Offer a deflated balloon", PrototypeActions.GiftBalloonToMaraId, StarterItems.DeflatedBalloonId),
            new NpcDialogueChoice("mock_balloon", "Mock with a deflated balloon", PrototypeActions.MockMaraWithBalloonId, StarterItems.DeflatedBalloonId)
        };
    }

    private void SeedConnectedPlayers()
    {
        foreach (var playerId in _state.Players.Keys.OrderBy(id => id).Take(Config.MaxPlayers))
        {
            _connectedPlayerIds.Add(playerId);
        }
    }

    private void SeedStarterNpcs()
    {
        _npcs[StarterNpcs.Mara.Id] = new NpcEntity(StarterNpcs.Mara, new TilePosition(3, 4));
        _npcs[StarterNpcs.Dallen.Id] = new NpcEntity(StarterNpcs.Dallen, new TilePosition(6, 4));
    }
}

public sealed record ServerProcessResult(bool WasAccepted, ServerEvent Event, string RejectionReason)
{
    public static ServerProcessResult Accepted(ServerEvent serverEvent)
    {
        return new ServerProcessResult(true, serverEvent, string.Empty);
    }

    public static ServerProcessResult Rejected(ServerEvent serverEvent, string reason)
    {
        return new ServerProcessResult(false, serverEvent, reason);
    }
}

public sealed record ServerJoinResult(bool WasAccepted, string PlayerId, string Message)
{
    public static ServerJoinResult Accepted(string playerId, string message)
    {
        return new ServerJoinResult(true, playerId, message);
    }

    public static ServerJoinResult Rejected(string playerId, string message)
    {
        return new ServerJoinResult(false, playerId, message);
    }
}
