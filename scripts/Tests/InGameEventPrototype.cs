using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Core;
using Karma.Data;
using Karma.Net;
using Karma.Player;

namespace Karma.Tests;

public partial class InGameEventPrototype : Node2D
{
    private const float TileSize = 32f;
    private static readonly TilePosition OutlawCampTile = new(18, 8);
    private static readonly TilePosition MaraCheckpointTile = new(30, 8);
    private static readonly TilePosition DallenCheckpointTile = new(34, 10);
    private static readonly TilePosition WraithCrashTile = new(45, 15);
    private static readonly TilePosition ClinicPatientTile = new(18, 6);
    private static readonly TilePosition ClinicTile = new(22, 6);
    private readonly Dictionary<string, int> _sequenceByPlayer = new();
    private readonly Dictionary<string, Node2D> _markers = new();
    private readonly List<string> _log = new();

    private GameState _state = null!;
    private AuthoritativeWorldServer _server = null!;
    private ServerConfig _config = null!;
    private CharacterBody2D _player = null!;
    private Label _objectiveLabel = null!;
    private Label _logLabel = null!;
    private string _scenario = "Event Gauntlet";
    private int _step;
    private string _activeDropId = string.Empty;

    public override void _Ready()
    {
        Name = "Main";
        _state = GetNode<GameState>("/root/GameState");
        _player = GetNode<CharacterBody2D>("Player");
        BuildOverlay();
        LoadEventGauntlet();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false } key)
            return;

        if (key.Keycode == Key.E || key.Keycode == Key.Space || key.Keycode == Key.Enter)
        {
            Interact();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        SyncMarkerPositions();
        RefreshOverlay();
    }

    private void BuildOverlay()
    {
        var layer = new CanvasLayer { Name = "EventPrototypeOverlay", Layer = 20 };
        AddChild(layer);
        var panel = new PanelContainer
        {
            AnchorLeft = 1f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 0f,
            OffsetLeft = -456f,
            OffsetTop = 16f,
            OffsetRight = -16f,
            OffsetBottom = 285f
        };
        layer.AddChild(panel);
        var root = new VBoxContainer();
        panel.AddChild(root);
        var title = new Label
        {
            Text = "In-Game Event Prototypes",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 22);
        root.AddChild(title);
        var row = new HBoxContainer();
        root.AddChild(row);
        AddButton(row, "Gauntlet", LoadEventGauntlet);
        AddButton(row, "Supply", LoadSupplyDrop);
        AddButton(row, "Clinic", LoadClinicRevive);
        AddButton(row, "Duel", LoadDuel);
        AddButton(row, "Rescue", LoadRescue);
        var row2 = new HBoxContainer();
        root.AddChild(row2);
        AddButton(row2, "Break", LoadKarmaBreakLoot);
        AddButton(row2, "Shop", LoadShop);
        AddButton(row2, "Structure", LoadStructure);
        AddButton(row2, "Posse", LoadPosse);
        AddButton(row2, "Chat", LoadLocalChat);
        var row3 = new HBoxContainer();
        root.AddChild(row3);
        AddButton(row3, "Mount", LoadMount);
        AddButton(row3, "Quest", LoadQuestDialogue);
        AddButton(row3, "Restart", RestartScenario);
        _objectiveLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        root.AddChild(_objectiveLabel);
        _logLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        root.AddChild(_logLabel);
    }

    private static void AddButton(HBoxContainer row, string text, Action action)
    {
        var button = new Button { Text = text };
        button.Pressed += action;
        row.AddChild(button);
    }

    private void CreateServer(string worldId, int durationSeconds)
    {
        _step = 0;
        _activeDropId = string.Empty;
        _sequenceByPlayer.Clear();
        _log.Clear();
        ClearMarkers();
        _config = new ServerConfig(
            MaxPlayers: 32,
            TargetPlayers: 32,
            Scale: WorldScale.Small,
            TickRate: 20,
            InterestRadiusTiles: 24,
            CombatRangeTiles: 2,
            ChunkSizeTiles: ServerConfig.DefaultChunkSizeTiles,
            MatchDurationSeconds: durationSeconds);
        _server = new AuthoritativeWorldServer(_state, worldId, _config);
        _state.SetPlayerPosition(GameState.LocalPlayerId, ToTile(_player.GlobalPosition));
    }

    private void LoadEventGauntlet()
    {
        _scenario = "Event Gauntlet";
        ResetLocalPlayer(new Vector2(64f, 64f));
        _state.RegisterPlayer("event_outlaw", "Wanted Outlaw");
        _state.RegisterPlayer("event_wraith", "Low-HP Wraith");
        _state.SetPlayerPosition("event_outlaw", OutlawCampTile);
        _state.SetPlayerPosition("event_wraith", WraithCrashTile);
        CreateServer("ingame-event-gauntlet", 18);
        _state.LocalPlayer.ApplyKarma(Math.Max(PerkCatalog.WardenThreshold - _state.LocalPlayer.Karma.Score, 0));
        _state.Players["event_outlaw"].ApplyKarma(AuthoritativeWorldServer.BountyKarmaThreshold - 25 - _state.Players["event_outlaw"].Karma.Score);
        _state.Players["event_wraith"].ApplyKarma(-PerkCatalog.WraithThreshold - _state.Players["event_wraith"].Karma.Score);
        _state.Players["event_wraith"].ApplyDamage(70);
        _state.AddItem("event_outlaw", StarterItems.ContrabandPackage);
        AddBoxMarker("camp_marker", "Warden\nStart", new TilePosition(2, 2), new Color(0.28f, 0.48f, 0.72f));
        AddPlayerMarker("event_outlaw", "Wanted Outlaw\ncontraband", new Color(0.85f, 0.25f, 0.18f));
        AddPlayerMarker("event_wraith", "Wraith Crash\nlow HP", new Color(0.55f, 0.35f, 0.95f));
        AddBoxMarker("mara_marker", "Mara\nLaw Checkpoint", MaraCheckpointTile, new Color(0.18f, 0.45f, 0.32f));
        AddBoxMarker("dallen_marker", "Dallen\nPatrol", DallenCheckpointTile, new Color(0.18f, 0.38f, 0.45f));
        AddBoxMarker("route_marker_1", "1. Wanted\nOutlaw Camp", new TilePosition(12, 6), new Color(0.46f, 0.34f, 0.18f));
        AddBoxMarker("route_marker_2", "2. Contraband\nCheckpoint", new TilePosition(26, 8), new Color(0.46f, 0.34f, 0.18f));
        AddBoxMarker("route_marker_3", "3. Supply\nCrash Site", new TilePosition(40, 13), new Color(0.46f, 0.34f, 0.18f));
        ReadyAllPlayers();
        AddLog("You are the local player/Warden. Follow the route east to the outlaw camp and press E.");
    }

    private void LoadSupplyDrop()
    {
        _scenario = "Supply Drop";
        ResetLocalPlayer(new Vector2(64f, 64f));
        CreateServer("ingame-supply-drop", 30);
        ReadyAllPlayers();
        AddBoxMarker("supply_lz_marker", "Open LZ\ncall drop", new TilePosition(12, 4), new Color(0.32f, 0.42f, 0.52f));
        AddBoxMarker("supply_expiry_marker", "Far Cache\nexpiry test", new TilePosition(26, 9), new Color(0.32f, 0.42f, 0.52f));
        AddLog("Walk to the open landing zone marker, then press E to call a supply drop.");
    }

    private void LoadClinicRevive()
    {
        _scenario = "Clinic Revive";
        ResetLocalPlayer(new Vector2(64f, 128f));
        _state.RegisterPlayer("clinic_patient_proto", "Clinic Patient");
        _state.SetPlayerPosition("clinic_patient_proto", ClinicPatientTile);
        _state.Players["clinic_patient_proto"].AddScrip(AuthoritativeWorldServer.ClinicReviveCost);
        CreateServer("ingame-clinic-revive", 30);
        AddBoxMarker("clinic_start_marker", "Sparring\nStart", new TilePosition(4, 5), new Color(0.32f, 0.42f, 0.52f));
        AddPlayerMarker("clinic_patient_proto", "Patient\nnear clinic", new Color(0.25f, 0.7f, 0.85f));
        AddBoxMarker("clinic_marker", "Clinic\nRevive Zone", ClinicTile, new Color(0.25f, 0.65f, 0.48f));
        ReadyAllPlayers();
        AddLog("Walk down the road to the patient near the clinic and press E to down them.");
    }

    private void LoadDuel()
    {
        _scenario = "Duel";
        ResetLocalPlayer(new Vector2(64f, 64f));
        _state.RegisterPlayer("duel_rival_proto", "Duel Rival");
        _state.SetPlayerPosition("duel_rival_proto", new TilePosition(12, 4));
        CreateServer("ingame-duel", 30);
        AddPlayerMarker("duel_rival_proto", "Duel Rival\nrequest + accept", new Color(0.85f, 0.32f, 0.22f));
        AddBoxMarker("duel_lane", "Duel Lane", new TilePosition(8, 4), new Color(0.45f, 0.28f, 0.22f));
        ReadyAllPlayers();
        AddLog("Walk to the duel rival and press E to request a duel.");
    }

    private void LoadRescue()
    {
        _scenario = "Rescue";
        ResetLocalPlayer(new Vector2(64f, 64f));
        _state.RegisterPlayer("rescue_patient_proto", "Rescue Patient");
        _state.RegisterPlayer("rescue_attacker_proto", "Rescue Attacker");
        _state.SetPlayerPosition("rescue_patient_proto", new TilePosition(13, 5));
        _state.SetPlayerPosition("rescue_attacker_proto", new TilePosition(14, 5));
        CreateServer("ingame-rescue", 30);
        AddPlayerMarker("rescue_patient_proto", "Rescue Patient", new Color(0.25f, 0.7f, 0.85f));
        AddPlayerMarker("rescue_attacker_proto", "Hazard\ndowns patient", new Color(0.85f, 0.25f, 0.18f));
        AddBoxMarker("rescue_marker", "Rescue Zone", new TilePosition(13, 5), new Color(0.22f, 0.44f, 0.56f));
        ReadyAllPlayers();
        AddLog("Walk to the rescue zone. Press E to have the hazard down the patient, then rescue them.");
    }

    private void LoadKarmaBreakLoot()
    {
        _scenario = "Karma Break Loot";
        ResetLocalPlayer(new Vector2(64f, 64f));
        _state.RegisterPlayer("break_victim_proto", "Break Victim");
        _state.SetPlayerPosition("break_victim_proto", new TilePosition(13, 5));
        _state.AddItem("break_victim_proto", StarterItems.DataChip);
        _state.AddItem("break_victim_proto", StarterItems.RationPack);
        CreateServer("ingame-karma-break-loot", 30);
        AddPlayerMarker("break_victim_proto", "Break Victim\ndrops loot", new Color(0.85f, 0.25f, 0.18f));
        AddBoxMarker("break_marker", "Break Site", new TilePosition(13, 5), new Color(0.5f, 0.24f, 0.46f));
        ReadyAllPlayers();
        AddLog("Walk to the break site and press E to trigger a Karma Break drop.");
    }

    private void LoadShop()
    {
        _scenario = "Shop";
        ResetLocalPlayer(new Vector2(96f, 128f));
        CreateServer("ingame-shop", 30);
        _state.LocalPlayer.AddScrip(100);
        AddBoxMarker("shop_dallen", "Dallen Shop\nbuy + use", new TilePosition(6, 4), new Color(0.18f, 0.38f, 0.45f));
        ReadyAllPlayers();
        AddLog("Walk to Dallen's shop marker and press E to buy a medi patch.");
    }

    private void LoadStructure()
    {
        _scenario = "Structure";
        ResetLocalPlayer(new Vector2(96f, 96f));
        CreateServer("ingame-structure", 30);
        _state.AddItem(GameState.LocalPlayerId, StarterItems.MultiTool);
        AddBoxMarker("structure_greenhouse_standard", "Greenhouse\ninspect/repair", new TilePosition(8, 3), new Color(0.25f, 0.52f, 0.32f));
        ReadyAllPlayers();
        AddLog("Walk to the greenhouse and press E to inspect, repair, sabotage, enter, and exit.");
    }

    private void LoadPosse()
    {
        _scenario = "Posse";
        ResetLocalPlayer(new Vector2(64f, 64f));
        _state.RegisterPlayer("posse_friend_proto", "Posse Friend");
        _state.SetPlayerPosition("posse_friend_proto", new TilePosition(11, 4));
        CreateServer("ingame-posse", 30);
        AddPlayerMarker("posse_friend_proto", "Posse Friend", new Color(0.25f, 0.7f, 0.85f));
        ReadyAllPlayers();
        AddLog("Walk to your friend and press E to invite them to a posse.");
    }

    private void LoadLocalChat()
    {
        _scenario = "Local Chat";
        ResetLocalPlayer(new Vector2(64f, 64f));
        _state.RegisterPlayer("chat_listener_proto", "Chat Listener");
        _state.SetPlayerPosition("chat_listener_proto", new TilePosition(12, 4));
        CreateServer("ingame-local-chat", 30);
        AddPlayerMarker("chat_listener_proto", "Listener\nnearby chat", new Color(0.25f, 0.7f, 0.85f));
        AddBoxMarker("chat_far_marker", "Far Echo\nmove here", new TilePosition(30, 8), new Color(0.25f, 0.35f, 0.62f));
        ReadyAllPlayers();
        AddLog("Press E near the listener to send local chat, then follow the far marker for distance testing.");
    }

    private void LoadMount()
    {
        _scenario = "Mount";
        ResetLocalPlayer(new Vector2(256f, 192f));
        CreateServer("ingame-mount", 30);
        AddBoxMarker("mount_hover_1", "Hover Scooter\nmount/dismount", new TilePosition(12, 8), new Color(0.62f, 0.5f, 0.18f));
        AddBoxMarker("mount_cargo_1", "Cargo Crawler\noccupied test", new TilePosition(15, 12), new Color(0.52f, 0.4f, 0.18f));
        ReadyAllPlayers();
        AddLog("Walk to the hover scooter and press E to mount it.");
    }

    private void LoadQuestDialogue()
    {
        _scenario = "Quest + Dialogue";
        ResetLocalPlayer(new Vector2(96f, 128f));
        CreateServer("ingame-quest-dialogue", 30);
        _state.AddItem(GameState.LocalPlayerId, StarterItems.RepairKit);
        AddBoxMarker("quest_mara", "Mara\nquest/dialogue", new TilePosition(3, 4), new Color(0.18f, 0.45f, 0.32f));
        AddBoxMarker("quest_dallen", "Dallen\nentanglement", new TilePosition(6, 4), new Color(0.18f, 0.38f, 0.45f));
        ReadyAllPlayers();
        AddLog("Walk to Mara and press E to start dialogue and the clinic filter quest.");
    }

    private void ResetLocalPlayer(Vector2 worldPosition)
    {
        _player.GlobalPosition = worldPosition;
        _player.Velocity = Vector2.Zero;
        _state.SetPlayerPosition(GameState.LocalPlayerId, ToTile(worldPosition));
    }

    private void Interact()
    {
        _state.SetPlayerPosition(GameState.LocalPlayerId, ToTile(_player.GlobalPosition));
        switch (_scenario)
        {
            case "Event Gauntlet":
                InteractGauntlet();
                break;
            case "Supply Drop":
                InteractSupplyDrop();
                break;
            case "Clinic Revive":
                InteractClinic();
                break;
            case "Duel":
                InteractDuel();
                break;
            case "Rescue":
                InteractRescue();
                break;
            case "Karma Break Loot":
                InteractKarmaBreakLoot();
                break;
            case "Shop":
                InteractShop();
                break;
            case "Structure":
                InteractStructure();
                break;
            case "Posse":
                InteractPosse();
                break;
            case "Local Chat":
                InteractLocalChat();
                break;
            case "Mount":
                InteractMount();
                break;
            case "Quest + Dialogue":
                InteractQuestDialogue();
                break;
        }
        RefreshOverlay();
    }

    private void InteractGauntlet()
    {
        switch (_step)
        {
            case 0:
                if (!IsLocalNear("event_outlaw", 3))
                {
                    AddLog("Get closer to the Wanted Outlaw first.");
                    return;
                }
                Send(GameState.LocalPlayerId, IntentType.IssueWanted, new Dictionary<string, string> { ["targetId"] = "event_outlaw" });
                _step++;
                AddLog("Wanted issued. Follow the road east to the law checkpoint, then press E near Mara/Dallen for contraband detection.");
                return;
            case 1:
                _state.SetPlayerPosition("event_outlaw", ToTile(_player.GlobalPosition));
                if (!IsNearLawTile(ToTile(_player.GlobalPosition)))
                {
                    AddLog("Stand near the law NPC markers to play out contraband detection.");
                    return;
                }
                _server.AdvanceIdleTicks(1);
                _step++;
                AddLog("Contraband detected. Keep heading southeast to the Wraith crash site and press E to call a drop there.");
                return;
            case 2:
                if (!IsLocalNear("event_wraith", 4))
                {
                    AddLog("Get closer to the Wraith before calling the drop.");
                    return;
                }
                _activeDropId = _server.ScheduleSupplyDrop(ToTile(_player.GlobalPosition), StarterItems.DataChip, expiryTicks: 30);
                AddBoxMarker(_activeDropId, "Supply Drop\nE to claim", ToTile(_player.GlobalPosition), new Color(0.95f, 0.78f, 0.25f));
                _step++;
                AddLog("Supply drop landed in-world. Press E while standing on it to claim.");
                return;
            case 3:
                if (!IsWorldItemNearLocal(_activeDropId, 2))
                {
                    AddLog("Stand on the supply drop to claim it.");
                    return;
                }
                Send(GameState.LocalPlayerId, IntentType.Interact, new Dictionary<string, string> { ["entityId"] = _activeDropId });
                HideMarker(_activeDropId);
                _step++;
                AddLog("Drop claimed by the player. Press E one more time to end the match and view summary.");
                return;
            case 4:
                _server.AdvanceMatchTime(_config.MatchDurationSeconds);
                _step++;
                AddLog("Match finished; summary is now live in the overlay.");
                return;
            default:
                AddLog("Gauntlet complete. Restart or choose another prototype.");
                return;
        }
    }

    private void InteractSupplyDrop()
    {
        switch (_step)
        {
            case 0:
                var dropTile = new TilePosition(ToTile(_player.GlobalPosition).X + 8, ToTile(_player.GlobalPosition).Y + 2);
                _activeDropId = _server.ScheduleSupplyDrop(dropTile, StarterItems.MediPatch, expiryTicks: 50);
                AddBoxMarker(_activeDropId, "Medi Drop\nE to claim", dropTile, new Color(0.95f, 0.78f, 0.25f));
                _step++;
                AddLog("Supply drop spawned in the world. Walk to it and press E.");
                return;
            case 1:
                if (!IsWorldItemNearLocal(_activeDropId, 2))
                {
                    AddLog("Move onto the Medi Drop before claiming it.");
                    return;
                }
                Send(GameState.LocalPlayerId, IntentType.Interact, new Dictionary<string, string> { ["entityId"] = _activeDropId });
                HideMarker(_activeDropId);
                _step++;
                AddLog("Medi Drop claimed. Press E to spawn an expiring drop.");
                return;
            case 2:
                var expiryTile = new TilePosition(ToTile(_player.GlobalPosition).X + 10, ToTile(_player.GlobalPosition).Y + 5);
                _activeDropId = _server.ScheduleSupplyDrop(expiryTile, StarterItems.DataChip, expiryTicks: 3);
                AddBoxMarker(_activeDropId, "Expiry Drop", expiryTile, new Color(0.95f, 0.45f, 0.2f));
                _step++;
                AddLog("Expiring drop spawned. Press E to wait out its timer.");
                return;
            case 3:
                _server.AdvanceIdleTicks(4);
                HideMarker(_activeDropId);
                _step++;
                AddLog("The unclaimed drop expired.");
                return;
            default:
                AddLog("Supply prototype complete.");
                return;
        }
    }

    private void InteractClinic()
    {
        switch (_step)
        {
            case 0:
                if (!IsLocalNear("clinic_patient_proto", 3))
                {
                    AddLog("Get closer to the patient before attacking.");
                    return;
                }
                AttackUntilDowned(GameState.LocalPlayerId, "clinic_patient_proto");
                _step++;
                AddLog("Patient downed near the clinic. Press E to wait through the countdown.");
                return;
            case 1:
                _server.AdvanceIdleTicks(AuthoritativeWorldServer.DownedCountdownTicks + 1);
                _step++;
                AddLog("Clinic revived the patient with scrip instead of a Karma Break.");
                return;
            default:
                AddLog("Clinic prototype complete.");
                return;
        }
    }

    private void InteractDuel()
    {
        switch (_step)
        {
            case 0:
                if (!IsLocalNear("duel_rival_proto", 3))
                {
                    AddLog("Get closer to the duel rival first.");
                    return;
                }
                Send(GameState.LocalPlayerId, IntentType.RequestDuel, new Dictionary<string, string> { ["targetId"] = "duel_rival_proto" });
                _step++;
                AddLog("Duel requested. Press E to have the rival accept.");
                return;
            case 1:
                Send("duel_rival_proto", IntentType.AcceptDuel, new Dictionary<string, string> { ["challengerId"] = GameState.LocalPlayerId });
                _step++;
                AddLog("Duel accepted. Press E to strike during the accepted duel.");
                return;
            case 2:
                Send(GameState.LocalPlayerId, IntentType.Attack, new Dictionary<string, string> { ["targetId"] = "duel_rival_proto" });
                _step++;
                AddLog("Duel strike recorded without outlaw-style karma pressure.");
                return;
            default:
                AddLog("Duel prototype complete.");
                return;
        }
    }

    private void InteractRescue()
    {
        switch (_step)
        {
            case 0:
                if (!IsLocalNear("rescue_patient_proto", 5))
                {
                    AddLog("Get closer to the rescue zone first.");
                    return;
                }
                AttackUntilDowned("rescue_attacker_proto", "rescue_patient_proto");
                _step++;
                AddLog("Patient is downed. Press E to rescue them before the countdown expires.");
                return;
            case 1:
                Send(GameState.LocalPlayerId, IntentType.Rescue, new Dictionary<string, string> { ["targetId"] = "rescue_patient_proto" });
                _step++;
                AddLog("Rescue completed and server event recorded.");
                return;
            default:
                AddLog("Rescue prototype complete.");
                return;
        }
    }

    private void InteractKarmaBreakLoot()
    {
        switch (_step)
        {
            case 0:
                if (!IsLocalNear("break_victim_proto", 5))
                {
                    AddLog("Get closer to the break site first.");
                    return;
                }
                Send("break_victim_proto", IntentType.KarmaBreak, new Dictionary<string, string>());
                foreach (var item in _server.WorldItems.Values.Where(item => item.IsAvailable && !string.IsNullOrWhiteSpace(item.DropOwnerId)).Take(2))
                    AddBoxMarker(item.EntityId, $"Dropped\n{item.Item.Name}", item.Position, new Color(0.8f, 0.45f, 0.2f));
                _step++;
                AddLog("Karma Break dropped the victim's inventory. Walk to a dropped item and press E to loot it.");
                return;
            case 1:
                var nearbyDrop = _server.WorldItems.Values.FirstOrDefault(item => item.IsAvailable && !string.IsNullOrWhiteSpace(item.DropOwnerId) && ToTile(_player.GlobalPosition).DistanceSquaredTo(item.Position) <= 4);
                if (nearbyDrop is null)
                {
                    AddLog("Stand on one of the dropped Karma Break items first.");
                    return;
                }
                Send(GameState.LocalPlayerId, IntentType.Interact, new Dictionary<string, string> { ["entityId"] = nearbyDrop.EntityId });
                HideMarker(nearbyDrop.EntityId);
                _step++;
                AddLog("Drop looted. Press E to return a claimed item to the victim and test the return flow.");
                return;
            case 2:
                Send(GameState.LocalPlayerId, IntentType.TransferItem, new Dictionary<string, string>
                {
                    ["targetId"] = "break_victim_proto",
                    ["itemId"] = StarterItems.DataChipId,
                    ["mode"] = "gift"
                });
                _step++;
                AddLog("Returned loot flow recorded.");
                return;
            default:
                AddLog("Karma Break loot prototype complete.");
                return;
        }
    }

    private void InteractShop()
    {
        switch (_step)
        {
            case 0:
                if (ToTile(_player.GlobalPosition).DistanceSquaredTo(new TilePosition(6, 4)) > 9)
                {
                    AddLog("Walk closer to Dallen's shop marker first.");
                    return;
                }
                Send(GameState.LocalPlayerId, IntentType.PurchaseItem, new Dictionary<string, string> { ["offerId"] = StarterShopCatalog.DallenMediPatchOfferId });
                _state.LocalPlayer.ApplyDamage(20);
                _step++;
                AddLog("Medi patch purchased and you took prototype damage. Press E to use it.");
                return;
            case 1:
                Send(GameState.LocalPlayerId, IntentType.UseItem, new Dictionary<string, string> { ["itemId"] = StarterItems.MediPatchId });
                _step++;
                AddLog("Purchased item use recorded.");
                return;
            default:
                AddLog("Shop prototype complete.");
                return;
        }
    }

    private void InteractStructure()
    {
        if (ToTile(_player.GlobalPosition).DistanceSquaredTo(new TilePosition(8, 3)) > 16)
        {
            AddLog("Walk closer to the greenhouse first.");
            return;
        }

        var actions = new[] { "inspect", "repair", "sabotage", "enter", "exit" };
        if (_step >= actions.Length)
        {
            AddLog("Structure prototype complete.");
            return;
        }

        var action = actions[_step];
        Send(GameState.LocalPlayerId, IntentType.Interact, new Dictionary<string, string>
        {
            ["entityId"] = "structure_greenhouse_standard",
            ["action"] = action
        });
        _step++;
        AddLog($"Structure {action} interaction recorded. Press E for the next structure action.");
    }

    private void InteractPosse()
    {
        switch (_step)
        {
            case 0:
                if (!IsLocalNear("posse_friend_proto", 4))
                {
                    AddLog("Get closer to your posse friend first.");
                    return;
                }
                Send(GameState.LocalPlayerId, IntentType.InvitePosse, new Dictionary<string, string> { ["targetId"] = "posse_friend_proto" });
                _step++;
                AddLog("Posse invite sent. Press E to accept as the friend.");
                return;
            case 1:
                Send("posse_friend_proto", IntentType.AcceptPosse, new Dictionary<string, string>());
                _step++;
                AddLog("Friend joined the posse. Press E to send posse chat.");
                return;
            case 2:
                Send(GameState.LocalPlayerId, IntentType.SendPosseChat, new Dictionary<string, string> { ["text"] = "Prototype posse check-in." });
                _step++;
                AddLog("Posse chat recorded. Press E to leave/disband.");
                return;
            case 3:
                Send("posse_friend_proto", IntentType.LeavePosse, new Dictionary<string, string>());
                Send(GameState.LocalPlayerId, IntentType.LeavePosse, new Dictionary<string, string>());
                _step++;
                AddLog("Posse leave/disband events recorded.");
                return;
            default:
                AddLog("Posse prototype complete.");
                return;
        }
    }

    private void InteractLocalChat()
    {
        switch (_step)
        {
            case 0:
                Send(GameState.LocalPlayerId, IntentType.SendLocalChat, new Dictionary<string, string> { ["text"] = "Local prototype ping near the listener." });
                _step++;
                AddLog("Nearby local chat recorded. Move toward the far echo marker and press E again.");
                return;
            case 1:
                Send(GameState.LocalPlayerId, IntentType.SendLocalChat, new Dictionary<string, string> { ["text"] = "Far-distance local chat falloff check." });
                _step++;
                AddLog("Second local chat event recorded from your new position.");
                return;
            default:
                AddLog("Local chat prototype complete.");
                return;
        }
    }

    private void InteractMount()
    {
        switch (_step)
        {
            case 0:
                Send(GameState.LocalPlayerId, IntentType.Mount, new Dictionary<string, string> { ["mountId"] = "mount_hover_1" });
                _step++;
                AddLog("Mounted the hover scooter. Press E to attempt another mount while occupied/mounted.");
                return;
            case 1:
                Send(GameState.LocalPlayerId, IntentType.Mount, new Dictionary<string, string> { ["mountId"] = "mount_cargo_1" });
                _step++;
                AddLog("Already-mounted rejection recorded. Press E to dismount.");
                return;
            case 2:
                Send(GameState.LocalPlayerId, IntentType.Dismount, new Dictionary<string, string>());
                _step++;
                AddLog("Dismount recorded.");
                return;
            default:
                AddLog("Mount prototype complete.");
                return;
        }
    }

    private void InteractQuestDialogue()
    {
        switch (_step)
        {
            case 0:
                if (ToTile(_player.GlobalPosition).DistanceSquaredTo(new TilePosition(3, 4)) > 9)
                {
                    AddLog("Walk closer to Mara first.");
                    return;
                }
                Send(GameState.LocalPlayerId, IntentType.StartDialogue, new Dictionary<string, string> { ["npcId"] = StarterNpcs.Mara.Id });
                _step++;
                AddLog("Dialogue started with Mara. Press E to start the Clinic Filters quest.");
                return;
            case 1:
                Send(GameState.LocalPlayerId, IntentType.StartQuest, new Dictionary<string, string> { ["questId"] = StarterQuests.MaraClinicFiltersId });
                _step++;
                AddLog("Quest started. Press E to complete it with your repair kit.");
                return;
            case 2:
                Send(GameState.LocalPlayerId, IntentType.CompleteQuest, new Dictionary<string, string> { ["questId"] = StarterQuests.MaraClinicFiltersId });
                _step++;
                AddLog("Quest completed. Press E to select a Mara dialogue choice.");
                return;
            case 3:
                Send(GameState.LocalPlayerId, IntentType.SelectDialogueChoice, new Dictionary<string, string>
                {
                    ["npcId"] = StarterNpcs.Mara.Id,
                    ["choiceId"] = "help_filters"
                });
                _step++;
                AddLog("Dialogue choice recorded. Press E to start an entanglement with Mara/Dallen.");
                return;
            case 4:
                Send(GameState.LocalPlayerId, IntentType.StartEntanglement, new Dictionary<string, string>
                {
                    ["npcId"] = StarterNpcs.Mara.Id,
                    ["affectedNpcId"] = StarterNpcs.Dallen.Id,
                    ["type"] = EntanglementType.Romantic.ToString()
                });
                _step++;
                AddLog("Entanglement started. Press E to expose it as a rumor event.");
                return;
            case 5:
                var entanglementId = _state.Entanglements.All.LastOrDefault()?.Id ?? string.Empty;
                Send(GameState.LocalPlayerId, IntentType.ExposeEntanglement, new Dictionary<string, string> { ["entanglementId"] = entanglementId });
                _step++;
                AddLog("Entanglement exposure recorded.");
                return;
            default:
                AddLog("Quest/dialogue prototype complete.");
                return;
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

    private void AddPlayerMarker(string playerId, string label, Color color)
    {
        var node = new Node2D { Name = $"Marker_{playerId}" };
        node.AddChild(new PrototypeCharacterSprite { Kind = PrototypeSpriteKind.Player });
        var text = MakeLabel(label, new Vector2(-54f, -62f));
        node.AddChild(text);
        AddChild(node);
        _markers[playerId] = node;
        SyncMarkerPositions();
    }

    private void AddBoxMarker(string id, string label, TilePosition tile, Color color)
    {
        var node = new Node2D { Name = $"Marker_{id}", Position = ToWorld(tile) };
        node.AddChild(new ColorRect { Color = color, Size = new Vector2(76f, 42f), Position = new Vector2(-38f, -21f) });
        node.AddChild(MakeLabel(label, new Vector2(-48f, -44f)));
        AddChild(node);
        _markers[id] = node;
    }

    private static Label MakeLabel(string text, Vector2 position)
    {
        return new Label
        {
            Text = text,
            Position = position,
            Size = new Vector2(108f, 42f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
    }

    private void SyncMarkerPositions()
    {
        foreach (var pair in _markers)
        {
            if (_state.Players.TryGetValue(pair.Key, out var player))
                pair.Value.Position = ToWorld(player.Position);
        }
    }

    private void HideMarker(string id)
    {
        if (_markers.TryGetValue(id, out var marker))
            marker.Visible = false;
    }

    private void ClearMarkers()
    {
        foreach (var marker in _markers.Values)
            marker.QueueFree();
        _markers.Clear();
    }

    private bool IsLocalNear(string playerId, int radiusTiles)
    {
        return _state.Players.TryGetValue(playerId, out var other) &&
            ToTile(_player.GlobalPosition).DistanceSquaredTo(other.Position) <= radiusTiles * radiusTiles;
    }

    private bool IsNearLawTile(TilePosition tile)
    {
        return new[] { MaraCheckpointTile, DallenCheckpointTile }
            .Any(pos => tile.DistanceSquaredTo(pos) <= 9);
    }

    private bool IsWorldItemNearLocal(string entityId, int radiusTiles)
    {
        return _server.WorldItems.TryGetValue(entityId, out var item) &&
            item.IsAvailable &&
            ToTile(_player.GlobalPosition).DistanceSquaredTo(item.Position) <= radiusTiles * radiusTiles;
    }

    private void RestartScenario()
    {
        switch (_scenario)
        {
            case "Event Gauntlet":
                LoadEventGauntlet();
                break;
            case "Supply Drop":
                LoadSupplyDrop();
                break;
            case "Clinic Revive":
                LoadClinicRevive();
                break;
            case "Duel":
                LoadDuel();
                break;
            case "Rescue":
                LoadRescue();
                break;
            case "Karma Break Loot":
                LoadKarmaBreakLoot();
                break;
            case "Shop":
                LoadShop();
                break;
            case "Structure":
                LoadStructure();
                break;
            case "Posse":
                LoadPosse();
                break;
            case "Local Chat":
                LoadLocalChat();
                break;
            case "Mount":
                LoadMount();
                break;
            case "Quest + Dialogue":
                LoadQuestDialogue();
                break;
        }
    }

    private void AddLog(string message)
    {
        _log.Add("• " + message);
        if (_log.Count > 7)
            _log.RemoveAt(0);
    }

    private void RefreshOverlay()
    {
        if (_objectiveLabel is null || _server is null)
            return;
        var snapshot = _server.CreateInterestSnapshot(GameState.LocalPlayerId);
        var summary = snapshot.MatchSummary is null
            ? "Match summary: not finished yet"
            : "Summary: " + string.Join(", ", snapshot.MatchSummary.Players.Select(p => $"{p.DisplayName} {p.FinalKarma}"));
        _objectiveLabel.Text =
            $"Scenario: {_scenario}\n" +
            "Controls: WASD move, Shift sprint, mouse wheel zoom, E interact\n" +
            $"Match: {_server.Match.Status} | Events: {_server.EventLog.Count}\n" +
            summary;
        var recentEvents = string.Join("\n", _server.EventLog.TakeLast(4).Select(e => $"- {e.EventId}: {e.Description}"));
        _logLabel.Text = string.Join("\n", _log) + "\n\nRecent server events:\n" + recentEvents;
    }

    private static TilePosition ToTile(Vector2 worldPosition)
    {
        return new TilePosition(Mathf.RoundToInt(worldPosition.X / TileSize), Mathf.RoundToInt(worldPosition.Y / TileSize));
    }

    private static Vector2 ToWorld(TilePosition tile)
    {
        return new Vector2(tile.X * TileSize, tile.Y * TileSize);
    }
}
