using System;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Core;
using Karma.Data;
using Karma.Quests;
using Karma.World;

namespace Karma.Net;

public sealed class AuthoritativeWorldServer
{
    private readonly GameState _state;
    private readonly Dictionary<string, int> _lastSequenceByPlayer = new();
    private readonly Dictionary<string, long> _lastAttackTickByPlayer = new();
    private readonly Dictionary<string, long> _karmaBreakGraceUntilTickByPlayer = new();
    private readonly Dictionary<string, string> _enteredStructureByPlayer = new();
    private readonly Dictionary<string, TilePosition> _entryPositionByPlayer = new();
    private readonly Dictionary<string, Queue<DropClaim>> _dropClaimsByHolderItem = new();
    private readonly Dictionary<string, TilePosition> _initialSpawnByPlayer = new();
    private readonly HashSet<string> _connectedPlayerIds = new();
    private readonly Dictionary<string, string> _pendingPosseInviteByInvitee = new();
    private readonly Dictionary<string, long> _downedUntilTickByPlayer = new();
    private readonly Dictionary<string, TilePosition> _downedDeathPositionByPlayer = new();
    private readonly Dictionary<string, WorldItemEntity> _worldItems = new();
    private readonly Dictionary<(int ChunkX, int ChunkY), int> _combatHeatByChunk = new();
    private readonly Dictionary<string, WorldStructureEntity> _worldStructures = new();
    private readonly Dictionary<string, NpcEntity> _npcs = new();
    private readonly Dictionary<string, MountEntity> _mounts = new();
    private readonly Dictionary<string, string> _mountedPlayerToMountId = new();
    private readonly List<ServerEvent> _eventLog = new();
    private readonly List<LocalChatMessage> _localChatLog = new();
    private readonly MatchState _match;
    private GeneratedTileMap _tileMap;
    private long _tick;
    private bool _matchRewardsPaid;
    private const long AttackCooldownTicks = 3;
    private const long KarmaBreakGraceTicks = 5;
    public const long DownedCountdownTicks = 120;
    public const int RescueHealAmount = 25;
    public const int ClinicReviveCost = 10;
    public const int ClinicReviveHealAmount = 50;
    public const int CombatHeatPerAttack = 100;
    public const int CombatHeatDecayPerTick = 1;
    public const int CombatHeatHotThreshold = 0;
    private const int MinimumInitialSpawnSeparationTiles = 10;
    private const int MinimumRespawnSeparationTiles = 12;
    private const int SpawnEdgePaddingTiles = 4;
    public const int LocalChatClearRadiusTiles = 4;
    public const int LocalChatMaxRadiusTiles = 18;
    public const int LocalChatMaxMessageLength = 180;
    public const long LocalChatRetainTicks = 240;
    public const int LocalChatMaxRetainedMessages = 80;
    private readonly Dictionary<string, int> _killsPerPlayer = new();
    private readonly Dictionary<string, string> _wantedPlayerToIssuerId = new();
    public const int WantedKarmaReward = 10;
    private readonly Dictionary<string, int> _bountyByPlayerId = new();
    public const int BountyKarmaThreshold = -50;
    private sealed record DropClaim(string OwnerId, string OwnerName);
    private sealed record LocalChatMessage(
        string MessageId,
        long Tick,
        string SpeakerId,
        string SpeakerName,
        string Text,
        TilePosition SpeakerPosition,
        string Channel = "local",
        bool SpeakerInsideStructure = false,
        string SpeakerPosseId = "");

    public AuthoritativeWorldServer(GameState state, string worldId, ServerConfig config = null)
    {
        _state = state;
        WorldId = worldId;
        Config = config ?? ServerConfig.Prototype4Player;
        Config.Validate();
        _match = new MatchState(Config.MatchDurationSeconds);
        SeedConnectedPlayers();
        SeedStarterNpcs();
        SeedStarterStructures();
        SeedStarterMounts();
    }

    public string WorldId { get; }
    public ServerConfig Config { get; }
    public long Tick => _tick;
    public IReadOnlyList<ServerEvent> EventLog => _eventLog;
    public IReadOnlyCollection<string> ConnectedPlayerIds => _connectedPlayerIds;
    public IReadOnlyDictionary<string, WorldItemEntity> WorldItems => _worldItems;
    public IReadOnlyDictionary<string, WorldStructureEntity> WorldStructures => _worldStructures;
    public IReadOnlyDictionary<string, NpcEntity> Npcs => _npcs;
    public IReadOnlyDictionary<string, MountEntity> Mounts => _mounts;
    public IReadOnlyList<LocalChatMessageSnapshot> LocalChatLog => _localChatLog
        .Select(message => ToLocalChatSnapshot(message, message.SpeakerPosition))
        .ToArray();
    public MatchSnapshot Match => _match.Snapshot(_state.GetLeaderboardStanding());

    public int GetBounty(string playerId) =>
        _bountyByPlayerId.TryGetValue(playerId, out var bounty) ? bounty : 0;

    public int GetChunkHeat(int chunkX, int chunkY) =>
        _combatHeatByChunk.TryGetValue((chunkX, chunkY), out var heat) ? heat : 0;

    public bool IsChunkHot(int chunkX, int chunkY) =>
        GetChunkHeat(chunkX, chunkY) > CombatHeatHotThreshold;

    public (int ChunkX, int ChunkY) GetChunkForTile(TilePosition position) =>
        (position.X / Config.ChunkSizeTiles, position.Y / Config.ChunkSizeTiles);

    public void SetTileMap(GeneratedTileMap tileMap)
    {
        _tileMap = tileMap;
        AssignConnectedInitialSpawns();
    }

    public void AdvanceIdleTicks(long ticks)
    {
        if (ticks <= 0)
        {
            return;
        }

        _tick += ticks;
        PruneLocalChatLog();
        DecayHeatMap(ticks);
        FinalizeExpiredDownedPlayers();
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
                    ["saintWinnerName"] = snapshot.SaintWinnerName,
                    ["saintWinnerScore"] = snapshot.SaintWinnerScore.ToString(),
                    ["saintScripReward"] = GetWinnerReward(snapshot.SaintWinnerId).ToString(),
                    ["scourgeWinnerId"] = snapshot.ScourgeWinnerId,
                    ["scourgeWinnerName"] = snapshot.ScourgeWinnerName,
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

    private MatchSummarySnapshot BuildMatchSummary()
    {
        var standing = _state.GetLeaderboardStanding();
        var questsCompleted = _state.Quests.Quests.Values.Count(q => q.Status == QuestStatus.Completed);
        var players = _connectedPlayerIds
            .Where(id => _state.Players.ContainsKey(id))
            .OrderBy(id => id)
            .Select(id =>
            {
                var player = _state.Players[id];
                return new PlayerMatchSummary(
                    id,
                    player.DisplayName,
                    player.Karma.Score,
                    player.Karma.TierName,
                    player.Karma.KarmaPeak,
                    player.Karma.KarmaFloor,
                    questsCompleted,
                    _killsPerPlayer.GetValueOrDefault(id));
            })
            .ToArray();
        return new MatchSummarySnapshot(SnapshotBuilder.LeaderboardFrom(standing), players);
    }

    public void SeedWorldItem(
        string entityId,
        GameItem item,
        TilePosition position,
        string dropOwnerId = "",
        string dropOwnerName = "")
    {
        _worldItems[entityId] = new WorldItemEntity(
            entityId,
            item,
            position,
            IsAvailable: true,
            dropOwnerId,
            dropOwnerName);
    }

    public void SeedGeneratedWorldContent(GeneratedWorld generatedWorld)
    {
        foreach (var location in generatedWorld.Locations.OrderBy(location => location.Id))
        {
            var entityId = $"generated_station_{SanitizeEntityId(location.Id)}";
            if (_worldStructures.ContainsKey(entityId))
            {
                continue;
            }

            SeedStationMarker(entityId, location);
        }

        foreach (var quest in generatedWorld.Quests.OrderBy(quest => quest.Id))
        {
            _state.Quests.AddDefinition(quest);
        }

        var locationRoleById = generatedWorld.Locations.ToDictionary(loc => loc.Id, loc => loc.Role);
        foreach (var placement in generatedWorld.StructurePlacements.OrderBy(placement => placement.StructureId))
        {
            var entityId = placement.StructureId;
            if (_worldStructures.ContainsKey(entityId))
            {
                continue;
            }

            var stationRole = locationRoleById.TryGetValue(placement.LocationId, out var role) ? role : "generated-structure";
            SeedGeneratedStructure(placement, stationRole);
        }

        var npcsById = generatedWorld.Npcs.ToDictionary(npc => npc.Id);
        foreach (var placement in generatedWorld.NpcPlacements.OrderBy(placement => placement.NpcId))
        {
            if (!npcsById.TryGetValue(placement.NpcId, out var profile) || _npcs.ContainsKey(profile.Id))
            {
                continue;
            }

            _npcs[profile.Id] = new NpcEntity(profile, new TilePosition(placement.X, placement.Y), placement.LocationId);
        }

        var odditiesById = generatedWorld.Oddities.ToDictionary(item => item.Id);
        for (var i = 0; i < generatedWorld.OddityPlacements.Count; i++)
        {
            var placement = generatedWorld.OddityPlacements[i];
            if (!odditiesById.TryGetValue(placement.ItemId, out var item))
            {
                continue;
            }

            var entityId = $"generated_oddity_{i}_{SanitizeEntityId(placement.ItemId)}";
            if (_worldItems.ContainsKey(entityId))
            {
                continue;
            }

            SeedWorldItem(entityId, item, new TilePosition(placement.X, placement.Y));
        }
    }

    private void SeedStationMarker(string entityId, GeneratedLocation location)
    {
        var markerDefinition = StructureArtCatalog.Get(StructureSpriteKind.GreenhouseSupportColumn);
        _worldStructures[entityId] = new WorldStructureEntity(
            entityId,
            markerDefinition.Id,
            location.Name,
            "station",
            new TilePosition(location.X, location.Y),
            IsVisible: true,
            IsInteractable: true,
            InteractionPrompt: FormatStationPrompt(location),
            InteractionResult: $"{location.Name} is a {location.ThemeTag} station for {location.Role}: {location.KarmaHook}. Future interior: {location.InteriorKind} ({location.InteriorId}). Faction interest: {location.SuggestedFaction}.",
            Integrity: 100,
            LocationId: location.Id);
    }

    private void SeedGeneratedStructure(GeneratedStructurePlacement placement, string category = "generated-structure")
    {
        var markerDefinition = StructureArtCatalog.Get(StructureSpriteKind.GreenhouseGlassPanel);
        _worldStructures[placement.StructureId] = new WorldStructureEntity(
            placement.StructureId,
            markerDefinition.Id,
            placement.Name,
            category,
            new TilePosition(placement.X, placement.Y),
            IsVisible: true,
            IsInteractable: true,
            InteractionPrompt: FormatStructurePrompt(placement.Name, placement.Integrity),
            InteractionResult: $"{placement.Name} is tied to {placement.SuggestedFaction}. Local pressure: {placement.GameplayHook}.",
            Integrity: placement.Integrity,
            FactionId: StarterFactions.ToId(placement.SuggestedFaction),
            LocationId: placement.LocationId);
    }

    public void SeedWorldStructure(string entityId, string structureId, TilePosition position)
    {
        var definition = StructureArtCatalog.GetById(structureId);
        _worldStructures[entityId] = new WorldStructureEntity(
            entityId,
            definition.Id,
            definition.DisplayName,
            definition.Category,
            position,
            IsVisible: true,
            IsInteractable: true,
            InteractionPrompt: FormatStructurePrompt(definition.DisplayName, 75),
            InteractionResult: $"{definition.DisplayName} climate controls hum with suspicious optimism.",
            Integrity: 75);
    }

    public TilePosition GetNpcPosition(string npcId)
    {
        return _npcs.TryGetValue(npcId, out var npc) ? npc.Position : TilePosition.Origin;
    }

    public void SeedWorldStructure(string entityId, string displayName, string category, TilePosition position, int integrity = 75)
    {
        var fallback = StructureArtCatalog.Get(StructureSpriteKind.GreenhouseGlassPanel);
        _worldStructures[entityId] = new WorldStructureEntity(
            entityId,
            fallback.Id,
            displayName,
            category,
            position,
            IsVisible: true,
            IsInteractable: true,
            InteractionPrompt: FormatStructurePrompt(displayName, integrity),
            InteractionResult: string.Empty,
            Integrity: integrity);
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
        var spawnPosition = AssignInitialSpawnPosition(playerId);
        _state.SetPlayerPosition(playerId, spawnPosition);
        _connectedPlayerIds.Add(playerId);
        AppendEvent(
            "player_joined",
            $"{displayName} joined {WorldId}",
            new Dictionary<string, string>
            {
                ["playerId"] = playerId,
                ["displayName"] = displayName,
                ["spawnX"] = spawnPosition.X.ToString(),
                ["spawnY"] = spawnPosition.Y.ToString(),
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

        if (_downedUntilTickByPlayer.ContainsKey(intent.PlayerId) && !IsDownedIntentAllowed(intent.Type))
        {
            var remaining = _downedUntilTickByPlayer[intent.PlayerId] - _tick;
            return Reject(intent, $"Player is downed ({remaining} ticks remaining); only chat is allowed.");
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
            IntentType.AdvanceQuestStep => ProcessAdvanceQuestStep(intent),
            IntentType.StartEntanglement => ProcessStartEntanglement(intent),
            IntentType.ExposeEntanglement => ProcessExposeEntanglement(intent),
            IntentType.SetAppearance => ProcessSetAppearance(intent),
            IntentType.SendLocalChat => ProcessSendLocalChat(intent),
            IntentType.KarmaAction => ProcessKarmaAction(intent),
            IntentType.KarmaBreak => ProcessKarmaBreak(intent),
            IntentType.InvitePosse => ProcessInvitePosse(intent),
            IntentType.AcceptPosse => ProcessAcceptPosse(intent),
            IntentType.LeavePosse => ProcessLeavePosse(intent),
            IntentType.SendPosseChat => ProcessSendPosseChat(intent),
            IntentType.Rescue => ProcessRescue(intent),
            IntentType.Mount => ProcessMount(intent),
            IntentType.Dismount => ProcessDismount(intent),
            IntentType.IssueWanted => ProcessIssueWanted(intent),
            _ => Reject(intent, $"Unsupported intent type: {intent.Type}")
        };
    }

    private static bool IsPostMatchIntentAllowed(IntentType type)
    {
        return type is IntentType.Move or IntentType.StartDialogue or IntentType.SetAppearance
            or IntentType.SendLocalChat or IntentType.SendPosseChat or IntentType.LeavePosse;
    }

    private static bool IsDownedIntentAllowed(IntentType type)
    {
        return type is IntentType.SendLocalChat or IntentType.SendPosseChat;
    }

    public PlayerInterest GetInterestFor(string playerId)
    {
        if (!_state.Players.TryGetValue(playerId, out var player))
        {
            return new PlayerInterest(playerId, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
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
        var visibleStructures = _worldStructures.Values
            .Where(entity => entity.IsVisible)
            .Where(entity => player.Position.DistanceSquaredTo(entity.Position) <= radiusSquared)
            .OrderBy(entity => entity.EntityId)
            .Select(entity => entity.EntityId)
            .ToArray();
        var visibleNpcs = _npcs.Values
            .Where(entity => player.Position.DistanceSquaredTo(entity.Position) <= radiusSquared)
            .OrderBy(entity => entity.Profile.Id)
            .Select(entity => entity.Profile.Id)
            .ToArray();

        return new PlayerInterest(playerId, visiblePlayers, visibleEntities, visibleStructures, visibleNpcs);
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
            .Select(quest => new QuestSnapshot(
                quest.Definition.Id,
                quest.Status,
                GetQuestScripRewardForCurrentStationState(quest.Definition),
                quest.CurrentStepIndex,
                quest.Definition.Steps?.Count ?? 0,
                quest.CurrentStep?.Description ?? ""))
            .ToArray();
        var visibleMapChunks = CreateVisibleMapChunks(playerId);
        var visibleWorldItems = _worldItems.Values
            .Where(entity => interest.VisibleEntityIds.Contains(entity.EntityId))
            .OrderBy(entity => entity.EntityId)
            .Select(ToSnapshot)
            .ToArray();
        var visibleStructures = _worldStructures.Values
            .Where(entity => interest.VisibleStructureIds.Contains(entity.EntityId))
            .OrderBy(entity => entity.EntityId)
            .Select(ToSnapshot)
            .ToArray();
        var visibleShopOffers = CreateVisibleShopOffers(playerId, interest.VisibleNpcIds, standing);
        var visibleLocalChat = CreateVisibleLocalChat(playerId);
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
        var mountRadiusSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        _state.Players.TryGetValue(playerId, out var mountViewer);
        var visibleMounts = _mounts.Count == 0 || mountViewer is null
            ? Array.Empty<MountSnapshot>()
            : _mounts.Values
                .Where(mount => mountViewer.Position.DistanceSquaredTo(mount.Position) <= mountRadiusSquared)
                .OrderBy(mount => mount.EntityId)
                .Select(ToMountSnapshot)
                .ToArray();

        return new ClientInterestSnapshot(
            WorldId,
            Tick,
            playerId,
            Config.InterestRadiusTiles,
            interest,
            SnapshotBuilder.PlayersFrom(visiblePlayers, standing, GetStatusEffectsFor),
            visibleNpcs,
            visibleDialogues,
            visibleQuests,
            visibleMapChunks,
            visibleWorldItems,
            visibleStructures,
            visibleShopOffers,
            visibleLocalChat,
            SnapshotBuilder.LeaderboardFrom(standing),
            _match.Snapshot(standing),
            visibleDuels,
            CreateSyncHint(afterTick, visibleMapChunks, events, worldEvents),
            events,
            worldEvents,
            visibleMounts,
            _match.Status == MatchStatus.Finished ? BuildMatchSummary() : null);
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

    private IReadOnlyList<LocalChatMessageSnapshot> CreateVisibleLocalChat(string playerId)
    {
        if (!_state.Players.TryGetValue(playerId, out var listener))
        {
            return Array.Empty<LocalChatMessageSnapshot>();
        }

        var listenerInsideStructure = _enteredStructureByPlayer.ContainsKey(playerId);
        var result = new List<LocalChatMessageSnapshot>();
        foreach (var message in _localChatLog.OrderBy(m => m.Tick))
        {
            if (message.Channel == "posse")
            {
                if (!string.IsNullOrEmpty(message.SpeakerPosseId) &&
                    listener.HasTeam && listener.TeamId == message.SpeakerPosseId)
                {
                    result.Add(ToLocalChatSnapshot(message, listener.Position, listenerInsideStructure));
                }
            }
            else
            {
                var crossInterior = message.SpeakerInsideStructure != listenerInsideStructure;
                var effectiveMaxRadius = crossInterior ? LocalChatMaxRadiusTiles / 2 : LocalChatMaxRadiusTiles;
                var maxDistanceSquared = effectiveMaxRadius * effectiveMaxRadius;
                if (listener.Position.DistanceSquaredTo(message.SpeakerPosition) <= maxDistanceSquared)
                {
                    result.Add(ToLocalChatSnapshot(message, listener.Position, listenerInsideStructure));
                }
            }
        }

        return result;
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

    private ServerProcessResult ProcessSetAppearance(ServerIntent intent)
    {
        if (!_state.Players.TryGetValue(intent.PlayerId, out var player))
        {
            return Reject(intent, $"Unknown player: {intent.PlayerId}.");
        }

        var selection = new PlayerAppearanceSelection(
            ReadPayloadOrDefault(intent.Payload, "baseLayerId", player.Appearance.BaseLayerId),
            ReadPayloadOrDefault(intent.Payload, "skinLayerId", player.Appearance.SkinLayerId),
            ReadPayloadOrDefault(intent.Payload, "hairLayerId", player.Appearance.HairLayerId),
            ReadPayloadOrDefault(intent.Payload, "outfitLayerId", player.Appearance.OutfitLayerId),
            ReadPayloadOrDefault(intent.Payload, "heldToolLayerId", player.Appearance.HeldToolLayerId));
        try
        {
            PlayerV2LayerManifest.LoadDefault().CreateAppearance(selection);
        }
        catch (Exception exception)
        {
            return Reject(intent, $"Invalid player appearance selection: {exception.Message}");
        }

        _state.SetPlayerAppearance(intent.PlayerId, selection);

        var updated = _state.Players[intent.PlayerId].Appearance;
        var serverEvent = AppendEvent(
            "player_appearance_changed",
            $"{player.DisplayName} changed appearance",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["baseLayerId"] = updated.BaseLayerId,
                ["skinLayerId"] = updated.SkinLayerId,
                ["hairLayerId"] = updated.HairLayerId,
                ["outfitLayerId"] = updated.OutfitLayerId,
                ["heldToolLayerId"] = updated.HeldToolLayerId
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessSendLocalChat(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("text", out var rawText))
        {
            return Reject(intent, "SendLocalChat intent requires text.");
        }

        if (!_state.Players.TryGetValue(intent.PlayerId, out var player))
        {
            return Reject(intent, $"Unknown player: {intent.PlayerId}.");
        }

        var text = SanitizeLocalChatText(rawText);
        if (string.IsNullOrWhiteSpace(text))
        {
            return Reject(intent, "Local chat message cannot be empty.");
        }

        var message = new LocalChatMessage(
            $"{WorldId}:{_tick}:local_chat:{intent.PlayerId}",
            _tick,
            player.Id,
            player.DisplayName,
            text,
            player.Position,
            SpeakerInsideStructure: _enteredStructureByPlayer.ContainsKey(intent.PlayerId));
        _localChatLog.Add(message);
        PruneLocalChatLog();

        var serverEvent = AppendEvent(
            "local_chat",
            $"{player.DisplayName}: {text}",
            new Dictionary<string, string>
            {
                ["playerId"] = player.Id,
                ["displayName"] = player.DisplayName,
                ["text"] = text,
                ["x"] = player.Position.X.ToString(),
                ["y"] = player.Position.Y.ToString(),
                ["clearRadiusTiles"] = LocalChatClearRadiusTiles.ToString(),
                ["maxRadiusTiles"] = LocalChatMaxRadiusTiles.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private void PruneLocalChatLog()
    {
        var oldestTickToKeep = Math.Max(0, _tick - LocalChatRetainTicks);
        _localChatLog.RemoveAll(message => message.Tick < oldestTickToKeep);
        if (_localChatLog.Count > LocalChatMaxRetainedMessages)
        {
            _localChatLog.RemoveRange(0, _localChatLog.Count - LocalChatMaxRetainedMessages);
        }
    }

    private void FinalizeExpiredDownedPlayers()
    {
        if (_downedUntilTickByPlayer.Count == 0)
        {
            return;
        }

        var expired = _downedUntilTickByPlayer
            .Where(pair => _tick >= pair.Value)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var playerId in expired)
        {
            _downedUntilTickByPlayer.Remove(playerId);
            _downedDeathPositionByPlayer.TryGetValue(playerId, out var deathPosition);
            _downedDeathPositionByPlayer.Remove(playerId);

            if (!_state.Players.TryGetValue(playerId, out var downedPlayer))
                continue;

            if (IsNearClinicNpc(downedPlayer.Position) && downedPlayer.SpendScrip(ClinicReviveCost))
            {
                downedPlayer.Rescue(ClinicReviveHealAmount);
                AppendEvent(
                    "clinic_revive",
                    $"{downedPlayer.DisplayName} was revived by the clinic.",
                    new Dictionary<string, string>
                    {
                        ["playerId"] = playerId,
                        ["healAmount"] = ClinicReviveHealAmount.ToString(),
                        ["scripCost"] = ClinicReviveCost.ToString()
                    });
            }
            else
            {
                var droppedItemCount = DropInventory(playerId).Count;
                _state.TriggerKarmaBreak(playerId);
                _wantedPlayerToIssuerId.Remove(playerId);
                RespawnPlayer(playerId, deathPosition);
                StartKarmaBreakGrace(playerId);
                AppendEvent(
                    "player_respawned",
                    $"{_state.Players[playerId].DisplayName} respawned after being downed.",
                    new Dictionary<string, string>
                    {
                        ["playerId"] = playerId,
                        ["droppedItemCount"] = droppedItemCount.ToString(),
                        ["respawnX"] = _state.Players[playerId].Position.X.ToString(),
                        ["respawnY"] = _state.Players[playerId].Position.Y.ToString()
                    });
            }
        }
    }

    private bool IsNearClinicNpc(TilePosition position)
    {
        var radiusSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        return _npcs.Values.Any(npc =>
            npc.Profile.Role.Contains("clinic", StringComparison.OrdinalIgnoreCase) &&
            npc.Position.DistanceSquaredTo(position) <= radiusSquared);
    }

    private void AddCombatHeat(TilePosition position)
    {
        var key = GetChunkForTile(position);
        _combatHeatByChunk.TryGetValue(key, out var existing);
        _combatHeatByChunk[key] = existing + CombatHeatPerAttack;
    }

    private void DecayHeatMap(long ticks)
    {
        var decay = (int)(ticks * CombatHeatDecayPerTick);
        if (decay <= 0 || _combatHeatByChunk.Count == 0)
        {
            return;
        }

        var keys = _combatHeatByChunk.Keys.ToArray();
        foreach (var key in keys)
        {
            var remaining = _combatHeatByChunk[key] - decay;
            if (remaining <= 0)
                _combatHeatByChunk.Remove(key);
            else
                _combatHeatByChunk[key] = remaining;
        }
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

        var shift = ApplyShift(intent.PlayerId, action);
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

        if (!_state.Players.TryGetValue(intent.PlayerId, out var player))
        {
            return Reject(intent, $"Unknown player: {intent.PlayerId}.");
        }

        if (_worldStructures.TryGetValue(entityId, out var structure))
        {
            return ProcessStructureInteract(intent, player, structure);
        }

        if (!_worldItems.TryGetValue(entityId, out var entity))
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

        var dropOwnerId = entity.DropOwnerId;
        var dropOwnerName = string.IsNullOrWhiteSpace(entity.DropOwnerName)
            ? dropOwnerId
            : entity.DropOwnerName;
        var karmaAmount = 0;
        if (!string.IsNullOrWhiteSpace(dropOwnerId) && dropOwnerId != intent.PlayerId)
        {
            var shift = ApplyShift(
                intent.PlayerId,
                new KarmaAction(
                    intent.PlayerId,
                    dropOwnerId,
                    new[] { "harmful", "selfish" },
                    $"{_state.Players[intent.PlayerId].DisplayName} claimed {entity.Item.Name} from {dropOwnerName}'s Karma Break drop."));
            karmaAmount = shift.Amount;
        }

        _state.AddItem(intent.PlayerId, entity.Item);
        if (!string.IsNullOrWhiteSpace(dropOwnerId) && dropOwnerId != intent.PlayerId)
        {
            RememberDropClaim(intent.PlayerId, entity.Item.Id, dropOwnerId, dropOwnerName);
        }

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
                ["dropOwnerName"] = dropOwnerName,
                ["karmaAmount"] = karmaAmount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessStructureInteract(
        ServerIntent intent,
        PlayerState player,
        WorldStructureEntity structure)
    {
        if (!structure.IsVisible || !structure.IsInteractable)
        {
            return Reject(intent, $"Structure cannot be interacted with: {structure.EntityId}.");
        }

        var radiusSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        if (player.Position.DistanceSquaredTo(structure.Position) > radiusSquared)
        {
            return Reject(intent, $"Structure is out of range: {structure.EntityId}.");
        }

        var action = intent.Payload.TryGetValue("action", out var payloadAction)
            ? payloadAction
            : "inspect";
        if (action != "inspect" && action != "repair" && action != "sabotage" && action != "enter" && action != "exit")
        {
            return Reject(intent, $"Unknown structure interaction action: {action}.");
        }

        if (action == "enter" || action == "exit")
        {
            return ProcessStructureEntryInteraction(intent, player, structure, action);
        }

        if (structure.Category == "station" && action != "inspect")
        {
            return Reject(intent, $"Station markers can only be inspected: {structure.EntityId}.");
        }

        var result = structure.InteractionResult;
        var karmaAmount = 0;
        var scripReward = 0;
        var factionDelta = 0;
        var factionId = string.IsNullOrWhiteSpace(structure.FactionId)
            ? StarterFactions.CivicRepairGuildId
            : structure.FactionId;
        var factionReputation = _state.Factions.GetReputation(factionId, intent.PlayerId);
        var nextIntegrity = structure.Integrity;
        if (action == "repair")
        {
            if (!HasStructureRepairTool(intent.PlayerId))
            {
                return Reject(intent, "Repairing a structure requires a multi-tool or welding torch.");
            }

            nextIntegrity = Math.Clamp(structure.Integrity + 20, 0, 100);
            var repairAmount = nextIntegrity - structure.Integrity;
            var shift = ApplyShift(
                intent.PlayerId,
                new KarmaAction(
                    intent.PlayerId,
                    structure.EntityId,
                    new[] { "helpful", "protective" },
                    $"{player.DisplayName} repaired {structure.Name}."));
            karmaAmount = shift.Amount;
            if (repairAmount > 0)
            {
                scripReward = Math.Max(1, repairAmount / 5);
                factionDelta = 4;
                factionReputation = _state.ApplyFactionReputation(factionId, intent.PlayerId, factionDelta);
                _state.AddScrip(intent.PlayerId, scripReward);
            }

            result = nextIntegrity == structure.Integrity
                ? $"{structure.Name} is already fully repaired."
                : $"{structure.Name} integrity restored to {nextIntegrity}%. Repair bounty: {scripReward} scrip.";
        }
        else if (action == "sabotage")
        {
            nextIntegrity = Math.Clamp(structure.Integrity - 25, 0, 100);
            var shift = ApplyShift(
                intent.PlayerId,
                new KarmaAction(
                    intent.PlayerId,
                    structure.EntityId,
                    new[] { "harmful", "destructive", "deceptive" },
                    $"{player.DisplayName} sabotaged {structure.Name}."));
            karmaAmount = shift.Amount;
            if (nextIntegrity < structure.Integrity)
            {
                factionDelta = -6;
                factionReputation = _state.ApplyFactionReputation(factionId, intent.PlayerId, factionDelta);
            }

            result = nextIntegrity == structure.Integrity
                ? $"{structure.Name} is already wrecked."
                : $"{structure.Name} integrity dropped to {nextIntegrity}%.";
        }

        var previousIntegrity = structure.Integrity;
        structure = structure with
        {
            Integrity = nextIntegrity,
            InteractionPrompt = FormatStructurePrompt(structure.Name, nextIntegrity)
        };
        _worldStructures[structure.EntityId] = structure;
        ApplyGeneratedStationStateEffect(structure, action, previousIntegrity, nextIntegrity);

        _state.AddWorldEvent(
            WorldEventType.Structure,
            $"{player.DisplayName} {FormatStructureAction(action)} {structure.Name}: {result}",
            intent.PlayerId,
            structure.EntityId);

        var serverEvent = AppendEvent(
            "structure_interacted",
            $"{intent.PlayerId} {action} {structure.StructureId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["entityId"] = structure.EntityId,
                ["structureId"] = structure.StructureId,
                ["action"] = action,
                ["result"] = result,
                ["integrity"] = nextIntegrity.ToString(),
                ["condition"] = FormatStructureCondition(nextIntegrity),
                ["karmaAmount"] = karmaAmount.ToString(),
                ["scripReward"] = scripReward.ToString(),
                ["factionId"] = factionId,
                ["factionDelta"] = factionDelta.ToString(),
                ["factionReputation"] = factionReputation.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessStructureEntryInteraction(
        ServerIntent intent,
        PlayerState player,
        WorldStructureEntity structure,
        string action)
    {
        var entering = action == "enter";
        var result = string.Empty;
        if (entering)
        {
            if (_enteredStructureByPlayer.TryGetValue(intent.PlayerId, out var currentEntityId) &&
                _worldStructures.TryGetValue(currentEntityId, out var currentStructure))
            {
                return Reject(intent, $"Already inside {currentStructure.Name}; exit before entering another structure.");
            }

            _entryPositionByPlayer[intent.PlayerId] = player.Position;
            _enteredStructureByPlayer[intent.PlayerId] = structure.EntityId;
            result = $"Entered {structure.Name}. Interior placeholder active; real rooms will attach here later.";
        }
        else
        {
            if (!_enteredStructureByPlayer.TryGetValue(intent.PlayerId, out var currentEntityId))
            {
                return Reject(intent, "You are not inside a structure.");
            }

            if (currentEntityId != structure.EntityId)
            {
                return Reject(intent, $"Exit the current structure before using {structure.Name}.");
            }

            _enteredStructureByPlayer.Remove(intent.PlayerId);
            if (_entryPositionByPlayer.Remove(intent.PlayerId, out var entryPosition))
            {
                _state.SetPlayerPosition(intent.PlayerId, entryPosition);
            }

            result = $"Exited {structure.Name}.";
        }

        _state.AddWorldEvent(
            WorldEventType.Structure,
            $"{player.DisplayName} {(entering ? "entered" : "exited")} {structure.Name}.",
            intent.PlayerId,
            structure.EntityId);

        var factionId = string.IsNullOrWhiteSpace(structure.FactionId)
            ? StarterFactions.CivicRepairGuildId
            : structure.FactionId;
        var serverEvent = AppendEvent(
            "structure_interacted",
            $"{intent.PlayerId} {action} {structure.StructureId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["entityId"] = structure.EntityId,
                ["structureId"] = structure.StructureId,
                ["action"] = action,
                ["result"] = result,
                ["entryState"] = entering ? "inside" : "outside",
                ["insideStructureId"] = entering ? structure.EntityId : string.Empty,
                ["insideStructureName"] = entering ? structure.Name : string.Empty,
                ["integrity"] = structure.Integrity.ToString(),
                ["condition"] = FormatStructureCondition(structure.Integrity),
                ["karmaAmount"] = "0",
                ["scripReward"] = "0",
                ["factionId"] = factionId,
                ["factionDelta"] = "0",
                ["factionReputation"] = _state.Factions.GetReputation(factionId, intent.PlayerId).ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private void ApplyGeneratedStationStateEffect(
        WorldStructureEntity structure,
        string action,
        int previousIntegrity,
        int nextIntegrity)
    {
        if (string.IsNullOrWhiteSpace(structure.LocationId) ||
            previousIntegrity == nextIntegrity)
        {
            return;
        }

        var stationEntityId = $"generated_station_{SanitizeEntityId(structure.LocationId)}";
        if (!_worldStructures.TryGetValue(stationEntityId, out var station))
        {
            return;
        }

        var stationState = action == "repair"
            ? "stabilized"
            : action == "sabotage"
                ? "compromised"
                : string.Empty;
        if (string.IsNullOrWhiteSpace(stationState))
        {
            return;
        }

        station = station with
        {
            InteractionPrompt = FormatStationStatePrompt(station.InteractionPrompt, stationState),
            InteractionResult = $"{station.InteractionResult} Current station state: {stationState} by {structure.Name}."
        };
        _worldStructures[stationEntityId] = station;
    }

    private string GetStationStateForLocation(string locationId)
    {
        if (string.IsNullOrWhiteSpace(locationId) ||
            !_worldStructures.TryGetValue($"generated_station_{SanitizeEntityId(locationId)}", out var station))
        {
            return string.Empty;
        }

        return station.InteractionPrompt.Contains("Station state: stabilized", StringComparison.Ordinal)
            ? "stabilized"
            : station.InteractionPrompt.Contains("Station state: compromised", StringComparison.Ordinal)
                ? "compromised"
                : string.Empty;
    }

    private int GetQuestScripRewardForCurrentStationState(QuestDefinition quest)
    {
        var reward = quest.ScripReward;
        var locationId = GetGeneratedQuestLocationId(quest);
        var stationState = GetStationStateForLocation(locationId);
        return stationState switch
        {
            "stabilized" => reward + 2,
            "compromised" => Math.Max(0, reward - 2),
            _ => reward
        };
    }

    private static string GetGeneratedQuestLocationId(QuestDefinition quest)
    {
        const string prefix = "generated_station_help:";
        return quest.CompletionActionId.StartsWith(prefix, StringComparison.Ordinal)
            ? quest.CompletionActionId[prefix.Length..]
            : string.Empty;
    }

    private bool HasStructureRepairTool(string playerId)
    {
        return _state.HasItem(playerId, StarterItems.MultiToolId) ||
               _state.HasItem(playerId, StarterItems.WeldingTorchId);
    }

    private bool PlayerHasPerk(string playerId, string perkId)
    {
        return _state.Players.TryGetValue(playerId, out var player) &&
               PerkCatalog.GetForPlayer(player, _state.GetLeaderboardStanding()).Any(p => p.Id == perkId);
    }

    private static string FormatStructurePrompt(string structureName, int integrity)
    {
        return $"Press E to inspect {structureName}. J repair / K sabotage. Integrity: {integrity}% ({FormatStructureCondition(integrity)}).";
    }

    private static string FormatStationPrompt(GeneratedLocation location)
    {
        return $"Press E to inspect {location.Name} ({location.Role}). Interior hook: {location.InteriorKind}. Karma hook: {location.KarmaHook}";
    }

    private static string FormatStationStatePrompt(string prompt, string stationState)
    {
        const string marker = " Station state:";
        var basePrompt = prompt.Contains(marker, StringComparison.Ordinal)
            ? prompt[..prompt.IndexOf(marker, StringComparison.Ordinal)]
            : prompt;
        return $"{basePrompt} Station state: {stationState}.";
    }

    private static string FormatStructureCondition(int integrity)
    {
        return integrity switch
        {
            >= 90 => "pristine",
            >= 60 => "stable",
            >= 30 => "damaged",
            _ => "critical"
        };
    }

    private static string FormatStructureAction(string action)
    {
        return action switch
        {
            "repair" => "repaired",
            "sabotage" => "sabotaged",
            _ => "inspected"
        };
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
        if (_karmaBreakGraceUntilTickByPlayer.TryGetValue(targetId, out var graceUntilTick) &&
            _tick <= graceUntilTick)
        {
            var remainingTicks = graceUntilTick - _tick + 1;
            return Reject(intent, $"{target.DisplayName} is protected by Karma Break grace for {remainingTicks} more server tick(s).");
        }

        if (_lastAttackTickByPlayer.TryGetValue(intent.PlayerId, out var lastAttackTick) &&
            _tick < lastAttackTick + AttackCooldownTicks)
        {
            var remainingTicks = lastAttackTick + AttackCooldownTicks - _tick;
            return Reject(intent, $"Attack is on cooldown for {remainingTicks} more server tick(s).");
        }

        var shift = isDuelAttack
            ? new KarmaShift(0, KarmaDirection.Neutral, $"{attacker.DisplayName} struck {target.DisplayName} during an accepted duel.")
            : ApplyShift(
                intent.PlayerId,
                new KarmaAction(
                    intent.PlayerId,
                    targetId,
                    new[] { "violent", "harmful", "chaotic" },
                    $"{attacker.DisplayName} attacked {target.DisplayName} outside a duel."));
        var damage = 30 + attacker.AttackPower;
        var wentDown = _state.DamagePlayer(intent.PlayerId, targetId, damage, isDuelAttack ? "accepted duel strike" : "server-authorized attack");
        _lastAttackTickByPlayer[intent.PlayerId] = _tick;
        AddCombatHeat(attacker.Position);
        if (wentDown)
        {
            _downedUntilTickByPlayer[targetId] = _tick + DownedCountdownTicks;
            _downedDeathPositionByPlayer[targetId] = target.Position;
            _killsPerPlayer[intent.PlayerId] = _killsPerPlayer.GetValueOrDefault(intent.PlayerId) + 1;
            if (_bountyByPlayerId.Remove(targetId, out var bountyAmount) && bountyAmount > 0)
            {
                _state.AddScrip(intent.PlayerId, bountyAmount);
                AppendEvent("bounty_claimed",
                    $"{attacker.DisplayName} claimed a {bountyAmount} scrip bounty on {target.DisplayName}.",
                    new Dictionary<string, string>
                    {
                        ["playerId"] = intent.PlayerId,
                        ["targetId"] = targetId,
                        ["scripAmount"] = bountyAmount.ToString()
                    });
            }

            if (_wantedPlayerToIssuerId.Remove(targetId))
            {
                ApplyShift(intent.PlayerId, new KarmaAction(
                    intent.PlayerId,
                    targetId,
                    new[] { "helpful", "lawful", "protective" },
                    $"{attacker.DisplayName} brought down the Wanted player {target.DisplayName}."));
                AppendEvent("wanted_bounty_claimed",
                    $"{attacker.DisplayName} claimed the Wanted bounty on {target.DisplayName}.",
                    new Dictionary<string, string>
                    {
                        ["playerId"] = intent.PlayerId,
                        ["targetId"] = targetId,
                        ["karmaReward"] = WantedKarmaReward.ToString()
                    });
            }
        }

        var serverEvent = AppendEvent(
            wentDown ? "player_downed" : "player_attacked",
            wentDown
                ? $"{target.DisplayName} was downed by {attacker.DisplayName} ({DownedCountdownTicks} tick countdown)."
                : $"{intent.PlayerId} attacked {targetId} for {damage} raw damage",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["targetId"] = targetId,
                ["rawDamage"] = damage.ToString(),
                ["downed"] = wentDown.ToString(),
                ["duel"] = isDuelAttack.ToString(),
                ["karmaAmount"] = shift.Amount.ToString(),
                ["targetHealth"] = _state.Players[targetId].Health.ToString(),
                ["targetMaxHealth"] = _state.Players[targetId].MaxHealth.ToString(),
                ["downedUntilTick"] = wentDown ? _downedUntilTickByPlayer[targetId].ToString() : "0"
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessRescue(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("targetId", out var targetId))
        {
            return Reject(intent, "Rescue intent requires targetId.");
        }

        if (targetId == intent.PlayerId)
        {
            return Reject(intent, "Players cannot rescue themselves.");
        }

        if (!_connectedPlayerIds.Contains(targetId) ||
            !_state.Players.TryGetValue(intent.PlayerId, out var rescuer) ||
            !_state.Players.TryGetValue(targetId, out var target))
        {
            return Reject(intent, $"Unknown rescue target: {targetId}.");
        }

        if (!_downedUntilTickByPlayer.ContainsKey(targetId))
        {
            return Reject(intent, $"{target.DisplayName} is not downed.");
        }

        var radiusSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        if (rescuer.Position.DistanceSquaredTo(target.Position) > radiusSquared)
        {
            return Reject(intent, $"Rescue target is out of range: {targetId}.");
        }

        _downedUntilTickByPlayer.Remove(targetId);
        _downedDeathPositionByPlayer.Remove(targetId);
        target.Rescue(RescueHealAmount);

        var shift = ApplyShift(intent.PlayerId, new KarmaAction(
            intent.PlayerId,
            targetId,
            new[] { "heroic", "protective", "generous" },
            $"{rescuer.DisplayName} revived {target.DisplayName} from a downed state."));

        var serverEvent = AppendEvent(
            "player_rescued",
            $"{rescuer.DisplayName} rescued {target.DisplayName}.",
            new Dictionary<string, string>
            {
                ["rescuerId"] = intent.PlayerId,
                ["targetId"] = targetId,
                ["healAmount"] = RescueHealAmount.ToString(),
                ["karmaAmount"] = shift.Amount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private KarmaShift ApplyShift(string playerId, KarmaAction action)
    {
        var before = _state.GetLeaderboardStanding();
        var shift = _state.ApplyShift(playerId, action);
        EmitTitleChangeEvents(before);
        UpdateBounty(playerId);
        return shift;
    }

    private void UpdateBounty(string playerId)
    {
        if (!_state.Players.TryGetValue(playerId, out var player))
            return;
        var karma = player.Karma.Score;
        var newBounty = karma < BountyKarmaThreshold ? -(karma - BountyKarmaThreshold) : 0;
        if (newBounty > 0)
            _bountyByPlayerId[playerId] = newBounty;
        else
            _bountyByPlayerId.Remove(playerId);
    }

    private void EmitTitleChangeEvents(LeaderboardStanding before)
    {
        var after = _state.GetLeaderboardStanding();
        if (before.SaintPlayerId != after.SaintPlayerId)
        {
            var newName = string.IsNullOrWhiteSpace(after.SaintPlayerId) ? "none" : after.SaintName;
            var oldName = string.IsNullOrWhiteSpace(before.SaintPlayerId) ? "none" : before.SaintName;
            AppendEvent("saint_title_changed",
                string.IsNullOrWhiteSpace(after.SaintPlayerId)
                    ? $"{before.SaintName} is no longer Saint."
                    : $"{after.SaintName} is now Saint.",
                new Dictionary<string, string>
                {
                    ["newHolderId"] = after.SaintPlayerId,
                    ["newHolderName"] = newName,
                    ["previousHolderId"] = before.SaintPlayerId,
                    ["previousHolderName"] = oldName,
                    ["score"] = after.ParagonScore.ToString()
                });
        }

        if (before.ScourgePlayerId != after.ScourgePlayerId)
        {
            var newName = string.IsNullOrWhiteSpace(after.ScourgePlayerId) ? "none" : after.ScourgeName;
            var oldName = string.IsNullOrWhiteSpace(before.ScourgePlayerId) ? "none" : before.ScourgeName;
            AppendEvent("scourge_title_changed",
                string.IsNullOrWhiteSpace(after.ScourgePlayerId)
                    ? $"{before.ScourgeName} is no longer Scourge."
                    : $"{after.ScourgeName} is now Scourge.",
                new Dictionary<string, string>
                {
                    ["newHolderId"] = after.ScourgePlayerId,
                    ["newHolderName"] = newName,
                    ["previousHolderId"] = before.ScourgePlayerId,
                    ["previousHolderName"] = oldName,
                    ["score"] = after.RenegadeScore.ToString()
                });
        }
    }

    private ServerProcessResult ProcessMount(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("mountId", out var mountId))
            return Reject(intent, "Mount intent requires mountId.");

        if (!_state.Players.TryGetValue(intent.PlayerId, out var player))
            return Reject(intent, "Unknown player.");

        if (!_mounts.TryGetValue(mountId, out var mount))
            return Reject(intent, $"Unknown mount: {mountId}.");

        if (!string.IsNullOrWhiteSpace(mount.OccupantPlayerId))
            return Reject(intent, $"{mount.Name} is already occupied.");

        if (_mountedPlayerToMountId.ContainsKey(intent.PlayerId))
            return Reject(intent, "Player is already mounted.");

        var radiusSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
        if (player.Position.DistanceSquaredTo(mount.Position) > radiusSquared)
            return Reject(intent, $"{mount.Name} is out of range.");

        _mounts[mountId] = mount with
        {
            IsParked = false,
            OccupantPlayerId = intent.PlayerId,
            Position = player.Position
        };
        _mountedPlayerToMountId[intent.PlayerId] = mountId;

        var shift = ApplyShift(intent.PlayerId, new KarmaAction(
            intent.PlayerId,
            intent.PlayerId,
            new[] { "helpful", "mobile" },
            $"{player.DisplayName} mounted {mount.Name}."));

        var serverEvent = AppendEvent(
            "player_mounted",
            $"{player.DisplayName} mounted {mount.Name}.",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["playerName"] = player.DisplayName,
                ["mountId"] = mountId,
                ["mountName"] = mount.Name,
                ["speedModifier"] = mount.SpeedModifier.ToString("F1"),
                ["karmaAmount"] = shift.Amount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessDismount(ServerIntent intent)
    {
        if (!_state.Players.TryGetValue(intent.PlayerId, out var player))
            return Reject(intent, "Unknown player.");

        if (!_mountedPlayerToMountId.TryGetValue(intent.PlayerId, out var mountId))
            return Reject(intent, "Player is not currently mounted.");

        if (!_mounts.TryGetValue(mountId, out var mount))
            return Reject(intent, $"Mount entity missing: {mountId}.");

        _mounts[mountId] = mount with
        {
            IsParked = true,
            OccupantPlayerId = string.Empty,
            Position = player.Position
        };
        _mountedPlayerToMountId.Remove(intent.PlayerId);

        var nearStation = _worldStructures.Values.Any(s =>
            !string.IsNullOrWhiteSpace(s.LocationId) &&
            player.Position.DistanceSquaredTo(s.Position) <= Config.InterestRadiusTiles * Config.InterestRadiusTiles);

        var shift = nearStation
            ? ApplyShift(intent.PlayerId, new KarmaAction(
                intent.PlayerId,
                intent.PlayerId,
                new[] { "helpful", "civic" },
                $"{player.DisplayName} parked {mount.Name} near a station."))
            : ApplyShift(intent.PlayerId, new KarmaAction(
                intent.PlayerId,
                intent.PlayerId,
                new[] { "neutral" },
                $"{player.DisplayName} dismounted {mount.Name}."));

        var serverEvent = AppendEvent(
            "player_dismounted",
            $"{player.DisplayName} dismounted {mount.Name}.",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["playerName"] = player.DisplayName,
                ["mountId"] = mountId,
                ["mountName"] = mount.Name,
                ["nearStation"] = nearStation.ToString(),
                ["karmaAmount"] = shift.Amount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessIssueWanted(ServerIntent intent)
    {
        if (!_state.Players.TryGetValue(intent.PlayerId, out var issuer))
            return Reject(intent, "Unknown player.");

        if (!intent.Payload.TryGetValue("targetId", out var targetId))
            return Reject(intent, "IssueWanted requires targetId.");

        if (targetId == intent.PlayerId)
            return Reject(intent, "Cannot issue a Wanted warrant on yourself.");

        if (!_connectedPlayerIds.Contains(targetId) || !_state.Players.TryGetValue(targetId, out var target))
            return Reject(intent, $"Target player not available: {targetId}.");

        var standing = _state.GetLeaderboardStanding();
        var perks = PerkCatalog.GetForPlayer(issuer, standing);
        if (!perks.Any(p => p.Id == PerkCatalog.WardenId))
            return Reject(intent, "IssueWanted requires the Warden perk (karma ≥ 150).");

        if (_wantedPlayerToIssuerId.ContainsKey(targetId))
            return Reject(intent, $"{target.DisplayName} is already Wanted.");

        _wantedPlayerToIssuerId[targetId] = intent.PlayerId;
        var serverEvent = AppendEvent(
            "player_wanted",
            $"{issuer.DisplayName} issued a Wanted warrant on {target.DisplayName}.",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["targetId"] = targetId,
                ["issuerName"] = issuer.DisplayName,
                ["targetName"] = target.DisplayName
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

        var stationState = GetStationStateForLocation(npc.LocationId);
        var standing = _state.GetLeaderboardStanding();
        var role = standing.GetRole(playerId, player.Karma.Score);
        return new NpcDialogueSnapshot(
            npc.Profile.Id,
            npc.Profile.Name,
            FormatNpcPrompt(npc.Profile, stationState, role),
            GetChoicesFor(npc.Profile, stationState, role));
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

        if (!TryResolveDialogueAction(intent.PlayerId, npcId, choice.ActionId, out var action))
        {
            return Reject(intent, $"Dialogue choice action is unknown: {choice.ActionId}.");
        }

        var shift = ApplyShift(intent.PlayerId, action);

        var paragonGift = 0;
        if (PlayerHasPerk(intent.PlayerId, PerkCatalog.ParagonFavorId) &&
            action.Tags.Contains("helpful"))
        {
            paragonGift = 1;
            _state.AddScrip(intent.PlayerId, paragonGift);
        }

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
                ["targetId"] = action.TargetId,
                ["paragonGift"] = paragonGift.ToString()
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

        var questDef = _state.Quests.Get(questId)?.Definition;
        var questModule = questDef != null
            ? QuestModuleRegistry.GetForCompletion(questDef.CompletionActionId)
            : null;
        var overrideAction = questModule?.ResolveCompletion(intent.PlayerId, questDef, intent.Payload);

        if (!_state.CompleteQuest(intent.PlayerId, questId, overrideAction))
        {
            return Reject(intent, $"Quest cannot be completed: {questId}.");
        }

        var quest = _state.Quests.Get(questId);
        var adjustedScripReward = GetQuestScripRewardForCurrentStationState(quest.Definition);
        var stationStateBonus = adjustedScripReward - quest.Definition.ScripReward;
        if (stationStateBonus > 0)
        {
            _state.AddScrip(intent.PlayerId, stationStateBonus);
        }
        else if (stationStateBonus < 0)
        {
            _state.SpendScrip(intent.PlayerId, Math.Abs(stationStateBonus));
        }

        var paragonQuestBonus = 0;
        if (PlayerHasPerk(intent.PlayerId, PerkCatalog.ParagonFavorId))
        {
            paragonQuestBonus = Math.Max(1, adjustedScripReward / 5);
            _state.AddScrip(intent.PlayerId, paragonQuestBonus);
        }

        var extraEventData = questModule?.GetCompletionEventData(questDef, intent.Payload)
            ?? new Dictionary<string, string>();
        var eventData = new Dictionary<string, string>
        {
            ["playerId"] = intent.PlayerId,
            ["questId"] = questId,
            ["targetId"] = quest.Definition.GiverNpcId,
            ["scripReward"] = adjustedScripReward.ToString(),
            ["stationStateBonus"] = stationStateBonus.ToString(),
            ["paragonQuestBonus"] = paragonQuestBonus.ToString()
        };
        foreach (var kv in extraEventData)
            eventData[kv.Key] = kv.Value;

        var serverEvent = AppendEvent(
            "quest_completed",
            $"{intent.PlayerId} completed quest {questId}",
            eventData);

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessAdvanceQuestStep(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("questId", out var questId))
            return Reject(intent, "AdvanceQuestStep intent requires questId.");

        if (!_state.Quests.Quests.TryGetValue(questId, out var quest))
            return Reject(intent, $"Unknown quest: {questId}.");

        if (quest.Status != QuestStatus.Active)
            return Reject(intent, $"Quest is not active: {questId}.");

        if (!quest.IsMultiStep)
            return Reject(intent, $"Quest does not have steps: {questId}.");

        var step = quest.CurrentStep;
        if (step == null)
            return Reject(intent, $"Quest has no remaining steps: {questId}.");

        if (!IsStepConditionMet(intent.PlayerId, step.Condition, out var conditionRejection))
            return Reject(intent, conditionRejection);

        var stepId = step.Id;
        var previousStepIndex = quest.CurrentStepIndex;
        if (!_state.AdvanceQuestStep(intent.PlayerId, questId))
            return Reject(intent, $"Quest step could not be advanced: {questId}.");

        var serverEvent = AppendEvent(
            "quest_step_advanced",
            $"{intent.PlayerId} advanced step '{stepId}' in quest {questId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["questId"] = questId,
                ["stepId"] = stepId,
                ["stepIndex"] = previousStepIndex.ToString(),
                ["allStepsDone"] = quest.AllStepsDone.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private bool IsStepConditionMet(string playerId, QuestStepCondition condition, out string rejection)
    {
        rejection = null;
        switch (condition.Kind)
        {
            case QuestStepConditionKind.None:
                return true;

            case QuestStepConditionKind.HoldItem:
                if (!_state.HasItem(playerId, condition.TargetId))
                {
                    var item = StarterItems.GetById(condition.TargetId);
                    rejection = $"You need to be holding: {item.Name}.";
                    return false;
                }
                return true;

            case QuestStepConditionKind.HoldRepairTool:
                if (!HasStructureRepairTool(playerId))
                {
                    rejection = "You need a repair tool (multi-tool or welding torch).";
                    return false;
                }
                return true;

            case QuestStepConditionKind.NearNpc:
                if (!_npcs.TryGetValue(condition.TargetId, out var npcEntity))
                {
                    rejection = $"NPC not found: {condition.TargetId}.";
                    return false;
                }
                if (!_state.Players.TryGetValue(playerId, out var playerForNpc))
                {
                    rejection = "Player not found.";
                    return false;
                }
                if (playerForNpc.Position.DistanceSquaredTo(npcEntity.Position) > Config.InterestRadiusTiles * Config.InterestRadiusTiles)
                {
                    rejection = $"You need to be closer to {npcEntity.Profile.Name}.";
                    return false;
                }
                return true;

            case QuestStepConditionKind.NearStructureCategory:
                if (!_state.Players.TryGetValue(playerId, out var playerForStructure))
                {
                    rejection = "Player not found.";
                    return false;
                }
                var radiusSquared = Config.InterestRadiusTiles * Config.InterestRadiusTiles;
                var nearMatch = _worldStructures.Values.Any(s =>
                    s.Category == condition.TargetId &&
                    playerForStructure.Position.DistanceSquaredTo(s.Position) <= radiusSquared);
                if (!nearMatch)
                {
                    rejection = $"You need to be near a {condition.TargetId}.";
                    return false;
                }
                return true;

            default:
                rejection = $"Unknown step condition: {condition.Kind}.";
                return false;
        }
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

        if (item.Id == StarterItems.RepairKitId)
        {
            return ProcessRepairKitUse(intent, item);
        }

        if (item.Id == StarterItems.RationPackId)
        {
            return ProcessHealingConsumableUse(intent, item, healing: 10);
        }

        if (item.Id == StarterItems.MediPatchId)
        {
            return ProcessHealingConsumableUse(intent, item, healing: 18);
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

    private ServerProcessResult ProcessRepairKitUse(ServerIntent intent, GameItem item)
    {
        var targetId = intent.Payload.TryGetValue("targetId", out var payloadTargetId)
            ? payloadTargetId
            : intent.PlayerId;
        if (targetId != intent.PlayerId && !CanReachPlayer(intent.PlayerId, targetId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        if (!_state.HasItem(intent.PlayerId, item.Id))
        {
            return Reject(intent, $"Player does not have item: {item.Name}.");
        }

        const int repairKitHealing = 25;
        if (!_state.HealPlayer(intent.PlayerId, targetId, repairKitHealing, $"{item.Name} field repair"))
        {
            return Reject(intent, $"{item.Name} had no injured target to repair.");
        }

        _state.ConsumeItem(intent.PlayerId, item.Id);
        var serverEvent = AppendEvent(
            "item_used",
            $"{intent.PlayerId} used {item.Id} on {targetId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["targetId"] = targetId,
                ["itemId"] = item.Id,
                ["healing"] = repairKitHealing.ToString(),
                ["targetHealth"] = _state.Players[targetId].Health.ToString(),
                ["targetMaxHealth"] = _state.Players[targetId].MaxHealth.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessHealingConsumableUse(ServerIntent intent, GameItem item, int healing)
    {
        var targetId = intent.Payload.TryGetValue("targetId", out var payloadTargetId)
            ? payloadTargetId
            : intent.PlayerId;
        if (targetId != intent.PlayerId && !CanReachPlayer(intent.PlayerId, targetId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        if (!_state.HasItem(intent.PlayerId, item.Id))
        {
            return Reject(intent, $"Player does not have item: {item.Name}.");
        }

        if (!_state.HealPlayer(intent.PlayerId, targetId, healing, $"{item.Name} field use"))
        {
            return Reject(intent, $"{item.Name} had no injured target.");
        }

        _state.ConsumeItem(intent.PlayerId, item.Id);
        var serverEvent = AppendEvent(
            "item_used",
            $"{intent.PlayerId} used {item.Id} on {targetId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["targetId"] = targetId,
                ["itemId"] = item.Id,
                ["healing"] = healing.ToString(),
                ["targetHealth"] = _state.Players[targetId].Health.ToString(),
                ["targetMaxHealth"] = _state.Players[targetId].MaxHealth.ToString()
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

        var returnedOwner = new DropClaim(string.Empty, string.Empty);
        var returnedDrop = mode == "gift" && TryReturnDropClaim(fromPlayerId, toPlayerId, itemId, out returnedOwner);
        var karmaAction = returnedDrop
            ? new KarmaAction(
                intent.PlayerId,
                targetId,
                new[] { "helpful", "generous", "protective" },
                $"{_state.Players[intent.PlayerId].DisplayName} returned {item.Name} from {returnedOwner.OwnerName}'s Karma Break drop.")
            : mode == "steal"
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
        if (!returnedDrop)
        {
            MoveDropClaim(fromPlayerId, toPlayerId, itemId);
        }

        var shift = ApplyShift(intent.PlayerId, karmaAction);
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
                ["returnedDrop"] = returnedDrop.ToString(),
                ["dropOwnerId"] = returnedDrop ? returnedOwner.OwnerId : string.Empty,
                ["dropOwnerName"] = returnedDrop ? returnedOwner.OwnerName : string.Empty,
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

        var mode = intent.Payload.TryGetValue("mode", out var payloadMode)
            ? payloadMode
            : "gift";
        if (mode != "gift" && mode != "steal")
        {
            return Reject(intent, $"Unknown currency transfer mode: {mode}.");
        }

        if (amount <= 0)
        {
            return Reject(intent, "TransferCurrency amount must be positive.");
        }

        if (!CanReachPlayer(intent.PlayerId, targetId, out var rejectionReason))
        {
            return Reject(intent, rejectionReason);
        }

        var fromPlayerId = mode == "steal" ? targetId : intent.PlayerId;
        var toPlayerId = mode == "steal" ? intent.PlayerId : targetId;
        if (!_state.TransferScrip(fromPlayerId, toPlayerId, amount))
        {
            return Reject(intent, $"Transfer failed for {amount} scrip.");
        }

        var karmaAction = mode == "steal"
            ? new KarmaAction(
                intent.PlayerId,
                targetId,
                new[] { "harmful", "selfish", "deceptive" },
                $"{_state.Players[intent.PlayerId].DisplayName} stole {amount} scrip from {_state.Players[targetId].DisplayName}.")
            : new KarmaAction(
                intent.PlayerId,
                targetId,
                new[] { "helpful", "generous" },
                $"{_state.Players[intent.PlayerId].DisplayName} gave {amount} scrip to {_state.Players[targetId].DisplayName}.");
        var shift = ApplyShift(intent.PlayerId, karmaAction);
        var serverEvent = AppendEvent(
            "currency_transferred",
            $"{intent.PlayerId} {mode} transferred {amount} scrip with {targetId}",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["targetId"] = targetId,
                ["fromPlayerId"] = fromPlayerId,
                ["toPlayerId"] = toPlayerId,
                ["amount"] = amount.ToString(),
                ["currency"] = "scrip",
                ["mode"] = mode,
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
        _bountyByPlayerId.Remove(intent.PlayerId);
        RespawnPlayer(intent.PlayerId, _state.Players[intent.PlayerId].Position);
        StartKarmaBreakGrace(intent.PlayerId);
        var serverEvent = AppendEvent(
            "karma_break",
            $"{intent.PlayerId} suffered a Karma Break",
            new Dictionary<string, string>
            {
                ["playerId"] = intent.PlayerId,
                ["respawnX"] = _state.Players[intent.PlayerId].Position.X.ToString(),
                ["respawnY"] = _state.Players[intent.PlayerId].Position.Y.ToString(),
                ["droppedItemCount"] = droppedItemCount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private void StartKarmaBreakGrace(string playerId)
    {
        _karmaBreakGraceUntilTickByPlayer[playerId] = _tick + KarmaBreakGraceTicks;
    }

    private TilePosition RespawnPlayer(string playerId, params TilePosition[] dangerPositions)
    {
        var respawnPosition = GetContextAwareRespawnPosition(playerId, dangerPositions);
        _state.SetPlayerPosition(playerId, respawnPosition);
        return respawnPosition;
    }

    private IReadOnlyList<string> GetStatusEffectsFor(PlayerState player)
    {
        var statuses = new List<string>();
        if (_downedUntilTickByPlayer.TryGetValue(player.Id, out var downedUntilTick) &&
            _tick <= downedUntilTick)
        {
            statuses.Add($"Downed ({downedUntilTick - _tick})");
        }

        if (_karmaBreakGraceUntilTickByPlayer.TryGetValue(player.Id, out var graceUntilTick) &&
            _tick <= graceUntilTick)
        {
            statuses.Add($"Karma Break Grace ({graceUntilTick - _tick + 1})");
        }

        if (_lastAttackTickByPlayer.TryGetValue(player.Id, out var lastAttackTick) &&
            _tick < lastAttackTick + AttackCooldownTicks)
        {
            statuses.Add($"Attack Cooldown ({lastAttackTick + AttackCooldownTicks - _tick})");
        }

        if (_enteredStructureByPlayer.TryGetValue(player.Id, out var structureEntityId) &&
            _worldStructures.TryGetValue(structureEntityId, out var structure))
        {
            statuses.Add($"Inside: {structure.Name}");
        }

        if (_wantedPlayerToIssuerId.ContainsKey(player.Id))
        {
            statuses.Add("Wanted");
        }

        if (_bountyByPlayerId.TryGetValue(player.Id, out var bounty) && bounty > 0)
        {
            statuses.Add($"Bounty: {bounty}");
        }

        return statuses;
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
            SeedWorldItem(entityId, item, position, player.Id, player.DisplayName);
            droppedEntityIds.Add(entityId);
            index++;
        }

        return droppedEntityIds;
    }

    private ServerProcessResult ProcessInvitePosse(ServerIntent intent)
    {
        intent.Payload.TryGetValue("targetPlayerId", out var targetId);
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return Reject(intent, "InvitePosse requires targetPlayerId payload.");
        }

        if (targetId == intent.PlayerId)
        {
            return Reject(intent, "Cannot invite yourself to a posse.");
        }

        if (!_connectedPlayerIds.Contains(targetId))
        {
            return Reject(intent, $"Target player {targetId} is not connected.");
        }

        if (!_state.Players.TryGetValue(targetId, out var target))
        {
            return Reject(intent, $"Unknown target player: {targetId}.");
        }

        if (target.HasTeam)
        {
            return Reject(intent, $"Target player {targetId} is already in a posse.");
        }

        if (_pendingPosseInviteByInvitee.ContainsKey(targetId))
        {
            return Reject(intent, $"Target player {targetId} already has a pending posse invite.");
        }

        var inviter = _state.Players[intent.PlayerId];
        var posseId = inviter.HasTeam ? inviter.TeamId : $"posse_{intent.PlayerId}";
        if (!inviter.HasTeam)
        {
            _state.SetPlayerTeam(intent.PlayerId, posseId);
        }

        _pendingPosseInviteByInvitee[targetId] = posseId;

        var serverEvent = AppendEvent(
            "posse_invite_sent",
            $"{inviter.DisplayName} invited {target.DisplayName} to a posse.",
            new Dictionary<string, string>
            {
                ["posseId"] = posseId,
                ["inviterId"] = intent.PlayerId,
                ["targetId"] = targetId
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessAcceptPosse(ServerIntent intent)
    {
        if (!_pendingPosseInviteByInvitee.TryGetValue(intent.PlayerId, out var posseId))
        {
            return Reject(intent, "No pending posse invite to accept.");
        }

        if (!_state.Players.TryGetValue(intent.PlayerId, out var player))
        {
            return Reject(intent, $"Unknown player: {intent.PlayerId}.");
        }

        _pendingPosseInviteByInvitee.Remove(intent.PlayerId);
        _state.SetPlayerTeam(intent.PlayerId, posseId);

        var memberCount = _state.Players.Values.Count(p => p.HasTeam && p.TeamId == posseId);

        var serverEvent = AppendEvent(
            "posse_accepted",
            $"{player.DisplayName} joined posse.",
            new Dictionary<string, string>
            {
                ["posseId"] = posseId,
                ["playerId"] = intent.PlayerId,
                ["memberCount"] = memberCount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessLeavePosse(ServerIntent intent)
    {
        if (!_state.Players.TryGetValue(intent.PlayerId, out var player) || !player.HasTeam)
        {
            return Reject(intent, "Not currently in a posse.");
        }

        var posseId = player.TeamId;
        _state.ClearPlayerTeamStatus(intent.PlayerId);

        var remainingCount = _state.Players.Values.Count(p => p.HasTeam && p.TeamId == posseId);
        var eventType = remainingCount == 0 ? "posse_disbanded" : "posse_member_left";

        var serverEvent = AppendEvent(
            eventType,
            remainingCount == 0
                ? $"Posse dissolved after last member left."
                : $"{player.DisplayName} left the posse.",
            new Dictionary<string, string>
            {
                ["posseId"] = posseId,
                ["playerId"] = intent.PlayerId,
                ["remainingMembers"] = remainingCount.ToString()
            });

        return ServerProcessResult.Accepted(serverEvent);
    }

    private ServerProcessResult ProcessSendPosseChat(ServerIntent intent)
    {
        if (!intent.Payload.TryGetValue("text", out var rawText))
        {
            return Reject(intent, "SendPosseChat intent requires text.");
        }

        if (!_state.Players.TryGetValue(intent.PlayerId, out var player))
        {
            return Reject(intent, $"Unknown player: {intent.PlayerId}.");
        }

        if (!player.HasTeam)
        {
            return Reject(intent, "SendPosseChat requires being in a posse.");
        }

        var text = SanitizeLocalChatText(rawText);
        if (string.IsNullOrWhiteSpace(text))
        {
            return Reject(intent, "Posse chat message cannot be empty.");
        }

        var message = new LocalChatMessage(
            $"{WorldId}:{_tick}:posse_chat:{intent.PlayerId}",
            _tick,
            player.Id,
            player.DisplayName,
            text,
            player.Position,
            Channel: "posse",
            SpeakerInsideStructure: _enteredStructureByPlayer.ContainsKey(intent.PlayerId),
            SpeakerPosseId: player.TeamId);
        _localChatLog.Add(message);
        PruneLocalChatLog();

        var serverEvent = AppendEvent(
            "posse_chat",
            $"[Posse] {player.DisplayName}: {text}",
            new Dictionary<string, string>
            {
                ["posseId"] = player.TeamId,
                ["playerId"] = player.Id,
                ["displayName"] = player.DisplayName,
                ["text"] = text
            });

        return ServerProcessResult.Accepted(serverEvent);
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
                ["intentType"] = intent.Type.ToString(),
                ["reason"] = reason
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

    private static string ReadPayloadOrDefault(
        IReadOnlyDictionary<string, string> payload,
        string key,
        string fallback)
    {
        return payload.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static Dictionary<string, string> WithRespawnData(
        Dictionary<string, string> data,
        bool includeRespawn,
        TilePosition respawnPosition)
    {
        if (!includeRespawn)
        {
            return data;
        }

        data["respawnX"] = respawnPosition.X.ToString();
        data["respawnY"] = respawnPosition.Y.ToString();
        return data;
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

    private void AssignConnectedInitialSpawns()
    {
        foreach (var playerId in _connectedPlayerIds.OrderBy(id => id))
        {
            if (_initialSpawnByPlayer.ContainsKey(playerId) || !_state.Players.TryGetValue(playerId, out var player) || player.Position != TilePosition.Origin)
            {
                continue;
            }

            _state.SetPlayerPosition(playerId, AssignInitialSpawnPosition(playerId));
        }
    }

    private TilePosition AssignInitialSpawnPosition(string playerId)
    {
        if (_initialSpawnByPlayer.TryGetValue(playerId, out var existing))
        {
            return existing;
        }

        var random = new Random(HashCode.Combine(WorldId, playerId, Config.MaxPlayers));
        var existingSpawns = _initialSpawnByPlayer.Values.ToArray();
        var candidates = BuildSpawnCandidates(random, existingSpawns);
        var spawn = candidates
            .OrderByDescending(candidate => GetNearestDistanceSquared(candidate, existingSpawns))
            .ThenBy(_ => random.Next())
            .FirstOrDefault();

        if (spawn == default && existingSpawns.Length > 0)
        {
            spawn = GetFallbackSpawnPosition(_initialSpawnByPlayer.Count);
        }

        _initialSpawnByPlayer[playerId] = spawn;
        return spawn;
    }

    private IReadOnlyList<TilePosition> BuildSpawnCandidates(Random random, IReadOnlyCollection<TilePosition> existingSpawns)
    {
        var candidates = new List<TilePosition>();
        var minimumDistanceSquared = MinimumInitialSpawnSeparationTiles * MinimumInitialSpawnSeparationTiles;

        for (var attempt = 0; attempt < 96; attempt++)
        {
            var candidate = GetRandomSpawnCandidate(random);
            if (existingSpawns.All(existing => candidate.DistanceSquaredTo(existing) >= minimumDistanceSquared))
            {
                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            for (var attempt = 0; attempt < 32; attempt++)
            {
                candidates.Add(GetRandomSpawnCandidate(random));
            }
        }

        return candidates;
    }

    private TilePosition GetRandomSpawnCandidate(Random random)
    {
        var minX = SpawnEdgePaddingTiles;
        var minY = SpawnEdgePaddingTiles;
        var maxX = 63;
        var maxY = 63;

        if (_tileMap is not null)
        {
            maxX = Math.Max(minX, _tileMap.Width - SpawnEdgePaddingTiles - 1);
            maxY = Math.Max(minY, _tileMap.Height - SpawnEdgePaddingTiles - 1);
        }

        return new TilePosition(random.Next(minX, maxX + 1), random.Next(minY, maxY + 1));
    }

    private static string SanitizeEntityId(string value)
    {
        return string.Concat(value.Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '_')).Trim('_');
    }

    private static int GetNearestDistanceSquared(TilePosition candidate, IReadOnlyCollection<TilePosition> existingSpawns)
    {
        return existingSpawns.Count == 0
            ? int.MaxValue
            : existingSpawns.Min(existing => candidate.DistanceSquaredTo(existing));
    }

    private static TilePosition GetFallbackSpawnPosition(int index)
    {
        return index switch
        {
            0 => new TilePosition(8, 8),
            1 => new TilePosition(55, 8),
            2 => new TilePosition(8, 55),
            3 => new TilePosition(55, 55),
            _ => new TilePosition(8 + (index * 7 % 48), 8 + (index * 11 % 48))
        };
    }

    private TilePosition GetContextAwareRespawnPosition(string playerId, IReadOnlyCollection<TilePosition> dangerPositions)
    {
        var width = _tileMap?.Width ?? 64;
        var height = _tileMap?.Height ?? 64;
        var reserved = _state.Players.Values
            .Where(player => player.Id != playerId && _connectedPlayerIds.Contains(player.Id))
            .Select(player => player.Position)
            .Concat(dangerPositions)
            .ToArray();
        var minimumDistanceSquared = MinimumRespawnSeparationTiles * MinimumRespawnSeparationTiles;
        if (TryGetStabilizedStationRespawnCandidate(reserved, minimumDistanceSquared, out var stationRespawnPosition))
        {
            return stationRespawnPosition;
        }

        var random = new Random(HashCode.Combine(WorldId, playerId, _tick, "karma-break-respawn"));
        var candidates = ProceduralPlacementSampler.GenerateSeparatedPoints(
            random,
            width,
            height,
            count: 12,
            edgePadding: SpawnEdgePaddingTiles,
            candidateAttemptsPerPoint: 32,
            reserved);
        var safeCandidates = candidates
            .Where(candidate => reserved.All(reservedPoint => candidate.DistanceSquaredTo(reservedPoint) >= minimumDistanceSquared))
            .ToArray();

        var coolSafeCandidate = safeCandidates
            .Where(candidate => !IsChunkHot(GetChunkForTile(candidate).ChunkX, GetChunkForTile(candidate).ChunkY))
            .OrderByDescending(candidate => ProceduralPlacementSampler.GetNearestDistanceSquared(candidate, reserved))
            .FirstOrDefault();

        if (coolSafeCandidate != default)
        {
            return coolSafeCandidate;
        }

        var safeCandidate = safeCandidates
            .OrderByDescending(candidate => ProceduralPlacementSampler.GetNearestDistanceSquared(candidate, reserved))
            .FirstOrDefault();

        if (safeCandidate != default)
        {
            return safeCandidate;
        }

        return candidates
            .OrderByDescending(candidate => ProceduralPlacementSampler.GetNearestDistanceSquared(candidate, reserved))
            .FirstOrDefault();
    }

    private bool TryGetStabilizedStationRespawnCandidate(
        IReadOnlyCollection<TilePosition> reserved,
        int minimumDistanceSquared,
        out TilePosition respawnPosition)
    {
        var stabilizedStations = _worldStructures.Values
            .Where(structure => structure.Category == "station" &&
                                structure.InteractionPrompt.Contains("Station state: stabilized", StringComparison.Ordinal))
            .Where(structure => reserved.All(reservedPoint => structure.Position.DistanceSquaredTo(reservedPoint) >= minimumDistanceSquared))
            .Where(structure => !IsChunkHot(GetChunkForTile(structure.Position).ChunkX, GetChunkForTile(structure.Position).ChunkY))
            .OrderByDescending(structure => ProceduralPlacementSampler.GetNearestDistanceSquared(structure.Position, reserved))
            .ThenBy(structure => structure.EntityId)
            .ToArray();
        if (stabilizedStations.Length == 0)
        {
            respawnPosition = default;
            return false;
        }

        respawnPosition = stabilizedStations[0].Position;
        return true;
    }

    private void RememberDropClaim(string holderId, string itemId, string ownerId, string ownerName)
    {
        var key = CreateDropClaimKey(holderId, itemId);
        if (!_dropClaimsByHolderItem.TryGetValue(key, out var claims))
        {
            claims = new Queue<DropClaim>();
            _dropClaimsByHolderItem[key] = claims;
        }

        claims.Enqueue(new DropClaim(ownerId, ownerName));
    }

    private bool TryReturnDropClaim(string fromPlayerId, string toPlayerId, string itemId, out DropClaim returnedOwner)
    {
        returnedOwner = null;
        var fromKey = CreateDropClaimKey(fromPlayerId, itemId);
        if (!_dropClaimsByHolderItem.TryGetValue(fromKey, out var claims) || claims.Count == 0)
        {
            return false;
        }

        var claim = claims.Peek();
        if (claim.OwnerId != toPlayerId)
        {
            return false;
        }

        returnedOwner = claims.Dequeue();
        if (claims.Count == 0)
        {
            _dropClaimsByHolderItem.Remove(fromKey);
        }

        return true;
    }

    private void MoveDropClaim(string fromPlayerId, string toPlayerId, string itemId)
    {
        var fromKey = CreateDropClaimKey(fromPlayerId, itemId);
        if (!_dropClaimsByHolderItem.TryGetValue(fromKey, out var claims) || claims.Count == 0)
        {
            return;
        }

        var claim = claims.Dequeue();
        if (claims.Count == 0)
        {
            _dropClaimsByHolderItem.Remove(fromKey);
        }

        RememberDropClaim(toPlayerId, itemId, claim.OwnerId, claim.OwnerName);
    }

    private static string CreateDropClaimKey(string holderId, string itemId)
    {
        return $"{holderId}\u001f{itemId}";
    }

    private static bool IsEventVisibleTo(ServerEvent serverEvent, IReadOnlySet<string> visiblePlayerIds)
    {
        if (serverEvent.EventId.Contains("local_chat"))
        {
            return false;
        }

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
        if (worldEvent.Type == WorldEventType.System || worldEvent.IsGlobal)
        {
            return true;
        }

        return visiblePlayerIds.Contains(worldEvent.SourcePlayerId) ||
            visiblePlayerIds.Contains(worldEvent.TargetId);
    }

    public static float CalculateLocalChatVolume(int distanceTiles)
    {
        if (distanceTiles <= LocalChatClearRadiusTiles)
        {
            return 1f;
        }

        if (distanceTiles >= LocalChatMaxRadiusTiles)
        {
            return 0f;
        }

        var t = (distanceTiles - LocalChatClearRadiusTiles) / (float)(LocalChatMaxRadiusTiles - LocalChatClearRadiusTiles);
        var smooth = t * t * (3f - (2f * t));
        return Math.Clamp(1f - smooth, 0f, 1f);
    }

    private static string SanitizeLocalChatText(string rawText)
    {
        var text = string.Join(" ", (rawText ?? string.Empty)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return text.Length <= LocalChatMaxMessageLength
            ? text
            : text[..LocalChatMaxMessageLength];
    }

    private static LocalChatMessageSnapshot ToLocalChatSnapshot(
        LocalChatMessage message,
        TilePosition listenerPosition,
        bool listenerInsideStructure = false)
    {
        var distanceTiles = (int)MathF.Ceiling(MathF.Sqrt(listenerPosition.DistanceSquaredTo(message.SpeakerPosition)));
        var volume = CalculateLocalChatVolume(distanceTiles);
        if (message.SpeakerInsideStructure != listenerInsideStructure)
        {
            volume *= 0.5f;
        }

        return new LocalChatMessageSnapshot(
            message.MessageId,
            message.Tick,
            message.SpeakerId,
            message.SpeakerName,
            message.Text,
            message.SpeakerPosition.X,
            message.SpeakerPosition.Y,
            distanceTiles,
            volume,
            message.Channel);
    }

    private static WorldItemSnapshot ToSnapshot(WorldItemEntity entity)
    {
        return new WorldItemSnapshot(
            entity.EntityId,
            entity.Item.Id,
            entity.Item.Name,
            entity.Item.Category,
            entity.Position.X,
            entity.Position.Y,
            entity.DropOwnerId,
            entity.DropOwnerName);
    }

    private static WorldStructureSnapshot ToSnapshot(WorldStructureEntity entity)
    {
        var definition = StructureArtCatalog.GetById(entity.StructureId);
        return new WorldStructureSnapshot(
            entity.EntityId,
            entity.StructureId,
            entity.Name,
            entity.Category,
            entity.Position.X,
            entity.Position.Y,
            (int)definition.Size.X,
            (int)definition.Size.Y,
            entity.IsInteractable,
            entity.InteractionPrompt,
            entity.Integrity,
            FormatStructureCondition(entity.Integrity));
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

    private static bool TryResolveDialogueAction(string playerId, string npcId, string actionId, out KarmaAction action)
    {
        if (PrototypeActions.TryGet(actionId, out action))
        {
            return true;
        }

        action = actionId switch
        {
            "generated_assist_need" => new KarmaAction(
                playerId,
                npcId,
                new[] { "helpful", "generous", "lawful" },
                "You offered practical help with a generated station need."),
            "generated_spread_rumor" => new KarmaAction(
                playerId,
                npcId,
                new[] { "deceptive", "chaotic", "harmful" },
                "You turned a generated station need into public rumor fuel."),
            "generated_apply_pressure" => new KarmaAction(
                playerId,
                npcId,
                new[] { "selfish", "harmful", "intimidating" },
                "You pressured a generated station contact for advantage."),
            "saint_bless" => new KarmaAction(
                playerId,
                npcId,
                new[] { "helpful", "protective" },
                "You extended the Saint's blessing to an NPC in need."),
            "scourge_tribute" => new KarmaAction(
                playerId,
                npcId,
                new[] { "harmful", "selfish" },
                "The Scourge demanded tribute — the NPC complied out of fear."),
            _ => null
        };

        return action is not null;
    }

    private static string FormatNpcPrompt(NpcProfile npc, string stationState, LeaderboardRole role)
    {
        var greeting = role switch
        {
            LeaderboardRole.Saint => $"[Saint] {npc.Name} greets you with relief and warmth. ",
            LeaderboardRole.Scourge => $"[Scourge] {npc.Name} sizes you up warily. ",
            _ => string.Empty
        };
        var prompt = $"{greeting}{npc.Name} needs {npc.Need}.";
        return string.IsNullOrWhiteSpace(stationState)
            ? prompt
            : $"{prompt} Their station is currently {stationState}.";
    }

    private static IReadOnlyList<NpcDialogueChoice> GetChoicesFor(NpcProfile npc, string stationState, LeaderboardRole role = LeaderboardRole.None)
    {
        if (npc.Id != StarterNpcs.Mara.Id)
        {
            var assistLabel = stationState switch
            {
                "stabilized" => $"Build on the stabilized station: {npc.Need}",
                "compromised" => $"Emergency help for compromised station: {npc.Need}",
                _ => $"Help with {npc.Need}"
            };
            var pressureLabel = stationState == "compromised"
                ? "Exploit the compromised station for leverage"
                : "Apply pressure for leverage";
            var generated = new List<NpcDialogueChoice>
            {
                new("assist_need", assistLabel, "generated_assist_need"),
                new("spread_rumor", "Turn this need into a rumor", "generated_spread_rumor"),
                new("apply_pressure", pressureLabel, "generated_apply_pressure")
            };
            if (role == LeaderboardRole.Saint)
            {
                generated.Add(new NpcDialogueChoice("saint_bless", "Offer your community blessing", "saint_bless"));
            }
            else if (role == LeaderboardRole.Scourge)
            {
                generated.Add(new NpcDialogueChoice("scourge_tribute", "Demand tribute", "scourge_tribute"));
            }
            return generated;
        }

        var maraChoices = new List<NpcDialogueChoice>
        {
            new("help_filters", "Repair the filters", PrototypeActions.HelpMaraId),
            new("prank_stool", "Plant a whoopie cushion", PrototypeActions.WhoopieCushionMaraId, StarterItems.WhoopieCushionId),
            new("steal_parts", "Steal spare parts", PrototypeActions.StealFromMaraId),
            new("gift_balloon", "Offer a deflated balloon", PrototypeActions.GiftBalloonToMaraId, StarterItems.DeflatedBalloonId),
            new("mock_balloon", "Mock with a deflated balloon", PrototypeActions.MockMaraWithBalloonId, StarterItems.DeflatedBalloonId)
        };
        if (role == LeaderboardRole.Saint)
        {
            maraChoices.Add(new NpcDialogueChoice("saint_bless", "Offer your community blessing", "saint_bless"));
        }
        else if (role == LeaderboardRole.Scourge)
        {
            maraChoices.Add(new NpcDialogueChoice("scourge_tribute", "Demand tribute", "scourge_tribute"));
        }
        return maraChoices;
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

    private void SeedStarterMounts()
    {
        _mounts["mount_hover_1"] = new MountEntity(
            "mount_hover_1",
            "Hover Scooter",
            new TilePosition(12, 8),
            SpeedModifier: 2.0f,
            IsParked: true);
        _mounts["mount_cargo_1"] = new MountEntity(
            "mount_cargo_1",
            "Cargo Crawler",
            new TilePosition(15, 12),
            SpeedModifier: 1.5f,
            IsParked: true);
    }

    private static MountSnapshot ToMountSnapshot(MountEntity mount) =>
        new(mount.EntityId, mount.Name, mount.Position.X, mount.Position.Y,
            mount.SpeedModifier, mount.IsParked, mount.OccupantPlayerId);

    private void SeedStarterStructures()
    {
        SeedWorldStructure(
            "structure_greenhouse_standard",
            StructureArtCatalog.Get(StructureSpriteKind.GreenhouseStandard).Id,
            new TilePosition(8, 3));
        SeedWorldStructure(
            "structure_greenhouse_planter",
            StructureArtCatalog.Get(StructureSpriteKind.GreenhousePlanter).Id,
            new TilePosition(6, 7));
        SeedWorldStructure(
            "structure_greenhouse_grow_rack",
            StructureArtCatalog.Get(StructureSpriteKind.GreenhouseGrowRack).Id,
            new TilePosition(10, 7));
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
