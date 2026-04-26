using System;
using System.Linq;
using Godot;
using Karma.Core;
using Karma.Data;
using Karma.Net;
using Karma.World;

namespace Karma.Tests;

public partial class GameplaySmokeTest : Node
{
    private int _failures;

    public override void _Ready()
    {
        Run();
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Run()
    {
        var state = GetNode<GameState>("/root/GameState");
        state.TriggerKarmaBreak();
        var localSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        ExpectTrue(localSession is not null, "prototype server session autoload is available");
        ExpectTrue(localSession.LastLocalSnapshot.Summary.Contains("visible"), "prototype server session exposes local interest snapshot");
        var localMatchRemaining = localSession.LastLocalSnapshot.Match.RemainingSeconds;
        localSession.AdvanceMatchTime(5);
        ExpectEqual(localMatchRemaining - 5, localSession.LastLocalSnapshot.Match.RemainingSeconds, "prototype server session advances match timer");
        localSession.RegisterWorldItem("session_test_item", StarterItems.DeflatedBalloon, TilePosition.Origin);
        ExpectTrue(
            localSession.LastLocalSnapshot.WorldItems.Any(entity => entity.EntityId == "session_test_item"),
            "prototype server session refreshes local snapshot after world item registration");
        var previousSessionTick = localSession.LastLocalSnapshot.Tick;
        localSession.SetTileMap(WorldGenerator.Generate(WorldConfig.CreatePrototype()).TileMap);
        ExpectTrue(localSession.LastLocalSnapshot.MapChunks.Any(), "prototype server session exposes local map chunks");
        var peerMove = localSession.Send(
            "peer_stand_in",
            IntentType.Move,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["x"] = "1",
                ["y"] = "0"
            });
        ExpectTrue(peerMove.WasAccepted, "prototype server session accepts stand-in player intents");
        ExpectTrue(
            localSession.LastLocalSnapshot.Tick > previousSessionTick,
            "prototype server session refreshes local snapshot after accepted stand-in intent");
        state.SetPlayerPosition("peer_stand_in", TilePosition.Origin);
        var server = new AuthoritativeWorldServer(state, "test-world");
        ServerConfig.Prototype4Player.Validate();
        ServerConfig.Large100Player.Validate();
        ExpectEqual(4, ServerConfig.Prototype4Player.MaxPlayers, "prototype server profile supports 4 players");
        ExpectEqual(100, ServerConfig.Large100Player.MaxPlayers, "large server profile supports 100 players");
        ExpectEqual(32, ServerConfig.Large100Player.ChunkSizeTiles, "large server profile uses chunked world tiles");
        ExpectEqual(30 * 60, ServerConfig.Prototype4Player.MatchDurationSeconds, "prototype server profile uses 30 minute matches");
        var matchServer = new AuthoritativeWorldServer(state, "match-test-world");
        ExpectEqual(MatchStatus.Running, matchServer.Match.Status, "new server match starts running");
        ExpectEqual(30 * 60, matchServer.Match.RemainingSeconds, "new server match starts with full duration remaining");
        ExpectEqual("rival_paragon", matchServer.Match.CurrentSaintId, "running match snapshot exposes current Saint leader");
        ExpectEqual("rival_renegade", matchServer.Match.CurrentScourgeId, "running match snapshot exposes current Scourge leader");
        matchServer.AdvanceMatchTime((30 * 60) - 1);
        ExpectEqual(MatchStatus.Running, matchServer.Match.Status, "match stays running before timer expires");
        matchServer.AdvanceMatchTime(1);
        ExpectEqual(MatchStatus.Finished, matchServer.Match.Status, "match finishes when timer expires");
        ExpectEqual("rival_paragon", matchServer.Match.SaintWinnerId, "finished match locks current Saint winner");
        ExpectEqual("rival_renegade", matchServer.Match.ScourgeWinnerId, "finished match locks current Scourge winner");
        ExpectTrue(matchServer.Match.Summary.Contains("Match complete"), "finished match summary reports completion");
        ExpectTrue(matchServer.EventLog.Any(serverEvent => serverEvent.EventId.Contains("match_finished")), "finished match emits server event");
        ExpectEqual("rival_paragon", matchServer.CreateInterestSnapshot(GameState.LocalPlayerId).Match.SaintWinnerId, "interest snapshot includes Saint match winner");
        var karmaBeforePostMatchIntent = state.LocalKarma.Score;
        var postMatchScoreIntent = matchServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            1,
            IntentType.KarmaAction,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["action"] = PrototypeActions.HelpPeerId
            }));
        ExpectFalse(postMatchScoreIntent.WasAccepted, "finished match rejects score-changing intents");
        ExpectEqual(karmaBeforePostMatchIntent, state.LocalKarma.Score, "rejected post-match score intent does not mutate karma");
        var postMatchMove = matchServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            1,
            IntentType.Move,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["x"] = "2",
                ["y"] = "2"
            }));
        ExpectTrue(postMatchMove.WasAccepted, "finished match still allows movement");
        ExpectEqual(4, server.ConnectedPlayerIds.Count, "prototype server starts with four connected player slots");
        ExpectFalse(server.JoinPlayer("overflow_player", "Overflow Player").WasAccepted, "prototype server rejects players beyond capacity");
        ExpectEqual(1000, WorldConfig.FromServerConfig(
            "scale-test",
            new WorldSeed(1, "Scale Test", "test"),
            ServerConfig.Large100Player).WidthTiles, "large world profile expands map size");
        var generatedA = WorldGenerator.Generate(WorldConfig.CreatePrototype());
        var generatedB = WorldGenerator.Generate(WorldConfig.CreatePrototype());
        ExpectEqual(generatedA.Summary, generatedB.Summary, "world generation is deterministic for the same seed");
        ExpectEqual(
            generatedA.Config.WidthTiles * generatedA.Config.HeightTiles,
            generatedA.TileMap.Tiles.Count,
            "world generation creates a logical tile for every coordinate");
        ExpectEqual(2, generatedA.TileMap.ChunkColumns, "prototype tile map exposes chunk columns");
        ExpectEqual(2, generatedA.TileMap.ChunkRows, "prototype tile map exposes chunk rows");
        ExpectEqual(
            new GeneratedChunkCoordinate(0, 0),
            generatedA.TileMap.GetChunkCoordinateForTile(3, 3),
            "tile map resolves tile coordinates to chunk coordinates");
        ExpectEqual(32 * 32, generatedA.TileMap.GetChunk(new GeneratedChunkCoordinate(0, 0)).Tiles.Count, "tile map can materialize one chunk");
        ExpectEqual(4, generatedA.TileMap.GetChunksAround(32, 32, radiusChunks: 1).Count, "tile map can query nearby chunks");
        ExpectEqual(WorldTileIds.ClinicFloor, generatedA.TileMap.Get(3, 3).FloorId, "world generation assigns starter clinic floor tiles");
        ExpectEqual(WorldTileIds.WallMetal, generatedA.TileMap.Get(2, 2).StructureId, "world generation assigns starter clinic wall structures");
        ExpectEqual(WorldTileIds.DoorAirlock, generatedA.TileMap.Get(5, 7).StructureId, "world generation assigns starter clinic door structure");
        ExpectTrue(
            generatedA.TileMap.Tiles.Any(tile => tile.ZoneId == "duel_ring" && tile.FloorId == WorldTileIds.DuelRingFloor),
            "world generation assigns logical duel ring tiles");
        var artSet = ThemeArtRegistry.GetForTheme(generatedA.Theme);
        ExpectTrue(artSet.Tiles.ContainsKey(WorldTileIds.ClinicFloor), "theme art registry maps clinic floor tile id");
        ExpectEqual(
            ThemeArtRegistry.PlaceholderAtlasPath,
            artSet.GetTile(WorldTileIds.DoorAirlock).AtlasPath,
            "theme art registry keeps future atlas path for tile ids");
        ExpectEqual(5, generatedA.Locations.Count, "small world generates prototype location count");
        ExpectEqual(12, generatedA.Npcs.Count, "prototype target players generate starter NPC population");
        ExpectTrue(generatedA.Oddities.Any(item => item.Id == StarterItems.DeflatedBalloonId), "generated world includes absurd oddities");
        var proposal = new WorldContentProposal(
            "junkyard-fantasy",
            new[]
            {
                new NpcProposal(
                    "generated_npc_test",
                    "Test Wrenchley",
                    "Bolt Poet",
                    "earnest, dramatic",
                    "Civic Repair Guild",
                    "needs a ceremonial wrench",
                    "secretly replaced the town bell")
            },
            new[]
            {
                new QuestProposal(
                    "generated_quest_test",
                    "Bell Trouble",
                    "generated_npc_test",
                    "Find out why the bell sounds like a spoon.",
                    new[] { StarterItems.RepairKitId },
                    PrototypeActions.HelpMaraId)
            },
            new[] { StarterItems.DeflatedBalloonId },
            new[]
            {
                new FactionProposal(
                    "spoon_union",
                    "Spoon Union",
                    "An alarmingly organized group of utensil loyalists.")
            });
        ExpectTrue(WorldContentProposalValidator.Validate(proposal).IsValid, "valid LLM-style proposal passes validation");
        var appliedProposal = WorldContentProposalValidator.ApplyToGeneratedWorld(generatedA.ToAdapter(), proposal);
        ExpectTrue(appliedProposal.WasApplied, "valid proposal can be applied to generated world adapter");
        ExpectTrue(appliedProposal.World.Npcs.Any(npc => npc.Id == "generated_npc_test"), "applied proposal adds generated NPC");
        ExpectTrue(appliedProposal.World.Factions.Any(faction => faction.Id == "spoon_union"), "applied proposal adds generated faction");
        var badProposal = proposal with
        {
            Quests = new[]
            {
                new QuestProposal(
                    "bad_quest",
                    "Impossible Favor",
                    "missing_npc",
                    "This should be rejected.",
                    new[] { "missing_item" },
                    "missing_action")
            }
        };
        ExpectFalse(WorldContentProposalValidator.Validate(badProposal).IsValid, "invalid LLM-style proposal is rejected");

        ExpectEqual(0, state.LocalKarma.Score, "new players start at neutral karma");
        ExpectEqual("Unmarked", state.LocalKarma.TierName, "new players start unmarked");
        ExpectEqual(4, state.Players.Count, "prototype world registers four player slots");
        ExpectEqual("Helpful Rival", state.GetLeaderboardStanding().SaintName, "positive rival starts as Saint");
        ExpectEqual("Shady Rival", state.GetLeaderboardStanding().ScourgeName, "negative rival starts as Scourge");
        for (var i = 0; i < 10; i++)
        {
            state.ApplyLocalShift(new KarmaAction(
                GameState.LocalPlayerId,
                StarterNpcs.Mara.Id,
                new[] { "helpful" },
                "You did a suspicious amount of helpful chores.",
                BaseMagnitude: 40));
        }
        ExpectTrue(state.LocalKarma.Score > 100, "karma can rise above old positive cap");
        ExpectEqual("Exalted 2", state.LocalKarma.TierName, "high positive karma gains infinite Exalted rank");
        ExpectEqual("Progress: 0/100 toward Exalted 3", state.LocalKarma.RankProgress.Summary, "high positive karma shows progress toward next Exalted rank");
        ExpectTrue(state.LocalPerks.Any(perk => perk.Name == "Exalted 2"), "repeat Exalted ranks unlock repeat ascension perks");
        state.TriggerKarmaBreak();
        ExpectEqual(QuestStatus.Available, state.Quests.Get(StarterQuests.MaraClinicFiltersId).Status, "starter quest begins available");

        state.AddItem(StarterItems.WhoopieCushion);
        ExpectTrue(state.HasItem(StarterItems.WhoopieCushionId), "whoopie cushion can be picked up");
        ExpectTrue(state.ConsumeItem(StarterItems.WhoopieCushionId), "whoopie cushion can be consumed");
        ExpectFalse(state.HasItem(StarterItems.WhoopieCushionId), "consumed whoopie cushion leaves inventory");
        var questServer = new AuthoritativeWorldServer(state, "quest-test-world");
        var serverStartQuest = questServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            1,
            IntentType.StartQuest,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["questId"] = StarterQuests.MaraClinicFiltersId
            }));
        ExpectTrue(serverStartQuest.WasAccepted, "server starts visible NPC quest");
        ExpectTrue(state.WorldEvents.Events.Any(worldEvent => worldEvent.Type == WorldEventType.Quest), "quest start records world event");
        ExpectEqual(QuestStatus.Active, state.Quests.Get(StarterQuests.MaraClinicFiltersId).Status, "started quest becomes active");
        ExpectFalse(questServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            2,
            IntentType.CompleteQuest,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["questId"] = StarterQuests.MaraClinicFiltersId
            })).WasAccepted, "server rejects quest completion without required item");
        state.AddItem(StarterItems.RepairKit);
        var serverCompleteQuest = questServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            3,
            IntentType.CompleteQuest,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["questId"] = StarterQuests.MaraClinicFiltersId
            }));
        ExpectTrue(serverCompleteQuest.WasAccepted, "server completes quest with required item");
        ExpectEqual(QuestStatus.Completed, state.Quests.Get(StarterQuests.MaraClinicFiltersId).Status, "completed quest is marked completed");
        ExpectTrue(serverCompleteQuest.Event.EventId.Contains("quest_completed"), "server quest completion emits quest event");

        var helpMara = state.ApplyLocalShift(PrototypeActions.HelpMara());
        ExpectTrue(helpMara.Amount > 0, "helping Mara ascends karma");
        ExpectTrue(state.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId) > 0, "helping Mara improves Mara relationship");
        ExpectTrue(state.Factions.GetReputation(StarterFactions.FreeSettlersId, GameState.LocalPlayerId) > 0, "helping Mara improves Free Settlers faction reputation");

        var scoreAfterHelp = state.LocalKarma.Score;
        var opinionBeforePrank = state.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId);
        var prankMara = state.ApplyLocalShift(PrototypeActions.WhoopieCushionMara());
        ExpectTrue(prankMara.Amount < 0, "whoopie cushion prank descends karma");
        ExpectTrue(state.LocalKarma.Score < scoreAfterHelp, "prank reduces current karma score");
        ExpectTrue(
            state.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId) < opinionBeforePrank,
            "humiliating Mara damages Mara relationship");

        state.TriggerKarmaBreak();
        ExpectEqual(0, state.LocalKarma.Score, "Karma Break resets karma score");
        ExpectEqual(KarmaDirection.Neutral, state.LocalKarma.Path, "Karma Break resets path");

        var peerHelp = state.ApplyLocalShift(PrototypeActions.HelpPeer());
        ExpectTrue(peerHelp.Amount > 0, "helping a player ascends karma");
        ExpectEqual("You", state.GetLeaderboardStanding().SaintName, "local player can take Saint standing");
        ExpectTrue(state.LocalPerks.Any(perk => perk.Name == "Trusted Discount"), "positive karma unlocks ascension perks");
        ExpectTrue(state.LocalPerks.Any(perk => perk.Name == "Saint"), "highest positive player gets Saint standing perk");

        var peerAttack = state.ApplyLocalShift(PrototypeActions.AttackPeer());
        ExpectTrue(peerAttack.Amount < 0, "attacking a player outside a duel descends karma");
        state.AddItem("peer_stand_in", StarterItems.WorkVest);
        ExpectTrue(state.EquipPlayer("peer_stand_in", StarterItems.WorkVestId), "players can equip armor");
        ExpectEqual(10, state.Players["peer_stand_in"].Defense, "equipped armor adds defense");
        ExpectFalse(state.DamagePlayer(GameState.LocalPlayerId, "peer_stand_in", 35, "test strike"), "non-lethal damage does not trigger Karma Break");
        ExpectTrue(state.WorldEvents.Events.Any(worldEvent => worldEvent.Type == WorldEventType.Combat), "combat damage records world event");
        ExpectEqual(75, state.Players["peer_stand_in"].Health, "armor reduces incoming damage");
        ExpectTrue(state.DamagePlayer(GameState.LocalPlayerId, "peer_stand_in", 100, "test lethal strike"), "lethal damage triggers Karma Break");
        ExpectEqual(100, state.Players["peer_stand_in"].Health, "Karma Break restores dead player's health");
        state.AddItem(StarterItems.PracticeStick);
        ExpectTrue(state.HasItem(GameState.LocalPlayerId, StarterItems.PracticeStickId), "local player inventory stores picked up weapon");
        ExpectTrue(state.EquipPlayer(GameState.LocalPlayerId, StarterItems.PracticeStickId), "players can equip weapons");
        ExpectEqual(10, state.LocalPlayer.AttackPower, "equipped weapon changes attack power");
        ExpectFalse(state.HasItem(StarterItems.PracticeStickId), "equipping local weapon consumes inventory item");

        state.TriggerKarmaBreak();
        var balloonGift = state.ApplyLocalShift(PrototypeActions.GiftBalloonToMara());
        ExpectTrue(balloonGift.Amount > 0, "sincere balloon gift ascends karma");

        var balloonMock = state.ApplyLocalShift(PrototypeActions.MockMaraWithBalloon());
        ExpectTrue(balloonMock.Amount < 0, "cruel balloon use descends karma");
        var dallenOpinionBefore = state.Relationships.GetOpinion(StarterNpcs.Dallen.Id, GameState.LocalPlayerId);
        var factionBeforeBetrayal = state.Factions.GetReputation(StarterFactions.FreeSettlersId, GameState.LocalPlayerId);
        ExpectTrue(state.StartEntanglement(
            GameState.LocalPlayerId,
            StarterNpcs.Mara.Id,
            StarterNpcs.Dallen.Id,
            EntanglementType.Romantic,
            PrototypeActions.StartMaraEntanglement()), "secret entanglement can be started");
        ExpectEqual(1, state.Entanglements.All.Count, "entanglement is tracked as world state");
        ExpectTrue(state.LocalKarma.Score < 0, "betrayal entanglement descends karma");
        ExpectTrue(
            state.Relationships.GetOpinion(StarterNpcs.Dallen.Id, GameState.LocalPlayerId) < dallenOpinionBefore,
            "entanglement damages affected NPC relationship");
        ExpectTrue(
            state.Factions.GetReputation(StarterFactions.FreeSettlersId, GameState.LocalPlayerId) < factionBeforeBetrayal,
            "betrayal damages faction reputation");
        ExpectFalse(state.StartEntanglement(
            GameState.LocalPlayerId,
            StarterNpcs.Mara.Id,
            StarterNpcs.Dallen.Id,
            EntanglementType.Romantic,
            PrototypeActions.StartMaraEntanglement()), "duplicate active entanglement is rejected");
        var entanglement = state.Entanglements.All.Single();
        var maraOpinionBeforeExposure = state.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId);
        ExpectTrue(state.ExposeEntanglement(
            GameState.LocalPlayerId,
            entanglement.Id,
            PrototypeActions.ExposeMaraEntanglement()), "active entanglement can be exposed");
        ExpectEqual(EntanglementStatus.Exposed, state.Entanglements.Get(entanglement.Id).Status, "exposed entanglement status is tracked");
        ExpectTrue(state.WorldEvents.Events.Any(worldEvent => worldEvent.Type == WorldEventType.Rumor), "exposing entanglement records a rumor event");
        ExpectTrue(
            state.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId) < maraOpinionBeforeExposure,
            "exposing entanglement damages primary NPC relationship");

        state.ApplyLocalShift(PrototypeActions.AttackPeer());
        ExpectTrue(state.LocalPerks.Any(perk => perk.Name == "Shifty Prices"), "negative karma unlocks descension perks");
        for (var i = 0; i < 8; i++)
        {
            state.ApplyLocalShift(new KarmaAction(
                GameState.LocalPlayerId,
                "peer_stand_in",
                new[] { "violent" },
                "You committed to a truly terrible streak.",
                BaseMagnitude: 40));
        }
        ExpectTrue(state.LocalKarma.Score < -100, "karma can fall below old negative cap");
        ExpectEqual("Abyssal 2", state.LocalKarma.TierName, "low negative karma gains infinite Abyssal rank");
        ExpectEqual("Progress: 3/100 toward Abyssal 3", state.LocalKarma.RankProgress.Summary, "low negative karma shows progress toward next Abyssal rank");
        ExpectTrue(state.LocalPerks.Any(perk => perk.Name == "Abyssal 2"), "repeat Abyssal ranks unlock repeat descension perks");
        ExpectEqual("You", state.GetLeaderboardStanding().ScourgeName, "lowest negative player gets Scourge standing");

        state.TriggerKarmaBreak();
        state.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(0, 0));
        state.SetPlayerPosition("peer_stand_in", new TilePosition(1, 0));
        var transferServer = new AuthoritativeWorldServer(state, "transfer-test-world");
        var stealTransfer = transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            1,
            IntentType.TransferItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in",
                ["itemId"] = StarterItems.RepairKitId,
                ["mode"] = "steal"
            }));
        ExpectTrue(stealTransfer.WasAccepted, "server transfers stolen player item");
        ExpectTrue(state.HasItem(GameState.LocalPlayerId, StarterItems.RepairKitId), "stolen item enters actor inventory");
        ExpectFalse(state.HasItem("peer_stand_in", StarterItems.RepairKitId), "stolen item leaves target inventory");
        var returnTransfer = transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            2,
            IntentType.TransferItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in",
                ["itemId"] = StarterItems.RepairKitId,
                ["mode"] = "gift"
            }));
        ExpectTrue(returnTransfer.WasAccepted, "server transfers gifted player item");
        ExpectFalse(state.HasItem(GameState.LocalPlayerId, StarterItems.RepairKitId), "gifted item leaves actor inventory");
        ExpectTrue(state.HasItem("peer_stand_in", StarterItems.RepairKitId), "gifted item returns to target inventory");
        ExpectTrue(returnTransfer.Event.EventId.Contains("item_transferred"), "item transfer emits server event");
        state.AddItem("peer_stand_in", StarterItems.WhoopieCushion);
        var peerKarmaBreak = transferServer.ProcessIntent(new ServerIntent(
            "peer_stand_in",
            1,
            IntentType.KarmaBreak,
            new System.Collections.Generic.Dictionary<string, string>()));
        ExpectTrue(peerKarmaBreak.WasAccepted, "server accepts peer Karma Break intent");
        ExpectFalse(state.HasItem("peer_stand_in", StarterItems.WhoopieCushionId), "Karma Break drains loose inventory");
        ExpectTrue(
            transferServer.WorldItems.Values.Any(entity => entity.EntityId.StartsWith("drop_peer_stand_in") && entity.Item.Id == StarterItems.WhoopieCushionId),
            "Karma Break drops loose inventory as world items");
        ExpectTrue(peerKarmaBreak.Event.Data["droppedItemCount"] != "0", "Karma Break event reports dropped items");
        var peerDropId = transferServer.WorldItems.Values
            .First(entity => entity.EntityId.StartsWith("drop_peer_stand_in") && entity.Item.Id == StarterItems.WhoopieCushionId)
            .EntityId;
        var karmaBeforeDropPickup = state.LocalKarma.Score;
        var dropPickup = transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            3,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = peerDropId
            }));
        ExpectTrue(dropPickup.WasAccepted, "server accepts pickup of another player's death drop");
        ExpectTrue(state.HasItem(GameState.LocalPlayerId, StarterItems.WhoopieCushionId), "death drop pickup enters inventory");
        ExpectTrue(state.LocalKarma.Score < karmaBeforeDropPickup, "claiming another player's death drop descends karma");
        ExpectEqual("peer_stand_in", dropPickup.Event.Data["dropOwnerId"], "death drop pickup event reports owner");
        ExpectFalse(transferServer.GetInterestFor(GameState.LocalPlayerId).VisibleEntityIds.Contains(peerDropId), "picked up death drop leaves interest set");
        state.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(0, 0));
        state.SetPlayerPosition("peer_stand_in", new TilePosition(1, 0));
        var duelServer = new AuthoritativeWorldServer(state, "duel-test-world");
        var requestDuel = duelServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            1,
            IntentType.RequestDuel,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in"
            }));
        ExpectTrue(requestDuel.WasAccepted, "server accepts nearby duel request");
        var acceptDuel = duelServer.ProcessIntent(new ServerIntent(
            "peer_stand_in",
            1,
            IntentType.AcceptDuel,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["challengerId"] = GameState.LocalPlayerId
            }));
        ExpectTrue(acceptDuel.WasAccepted, "server accepts matching duel acceptance");
        ExpectTrue(state.Duels.IsActive(GameState.LocalPlayerId, "peer_stand_in"), "accepted duel becomes active");
        state.SetPlayerPosition("rival_renegade", new TilePosition(80, 80));
        ExpectTrue(
            duelServer.CreateInterestSnapshot(GameState.LocalPlayerId).Duels.Any(duel => duel.Status == DuelStatus.Active),
            "interest snapshot includes visible active duel state");
        ExpectFalse(
            duelServer.CreateInterestSnapshot("rival_renegade").Duels.Any(),
            "interest snapshot hides distant duel state");
        var karmaBeforeDuelAttack = state.LocalKarma.Score;
        var duelAttack = duelServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            2,
            IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in"
            }));
        ExpectTrue(duelAttack.WasAccepted, "server accepts attack during active duel");
        ExpectEqual(karmaBeforeDuelAttack, state.LocalKarma.Score, "accepted duel attack does not descend karma");
        ExpectEqual("True", duelAttack.Event.Data["duel"], "duel attack event is marked as duel combat");
        state.TriggerKarmaBreak();
        ExpectFalse(state.Duels.IsActive(GameState.LocalPlayerId, "peer_stand_in"), "Karma Break ends active duels");
        state.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(0, 0));
        state.SetPlayerPosition("peer_stand_in", new TilePosition(4, 4));
        state.SetPlayerPosition("rival_paragon", new TilePosition(80, 80));
        state.SetPlayerPosition("rival_renegade", new TilePosition(-80, -80));
        server.SeedWorldItem("pickup_practice_stick", StarterItems.PracticeStick, new TilePosition(3, 5));
        var serverMove = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            1,
            IntentType.Move,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["x"] = "3",
                ["y"] = "4"
            }));
        ExpectTrue(serverMove.WasAccepted, "server accepts sequenced move intent");
        ExpectEqual(new TilePosition(3, 4), state.LocalPlayer.Position, "accepted move intent updates authoritative position");
        var localInterest = server.GetInterestFor(GameState.LocalPlayerId);
        ExpectTrue(localInterest.VisiblePlayerIds.Contains("peer_stand_in"), "interest area includes nearby players");
        ExpectFalse(localInterest.VisiblePlayerIds.Contains("rival_paragon"), "interest area excludes distant players");
        ExpectTrue(localInterest.VisibleEntityIds.Contains("pickup_practice_stick"), "interest area includes nearby pickup entities");

        var serverHelp = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            2,
            IntentType.KarmaAction,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["action"] = PrototypeActions.HelpPeerId
            }));
        ExpectTrue(serverHelp.WasAccepted, "server accepts sequenced karma action intent");
        ExpectTrue(state.LocalKarma.Score > 0, "accepted server intent mutates authoritative state");
        ExpectEqual(2, server.EventLog.Count, "server records accepted move and karma intent events");
        var interestSnapshot = server.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectEqual(2, interestSnapshot.Players.Count, "interest snapshot includes self and nearby players");
        ExpectEqual(MatchStatus.Running, interestSnapshot.Match.Status, "interest snapshot includes match status");
        ExpectTrue(interestSnapshot.Players.Any(player => player.Id == GameState.LocalPlayerId), "interest snapshot includes local player");
        ExpectTrue(interestSnapshot.Players.Any(player => player.Id == "peer_stand_in"), "interest snapshot includes nearby peer");
        ExpectFalse(interestSnapshot.Players.Any(player => player.Id == "rival_paragon"), "interest snapshot excludes distant rival");
        ExpectTrue(interestSnapshot.Npcs.Any(npc => npc.Id == StarterNpcs.Mara.Id), "interest snapshot includes visible NPCs");
        server.SetTileMap(generatedA.TileMap);
        var mapChunkSnapshot = server.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(mapChunkSnapshot.MapChunks.Any(chunk => chunk.Tiles.Any(tile => tile.FloorId == WorldTileIds.ClinicFloor)), "interest snapshot includes nearby map chunk tiles");
        ExpectTrue(interestSnapshot.Dialogues.Any(dialogue => dialogue.NpcId == StarterNpcs.Mara.Id), "interest snapshot includes visible NPC dialogue");
        ExpectTrue(
            interestSnapshot.Dialogues.Any(dialogue => dialogue.Choices.Any(choice => choice.Id == "help_filters")),
            "interest snapshot includes server-approved dialogue choices");
        ExpectTrue(interestSnapshot.Quests.Any(quest => quest.Id == StarterQuests.MaraClinicFiltersId), "interest snapshot includes visible NPC quests");
        ExpectTrue(interestSnapshot.WorldItems.Any(entity => entity.EntityId == "pickup_practice_stick"), "interest snapshot includes visible item entity");
        ExpectTrue(interestSnapshot.WorldItems.Any(entity => entity.ItemId == StarterItems.PracticeStickId && entity.TileX == 3 && entity.TileY == 5), "interest snapshot includes item render data");
        ExpectTrue(interestSnapshot.ServerEvents.Any(serverEvent => serverEvent.EventId.Contains("player_moved")), "interest snapshot includes visible movement events");
        ExpectTrue(interestSnapshot.ServerEvents.Any(serverEvent => serverEvent.EventId.Contains("karma_shift")), "interest snapshot includes visible karma events");
        ExpectTrue(interestSnapshot.WorldEvents.Any(worldEvent => worldEvent.SourcePlayerId == GameState.LocalPlayerId), "interest snapshot includes visible world events");
        ExpectEqual("You", interestSnapshot.Leaderboard.SaintName, "interest snapshot carries global leaderboard");
        var distantNpcSnapshot = server.CreateInterestSnapshot("rival_paragon");
        ExpectFalse(distantNpcSnapshot.Dialogues.Any(), "interest snapshot hides distant NPC dialogue");
        ExpectFalse(distantNpcSnapshot.Quests.Any(), "interest snapshot hides distant NPC quests");

        state.AddItem(StarterItems.DeflatedBalloon);
        var serverPlace = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            3,
            IntentType.PlaceObject,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = StarterItems.DeflatedBalloonId,
                ["x"] = "4",
                ["y"] = "4"
            }));
        ExpectTrue(serverPlace.WasAccepted, "server accepts nearby place object intent");
        ExpectFalse(state.HasItem(GameState.LocalPlayerId, StarterItems.DeflatedBalloonId), "placing object consumes player inventory item");
        var placedEntityId = serverPlace.Event.Data["entityId"];
        ExpectTrue(server.GetInterestFor(GameState.LocalPlayerId).VisibleEntityIds.Contains(placedEntityId), "placed object enters interest set");
        var placedSnapshot = server.CreateInterestSnapshot(GameState.LocalPlayerId, afterTick: 2);
        ExpectTrue(placedSnapshot.WorldItems.Any(entity => entity.EntityId == placedEntityId && entity.ItemId == StarterItems.DeflatedBalloonId), "interest snapshot includes placed object render data");
        ExpectTrue(placedSnapshot.ServerEvents.Any(serverEvent => serverEvent.EventId.Contains("item_placed")), "interest snapshot includes visible place object events");
        var peerPickupPlaced = server.ProcessIntent(new ServerIntent(
            "peer_stand_in",
            1,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = placedEntityId
            }));
        ExpectTrue(peerPickupPlaced.WasAccepted, "server accepts pickup of placed object");
        ExpectTrue(state.HasItem("peer_stand_in", StarterItems.DeflatedBalloonId), "picked up placed object enters player inventory");
        ExpectFalse(server.GetInterestFor(GameState.LocalPlayerId).VisibleEntityIds.Contains(placedEntityId), "picked up placed object leaves interest set");

        var serverPickup = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            4,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "pickup_practice_stick"
            }));
        ExpectTrue(serverPickup.WasAccepted, "server accepts nearby pickup intent");
        ExpectTrue(state.HasItem(GameState.LocalPlayerId, StarterItems.PracticeStickId), "server pickup adds item to player inventory");
        ExpectFalse(server.GetInterestFor(GameState.LocalPlayerId).VisibleEntityIds.Contains("pickup_practice_stick"), "picked up entity leaves interest set");
        ExpectFalse(server.CreateInterestSnapshot(GameState.LocalPlayerId).WorldItems.Any(entity => entity.EntityId == "pickup_practice_stick"), "picked up entity leaves interest snapshot");
        ExpectFalse(server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            5,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "pickup_practice_stick"
            })).WasAccepted, "server rejects duplicate pickup intent");

        var serverEquip = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            6,
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = StarterItems.PracticeStickId
            }));
        ExpectTrue(serverEquip.WasAccepted, "server accepts equippable item intent");
        ExpectEqual(10, state.LocalPlayer.AttackPower, "server item intent equips weapon");
        ExpectTrue(serverEquip.Event.EventId.Contains("item_equipped"), "server item intent emits equipment event");
        ExpectFalse(server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            7,
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = "missing_item"
            })).WasAccepted, "server rejects unknown item intent");

        var peerHealthBeforeAttack = state.Players["peer_stand_in"].Health;
        var serverAttack = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            8,
            IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in"
            }));
        ExpectTrue(serverAttack.WasAccepted, "server accepts in-range attack intent");
        ExpectTrue(state.Players["peer_stand_in"].Health < peerHealthBeforeAttack, "server attack damages target");
        ExpectTrue(state.LocalKarma.Score < 12, "server attack descends attacker karma");
        ExpectTrue(serverAttack.Event.EventId.Contains("player_attacked"), "server attack emits combat event");
        var postAttackSnapshot = server.CreateInterestSnapshot(GameState.LocalPlayerId, afterTick: 2);
        ExpectTrue(postAttackSnapshot.ServerEvents.Any(serverEvent => serverEvent.EventId.Contains("item_placed")), "interest snapshot includes visible placement events");
        ExpectTrue(postAttackSnapshot.ServerEvents.Any(serverEvent => serverEvent.EventId.Contains("item_picked_up")), "interest snapshot includes visible pickup events");
        ExpectTrue(postAttackSnapshot.ServerEvents.Any(serverEvent => serverEvent.EventId.Contains("item_equipped")), "interest snapshot includes visible equipment events");
        ExpectTrue(postAttackSnapshot.ServerEvents.Any(serverEvent => serverEvent.EventId.Contains("player_attacked")), "interest snapshot includes visible attack events");
        ExpectTrue(postAttackSnapshot.WorldEvents.Any(worldEvent => worldEvent.Type == WorldEventType.Combat), "interest snapshot includes visible combat world events");

        var distantRivalHelp = server.ProcessIntent(new ServerIntent(
            "rival_renegade",
            1,
            IntentType.KarmaAction,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["action"] = PrototypeActions.HelpMaraId
            }));
        ExpectTrue(distantRivalHelp.WasAccepted, "server accepts distant player intent");
        var distantFilteredSnapshot = server.CreateInterestSnapshot(GameState.LocalPlayerId, afterTick: 2);
        ExpectFalse(distantFilteredSnapshot.ServerEvents.Any(serverEvent => serverEvent.Data["playerId"] == "rival_renegade"), "interest snapshot hides distant server events");
        ExpectFalse(distantFilteredSnapshot.WorldEvents.Any(worldEvent => worldEvent.SourcePlayerId == "rival_renegade"), "interest snapshot hides distant world events");

        var staleIntent = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            8,
            IntentType.KarmaAction,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["action"] = PrototypeActions.AttackPeerId
            }));
        ExpectFalse(staleIntent.WasAccepted, "server rejects duplicate sequence intent");
        ExpectTrue(state.LocalKarma.Score > 0, "rejected stale intent does not mutate karma");
        var deltaInterestSnapshot = server.CreateInterestSnapshot(GameState.LocalPlayerId, afterTick: 2);
        ExpectEqual(8, deltaInterestSnapshot.ServerEvents.Count, "interest snapshot can return visible events after a tick");
        ExpectFalse(server.ProcessIntent(new ServerIntent(
            "rival_paragon",
            1,
            IntentType.KarmaAction,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["action"] = PrototypeActions.HelpPeerId
            })).WasAccepted, "server rejects out-of-range player-targeted karma action");
        var maraDialogue = server.GetDialogueFor(GameState.LocalPlayerId, StarterNpcs.Mara.Id);
        ExpectTrue(maraDialogue.Choices.Any(choice => choice.ActionId == PrototypeActions.HelpMaraId), "server dialogue exposes approved NPC choices");
        var startDialogue = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            9,
            IntentType.StartDialogue,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Mara.Id
            }));
        ExpectTrue(startDialogue.WasAccepted, "server accepts visible NPC dialogue intent");
        ExpectTrue(startDialogue.Event.Data["choiceIds"].Contains("help_filters"), "dialogue event includes approved choice ids");
        var karmaBeforeDialogueChoice = state.LocalKarma.Score;
        var selectDialogueChoice = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            10,
            IntentType.SelectDialogueChoice,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Mara.Id,
                ["choiceId"] = "help_filters"
            }));
        ExpectTrue(selectDialogueChoice.WasAccepted, "server accepts approved dialogue choice intent");
        ExpectTrue(state.LocalKarma.Score > karmaBeforeDialogueChoice, "dialogue choice applies authoritative karma action");
        ExpectTrue(selectDialogueChoice.Event.EventId.Contains("dialogue_choice_selected"), "dialogue choice emits server event");
        ExpectFalse(server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            11,
            IntentType.SelectDialogueChoice,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Mara.Id,
                ["choiceId"] = "gift_balloon"
            })).WasAccepted, "server rejects dialogue choice with missing required item");
        var serverStartEntanglement = server.ProcessIntent(new ServerIntent(
            "peer_stand_in",
            2,
            IntentType.StartEntanglement,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Mara.Id,
                ["affectedNpcId"] = StarterNpcs.Dallen.Id,
                ["type"] = EntanglementType.Romantic.ToString(),
                ["action"] = PrototypeActions.StartMaraEntanglementId
            }));
        ExpectTrue(serverStartEntanglement.WasAccepted, "server accepts visible NPC entanglement intent");
        var serverEntanglementId = serverStartEntanglement.Event.Data["entanglementId"];
        ExpectEqual(EntanglementStatus.Secret, state.Entanglements.Get(serverEntanglementId).Status, "server-created entanglement starts secret");
        ExpectTrue(serverStartEntanglement.Event.EventId.Contains("entanglement_started"), "server entanglement start emits event");
        var serverExposeEntanglement = server.ProcessIntent(new ServerIntent(
            "peer_stand_in",
            3,
            IntentType.ExposeEntanglement,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entanglementId"] = serverEntanglementId,
                ["action"] = PrototypeActions.ExposeMaraEntanglementId
            }));
        ExpectTrue(serverExposeEntanglement.WasAccepted, "server accepts entanglement exposure intent");
        ExpectEqual(EntanglementStatus.Exposed, state.Entanglements.Get(serverEntanglementId).Status, "server exposure updates entanglement status");
        ExpectTrue(serverExposeEntanglement.Event.EventId.Contains("entanglement_exposed"), "server entanglement exposure emits event");
        ExpectTrue(server.CreateInterestSnapshot(GameState.LocalPlayerId, afterTick: 2)
            .ServerEvents.Any(serverEvent => serverEvent.EventId.Contains("entanglement_exposed")), "interest snapshot includes visible entanglement events");

        var snapshot = state.CreateSnapshot();
        ExpectEqual(4, snapshot.Players.Count, "snapshot captures player state");
        ExpectEqual(1, snapshot.Players.Count(player => player.Standing == LeaderboardRole.Saint), "snapshot has one Saint");
        ExpectTrue(snapshot.Players.Count(player => player.Standing == LeaderboardRole.Scourge) <= 1, "snapshot has at most one Scourge");
        ExpectEqual(snapshot.Leaderboard.SaintPlayerId, snapshot.Players.Single(player => player.Standing == LeaderboardRole.Saint).Id, "snapshot player standing matches leaderboard Saint");
        ExpectTrue(snapshot.Quests.Any(quest => quest.Id == StarterQuests.MaraClinicFiltersId), "snapshot captures quest state");
        ExpectTrue(snapshot.Factions.Any(faction => faction.FactionId == StarterFactions.FreeSettlersId), "snapshot captures faction state");
        ExpectTrue(snapshot.Duels.Any(duel => duel.Id.StartsWith("duel_")), "snapshot captures duel state");
        ExpectTrue(snapshot.WorldEvents.Count > 0, "snapshot captures world event history");
        ExpectTrue(snapshot.Players.All(player => player.KarmaRank >= 0), "snapshot captures karma rank");
        ExpectTrue(snapshot.Players.All(player => player.KarmaProgress.StartsWith("Progress:")), "snapshot captures karma progress");
        ExpectTrue(snapshot.Players.All(player => player.InventoryItemIds is not null), "snapshot captures per-player inventory");
        ExpectTrue(snapshot.Players.Any(player => player.Id == GameState.LocalPlayerId && player.TileX == 3 && player.TileY == 4), "snapshot captures player tile position");
        ExpectTrue(snapshot.Summary.Contains("players"), "snapshot has readable summary");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"leaderboard\""), "snapshot JSON includes leaderboard");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"standing\""), "snapshot JSON includes player standing");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"tileX\""), "snapshot JSON includes tile position");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"inventory\""), "snapshot JSON includes per-player inventory");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"duels\""), "snapshot JSON includes duels");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"karmaProgress\""), "snapshot JSON includes karma progress");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"players\""), "snapshot can be exported as JSON debug text");

        var largeServer = new AuthoritativeWorldServer(state, "large-test-world", ServerConfig.Large100Player);
        ExpectTrue(largeServer.JoinPlayer("large_extra_player", "Large Extra Player").WasAccepted, "large server profile accepts extra player slots");

        if (_failures == 0)
        {
            GD.Print("Gameplay smoke tests passed.");
        }
        else
        {
            GD.PushError($"Gameplay smoke tests failed: {_failures}");
        }
    }

    private void ExpectTrue(bool condition, string description)
    {
        if (condition)
        {
            GD.Print($"PASS: {description}");
            return;
        }

        _failures++;
        GD.PushError($"FAIL: {description}");
    }

    private void ExpectFalse(bool condition, string description)
    {
        ExpectTrue(!condition, description);
    }

    private void ExpectEqual<T>(T expected, T actual, string description)
    {
        if (Equals(expected, actual))
        {
            GD.Print($"PASS: {description}");
            return;
        }

        _failures++;
        GD.PushError($"FAIL: {description}. Expected {expected}, got {actual}.");
    }
}
