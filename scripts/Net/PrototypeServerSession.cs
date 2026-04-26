using System.Collections.Generic;
using System.Linq;
using Godot;
using Karma.Core;
using Karma.Data;

namespace Karma.Net;

public partial class PrototypeServerSession : Node
{
    private readonly Dictionary<string, int> _nextSequenceByPlayer = new();
    private readonly InterestSnapshotCache _localSnapshotCache = new();
    private GameState _state = null!;
    private AuthoritativeWorldServer _server = null!;
    private double _matchSecondAccumulator;

    public AuthoritativeWorldServer Server => _server;
    public InterestSnapshotCache LocalSnapshotCache => _localSnapshotCache;
    public ClientInterestSnapshot LastLocalSnapshot { get; private set; }

    [Signal]
    public delegate void LocalSnapshotChangedEventHandler(string snapshotSummary);

    public override void _Ready()
    {
        _state = GetNode<GameState>("/root/GameState");
        _server = new AuthoritativeWorldServer(_state, "local-prototype", ServerConfig.Prototype4Player);
        RefreshLocalSnapshot();
    }

    public override void _Process(double delta)
    {
        if (_server is null || _server.Match.Status == MatchStatus.Finished)
        {
            return;
        }

        _matchSecondAccumulator += delta;
        var elapsedSeconds = (int)_matchSecondAccumulator;
        if (elapsedSeconds <= 0)
        {
            return;
        }

        _matchSecondAccumulator -= elapsedSeconds;
        AdvanceMatchTime(elapsedSeconds);
    }

    public void AdvanceMatchTime(int seconds)
    {
        _server.AdvanceMatchTime(seconds);
        RefreshLocalSnapshot();
    }

    public void RegisterWorldItem(string entityId, GameItem item, TilePosition position)
    {
        _server.SeedWorldItem(entityId, item, position);
        RefreshLocalSnapshot();
    }

    public void SetTileMap(Karma.World.GeneratedTileMap tileMap)
    {
        _server.SetTileMap(tileMap);
        RefreshLocalSnapshot();
    }

    public ServerProcessResult SendLocal(
        IntentType type,
        IReadOnlyDictionary<string, string> payload)
    {
        return Send(GameState.LocalPlayerId, type, payload);
    }

    public ServerProcessResult Send(
        string playerId,
        IntentType type,
        IReadOnlyDictionary<string, string> payload)
    {
        var sequence = NextSequence(playerId);
        var result = _server.ProcessIntent(new ServerIntent(playerId, sequence, type, payload));
        if (playerId == GameState.LocalPlayerId || result.WasAccepted)
        {
            RefreshLocalSnapshot();
        }

        return result;
    }

    public ClientInterestSnapshot CreateLocalSnapshot(long afterTick = 0)
    {
        return _server.CreateInterestSnapshot(GameState.LocalPlayerId, afterTick);
    }

    private int NextSequence(string playerId)
    {
        _nextSequenceByPlayer.TryGetValue(playerId, out var previous);
        var next = previous + 1;
        _nextSequenceByPlayer[playerId] = next;
        return next;
    }

    private void RefreshLocalSnapshot()
    {
        LastLocalSnapshot = CreateLocalSnapshot(_localSnapshotCache.LastAppliedTick);
        var applyResult = _localSnapshotCache.Apply(LastLocalSnapshot);
        EmitSignal(SignalName.LocalSnapshotChanged, FormatLocalSnapshot(LastLocalSnapshot, applyResult));
    }

    private static string FormatLocalSnapshot(ClientInterestSnapshot snapshot, InterestSnapshotApplyResult applyResult)
    {
        var dialogueText = snapshot.Dialogues.Count == 0
            ? "Dialogues: none"
            : "Dialogues: " + string.Join(", ", snapshot.Dialogues
                .Select(dialogue => $"{dialogue.NpcName} ({dialogue.Choices.Count} choices)"));
        var questText = snapshot.Quests.Count == 0
            ? "Quests: none"
            : "Quests: " + string.Join(", ", snapshot.Quests
                .Select(quest => $"{quest.Id}:{quest.Status}"));
        var eventText = snapshot.ServerEvents.Count == 0
            ? "Events: quiet"
            : $"Events: {snapshot.ServerEvents[^1].Description}";
        var syncMode = snapshot.SyncHint.IsDelta ? "delta" : "full";
        var syncText = $"Sync: {syncMode}, after tick {snapshot.SyncHint.AfterTick}, map rev {snapshot.SyncHint.VisibleMapRevision}, chunks +{applyResult.AddedChunks}/~{applyResult.UnchangedChunks}/-{applyResult.RemovedChunks}";

        return $"{snapshot.Summary}\n{dialogueText} | {questText}\n{eventText}\n{syncText}";
    }
}
