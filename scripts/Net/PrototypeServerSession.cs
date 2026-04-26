using System.Collections.Generic;
using System.Linq;
using Godot;
using Karma.Core;
using Karma.Data;

namespace Karma.Net;

public partial class PrototypeServerSession : Node
{
    private readonly Dictionary<string, int> _nextSequenceByPlayer = new();
    private GameState _state = null!;
    private AuthoritativeWorldServer _server = null!;
    private long _lastLocalSnapshotTick;

    public AuthoritativeWorldServer Server => _server;
    public ClientInterestSnapshot LastLocalSnapshot { get; private set; }

    [Signal]
    public delegate void LocalSnapshotChangedEventHandler(string snapshotSummary);

    public override void _Ready()
    {
        _state = GetNode<GameState>("/root/GameState");
        _server = new AuthoritativeWorldServer(_state, "local-prototype", ServerConfig.Prototype4Player);
        RefreshLocalSnapshot();
    }

    public void RegisterWorldItem(string entityId, GameItem item, TilePosition position)
    {
        _server.SeedWorldItem(entityId, item, position);
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
        LastLocalSnapshot = CreateLocalSnapshot(_lastLocalSnapshotTick);
        _lastLocalSnapshotTick = _server.Tick;
        EmitSignal(SignalName.LocalSnapshotChanged, FormatLocalSnapshot(LastLocalSnapshot));
    }

    private static string FormatLocalSnapshot(ClientInterestSnapshot snapshot)
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

        return $"{snapshot.Summary}\n{dialogueText} | {questText}\n{eventText}";
    }
}
