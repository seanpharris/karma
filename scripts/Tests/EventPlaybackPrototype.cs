using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Core;
using Karma.Data;
using Karma.Net;

namespace Karma.Tests;

public partial class EventPlaybackPrototype : Control
{
    private const float TileSize = 48f;
    private const float PlayerWalkTilesPerSecond = 3.2f;
    private const float PlayerSprintMultiplier = 1.55f;

    private readonly Dictionary<string, int> _sequenceByPlayer = new();
    private readonly Dictionary<string, ColorRect> _actorBoxes = new();
    private readonly Dictionary<string, Label> _actorLabels = new();
    private readonly Dictionary<string, Node2D> _actorSprites = new();
    private readonly Dictionary<string, Vector2> _actorTilePositions = new();
    private readonly List<string> _timeline = new();
    private readonly List<string> _controllablePlayerIds = new();

    private GameState _state = null!;
    private AuthoritativeWorldServer _server = null!;
    private ServerConfig _config = null!;
    private Panel _mapPanel = null!;
    private Label _titleLabel = null!;
    private Label _instructionsLabel = null!;
    private Label _stateLabel = null!;
    private RichTextLabel _timelineLabel = null!;
    private Button _nextButton = null!;
    private Button _autoButton = null!;
    private Timer _autoTimer = null!;
    private string _scenario = "Event Gauntlet";
    private string _controlledPlayerId = string.Empty;
    private int _step;
    private bool _autoPlay;
    private string _activeDropId = string.Empty;

    public override void _Ready()
    {
        BuildUi();
        LoadEventGauntlet();
    }

    private void BuildUi()
    {
        var root = new VBoxContainer
        {
            Name = "PrototypeRoot",
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 16f,
            OffsetTop = 16f,
            OffsetRight = -16f,
            OffsetBottom = -16f
        };
        AddChild(root);

        _titleLabel = new Label
        {
            Text = "Karma playable event prototypes",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 24);
        root.AddChild(_titleLabel);

        _instructionsLabel = new Label
        {
            Text = "Choose a prototype. Move like the main game: hold WASD/arrow keys, hold Shift to sprint, Tab to switch actors, and E/Space to interact with the nearby event.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        root.AddChild(_instructionsLabel);

        var buttonRow = new HBoxContainer();
        root.AddChild(buttonRow);
        AddScenarioButton(buttonRow, "Event Gauntlet", LoadEventGauntlet);
        AddScenarioButton(buttonRow, "Supply Drop", LoadSupplyDrop);
        AddScenarioButton(buttonRow, "Clinic Revive", LoadClinicRevive);
        _nextButton = AddScenarioButton(buttonRow, "Interact / Next Event", PlayNextStep);
        _autoButton = AddScenarioButton(buttonRow, "Auto Play", ToggleAutoPlay);
        AddScenarioButton(buttonRow, "Restart", RestartScenario);

        var body = new HBoxContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddChild(body);

        _mapPanel = new Panel
        {
            CustomMinimumSize = new Vector2(720f, 520f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        body.AddChild(_mapPanel);

        var side = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(420f, 520f),
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        body.AddChild(side);

        _stateLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        side.AddChild(_stateLabel);

        _timelineLabel = new RichTextLabel
        {
            BbcodeEnabled = false,
            FitContent = false,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        side.AddChild(_timelineLabel);

        _autoTimer = new Timer
        {
            WaitTime = 1.2,
            OneShot = false
        };
        _autoTimer.Timeout += PlayNextStep;
        AddChild(_autoTimer);
    }

    private Button AddScenarioButton(HBoxContainer row, string text, System.Action action)
    {
        var button = new Button { Text = text };
        button.Pressed += action;
        row.AddChild(button);
        return button;
    }

    private void LoadEventGauntlet()
    {
        _scenario = "Event Gauntlet";
        CreateServer("event-playable-gauntlet", 12);
        AddPrototypePlayer("proto_warden", "Warden", new TilePosition(0, 0), new Color(0.25f, 0.55f, 0.95f));
        AddPrototypePlayer("proto_outlaw", "Wanted Outlaw", new TilePosition(3, 4), new Color(0.85f, 0.32f, 0.22f));
        AddPrototypePlayer("proto_wraith", "Low-HP Wraith", new TilePosition(1, 0), new Color(0.55f, 0.35f, 0.95f));
        _state.Players["proto_warden"].ApplyKarma(PerkCatalog.WardenThreshold);
        _state.Players["proto_outlaw"].ApplyKarma(AuthoritativeWorldServer.BountyKarmaThreshold - 25);
        _state.Players["proto_wraith"].ApplyKarma(-PerkCatalog.WraithThreshold);
        _state.Players["proto_wraith"].ApplyDamage(70);
        _state.AddItem("proto_outlaw", StarterItems.ContrabandPackage);
        SelectPlayer("proto_warden");
        AddLog("Prototype loaded. You are Warden. Walk beside Wanted Outlaw and press E to start the chain." );
        Render();
    }

    private void LoadSupplyDrop()
    {
        _scenario = "Supply Drop";
        CreateServer("event-playable-supply-drop", 30);
        AddPrototypePlayer("drop_runner", "Drop Runner", new TilePosition(0, 0), new Color(0.28f, 0.75f, 0.42f));
        SelectPlayer("drop_runner");
        AddLog("Prototype loaded. You are Drop Runner. Press E to start, then walk to the drop and press E to claim it." );
        Render();
    }

    private void LoadClinicRevive()
    {
        _scenario = "Clinic Revive";
        CreateServer("event-playable-clinic", 30);
        AddPrototypePlayer("clinic_striker", "Striker", new TilePosition(2, 4), new Color(0.8f, 0.25f, 0.2f));
        AddPrototypePlayer("clinic_patient", "Patient", new TilePosition(3, 4), new Color(0.25f, 0.7f, 0.85f));
        _state.Players["clinic_patient"].AddScrip(AuthoritativeWorldServer.ClinicReviveCost);
        SelectPlayer("clinic_striker");
        AddLog("Prototype loaded. You are Striker. Walk beside Patient and press E to down them near the clinic." );
        Render();
    }

    private void CreateServer(string worldId, int durationSeconds)
    {
        _step = 0;
        _activeDropId = string.Empty;
        _sequenceByPlayer.Clear();
        _actorTilePositions.Clear();
        _controllablePlayerIds.Clear();
        _controlledPlayerId = string.Empty;
        _timeline.Clear();
        _config = new ServerConfig(
            MaxPlayers: 7,
            TargetPlayers: 7,
            Scale: WorldScale.Small,
            TickRate: 20,
            InterestRadiusTiles: 24,
            CombatRangeTiles: 2,
            ChunkSizeTiles: ServerConfig.DefaultChunkSizeTiles,
            MatchDurationSeconds: durationSeconds);
        _state = new GameState();
        _server = new AuthoritativeWorldServer(_state, worldId, _config);
        ClearMap();
    }

    private void AddPrototypePlayer(string id, string name, TilePosition tile, Color color)
    {
        _state.RegisterPlayer(id, name);
        _state.SetPlayerPosition(id, tile);
        _actorTilePositions[id] = new Vector2(tile.X, tile.Y);
        _controllablePlayerIds.Add(id);
        CreatePlayerActor(id, name, tile, color);
    }

    private void PlayNextStep()
    {
        if (_scenario == "Event Gauntlet")
            PlayEventGauntletStep();
        else if (_scenario == "Supply Drop")
            PlaySupplyDropStep();
        else
            PlayClinicReviveStep();

        Render();
    }

    private void PlayEventGauntletStep()
    {
        switch (_step)
        {
            case 0:
                ReadyAllPlayers();
                _step++;
                AddLog("All connected prototype players readied up; the match starts. As Warden, stand near Wanted Outlaw and press E.");
                break;
            case 1:
                if (!RequireControlledPlayer("proto_warden") || !ArePlayersNear("proto_warden", "proto_outlaw", 2))
                {
                    AddLog("Move Warden close to Wanted Outlaw before issuing Wanted.");
                    break;
                }
                Send("proto_warden", IntentType.IssueWanted, new Dictionary<string, string> { ["targetId"] = "proto_outlaw" });
                SelectPlayer("proto_outlaw");
                _step++;
                AddLog("Warden marked the outlaw Wanted. You are now the outlaw; walk near Mara/Dallen and press E to feel contraband pressure.");
                break;
            case 2:
                if (!RequireControlledPlayer("proto_outlaw") || !IsNearLawNpc("proto_outlaw"))
                {
                    AddLog("Move Wanted Outlaw near Mara or Dallen before triggering contraband detection.");
                    break;
                }
                _server.AdvanceIdleTicks(1);
                SelectPlayer("proto_wraith");
                _step++;
                AddLog("Contraband detection fired. You are now the wounded Wraith; press E to call in a nearby supply drop.");
                break;
            case 3:
                _activeDropId = _server.ScheduleSupplyDrop(_state.Players["proto_wraith"].Position, StarterItems.DataChip, expiryTicks: 20);
                CreateActorBox(_activeDropId, "Supply Drop\npress E", _state.Players["proto_wraith"].Position, new Color(0.95f, 0.78f, 0.25f));
                _step++;
                AddLog("A supply drop landed on your Wraith. Press E again to claim it.");
                break;
            case 4:
                if (!IsWorldItemNear("proto_wraith", _activeDropId, 1))
                {
                    AddLog("Move Wraith onto the supply drop before claiming it.");
                    break;
                }
                Send("proto_wraith", IntentType.Interact, new Dictionary<string, string> { ["entityId"] = _activeDropId });
                _step++;
                AddLog("You claimed the drop as Wraith; low-health speed remains visible in the status panel. Press E to finish the match.");
                break;
            case 5:
                _server.AdvanceMatchTime(_config.MatchDurationSeconds);
                _step++;
                AddLog("Time advanced to match end and generated the summary.");
                break;
            default:
                AddLog("Event Gauntlet is complete. Restart or choose another prototype.");
                StopAutoPlay();
                break;
        }
    }

    private void PlaySupplyDropStep()
    {
        switch (_step)
        {
            case 0:
                ReadyAllPlayers();
                _step++;
                AddLog("Match started. Press E to spawn a supply drop.");
                break;
            case 1:
                _activeDropId = _server.ScheduleSupplyDrop(new TilePosition(2, 2), StarterItems.MediPatch, expiryTicks: 50);
                CreateActorBox(_activeDropId, "Medi Drop\npress E", new TilePosition(2, 2), new Color(0.95f, 0.78f, 0.25f));
                _step++;
                AddLog("Supply drop spawned. Walk Drop Runner onto it and press E to claim it.");
                break;
            case 2:
                if (!IsWorldItemNear("drop_runner", _activeDropId, 1))
                {
                    AddLog("Move Drop Runner onto the Medi Drop before claiming it.");
                    break;
                }
                Send("drop_runner", IntentType.Interact, new Dictionary<string, string> { ["entityId"] = _activeDropId });
                _step++;
                AddLog("You claimed the Medi Patch. Press E to spawn a drop that will expire.");
                break;
            case 3:
                _activeDropId = _server.ScheduleSupplyDrop(new TilePosition(5, 2), StarterItems.DataChip, expiryTicks: 3);
                CreateActorBox(_activeDropId, "Expiry Drop", new TilePosition(5, 2), new Color(0.95f, 0.45f, 0.2f));
                _step++;
                AddLog("A second drop spawned with a tiny expiry timer. Press E to wait and watch it expire.");
                break;
            case 4:
                _server.AdvanceIdleTicks(4);
                _step++;
                AddLog("Nobody claimed the second drop, so it expired and disappeared from the world item cache.");
                break;
            default:
                AddLog("Supply Drop prototype is complete.");
                StopAutoPlay();
                break;
        }
    }

    private void PlayClinicReviveStep()
    {
        switch (_step)
        {
            case 0:
                ReadyAllPlayers();
                _step++;
                AddLog("Match started. As Striker, stand beside Patient and press E to attack/down them.");
                break;
            case 1:
                if (!RequireControlledPlayer("clinic_striker") || !ArePlayersNear("clinic_striker", "clinic_patient", 2))
                {
                    AddLog("Move Striker close to Patient before attacking.");
                    break;
                }
                AttackUntilDowned("clinic_striker", "clinic_patient");
                SelectPlayer("clinic_patient");
                _step++;
                AddLog("Patient is downed near the clinic. You are now Patient; press E to wait through the revive countdown.");
                break;
            case 2:
                _server.AdvanceIdleTicks(AuthoritativeWorldServer.DownedCountdownTicks + 1);
                _step++;
                AddLog("The clinic revived you with scrip instead of a Karma Break.");
                break;
            default:
                AddLog("Clinic Revive prototype is complete.");
                StopAutoPlay();
                break;
        }
    }

    private void ReadyAllPlayers()
    {
        foreach (var playerId in _server.ConnectedPlayerIds.ToArray())
            Send(playerId, IntentType.ReadyUp, new Dictionary<string, string>());
    }

    private void AttackUntilDowned(string attackerId, string targetId)
    {
        for (var i = 0; i < 5 && _state.Players[targetId].Health > 0; i++)
        {
            Send(attackerId, IntentType.Attack, new Dictionary<string, string> { ["targetId"] = targetId });
            _server.AdvanceIdleTicks(50);
        }
    }

    private ServerProcessResult Send(string playerId, IntentType type, IReadOnlyDictionary<string, string> payload)
    {
        _sequenceByPlayer.TryGetValue(playerId, out var previous);
        var next = previous + 1;
        _sequenceByPlayer[playerId] = next;
        return _server.ProcessIntent(new ServerIntent(playerId, next, type, payload));
    }

    public override void _Process(double delta)
    {
        MoveControlledPlayerFromHeldInput((float)delta);
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey key || !key.Pressed || key.Echo)
            return;

        if (key.Keycode == Key.Tab)
        {
            CycleControlledPlayer();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == Key.E || key.Keycode == Key.Space || key.Keycode == Key.Enter)
        {
            PlayNextStep();
            GetViewport().SetInputAsHandled();
        }
    }

    private void MoveControlledPlayerFromHeldInput(float deltaSeconds)
    {
        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        if (input == Vector2.Zero)
            return;

        var speed = PlayerWalkTilesPerSecond;
        if (Input.IsActionPressed("sprint"))
            speed *= PlayerSprintMultiplier;
        MoveControlledPlayer(input * speed * deltaSeconds);
    }

    private void MoveControlledPlayer(Vector2 deltaTiles)
    {
        if (string.IsNullOrWhiteSpace(_controlledPlayerId) || !_state.Players.TryGetValue(_controlledPlayerId, out var player))
            return;

        if (!_actorTilePositions.TryGetValue(_controlledPlayerId, out var current))
            current = new Vector2(player.Position.X, player.Position.Y);

        var next = new Vector2(
            Math.Clamp(current.X + deltaTiles.X, 0f, 10f),
            Math.Clamp(current.Y + deltaTiles.Y, 0f, 7f));
        _actorTilePositions[_controlledPlayerId] = next;
        _state.SetPlayerPosition(
            _controlledPlayerId,
            new TilePosition(Math.Clamp(Mathf.RoundToInt(next.X), 0, 10), Math.Clamp(Mathf.RoundToInt(next.Y), 0, 7)));
        Render();
    }

    private void SelectPlayer(string playerId)
    {
        if (!_controllablePlayerIds.Contains(playerId))
            return;
        _controlledPlayerId = playerId;
        Render();
    }

    private void CycleControlledPlayer()
    {
        if (_controllablePlayerIds.Count == 0)
            return;
        var index = _controllablePlayerIds.IndexOf(_controlledPlayerId);
        var next = (index + 1) % _controllablePlayerIds.Count;
        _controlledPlayerId = _controllablePlayerIds[next];
        AddLog($"Now controlling {_state.Players[_controlledPlayerId].DisplayName}.");
        Render();
    }

    private bool RequireControlledPlayer(string playerId)
    {
        if (_controlledPlayerId == playerId)
            return true;
        SelectPlayer(playerId);
        AddLog($"Switched control to {_state.Players[playerId].DisplayName} for this event.");
        return true;
    }

    private bool ArePlayersNear(string leftId, string rightId, int radiusTiles)
    {
        return _state.Players.TryGetValue(leftId, out var left) &&
            _state.Players.TryGetValue(rightId, out var right) &&
            left.Position.DistanceSquaredTo(right.Position) <= radiusTiles * radiusTiles;
    }

    private bool IsNearLawNpc(string playerId)
    {
        if (!_state.Players.TryGetValue(playerId, out var player))
            return false;
        return new[] { new TilePosition(3, 4), new TilePosition(6, 4) }
            .Any(pos => player.Position.DistanceSquaredTo(pos) <= 2);
    }

    private bool IsWorldItemNear(string playerId, string entityId, int radiusTiles)
    {
        return _state.Players.TryGetValue(playerId, out var player) &&
            _server.WorldItems.TryGetValue(entityId, out var item) &&
            item.IsAvailable &&
            player.Position.DistanceSquaredTo(item.Position) <= radiusTiles * radiusTiles;
    }

    private void RestartScenario()
    {
        if (_scenario == "Event Gauntlet")
            LoadEventGauntlet();
        else if (_scenario == "Supply Drop")
            LoadSupplyDrop();
        else
            LoadClinicRevive();
    }

    private void ToggleAutoPlay()
    {
        if (_autoPlay)
            StopAutoPlay();
        else
        {
            _autoPlay = true;
            _autoButton.Text = "Stop Auto";
            _autoTimer.Start();
        }
    }

    private void StopAutoPlay()
    {
        _autoPlay = false;
        if (_autoButton is not null)
            _autoButton.Text = "Auto Play";
        _autoTimer?.Stop();
    }

    private void AddLog(string message)
    {
        _timeline.Add($"• {message}");
        if (_timeline.Count > 18)
            _timeline.RemoveAt(0);
    }

    private void Render()
    {
        RenderActorsFromState();
        var snapshot = _server.CreateInterestSnapshot(FirstPrototypePlayerId());
        var recentEvents = _server.EventLog.TakeLast(6).Select(e => $"- {e.EventId}: {e.Description}");
        var summary = snapshot.MatchSummary is null
            ? "Match summary: not finished yet"
            : "Match summary: " + string.Join(", ", snapshot.MatchSummary.Players.Select(p => $"{p.DisplayName} {p.FinalKarma}"));
        var controlledName = string.IsNullOrWhiteSpace(_controlledPlayerId) || !_state.Players.ContainsKey(_controlledPlayerId)
            ? "none"
            : _state.Players[_controlledPlayerId].DisplayName;
        _stateLabel.Text =
            $"Scenario: {_scenario}\n" +
            $"Control: {controlledName} | hold WASD/arrows to move | Shift sprint | E/Space interact | Tab switches\n" +
            $"Match: {_server.Match.Status} | Tick: {_server.Tick} | Events: {_server.EventLog.Count}\n" +
            summary + "\n\n" +
            string.Join("\n", snapshot.Players.Select(FormatPlayer));
        _timelineLabel.Text = string.Join("\n", _timeline.Concat(new[] { "", "Recent server events:" }).Concat(recentEvents));
    }

    private string FormatPlayer(PlayerSnapshot player)
    {
        var statuses = player.StatusEffects.Count == 0 ? "no status" : string.Join(", ", player.StatusEffects);
        return $"{player.DisplayName}: karma {player.Karma}, hp {player.Health}/{player.MaxHealth}, speed x{player.SpeedModifier:0.00}, {statuses}";
    }

    private string FirstPrototypePlayerId()
    {
        return _scenario switch
        {
            "Supply Drop" => "drop_runner",
            "Clinic Revive" => "clinic_striker",
            _ => "proto_warden"
        };
    }

    private void RenderActorsFromState()
    {
        foreach (var pair in _state.Players)
        {
            if (!_actorBoxes.ContainsKey(pair.Key))
                continue;
            if (_actorTilePositions.TryGetValue(pair.Key, out var smoothTile))
                MoveActorBox(pair.Key, smoothTile);
            else
                MoveActorBox(pair.Key, pair.Value.Position);
            _actorLabels[pair.Key].Text = BuildActorLabel(pair.Key, pair.Value.DisplayName);
            _actorLabels[pair.Key].Modulate = pair.Key == _controlledPlayerId ? new Color(1f, 0.92f, 0.25f) : Colors.White;
        }

        foreach (var pair in _server.WorldItems)
        {
            if (!_actorBoxes.ContainsKey(pair.Key))
                continue;
            if (!pair.Value.IsAvailable)
            {
                _actorBoxes[pair.Key].Visible = false;
                _actorLabels[pair.Key].Visible = false;
                continue;
            }

            _actorBoxes[pair.Key].Visible = true;
            _actorLabels[pair.Key].Visible = true;
        }
    }

    private string BuildActorLabel(string id, string displayName)
    {
        if (!_state.Players.TryGetValue(id, out var player))
            return displayName;
        var tags = new List<string>();
        if (player.HasItem(StarterItems.ContrabandPackageId)) tags.Add("contraband");
        if (player.Health < player.MaxHealth) tags.Add($"HP {player.Health}");
        if (player.Inventory.Count > 0) tags.Add($"items {player.Inventory.Count}");
        var selectedPrefix = id == _controlledPlayerId ? "▶ " : string.Empty;
        return tags.Count == 0
            ? selectedPrefix + displayName
            : $"{selectedPrefix}{displayName}\n{string.Join(" | ", tags)}";
    }

    private void CreatePlayerActor(string id, string label, TilePosition tile, Color color)
    {
        if (_actorBoxes.ContainsKey(id))
            return;

        var baseMarker = new ColorRect
        {
            Name = $"PlayerBase_{id}",
            Color = new Color(color.R, color.G, color.B, 0.35f),
            Size = new Vector2(86f, 14f)
        };
        var spriteRoot = new Node2D
        {
            Name = $"PlayerModel_{id}"
        };
        spriteRoot.AddChild(new PrototypeCharacterSprite
        {
            Name = "PrototypePlayerSprite",
            Kind = PrototypeSpriteKind.Player
        });
        var text = new Label
        {
            Name = $"Label_{id}",
            Text = label,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Size = new Vector2(120f, 42f),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };

        _mapPanel.AddChild(baseMarker);
        _mapPanel.AddChild(spriteRoot);
        _mapPanel.AddChild(text);
        _actorBoxes[id] = baseMarker;
        _actorSprites[id] = spriteRoot;
        _actorLabels[id] = text;
        MoveActorBox(id, tile);
    }

    private void CreateActorBox(string id, string label, TilePosition tile, Color color)
    {
        if (_actorBoxes.ContainsKey(id))
            return;
        var box = new ColorRect
        {
            Name = $"Box_{id}",
            Color = color,
            Size = new Vector2(86f, 48f)
        };
        var text = new Label
        {
            Name = $"Label_{id}",
            Text = label,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Size = box.Size,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _mapPanel.AddChild(box);
        _mapPanel.AddChild(text);
        _actorBoxes[id] = box;
        _actorLabels[id] = text;
        _actorTilePositions[id] = new Vector2(tile.X, tile.Y);
        MoveActorBox(id, tile);
    }

    private void MoveActorBox(string id, TilePosition tile)
    {
        MoveActorBox(id, new Vector2(tile.X, tile.Y));
    }

    private void MoveActorBox(string id, Vector2 tile)
    {
        var pos = new Vector2(80f + tile.X * TileSize, 80f + tile.Y * TileSize);
        var hasPlayerModel = _actorSprites.ContainsKey(id);
        if (_actorBoxes.TryGetValue(id, out var box))
            box.Position = hasPlayerModel ? pos + new Vector2(0f, 54f) : pos;
        if (_actorLabels.TryGetValue(id, out var label))
            label.Position = hasPlayerModel ? pos + new Vector2(-17f, -28f) : pos;
        if (_actorSprites.TryGetValue(id, out var sprite))
            sprite.Position = pos + new Vector2(43f, 58f);
    }

    private void ClearMap()
    {
        foreach (var node in _actorBoxes.Values)
            node.QueueFree();
        foreach (var node in _actorLabels.Values)
            node.QueueFree();
        foreach (var node in _actorSprites.Values)
            node.QueueFree();
        _actorBoxes.Clear();
        _actorLabels.Clear();
        _actorSprites.Clear();

        CreateActorBox("npc_mara", "Mara\nLaw NPC", new TilePosition(3, 4), new Color(0.18f, 0.45f, 0.32f));
        CreateActorBox("npc_dallen", "Dallen\nLaw NPC", new TilePosition(6, 4), new Color(0.18f, 0.38f, 0.45f));
        _actorBoxes["npc_mara"].MouseFilter = MouseFilterEnum.Ignore;
        _actorBoxes["npc_dallen"].MouseFilter = MouseFilterEnum.Ignore;
    }
}
