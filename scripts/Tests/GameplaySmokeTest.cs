using System;
using System.Linq;
using Godot;
using Karma.Art;
using Karma.Core;
using Karma.Data;
using Karma.Generation;
using Karma.Net;
using Karma.Npc;
using Karma.Player;
using Karma.UI;
using Karma.Util;
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
        ExpectTrue(localSession.LastLocalSnapshot.ShopOffers.Any(), "prototype server session exposes nearby shop offers");
        ExpectEqual(25, state.LocalScrip, "prototype local player starts with scrip");
        var localMatchRemaining = localSession.LastLocalSnapshot.Match.RemainingSeconds;
        localSession.AdvanceMatchTime(5);
        ExpectEqual(localMatchRemaining - 5, localSession.LastLocalSnapshot.Match.RemainingSeconds, "prototype server session advances match timer");
        localSession.RegisterWorldItem("session_test_item", StarterItems.DeflatedBalloon, TilePosition.Origin);
        ExpectTrue(
            localSession.LastLocalSnapshot.WorldItems.Any(entity => entity.EntityId == "session_test_item"),
            "prototype server session refreshes local snapshot after world item registration");
        var previousSessionTick = localSession.LastLocalSnapshot.Tick;
        var localTileMap = WorldGenerator.Generate(WorldConfig.CreatePrototype()).TileMap;
        localSession.SetTileMap(localTileMap);
        ExpectTrue(localSession.LastLocalSnapshot.MapChunks.Any(), "prototype server session exposes local map chunks");
        ExpectEqual(
            localSession.LastLocalSnapshot.MapChunks.Count,
            localSession.LocalSnapshotCache.KnownChunkCount,
            "prototype client snapshot cache tracks visible chunk revisions");
        ExpectEqual(
            localSession.LastLocalSnapshot.SyncHint.VisibleMapRevision,
            localSession.LocalSnapshotCache.LastVisibleMapRevision,
            "prototype client snapshot cache records visible map revision");
        var cachedChunkCount = localSession.LocalSnapshotCache.KnownChunkCount;
        localSession.SetTileMap(localTileMap);
        ExpectEqual(cachedChunkCount, localSession.LocalSnapshotCache.LastApplyResult.UnchangedChunks, "prototype client snapshot cache detects unchanged chunk revisions");
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
        var shopApproach = localSession.SendLocal(
            IntentType.Move,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["x"] = "6",
                ["y"] = "4"
            });
        ExpectTrue(shopApproach.WasAccepted, "prototype server session can move local player into shop range after random spawn");
        var localScripBeforeOffer = state.LocalScrip;
        var localPurchase = localSession.PurchaseOffer(StarterShopCatalog.DallenWhoopieCushionOfferId);
        ExpectTrue(localPurchase.WasAccepted, "prototype server session purchases visible shop offers");
        ExpectEqual(localScripBeforeOffer - 7, state.LocalScrip, "prototype shop helper debits local scrip");
        ExpectTrue(state.HasItem(GameState.LocalPlayerId, StarterItems.WhoopieCushionId), "prototype shop helper adds purchased item");
        state.SetPlayerPosition("peer_stand_in", TilePosition.Origin);
        var server = new AuthoritativeWorldServer(state, "test-world");
        ServerConfig.Prototype4Player.Validate();
        ServerConfig.Large100Player.Validate();
        ExpectEqual(4, ServerConfig.Prototype4Player.MaxPlayers, "prototype server profile supports 4 players");
        ExpectEqual(100, ServerConfig.Large100Player.MaxPlayers, "large server profile supports 100 players");
        ExpectEqual(32, ServerConfig.Large100Player.ChunkSizeTiles, "large server profile uses chunked world tiles");
        ExpectEqual(1, ServerConfig.Large100Player.InterestRadiusChunks, "large server profile derives map chunk interest radius");
        var wideInterestConfig = new ServerConfig(
            MaxPlayers: 4,
            TargetPlayers: 4,
            Scale: WorldScale.Small,
            TickRate: 20,
            InterestRadiusTiles: 65,
            CombatRangeTiles: 2,
            ChunkSizeTiles: 32,
            MatchDurationSeconds: 30 * 60);
        ExpectEqual(3, wideInterestConfig.InterestRadiusChunks, "server config expands chunk radius for wider interest ranges");
        ExpectEqual(30 * 60, ServerConfig.Prototype4Player.MatchDurationSeconds, "prototype server profile uses 30 minute matches");
        ExpectEqual(3.25f, PlayerController.CalculateCameraZoom(3f, 0.25f, 1.25f, 5f), "camera zoom can move closer");
        ExpectEqual(2.75f, PlayerController.CalculateCameraZoom(3f, -0.25f, 1.25f, 5f), "camera zoom can move farther out");
        ExpectEqual(1.25f, PlayerController.CalculateCameraZoom(1.25f, -5f, 1.25f, 5f), "camera zoom clamps farthest view");
        ExpectEqual(5f, PlayerController.CalculateCameraZoom(5f, 5f, 1.25f, 5f), "camera zoom clamps closest view");
        ExpectEqual(new Vector2(120f, 0f), PlayerController.CalculateVelocity(Vector2.Right, 120f, 1.6f, false), "player walk velocity uses base speed");
        ExpectEqual(new Vector2(192f, 0f), PlayerController.CalculateVelocity(Vector2.Right, 120f, 1.6f, true), "player sprint velocity uses sprint multiplier");
        ExpectEqual(new Vector2(120f, 0f), PlayerController.CalculateVelocity(Vector2.Right, 120f, 0.5f, true), "player sprint multiplier cannot slow movement");
        ExpectEqual(
            new Vector2(60f, 0f),
            PlayerController.CalculateSmoothedVelocity(Vector2.Zero, Vector2.Right, 120f, 1.6f, false, 600f, 900f, 0.1f),
            "player movement accelerates toward target velocity");
        ExpectEqual(
            new Vector2(30f, 0f),
            PlayerController.CalculateSmoothedVelocity(new Vector2(120f, 0f), Vector2.Zero, 120f, 1.6f, false, 600f, 900f, 0.1f),
            "player movement uses stronger friction when input stops");
        ExpectEqual(Vector2I.Right, DirectionHelper.ToCardinalVector(new Vector2(0.8f, 0.2f)), "direction helper resolves dominant horizontal movement");
        ExpectEqual(Vector2I.Down, DirectionHelper.ToCardinalVector(new Vector2(0.2f, 0.8f)), "direction helper resolves dominant vertical movement");
        ExpectEqual(CardinalDirection.Left, DirectionHelper.ToCardinalDirection(Vector2I.Left), "direction helper maps vectors to cardinal directions");
        ExpectEqual("up", DirectionHelper.ToName(CardinalDirection.Up), "direction helper maps directions to animation names");
        ExpectEqual(4, DirectionHelper.ToBit(CardinalDirection.Right), "direction helper maps directions to state bits");
        ExpectEqual(96, TopDownDepth.CalculateZIndex(96f), "top-down depth uses actor foot position");
        ExpectEqual(98, TopDownDepth.CalculateZIndex(96f, TopDownDepth.ItemOffsetZ), "top-down depth can offset item pickups above actors at the same foot");
        ExpectTrue(
            TopDownDepth.CalculateZIndex(128f, TopDownDepth.StructureOffsetZ) > TopDownDepth.CalculateZIndex(96f),
            "top-down depth draws lower-foot structures in front of higher actors");
        ExpectEqual(
            PrototypeCharacterSprite.WalkRightAnimation,
            PrototypeCharacterSprite.ResolveAnimationName(Vector2.Right),
            "native character sprite resolves right movement to a walk animation");
        ExpectEqual(
            PrototypeCharacterSprite.IdleDownAnimation,
            PrototypeCharacterSprite.ResolveAnimationName(Vector2.Zero),
            "native character sprite idles when parent is not moving");
        ExpectEqual(
            PrototypeCharacterSprite.IdleLeftAnimation,
            PrototypeCharacterSprite.ResolveAnimationName(Vector2.Zero, CardinalDirection.Left),
            "native character sprite preserves last facing direction while idle");
        ExpectEqual(
            PrototypeCharacterSprite.WalkUpRightAnimation,
            PrototypeCharacterSprite.ResolveAnimationName(new Vector2(1f, -1f)),
            "native character sprite resolves diagonal movement to an eight-way animation when available");
        ExpectEqual(
            CharacterFacingDirection.DownLeft,
            PrototypeCharacterSprite.ToFacingDirection(new Vector2(-1f, 1f)),
            "native character sprite tracks diagonal facing direction");
        var gridAnimations = PrototypeSpriteCatalog.FourDirectionGridAnimations(Vector2.Zero);
        ExpectEqual(
            new Rect2(0f, 32f, 32f, 32f),
            gridAnimations.First(animation => animation.Name == PrototypeCharacterSprite.WalkDownAnimation).Frames.First(),
            "four-direction character grid maps walk-down to row one");
        ExpectEqual(
            4,
            gridAnimations.First(animation => animation.Name == PrototypeCharacterSprite.WalkRightAnimation).Frames.Count,
            "four-direction character grid maps four walk frames per direction");
        var templateAnimations = CharacterSheetLayout.EightDirectionTemplate(Vector2.Zero);
        ExpectEqual(
            new Vector2(256f, 288f),
            CharacterSheetLayout.CalculateSheetSize(),
            "eight-direction character template uses a 256x288 runtime sheet");
        ExpectEqual(
            new Rect2(4 * 32, 0, 32, 32),
            CharacterSheetLayout.DirectionFrame(Vector2.Zero, CharacterSheetDirection.Back, CharacterSheetLayout.StandardIdleRow, new Vector2(32f, 32f)),
            "eight-direction character template maps back idle to the back column");
        ExpectEqual(
            new Rect2(2 * 32, 1 * 32, 32, 32),
            templateAnimations.First(animation => animation.Name == PrototypeCharacterSprite.WalkRightAnimation).Frames[0],
            "eight-direction character template maps walk-right to the right column");
        ExpectEqual(
            new Rect2(1 * 32, 1 * 32, 32, 32),
            templateAnimations.First(animation => animation.Name == PrototypeCharacterSprite.WalkDownRightAnimation).Frames[0],
            "eight-direction character template maps walk-front-right to the front-right column");
        ExpectEqual(
            new Rect2(2 * 32, 0, 32, 32),
            templateAnimations.First(animation => animation.Name == PrototypeCharacterSprite.WalkRightAnimation).Frames[2],
            "eight-direction character template inserts idle as a mid-step walk frame");
        ExpectEqual(
            new Rect2(6 * 32, 2 * 32, 32, 32),
            templateAnimations.First(animation => animation.Name == PrototypeCharacterSprite.WalkLeftAnimation).Frames[^1],
            "eight-direction character template returns through the pass step for smoother looping");
        ExpectEqual(
            new Rect2(2 * 32, 4 * 32, 32, 32),
            templateAnimations.First(animation => animation.Name == PrototypeCharacterSprite.WalkRightAnimation).Frames[4],
            "eight-direction character template keeps the strongest stride frame in the walk cycle");
        var testImage = Image.CreateEmpty(128, 160, false, Image.Format.Rgba8);
        var testTexture = ImageTexture.CreateFromImage(testImage);
        var testDefinition = new PrototypeSpriteDefinition(
            PrototypeSpriteKind.Player,
            "Test Grid Character",
            new Vector2(32f, 32f),
            System.Array.Empty<PrototypeSpriteLayer>(),
            "res://test-grid.png",
            new Rect2(0f, 0f, 32f, 32f),
            HasAtlasRegion: true,
            Animations: gridAnimations);
        var nativeFrames = PrototypeCharacterSprite.CreateSpriteFrames(testTexture, testDefinition);
        ExpectEqual(
            4,
            nativeFrames.GetFrameCount(PrototypeCharacterSprite.WalkRightAnimation),
            "native character sprite creates multi-frame walk animations");
        ExpectEqual(
            PrototypeCharacterSprite.WalkUpAnimation,
            PrototypeCharacterSprite.ResolveAvailableAnimation(nativeFrames, PrototypeCharacterSprite.WalkUpRightAnimation),
            "native character sprite falls back from diagonal to cardinal animations for four-direction sheets");
        var testPropDefinition = new PrototypeSpriteDefinition(
            PrototypeSpriteKind.WhoopieCushion,
            "Test Prop",
            new Vector2(24f, 18f),
            System.Array.Empty<PrototypeSpriteLayer>(),
            "res://test-prop.png",
            new Rect2(8f, 10f, 96f, 72f),
            HasAtlasRegion: true);
        ExpectTrue(
            AtlasTextureLoader.Load(PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath, forceImageLoad: true) is not null,
            "atlas texture loader can force raw image loading for generated transparent runtime sheets");
        var testPropAtlas = PrototypeAtlasSprite.CreateAtlasTexture(testTexture, testPropDefinition);
        ExpectEqual(
            testPropDefinition.AtlasRegion,
            testPropAtlas.Region,
            "native atlas sprite preserves catalog source region");
        var testPropFrame = AtlasFrames.FromPrototype(testPropDefinition);
        ExpectTrue(testPropFrame.IsValid, "atlas frame validates mapped prop art");
        ExpectEqual(
            new Vector2(0.25f, 0.25f),
            testPropFrame.CalculateScale(),
            "shared atlas frame scales source art to catalog display size");
        ExpectEqual(
            testPropFrame.CalculateScale(),
            PrototypeAtlasSprite.CalculateScale(testPropDefinition),
            "native atlas sprite uses shared atlas frame scaling");
        ExpectEqual(
            new Vector2(0f, -5.4f),
            testPropFrame.CalculateOffset(),
            "shared atlas frame anchors prop art near its lower edge");
        ExpectEqual(
            0,
            ProjectSettings.GetSetting("rendering/textures/canvas_textures/default_texture_filter").AsInt32(),
            "project uses nearest-neighbor texture filtering for pixel art");
        var artAssets = ArtAssetManifest.GetUniqueAssets();
        ExpectTrue(artAssets.Count >= 8, "art manifest discovers cataloged atlas assets");
        var expectedPlayerAtlasPath = FileAccess.FileExists(PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath)
            ? PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath
            : PrototypeSpriteCatalog.EngineerPlayerAtlasPath;
        var expectedPlayerSize = expectedPlayerAtlasPath == PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath
            ? new Vector2(32f, 32f)
            : new Vector2(30f, 40f);
        if (expectedPlayerAtlasPath == PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath)
        {
            var engineerImage = Image.LoadFromFile(expectedPlayerAtlasPath);
            ExpectEqual(256, engineerImage.GetWidth(), "engineer 8-direction runtime sheet has expected width");
            ExpectEqual(288, engineerImage.GetHeight(), "engineer 8-direction runtime sheet has expected height");
            ExpectTrue(CountTransparentPixels(engineerImage) > 0, "engineer 8-direction runtime sheet keeps transparent background pixels");
            ExpectTrue(
                CountPixelDifferences(engineerImage, new Vector2I(1, 0), new Vector2I(2, 0)) > 0,
                "engineer 8-direction runtime sheet gives front-right a distinct placeholder frame");
            ExpectTrue(
                CountPixelDifferences(engineerImage, new Vector2I(3, 0), new Vector2I(2, 0)) > 0,
                "engineer 8-direction runtime sheet gives back-right a distinct placeholder frame");
        }

        ExpectTrue(
            artAssets.Any(asset => asset.Path == expectedPlayerAtlasPath),
            "art manifest includes active generated engineer character sheet");
        ExpectTrue(
            artAssets.Any(asset => asset.Path == StructureArtCatalog.GreenhouseAtlasPath),
            "art manifest includes greenhouse structure sheet");
        ExpectTrue(
            artAssets.Any(asset => asset.Path == ThemeArtRegistry.PlaceholderAtlasPath),
            "art manifest includes mapped tile sheet");
        ExpectEqual(0, ArtAssetManifest.GetMissingAssets().Count, "all cataloged atlas assets exist on disk");
        ExpectTrue(ArtAssetManifest.FormatSummary().Contains("missing"), "art manifest summary reports missing count");
        ExpectTrue(
            ProjectSettings.GetSetting("rendering/2d/snap/snap_2d_transforms_to_pixel").AsBool(),
            "project snaps 2D transforms to pixels for native pixel-art scenes");
        ExpectTrue(PlayerController.CanSprint(Vector2.Right, true, 1f, false), "player can sprint while moving with stamina");
        ExpectFalse(PlayerController.CanSprint(Vector2.Zero, true, 100f, false), "player cannot sprint while idle");
        ExpectFalse(PlayerController.CanSprint(Vector2.Right, true, 0f, false), "player cannot sprint without stamina");
        ExpectFalse(PlayerController.CanSprint(Vector2.Right, true, 50f, true), "player cannot sprint while winded");
        ExpectEqual(76f, PlayerController.CalculateNextStamina(100f, 1.0, true, 100f, 24f, 18f), "sprinting drains stamina");
        ExpectEqual(94f, PlayerController.CalculateNextStamina(76f, 1.0, false, 100f, 24f, 18f), "not sprinting recovers stamina");
        ExpectEqual(0f, PlayerController.CalculateNextStamina(5f, 1.0, true, 100f, 24f, 18f), "stamina drain clamps at zero");
        ExpectEqual(100f, PlayerController.CalculateNextStamina(95f, 1.0, false, 100f, 24f, 18f), "stamina recovery clamps at maximum");
        ExpectTrue(PlayerController.CalculateExhausted(false, 0f, 25f), "empty stamina makes player winded");
        ExpectTrue(PlayerController.CalculateExhausted(true, 20f, 25f), "winded player stays winded below resume stamina");
        ExpectFalse(PlayerController.CalculateExhausted(true, 25f, 25f), "winded player recovers at resume stamina");
        ExpectEqual("Stamina: 20/100 (low)", PlayerController.FormatStamina(20f, 100f, false), "low stamina label is visible");
        ExpectEqual("Stamina: 0/100 (winded)", PlayerController.FormatStamina(0f, 100f, true), "winded stamina label is visible");
        ExpectEqual(new Vector2(96f, 128f), PlayerController.CalculateWorldPosition(3, 4), "player controller maps authoritative tiles to world pixels");
        ExpectEqual(
            expectedPlayerAtlasPath,
            PrototypeSpriteCatalog.Get(PrototypeSpriteKind.Player).AtlasPath,
            "local player uses active generated engineer character sheet");
        ExpectEqual(
            expectedPlayerSize,
            PrototypeSpriteCatalog.Get(PrototypeSpriteKind.Player).Size,
            "generated engineer player renders with corrected proportions");
        ExpectEqual(
            PrototypeSpriteCatalog.CharacterAtlasPath,
            PrototypeSpriteCatalog.Get(PrototypeSpriteKind.Mara).AtlasPath,
            "NPCs keep the shared character atlas");
        ExpectTrue(
            PlayerController.ShouldSnapToAuthoritativePosition(Vector2.Zero, new Vector2(96f, 128f)),
            "player controller snaps large authoritative corrections such as respawn");
        ExpectFalse(
            PlayerController.ShouldSnapToAuthoritativePosition(new Vector2(90f, 124f), new Vector2(96f, 128f)),
            "player controller preserves small local movement between authoritative tiles");
        ExpectEqual("Health: 75/100", HudController.FormatHealth(75, 100), "health label formats current and maximum health");
        ExpectEqual(75f, HudController.CalculateHealthPercent(75, 100), "health bar percent follows authoritative health");
        ExpectEqual(0f, HudController.CalculateHealthPercent(-5, 100), "health bar percent clamps below zero");
        ExpectEqual(100f, HudController.CalculateHealthPercent(125, 100), "health bar percent clamps above maximum");
        ExpectEqual(
            "Combat: none | You ATK:10 DEF:3 | Status: none",
            HudController.FormatCombatLine("Combat: none", 10, 3, System.Array.Empty<string>()),
            "combat HUD line clears status effects when none are active");
        ExpectEqual(
            "Combat: hit | You ATK:10 DEF:3 | Status: Attack Cooldown (2)",
            HudController.FormatCombatLine("Combat: hit", 10, 3, new[] { "Attack Cooldown (2)" }),
            "combat HUD line includes active status effects");
        var itemPrompt = ItemText.FormatPickupPrompt(StarterItems.PracticeStick);
        ExpectTrue(itemPrompt.Contains("Power 10"), "item pickup prompt exposes item power");
        ExpectTrue(itemPrompt.Contains("Tags: training, violent"), "item pickup prompt exposes sorted tags");
        ExpectTrue(itemPrompt.Contains("Press E to pick it up."), "item pickup prompt explains pickup control");
        var inventoryOverlay = HudController.FormatInventoryOverlay(
            new[] { StarterItems.RepairKit, StarterItems.RepairKit, StarterItems.WhoopieCushion },
            42,
            new System.Collections.Generic.Dictionary<EquipmentSlot, GameItem>
            {
                [EquipmentSlot.MainHand] = StarterItems.PracticeStick
            });
        ExpectTrue(inventoryOverlay.Contains("Scrip: 42"), "inventory overlay shows scrip");
        ExpectTrue(inventoryOverlay.Contains("Main Hand: Practice Stick"), "inventory overlay shows equipped weapon");
        ExpectTrue(inventoryOverlay.Contains("Repair Kit x2"), "inventory overlay groups matching items");
        ExpectTrue(inventoryOverlay.Contains("Power 10"), "inventory overlay shows equipped item stats");
        ExpectTrue(inventoryOverlay.Contains("Tool"), "inventory overlay shows item categories");
        ExpectTrue(inventoryOverlay.Contains("I - Close"), "inventory overlay explains close control");
        ExpectEqual(25f, WorldHealthBar.CalculateHealthPercent(25, 100), "world health bar percent follows visible player health");
        var peerPrompt = PeerStandInController.FormatPrompt(
            hasBeenRobbed: true,
            health: 25,
            maxHealth: 100,
            new[] { "Karma Break Grace (4)" },
            "Duel: Active");
        ExpectTrue(peerPrompt.Contains("HP: 25/100"), "peer prompt includes authoritative health");
        ExpectTrue(peerPrompt.Contains("Status: Karma Break Grace (4)"), "peer prompt includes active status effects");
        ExpectTrue(peerPrompt.Contains("Duel: Active"), "peer prompt includes duel state");
        ExpectTrue(peerPrompt.Contains("Attack blocked by Karma Break grace"), "peer prompt explains blocked attacks during grace");
        ExpectTrue(peerPrompt.Contains("Duel already pending/active"), "peer prompt explains unavailable duel requests");
        ExpectTrue(peerPrompt.Contains("Let them duel strike you"), "peer prompt labels peer-authored duel attacks");
        ExpectTrue(peerPrompt.Contains("Swipe 3 scrip"), "peer prompt exposes scrip theft test action");
        ExpectTrue(peerPrompt.Contains("Satchel: stolen"), "peer prompt includes satchel state");
        ExpectTrue(
            NpcController.FormatQuestPromptLine(QuestStatus.Available, "Clinic Filters", "Repair Kit", false, 12).Contains("Start Clinic Filters"),
            "NPC prompt labels available quests");
        ExpectTrue(
            NpcController.FormatQuestPromptLine(QuestStatus.Active, "Clinic Filters", "Repair Kit", false, 12).Contains("Need Repair Kit"),
            "NPC prompt labels missing quest items");
        ExpectTrue(
            NpcController.FormatQuestPromptLine(QuestStatus.Active, "Clinic Filters", "Repair Kit", true, 12).Contains("Complete Clinic Filters"),
            "NPC prompt labels completable quests");
        ExpectTrue(
            NpcController.FormatQuestPromptLine(QuestStatus.Completed, "Clinic Filters", "Repair Kit", true, 12).Contains("complete"),
            "NPC prompt labels completed quests");
        ExpectEqual(
            "2 - Attack as duel strike",
            PeerStandInController.FormatAttackLabel(System.Array.Empty<string>(), "Duel: Active"),
            "peer prompt labels active duel attacks");
        ExpectEqual(
            "2 - Attack them outside a duel",
            PeerStandInController.FormatAttackLabel(System.Array.Empty<string>(), "Duel: none"),
            "peer prompt labels non-duel attacks");
        ExpectEqual(
            "8 - Their attack is cooling down",
            PeerStandInController.FormatPeerAttackLabel(new[] { "Attack Cooldown (2)" }, "Duel: Active"),
            "peer prompt labels peer attack cooldown");
        ExpectEqual(
            "8 - Let them attack you",
            PeerStandInController.FormatPeerAttackLabel(System.Array.Empty<string>(), "Duel: none"),
            "peer prompt labels peer-authored attacks");
        var beaconPerks = new[] { new KarmaPerk(PerkCatalog.BeaconAuraId, "Beacon Aura", PerkPath.Ascension, 35, "test") };
        var nervePerks = new[] { new KarmaPerk(PerkCatalog.RenegadeNerveId, "Renegade Nerve", PerkPath.Descension, 35, "test") };
        ExpectEqual(22.5f, PlayerController.CalculateEffectiveStaminaRecovery(18f, beaconPerks), "Beacon Aura improves stamina recovery");
        ExpectTrue(Mathf.Abs(PlayerController.CalculateEffectiveSprintCost(24f, nervePerks) - 20.4f) < 0.01f, "Renegade Nerve reduces sprint stamina cost");
        var matchServer = new AuthoritativeWorldServer(state, "match-test-world");
        ExpectEqual(MatchStatus.Running, matchServer.Match.Status, "new server match starts running");
        ExpectEqual(30 * 60, matchServer.Match.RemainingSeconds, "new server match starts with full duration remaining");
        ExpectEqual("rival_paragon", matchServer.Match.CurrentSaintId, "running match snapshot exposes current Saint leader");
        ExpectEqual("rival_renegade", matchServer.Match.CurrentScourgeId, "running match snapshot exposes current Scourge leader");
        matchServer.AdvanceMatchTime((30 * 60) - 1);
        ExpectEqual(MatchStatus.Running, matchServer.Match.Status, "match stays running before timer expires");
        var saintScripBeforeMatchEnd = state.Players["rival_paragon"].Scrip;
        var scourgeScripBeforeMatchEnd = state.Players["rival_renegade"].Scrip;
        matchServer.AdvanceMatchTime(1);
        ExpectEqual(MatchStatus.Finished, matchServer.Match.Status, "match finishes when timer expires");
        ExpectEqual("rival_paragon", matchServer.Match.SaintWinnerId, "finished match locks current Saint winner");
        ExpectEqual("rival_renegade", matchServer.Match.ScourgeWinnerId, "finished match locks current Scourge winner");
        ExpectEqual(saintScripBeforeMatchEnd + ServerConfig.DefaultMatchWinnerScripReward, state.Players["rival_paragon"].Scrip, "finished match pays Saint scrip reward");
        ExpectEqual(scourgeScripBeforeMatchEnd + ServerConfig.DefaultMatchWinnerScripReward, state.Players["rival_renegade"].Scrip, "finished match pays Scourge scrip reward");
        ExpectTrue(matchServer.Match.Summary.Contains("Match complete"), "finished match summary reports completion");
        ExpectTrue(HudController.FormatMatchStatus(matchServer.Match).Contains("RESULTS LOCKED"), "HUD match status makes locked results obvious");
        ExpectTrue(HudController.FormatMatchStatus(matchServer.Match).Contains("Post-match free roam"), "HUD match status explains post-match limits");
        ExpectTrue(matchServer.EventLog.Any(serverEvent => serverEvent.EventId.Contains("match_finished")), "finished match emits server event");
        var matchFinishedEvent = matchServer.EventLog.First(serverEvent => serverEvent.EventId.Contains("match_finished"));
        ExpectEqual("Helpful Rival", matchFinishedEvent.Data["saintWinnerName"], "finished match event reports Saint winner name");
        ExpectEqual("Shady Rival", matchFinishedEvent.Data["scourgeWinnerName"], "finished match event reports Scourge winner name");
        ExpectEqual(ServerConfig.DefaultMatchWinnerScripReward.ToString(), matchFinishedEvent.Data["saintScripReward"], "finished match event reports Saint scrip reward");
        ExpectEqual(ServerConfig.DefaultMatchWinnerScripReward.ToString(), matchFinishedEvent.Data["scourgeScripReward"], "finished match event reports Scourge scrip reward");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { matchFinishedEvent }).Contains("results locked"),
            "HUD formats match finished events");
        matchServer.AdvanceMatchTime(60);
        ExpectEqual(saintScripBeforeMatchEnd + ServerConfig.DefaultMatchWinnerScripReward, state.Players["rival_paragon"].Scrip, "finished match does not pay Saint reward twice");
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
        var spawnState = new GameState();
        spawnState.RegisterPlayer(GameState.LocalPlayerId, "Spawn Tester");
        spawnState.RegisterPlayer("peer_stand_in", "Spawn Peer");
        spawnState.RegisterPlayer("rival_paragon", "Spawn Paragon");
        spawnState.RegisterPlayer("rival_renegade", "Spawn Renegade");
        var spawnServer = new AuthoritativeWorldServer(spawnState, "spawn-test-world");
        var spawnWorld = WorldGenerator.Generate(WorldConfig.FromServerConfig(
            "spawn-test-world",
            new WorldSeed(42, "Spawn Test", "test"),
            ServerConfig.Prototype4Player));
        spawnServer.SetTileMap(spawnWorld.TileMap);
        var initialSpawns = spawnServer.ConnectedPlayerIds
            .Select(playerId => spawnState.Players[playerId].Position)
            .ToArray();
        ExpectEqual(4, initialSpawns.Distinct().Count(), "initial match spawns are unique per connected player");
        ExpectTrue(initialSpawns.All(spawn => spawn.X >= 4 && spawn.Y >= 4 && spawn.X < spawnWorld.TileMap.Width - 4 && spawn.Y < spawnWorld.TileMap.Height - 4), "initial match spawns avoid map edges");
        ExpectTrue(initialSpawns.SelectMany((spawn, index) => initialSpawns.Skip(index + 1).Select(other => spawn.DistanceSquaredTo(other))).All(distanceSquared => distanceSquared >= 100), "initial match spawns keep players separated");
        ExpectFalse(server.JoinPlayer("overflow_player", "Overflow Player").WasAccepted, "prototype server rejects players beyond capacity");
        ExpectEqual(1000, WorldConfig.FromServerConfig(
            "scale-test",
            new WorldSeed(1, "Scale Test", "test"),
            ServerConfig.Large100Player).WidthTiles, "large world profile expands map size");
        var cooldownState = new GameState();
        cooldownState.RegisterPlayer(GameState.LocalPlayerId, "Cooldown Tester");
        cooldownState.RegisterPlayer("cooldown_target", "Cooldown Target");
        cooldownState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        cooldownState.SetPlayerPosition("cooldown_target", TilePosition.Origin);
        var cooldownServer = new AuthoritativeWorldServer(cooldownState, "cooldown-test-world");
        ExpectTrue(cooldownServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            1,
            IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "cooldown_target"
            })).WasAccepted, "server accepts first attack before cooldown starts");
        var cooldownRejectedAttack = cooldownServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            2,
            IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "cooldown_target"
            }));
        ExpectFalse(cooldownRejectedAttack.WasAccepted, "server rejects attacks while cooldown is active");
        ExpectTrue(cooldownRejectedAttack.Event.Data["reason"].Contains("cooldown"), "rejected attack event carries cooldown reason");
        ExpectTrue(
            cooldownServer.CreateInterestSnapshot(GameState.LocalPlayerId)
                .Players.First(player => player.Id == GameState.LocalPlayerId)
                .StatusEffects.Any(status => status.Contains("Attack Cooldown")),
            "interest snapshot exposes local attack cooldown status");
        cooldownServer.AdvanceIdleTicks(2);
        ExpectFalse(
            cooldownServer.CreateInterestSnapshot(GameState.LocalPlayerId)
                .Players.First(player => player.Id == GameState.LocalPlayerId)
                .StatusEffects.Any(status => status.Contains("Attack Cooldown")),
            "idle server ticks clear local attack cooldown status");
        ExpectTrue(cooldownServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            3,
            IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "cooldown_target"
            })).WasAccepted, "server accepts attack after idle cooldown ticks pass");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { cooldownRejectedAttack.Event }).Contains("Attack rejected"),
            "HUD formats rejected attack events");
        ExpectEqual(
            "Status: Attack Cooldown (2)",
            HudController.FormatStatusEffects(new[] { "Attack Cooldown (2)" }),
            "HUD formats active status effects");
        var counterAttackState = new GameState();
        counterAttackState.RegisterPlayer(GameState.LocalPlayerId, "Counter Target");
        counterAttackState.RegisterPlayer("peer_stand_in", "Counter Attacker");
        counterAttackState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        counterAttackState.SetPlayerPosition("peer_stand_in", TilePosition.Origin);
        var counterAttackServer = new AuthoritativeWorldServer(counterAttackState, "counter-attack-test-world");
        var localHealthBeforeCounter = counterAttackState.LocalPlayer.Health;
        var peerCounterAttack = counterAttackServer.ProcessIntent(new ServerIntent(
            "peer_stand_in",
            1,
            IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = GameState.LocalPlayerId
            }));
        ExpectTrue(peerCounterAttack.WasAccepted, "server accepts stand-in attacks against the local player");
        ExpectTrue(counterAttackState.LocalPlayer.Health < localHealthBeforeCounter, "stand-in attacks damage the local player");
        ExpectTrue(
            counterAttackServer.CreateInterestSnapshot(GameState.LocalPlayerId)
                .Players.First(player => player.Id == "peer_stand_in")
                .StatusEffects.Any(status => status.Contains("Attack Cooldown")),
            "interest snapshot exposes stand-in attack cooldown after peer-authored attack");
        counterAttackState.AddItem(GameState.LocalPlayerId, StarterItems.RepairKit);
        var localHealthBeforeSelfRepair = counterAttackState.LocalPlayer.Health;
        var selfRepair = counterAttackServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            1,
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = StarterItems.RepairKitId,
                ["targetId"] = GameState.LocalPlayerId
            }));
        ExpectTrue(selfRepair.WasAccepted, "server accepts local self repair kit use");
        ExpectTrue(counterAttackState.LocalPlayer.Health > localHealthBeforeSelfRepair, "self repair kit restores local health");
        ExpectFalse(counterAttackState.HasItem(GameState.LocalPlayerId, StarterItems.RepairKitId), "self repair kit consumes the local repair kit");
        ExpectEqual(counterAttackState.LocalPlayer.Health.ToString(), selfRepair.Event.Data["targetHealth"], "self repair event reports target health");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { selfRepair.Event }).Contains("HP:"),
            "HUD formats repair kit healing outcome");
        counterAttackState.DamagePlayer("peer_stand_in", GameState.LocalPlayerId, 20, "ration smoke test");
        counterAttackState.AddItem(GameState.LocalPlayerId, StarterItems.RationPack);
        var localHealthBeforeRation = counterAttackState.LocalPlayer.Health;
        var rationUse = counterAttackServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            2,
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = StarterItems.RationPackId
            }));
        ExpectTrue(rationUse.WasAccepted, "server accepts ration pack consumable use");
        ExpectEqual(localHealthBeforeRation + 10, counterAttackState.LocalPlayer.Health, "ration pack restores a small amount of health");
        ExpectFalse(counterAttackState.HasItem(GameState.LocalPlayerId, StarterItems.RationPackId), "ration pack use consumes the ration pack");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { rationUse.Event }).Contains("used Ration Pack"),
            "HUD formats non-repair consumable healing outcome");
        var graceState = new GameState();
        graceState.RegisterPlayer(GameState.LocalPlayerId, "Grace Tester");
        graceState.RegisterPlayer("grace_target", "Grace Target");
        graceState.RegisterPlayer("grace_attacker_two", "Grace Attacker Two");
        graceState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        graceState.SetPlayerPosition("grace_target", TilePosition.Origin);
        graceState.SetPlayerPosition("grace_attacker_two", TilePosition.Origin);
        graceState.DamagePlayer(GameState.LocalPlayerId, "grace_target", 90, "setup damage");
        graceState.AddItem("grace_target", StarterItems.WhoopieCushion);
        var graceServer = new AuthoritativeWorldServer(graceState, "grace-test-world");
        var lethalGraceAttack = graceServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            1,
            IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "grace_target"
            }));
        ExpectTrue(lethalGraceAttack.WasAccepted, "server accepts lethal attack before Karma Break grace starts");
        var graceRespawnPosition = graceState.Players["grace_target"].Position;
        ExpectTrue(graceRespawnPosition.DistanceSquaredTo(TilePosition.Origin) >= 144, "lethal Karma Break respawns away from the death pile");
        ExpectEqual(graceRespawnPosition.X.ToString(), lethalGraceAttack.Event.Data["respawnX"], "lethal attack event reports respawn x");
        ExpectEqual(graceRespawnPosition.Y.ToString(), lethalGraceAttack.Event.Data["respawnY"], "lethal attack event reports respawn y");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { lethalGraceAttack.Event }).Contains($"respawned at {graceRespawnPosition.X},{graceRespawnPosition.Y}"),
            "HUD formats lethal attack outcome from server event data");
        ExpectTrue(
            graceServer.CreateInterestSnapshot("grace_target")
                .Players.First(player => player.Id == "grace_target")
                .StatusEffects.Any(status => status.Contains("Karma Break Grace")),
            "interest snapshot exposes Karma Break grace status");
        var karmaBreakDrop = graceServer.WorldItems.Values.First(entity =>
            entity.EntityId.StartsWith("drop_grace_target") &&
            entity.Item.Id == StarterItems.WhoopieCushionId &&
            entity.Position == TilePosition.Origin);
        ExpectEqual("grace_target", karmaBreakDrop.DropOwnerId, "Karma Break drops remember owner id");
        ExpectEqual("Grace Target", karmaBreakDrop.DropOwnerName, "Karma Break drops remember owner display name");
        ExpectTrue(
            graceServer.CreateInterestSnapshot(GameState.LocalPlayerId).WorldItems.Any(entity =>
                entity.EntityId == karmaBreakDrop.EntityId &&
                entity.DropOwnerId == "grace_target" &&
                entity.DropOwnerName == "Grace Target"),
            "interest snapshot exposes Karma Break drop ownership");
        var localKarmaBeforeClaimingDrop = graceState.LocalKarma.Score;
        var claimedDrop = graceServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            2,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = karmaBreakDrop.EntityId
            }));
        ExpectTrue(claimedDrop.WasAccepted, "server accepts claiming another player's Karma Break drop");
        ExpectTrue(graceState.LocalKarma.Score < localKarmaBeforeClaimingDrop, "claiming another player's Karma Break drop descends picker karma");
        ExpectEqual("Grace Target", claimedDrop.Event.Data["dropOwnerName"], "drop pickup event reports owner display name");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { claimedDrop.Event }).Contains("Grace Target's Karma Break drop"),
            "HUD formats Karma Break drop ownership");
        graceState.SetPlayerPosition("grace_attacker_two", graceRespawnPosition);
        var graceRejectedAttack = graceServer.ProcessIntent(new ServerIntent(
            "grace_attacker_two",
            1,
            IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "grace_target"
            }));
        ExpectFalse(graceRejectedAttack.WasAccepted, "server rejects attacks against Karma Break grace");
        ExpectTrue(graceRejectedAttack.Event.Data["reason"].Contains("Karma Break grace"), "rejected attack event carries Karma Break grace reason");
        graceServer.AdvanceIdleTicks(5);
        ExpectFalse(
            graceServer.CreateInterestSnapshot("grace_target")
                .Players.First(player => player.Id == "grace_target")
                .StatusEffects.Any(status => status.Contains("Karma Break Grace")),
            "idle server ticks clear Karma Break grace status");
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
        var artSet = ThemeArtRegistry.GetForTheme(generatedA.Theme);
        ExpectEqual(
            1,
            artSet.GetTile(WorldTileIds.ClinicFloor).AtlasTileSizePixels,
            "theme art registry records pixel atlas source scale");
        ExpectEqual(
            new Rect2(14, 241, 47, 48),
            artSet.GetTile(WorldTileIds.ClinicFloor).SourceRegion,
            "theme art registry can calculate sci-fi atlas source regions");
        ExpectTrue(artSet.GetTile(WorldTileIds.ClinicFloor).HasAtlasRegion, "theme art registry enables mapped sci-fi atlas regions");
        var renderer = new GeneratedTileMapRenderer();
        ExpectTrue(renderer.PreferAtlasArt, "tile renderer prefers mapped atlas art");
        renderer.SetChunks(
            new[] { ToMapChunkSnapshot(generatedA.TileMap.GetChunk(new GeneratedChunkCoordinate(0, 0))) },
            artSet);
        ExpectEqual(1, renderer.LoadedChunkCount, "tile renderer caches visible chunks");
        ExpectEqual(1, renderer.LastUpdatedChunkCount, "tile renderer records newly streamed chunks");
        renderer.SetChunks(
            new[] { ToMapChunkSnapshot(generatedA.TileMap.GetChunk(new GeneratedChunkCoordinate(0, 0))) },
            artSet);
        ExpectEqual(0, renderer.LastUpdatedChunkCount, "tile renderer skips unchanged chunk revisions");
        renderer.SetChunks(
            new[] { ToMapChunkSnapshot(generatedA.TileMap.GetChunk(new GeneratedChunkCoordinate(1, 0))) },
            artSet);
        ExpectEqual(1, renderer.LoadedChunkCount, "tile renderer evicts chunks that leave interest");
        ExpectEqual(2, renderer.LastUpdatedChunkCount, "tile renderer records chunk eviction and replacement");
        ExpectEqual(WorldTileIds.ClinicFloor, generatedA.TileMap.Get(3, 3).FloorId, "world generation assigns starter clinic floor tiles");
        ExpectEqual(WorldTileIds.WallMetal, generatedA.TileMap.Get(2, 2).StructureId, "world generation assigns starter clinic wall structures");
        ExpectEqual(WorldTileIds.DoorAirlock, generatedA.TileMap.Get(5, 7).StructureId, "world generation assigns starter clinic door structure");
        ExpectTrue(
            generatedA.TileMap.Tiles.Any(tile => tile.ZoneId == "duel_ring" && tile.FloorId == WorldTileIds.DuelRingFloor),
            "world generation assigns logical duel ring tiles");
        ExpectTrue(
            generatedA.Locations.Any(location => location.KarmaHook.Contains("repair") || location.KarmaHook.Contains("sabotage")),
            "world generation creates locations with karma gameplay hooks");
        ExpectTrue(
            generatedA.Npcs.Any(npc => npc.Need.Contains("leverage") || npc.Need.Contains("proof") || npc.Need.Contains("repair")),
            "NPC generation derives actionable needs from social stations");
        ExpectEqual(generatedA.Npcs.Count, generatedA.NpcPlacements.Count, "generated NPCs receive social-station placements");
        ExpectTrue(
            generatedA.NpcPlacements.Any(placement => placement.GameplayHook.Contains("rumor") || placement.GameplayHook.Contains("sabotage") || placement.GameplayHook.Contains("gift")),
            "NPC placements expose the local karma hook that spawned them");
        ExpectTrue(generatedA.Quests.Any(quest => quest.CompletionActionId.StartsWith("generated_station_help:")), "world generation creates station-driven quests");
        ExpectTrue(
            generatedA.Quests.Any(quest => quest.Description.Contains("local karma hook")),
            "generated station quests explain the local karma hook");
        ExpectEqual(generatedA.Locations.Count, generatedA.StructurePlacements.Count, "world generation creates one repairable/sabotageable structure per station");
        ExpectTrue(
            generatedA.StructurePlacements.Any(placement => placement.GameplayHook.Contains("repair") || placement.GameplayHook.Contains("sabotage") || placement.GameplayHook.Contains("rumor")),
            "generated structures carry station gameplay hooks");
        var samplePoints = ProceduralPlacementSampler.GenerateSeparatedPoints(
            new Random(99),
            width: 64,
            height: 64,
            count: 8,
            edgePadding: 4,
            candidateAttemptsPerPoint: 24,
            reservedPoints: new[] { new TilePosition(32, 32) });
        ExpectEqual(8, samplePoints.Count, "procedural placement sampler returns requested point count");
        ExpectTrue(samplePoints.All(point => point.X >= 4 && point.Y >= 4 && point.X < 60 && point.Y < 60), "procedural placement sampler respects edge padding");
        ExpectTrue(samplePoints.All(point => point.DistanceSquaredTo(new TilePosition(32, 32)) >= 25), "procedural placement sampler avoids reserved points");
        ExpectEqual(generatedA.Oddities.Count, generatedA.OddityPlacements.Count, "generated oddities receive deterministic world placements");
        ExpectTrue(
            generatedA.OddityPlacements.All(placement => placement.X >= 3 && placement.Y >= 3 && placement.X < generatedA.Config.WidthTiles - 3 && placement.Y < generatedA.Config.HeightTiles - 3),
            "generated oddity placements respect edge padding");
        ExpectTrue(
            generatedA.OddityPlacements.Any(placement => placement.PlacementReason.Contains("choices")),
            "generated oddity placements explain their local gameplay reason");
        var generatedContentState = new GameState();
        generatedContentState.RegisterPlayer(GameState.LocalPlayerId, "Generated Content Tester");
        var generatedContentServer = new AuthoritativeWorldServer(generatedContentState, "generated-content-test");
        generatedContentServer.SeedGeneratedWorldContent(generatedA);
        ExpectEqual(3 + generatedA.Locations.Count + generatedA.StructurePlacements.Count, generatedContentServer.WorldStructures.Count, "server seeds generated station markers and generated structures alongside starter structures");
        var firstGeneratedLocation = generatedA.Locations[0];
        generatedContentState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(firstGeneratedLocation.X, firstGeneratedLocation.Y));
        var generatedStationSnapshot = generatedContentServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        var generatedStation = generatedStationSnapshot.Structures.FirstOrDefault(structure => structure.Name == firstGeneratedLocation.Name && structure.Category == "station");
        ExpectTrue(generatedStation is not null, "interest snapshot exposes generated station markers near the player");
        ExpectTrue(generatedStation?.InteractionPrompt.Contains(firstGeneratedLocation.KarmaHook) == true, "generated station marker prompt exposes its karma hook");
        var firstGeneratedStructurePlacement = generatedA.StructurePlacements[0];
        generatedContentState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(firstGeneratedStructurePlacement.X, firstGeneratedStructurePlacement.Y));
        var generatedStructureSnapshot = generatedContentServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        var generatedStructure = generatedStructureSnapshot.Structures.FirstOrDefault(structure => structure.EntityId == firstGeneratedStructurePlacement.StructureId);
        ExpectTrue(generatedStructure is not null, "interest snapshot exposes generated station structures near the player");
        ExpectTrue(generatedStructure?.InteractionPrompt.Contains("repair") == true, "generated station structures expose repair interactions");
        generatedContentState.AddItem(GameState.LocalPlayerId, StarterItems.MultiTool);
        var repairGeneratedStructure = generatedContentServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            72,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = firstGeneratedStructurePlacement.StructureId,
                ["action"] = "repair"
            }));
        ExpectTrue(repairGeneratedStructure.WasAccepted, "server accepts repair on generated station structures");
        ExpectEqual(StarterFactions.ToId(firstGeneratedStructurePlacement.SuggestedFaction), repairGeneratedStructure.Event.Data["factionId"], "generated structure repair affects the station suggested faction");
        ExpectTrue(int.Parse(repairGeneratedStructure.Event.Data["integrity"]) > firstGeneratedStructurePlacement.Integrity, "generated structure repair improves integrity");
        var sabotageGeneratedStructure = generatedContentServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            73,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = firstGeneratedStructurePlacement.StructureId,
                ["action"] = "sabotage"
            }));
        ExpectTrue(sabotageGeneratedStructure.WasAccepted, "server accepts sabotage on generated station structures");
        ExpectEqual(StarterFactions.ToId(firstGeneratedStructurePlacement.SuggestedFaction), sabotageGeneratedStructure.Event.Data["factionId"], "generated structure sabotage affects the station suggested faction");
        ExpectTrue(int.Parse(sabotageGeneratedStructure.Event.Data["integrity"]) < int.Parse(repairGeneratedStructure.Event.Data["integrity"]), "generated structure sabotage damages integrity");
        var seededGeneratedNpcCount = generatedA.Npcs.Count(npc => npc.Id != StarterNpcs.Mara.Id && npc.Id != StarterNpcs.Dallen.Id);
        ExpectEqual(2 + seededGeneratedNpcCount, generatedContentServer.Npcs.Count, "server seeds generated NPC placements without duplicating starter NPCs");
        var firstGeneratedNpcPlacement = generatedA.NpcPlacements.First(placement => placement.NpcId != StarterNpcs.Mara.Id && placement.NpcId != StarterNpcs.Dallen.Id);
        generatedContentState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(firstGeneratedNpcPlacement.X, firstGeneratedNpcPlacement.Y));
        var generatedNpcSnapshot = generatedContentServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(
            generatedNpcSnapshot.Npcs.Any(npc => npc.Id == firstGeneratedNpcPlacement.NpcId && npc.TileX == firstGeneratedNpcPlacement.X && npc.TileY == firstGeneratedNpcPlacement.Y),
            "interest snapshot exposes seeded generated NPCs near the player");
        ExpectTrue(
            generatedNpcSnapshot.Dialogues.Any(dialogue => dialogue.NpcId == firstGeneratedNpcPlacement.NpcId && dialogue.Choices.Any(choice => choice.Id == "assist_need")),
            "generated NPC dialogue exposes station-specific assist choices");
        ExpectTrue(
            generatedNpcSnapshot.Quests.Any(quest => generatedA.Quests.Any(definition => definition.Id == quest.Id && definition.GiverNpcId == firstGeneratedNpcPlacement.NpcId)),
            "interest snapshot exposes generated station quests for nearby generated NPCs");
        var firstGeneratedQuest = generatedA.Quests.First(quest => quest.GiverNpcId == firstGeneratedNpcPlacement.NpcId);
        var startGeneratedQuest = generatedContentServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            80,
            IntentType.StartQuest,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["questId"] = firstGeneratedQuest.Id
            }));
        ExpectTrue(startGeneratedQuest.WasAccepted, "server accepts generated station quest start near giver");
        var completeGeneratedQuest = generatedContentServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            81,
            IntentType.CompleteQuest,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["questId"] = firstGeneratedQuest.Id
            }));
        ExpectTrue(completeGeneratedQuest.WasAccepted, "server accepts generated station quest completion through dynamic action resolution");
        ExpectEqual(generatedA.OddityPlacements.Count, generatedContentServer.WorldItems.Count, "server seeds generated oddity placements as world items");
        var firstOddityPlacement = generatedA.OddityPlacements[0];
        generatedContentState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(firstOddityPlacement.X, firstOddityPlacement.Y));
        var generatedContentSnapshot = generatedContentServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(
            generatedContentSnapshot.WorldItems.Any(item => item.ItemId == firstOddityPlacement.ItemId && item.TileX == firstOddityPlacement.X && item.TileY == firstOddityPlacement.Y),
            "interest snapshot exposes seeded generated oddities near the player");
        ExpectTrue(artSet.Tiles.ContainsKey(WorldTileIds.ClinicFloor), "theme art registry maps clinic floor tile id");
        ExpectEqual(
            ThemeArtRegistry.PlaceholderAtlasPath,
            artSet.GetTile(WorldTileIds.DoorAirlock).AtlasPath,
            "theme art registry keeps future atlas path for tile ids");
        var playerSprite = PrototypeSpriteCatalog.Get(PrototypeSpriteKind.Player);
        var whoopieSprite = PrototypeSpriteCatalog.Get(PrototypeSpriteKind.WhoopieCushion);
        ExpectEqual("Player", playerSprite.DisplayName, "prototype sprite catalog names player model");
        ExpectTrue(playerSprite.Layers.Count >= 8, "prototype player sprite has layered pixel art");
        ExpectEqual(expectedPlayerAtlasPath, playerSprite.AtlasPath, "prototype player sprite records active engineer atlas path");
        ExpectTrue(playerSprite.HasAtlasRegion, "prototype player sprite can use character atlas art");
        var expectedPlayerWalkFrameCount = expectedPlayerAtlasPath == PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath
            ? 7
            : 4;
        ExpectEqual(
            expectedPlayerWalkFrameCount,
            playerSprite.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkDownAnimation).Frames.Count,
            "prototype player sprite maps multi-frame walk animations");
        var expectedWalkRightFinalFrame = expectedPlayerAtlasPath == PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath
            ? new Rect2(64f, 64f, 32f, 32f)
            : new Rect2(963f, 860f, 120f, 190f);
        ExpectEqual(
            expectedWalkRightFinalFrame,
            playerSprite.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkRightAnimation).Frames[^1],
            "prototype player sprite maps the active engineer sheet walk-right row");
        ExpectEqual(PrototypeSpriteKind.Dallen, PrototypeSpriteCatalog.GetKindForNpc(StarterNpcs.Dallen.Id), "prototype sprite catalog maps Dallen NPC visuals");
        ExpectTrue(PrototypeSpriteCatalog.Get(PrototypeSpriteKind.Dallen).HasAtlasRegion, "prototype Dallen sprite can use character atlas art");
        ExpectTrue(ServerNpcObject.FormatPrompt("Dallen Venn", "Trader", "Free Settlers").Contains("Faction: Free Settlers"), "server NPC prompt formats faction");
        var vendorPrompt = ServerNpcObject.FormatVendorPrompt(
            "Dallen Venn",
            "Trader",
            "Free Settlers",
            new[]
            {
                new ShopOfferSnapshot("offer_one", StarterNpcs.Dallen.Id, StarterItems.WhoopieCushionId, "Whoopie Cushion", ItemCategory.Oddity, 7, "scrip"),
                new ShopOfferSnapshot("offer_two", StarterNpcs.Dallen.Id, StarterItems.RepairKitId, "Repair Kit", ItemCategory.Tool, 18, "scrip")
            },
            1);
        ExpectTrue(vendorPrompt.Contains("9 - Buy Repair Kit"), "server NPC vendor prompt shows selected offer");
        ExpectTrue(vendorPrompt.Contains("[Tool]"), "server NPC vendor prompt shows selected offer category");
        ExpectTrue(vendorPrompt.Contains("Browse shop (2/2)"), "server NPC vendor prompt shows browse position");
        var weaponOfferLine = ShopText.FormatOfferLine(new ShopOfferSnapshot(
            "offer_weapon",
            StarterNpcs.Dallen.Id,
            StarterItems.Rifle27Id,
            "Rifle-27",
            ItemCategory.Weapon,
            86,
            "scrip"));
        ExpectTrue(weaponOfferLine.Contains("Power 24"), "shop offer text shows weapon stats");
        ExpectTrue(whoopieSprite.Layers.Any(layer => layer.Shape == PrototypeSpriteShape.Circle), "prototype item sprite supports rounded prop art");
        ExpectEqual(PrototypeSpriteCatalog.UtilityItemAtlasPath, whoopieSprite.AtlasPath, "prototype item sprite records utility atlas path");
        ExpectTrue(whoopieSprite.HasAtlasRegion, "prototype item sprite can use item atlas art");
        ExpectTrue(PrototypeSpriteCatalog.Get(PrototypeSpriteKind.Scrip).HasAtlasRegion, "prototype currency sprite can use item atlas art");
        ExpectTrue(PrototypeSpriteCatalog.Get(PrototypeSpriteKind.RepairKit).Layers.Count >= 4, "prototype tool sprite has recognizable layers");
        ExpectEqual(PrototypeSpriteCatalog.ItemAtlasPath, PrototypeSpriteCatalog.Get(PrototypeSpriteKind.RationPack).AtlasPath, "prototype support item sprite records item atlas path");
        ExpectTrue(PrototypeSpriteCatalog.Get(PrototypeSpriteKind.PortableTerminal).HasAtlasRegion, "prototype utility item sprite can use utility atlas art");
        ExpectEqual(PrototypeSpriteCatalog.WeaponAtlasPath, PrototypeSpriteCatalog.Get(PrototypeSpriteKind.StunBaton).AtlasPath, "prototype weapon sprite records weapon atlas path");
        ExpectTrue(PrototypeSpriteCatalog.Get(PrototypeSpriteKind.Rifle27).HasAtlasRegion, "prototype weapon sprite can use weapon atlas art");
        ExpectEqual(PrototypeSpriteCatalog.ToolAtlasPath, PrototypeSpriteCatalog.Get(PrototypeSpriteKind.MultiTool).AtlasPath, "prototype tool sprite records tool atlas path");
        ExpectTrue(PrototypeSpriteCatalog.Get(PrototypeSpriteKind.PortableShield).HasAtlasRegion, "prototype tool sprite can use tool atlas art");
        ExpectEqual(36, StarterItems.All.Count, "starter item catalog exposes all prototype items");
        ExpectEqual(
            StarterItems.All.Count,
            StarterItems.All.Select(item => item.Id).Distinct().Count(),
            "starter item catalog ids are unique");
        foreach (var starterItem in StarterItems.All)
        {
            ExpectTrue(StarterItems.TryGetById(starterItem.Id, out _), $"starter item catalog can resolve {starterItem.Id}");
            ExpectTrue(
                PrototypeSpriteCatalog.Get(PrototypeSpriteCatalog.GetKindForItem(starterItem.Id)).HasAtlasRegion,
                $"starter item {starterItem.Id} has mapped prototype art");
        }

        ExpectEqual(new Vector2(620f, 220f), WorldRoot.CalculateCatalogShowcasePosition(0), "catalog showcase starts near the prototype item area");
        ExpectEqual(new Vector2(620f + (6 * 48f), 220f), WorldRoot.CalculateCatalogShowcasePosition(6), "catalog showcase fills a row before wrapping");
        ExpectEqual(new Vector2(620f, 268f), WorldRoot.CalculateCatalogShowcasePosition(7), "catalog showcase wraps to the next row");
        var greenhouse = StructureArtCatalog.Get(StructureSpriteKind.GreenhouseStandard);
        ExpectEqual(StructureArtCatalog.GreenhouseAtlasPath, greenhouse.AtlasPath, "structure catalog records greenhouse atlas path");
        ExpectTrue(greenhouse.HasAtlasRegion, "structure catalog maps greenhouse atlas art");
        ExpectTrue(StructureArtCatalog.All.ContainsKey(StructureSpriteKind.GreenhouseDamaged), "structure catalog maps greenhouse variants");
        var testStructureAtlas = StructureSprite.CreateAtlasTexture(testTexture, greenhouse);
        ExpectEqual(greenhouse.AtlasRegion, testStructureAtlas.Region, "native structure sprite preserves catalog source region");
        var greenhouseFrame = AtlasFrames.FromStructure(greenhouse);
        ExpectEqual(
            new Vector2(greenhouse.Size.X / greenhouse.AtlasRegion.Size.X, greenhouse.Size.Y / greenhouse.AtlasRegion.Size.Y),
            greenhouseFrame.CalculateScale(),
            "shared atlas frame scales structure source art to catalog display size");
        ExpectEqual(new Vector2(0f, -greenhouse.Size.Y * 0.5f), StructureSprite.CalculateOffset(greenhouse), "native structure sprite anchors art to its footprint");
        var greenhouseNode = new StructureSprite();
        ExpectTrue(greenhouseNode.PreferAtlasArt, "structure sprite prefers mapped atlas art");
        ExpectEqual(
            PrototypeSpriteKind.WorkVest,
            PrototypeSpriteCatalog.GetKindForItem(StarterItems.WorkVestId),
            "prototype sprite catalog maps armor item visuals");
        ExpectEqual(
            PrototypeSpriteKind.PracticeStick,
            PrototypeSpriteCatalog.GetKindForItem(StarterItems.PracticeStickId),
            "prototype sprite catalog maps weapon item visuals");
        ExpectEqual(
            PrototypeSpriteKind.PortableTerminal,
            PrototypeSpriteCatalog.GetKindForItem(StarterItems.PortableTerminalId),
            "prototype sprite catalog maps interactible object visuals");
        ExpectEqual(5, generatedA.Locations.Count, "small world generates prototype location count");
        ExpectEqual(12, generatedA.Npcs.Count, "prototype target players generate starter NPC population");
        ExpectTrue(generatedA.Oddities.Any(item => item.Id == StarterItems.DeflatedBalloonId), "generated world includes absurd oddities");
        ExpectTrue(generatedA.Oddities.Any(item => item.Id == StarterItems.PortableTerminalId), "generated world includes interactible prototype objects");
        ExpectTrue(StarterItems.TryGetById(StarterItems.FilterCoreId, out var filterCore) && filterCore.Tags.Contains("quest"), "starter item catalog includes quest objects");
        ExpectTrue(StarterItems.TryGetById(StarterItems.Rifle27Id, out var rifle) && rifle.Power == 24, "starter item catalog includes sci-fi weapons");
        ExpectTrue(StarterItems.TryGetById(StarterItems.MultiToolId, out var multiTool) && multiTool.Tags.Contains("utility"), "starter item catalog includes sci-fi tools");
        ExpectTrue(StarterShopCatalog.Offers.Any(offer => offer.ItemId == StarterItems.RationPackId), "starter shop sells support consumables");
        ExpectTrue(StarterShopCatalog.Offers.Any(offer => offer.ItemId == StarterItems.ElectroPistolId), "starter shop sells prototype weapons");
        ExpectTrue(StarterShopCatalog.Offers.Any(offer => offer.ItemId == StarterItems.PortableShieldId), "starter shop sells prototype tools");
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
        var contentModel = new DeterministicWorldContentModel();
        var modelPrompt = new WorldContentPrompt(
            "test-world",
            "junkyard-fantasy",
            TargetNpcCount: 3,
            TargetQuestCount: 2,
            TargetFactionCount: 2,
            Seed: 42);
        var generatedContent = contentModel.Generate(modelPrompt);
        ExpectTrue(generatedContent.IsUsable, "content generation model returns validated proposals");
        ExpectEqual(contentModel.ModelId, generatedContent.ModelId, "content generation result records model id");
        ExpectEqual(3, generatedContent.Proposal.Npcs.Count, "content generation model respects requested NPC count");
        var contentService = new WorldContentGenerationService(contentModel);
        var generatedApply = contentService.GenerateAndApply(generatedA.ToAdapter(), modelPrompt);
        ExpectTrue(generatedApply.WasApplied, "content generation service applies usable model proposals");
        ExpectTrue(generatedApply.ApplyResult.World.Npcs.Any(npc => npc.Id.StartsWith("generated_npc_42_")), "content generation service adds generated NPCs through adapter boundary");
        var repeatedContent = contentModel.Generate(modelPrompt);
        ExpectEqual(generatedContent.Proposal.Npcs[0].Name, repeatedContent.Proposal.Npcs[0].Name, "deterministic content model is stable for a prompt seed");
        var proposalJson = WorldContentProposalJson.Write(generatedContent.Proposal);
        ExpectTrue(proposalJson.Contains("generated_npc_42_0"), "content proposal JSON writes generated NPC ids");
        var parsedProposal = WorldContentProposalJson.ParseAndValidate(proposalJson);
        ExpectTrue(parsedProposal.IsUsable, "content proposal JSON parses and validates model output");
        ExpectEqual(generatedContent.Proposal.Npcs[0].Name, parsedProposal.Proposal.Npcs[0].Name, "content proposal JSON round-trips NPC data");
        var jsonContentModel = new JsonWorldContentModel(
            "json-prototype-content-v1",
            _ => proposalJson);
        var jsonModelResult = jsonContentModel.Generate(modelPrompt);
        ExpectTrue(jsonModelResult.IsUsable, "JSON content model adapts provider text into validated proposals");
        ExpectEqual("json-prototype-content-v1", jsonModelResult.ModelId, "JSON content model records provider model id");
        var badJsonContentModel = new JsonWorldContentModel(
            "json-bad-content-v1",
            _ => "{ nope");
        ExpectFalse(badJsonContentModel.Generate(modelPrompt).IsUsable, "JSON content model rejects malformed provider text");

        ExpectEqual(0, state.LocalKarma.Score, "new players start at neutral karma");
        ExpectEqual("Unmarked", state.LocalKarma.TierName, "new players start unmarked");
        ExpectTrue(state.LocalScrip > 0, "prototype local player keeps spendable scrip");
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
        var whoopieCountBeforeConsume = state.Inventory.Count(item => item.Id == StarterItems.WhoopieCushionId);
        ExpectTrue(state.HasItem(StarterItems.WhoopieCushionId), "whoopie cushion can be picked up");
        ExpectTrue(state.ConsumeItem(StarterItems.WhoopieCushionId), "whoopie cushion can be consumed");
        ExpectEqual(
            whoopieCountBeforeConsume - 1,
            state.Inventory.Count(item => item.Id == StarterItems.WhoopieCushionId),
            "consumed whoopie cushion removes one inventory item");
        var questServer = new AuthoritativeWorldServer(state, "quest-test-world");
        state.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(3, 4));
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
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { serverStartQuest.Event }).Contains("started Clinic Filters"),
            "HUD formats quest start events");
        ExpectFalse(questServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            2,
            IntentType.CompleteQuest,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["questId"] = StarterQuests.MaraClinicFiltersId
            })).WasAccepted, "server rejects quest completion without required item");
        state.AddItem(StarterItems.RepairKit);
        var scripBeforeQuestCompletion = state.LocalScrip;
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
        ExpectEqual(scripBeforeQuestCompletion + 12, state.LocalScrip, "server quest completion pays scrip reward");
        ExpectEqual("12", serverCompleteQuest.Event.Data["scripReward"], "quest completion event reports scrip reward");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { serverCompleteQuest.Event }).Contains("earned 12 scrip"),
            "HUD formats quest completion events");

        var helpMara = state.ApplyLocalShift(PrototypeActions.HelpMara());
        ExpectTrue(helpMara.Amount > 0, "helping Mara ascends karma");
        ExpectTrue(state.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId) > 0, "helping Mara improves Mara relationship");
        ExpectTrue(state.Factions.GetReputation(StarterFactions.FreeSettlersId, GameState.LocalPlayerId) > 0, "helping Mara improves Free Settlers faction reputation");
        state.ApplyLocalShift(PrototypeActions.HelpPeer());
        ExpectTrue(state.LocalPerks.Any(perk => perk.Id == PerkCatalog.CalmingPresenceId), "positive karma unlocks Calming Presence");

        var scoreAfterHelp = state.LocalKarma.Score;
        var opinionBeforePrank = state.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId);
        var prankMara = state.ApplyLocalShift(PrototypeActions.WhoopieCushionMara());
        ExpectTrue(prankMara.Amount < 0, "whoopie cushion prank descends karma");
        ExpectTrue(state.LocalKarma.Score < scoreAfterHelp, "prank reduces current karma score");
        ExpectEqual(opinionBeforePrank - 4, state.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId), "Calming Presence softens NPC relationship damage");

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
        ExpectTrue(state.LocalPerks.Any(perk => perk.Id == PerkCatalog.RumorcraftId), "negative karma unlocks Rumorcraft");
        ExpectTrue(state.StartEntanglement(
            GameState.LocalPlayerId,
            StarterNpcs.Mara.Id,
            StarterNpcs.Dallen.Id,
            EntanglementType.Debt,
            PrototypeActions.StartMaraEntanglement()), "rumorcraft test entanglement can be started");
        var rumorcraftEntanglement = state.Entanglements.All.Last();
        ExpectTrue(state.ExposeEntanglement(
            GameState.LocalPlayerId,
            rumorcraftEntanglement.Id,
            PrototypeActions.ExposeMaraEntanglement()), "Rumorcraft player can expose another entanglement");
        var rumorcraftRumor = state.WorldEvents.Events.Last(worldEvent => worldEvent.Type == WorldEventType.Rumor);
        ExpectTrue(rumorcraftRumor.IsGlobal, "Rumorcraft makes exposed rumors global");
        ExpectTrue(rumorcraftRumor.Summary.Contains("Rumorcraft"), "Rumorcraft marks amplified rumor summaries");
        state.ApplyLocalShift(PrototypeActions.AttackPeer());
        ExpectTrue(state.LocalPerks.Any(perk => perk.Id == PerkCatalog.DreadReputationId), "negative karma unlocks Dread Reputation");
        var maraOpinionBeforeDreadMock = state.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId);
        state.ApplyLocalShift(PrototypeActions.MockMaraWithBalloon());
        ExpectEqual(
            maraOpinionBeforeDreadMock - 11,
            state.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId),
            "Dread Reputation softens intimidation-style NPC reaction damage");

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
        ExpectEqual("Progress: 54/100 toward Abyssal 3", state.LocalKarma.RankProgress.Summary, "low negative karma shows progress toward next Abyssal rank");
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
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { stealTransfer.Event }).Contains("stole Repair Kit"),
            "HUD formats stolen item transfers");
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
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { returnTransfer.Event }).Contains("gave Repair Kit"),
            "HUD formats gifted item transfers");
        var localScripBeforeTransfer = state.LocalScrip;
        var peerScripBeforeTransfer = state.Players["peer_stand_in"].Scrip;
        var scripTransfer = transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            3,
            IntentType.TransferCurrency,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in",
                ["amount"] = "5",
                ["mode"] = "gift"
            }));
        ExpectTrue(scripTransfer.WasAccepted, "server transfers scrip gifts between nearby players");
        ExpectEqual(localScripBeforeTransfer - 5, state.LocalScrip, "scrip gift debits actor wallet");
        ExpectEqual(peerScripBeforeTransfer + 5, state.Players["peer_stand_in"].Scrip, "scrip gift credits target wallet");
        ExpectEqual("gift", scripTransfer.Event.Data["mode"], "scrip gift event reports mode");
        ExpectTrue(scripTransfer.Event.EventId.Contains("currency_transferred"), "scrip transfer emits server event");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { scripTransfer.Event }).Contains("gave 5 scrip"),
            "HUD formats currency gifts");
        var localScripBeforeSteal = state.LocalScrip;
        var peerScripBeforeSteal = state.Players["peer_stand_in"].Scrip;
        var scripSteal = transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            4,
            IntentType.TransferCurrency,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in",
                ["amount"] = "3",
                ["mode"] = "steal"
            }));
        ExpectTrue(scripSteal.WasAccepted, "server transfers stolen scrip between nearby players");
        ExpectEqual(localScripBeforeSteal + 3, state.LocalScrip, "scrip steal credits actor wallet");
        ExpectEqual(peerScripBeforeSteal - 3, state.Players["peer_stand_in"].Scrip, "scrip steal debits target wallet");
        ExpectEqual("steal", scripSteal.Event.Data["mode"], "scrip steal event reports mode");
        ExpectTrue(int.Parse(scripSteal.Event.Data["karmaAmount"]) < 0, "scrip steal descends actor karma");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { scripSteal.Event }).Contains("stole 3 scrip"),
            "HUD formats currency theft");
        ExpectFalse(transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            5,
            IntentType.TransferCurrency,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in",
                ["amount"] = "9999",
                ["mode"] = "gift"
            })).WasAccepted, "server rejects scrip transfer without funds");
        ExpectFalse(transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            6,
            IntentType.TransferCurrency,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in",
                ["amount"] = "1",
                ["mode"] = "borrow-ish"
            })).WasAccepted, "server rejects unknown scrip transfer mode");
        var localScripBeforePurchase = state.LocalScrip;
        var shopPurchase = transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            7,
            IntentType.PurchaseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["offerId"] = StarterShopCatalog.DallenWhoopieCushionOfferId
            }));
        ExpectTrue(shopPurchase.WasAccepted, "server accepts nearby shop purchase");
        ExpectEqual(localScripBeforePurchase - 7, state.LocalScrip, "shop purchase debits scrip");
        ExpectTrue(state.HasItem(GameState.LocalPlayerId, StarterItems.WhoopieCushionId), "shop purchase adds item to inventory");
        ExpectTrue(shopPurchase.Event.EventId.Contains("item_purchased"), "shop purchase emits server event");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { shopPurchase.Event }).Contains("bought Whoopie Cushion"),
            "HUD formats shop purchases");
        var shopSnapshot = transferServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(
            shopSnapshot.ShopOffers.Any(offer => offer.OfferId == StarterShopCatalog.DallenWhoopieCushionOfferId && offer.Price == 7),
            "interest snapshot includes nearby shop offers");
        ExpectFalse(transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            8,
            IntentType.PurchaseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["offerId"] = StarterShopCatalog.DallenWorkVestOfferId
            })).WasAccepted, "server rejects shop purchase without enough scrip");
        state.ApplyShift(GameState.LocalPlayerId, PrototypeActions.HelpPeer());
        state.ApplyShift(GameState.LocalPlayerId, PrototypeActions.HelpPeer());
        state.AddScrip(GameState.LocalPlayerId, 20);
        var discountedShopSnapshot = transferServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(
            discountedShopSnapshot.ShopOffers.Any(offer => offer.OfferId == StarterShopCatalog.DallenRepairKitOfferId && offer.Price == 17),
            "interest snapshot applies trusted shop discount");
        var localScripBeforeDiscountPurchase = state.LocalScrip;
        var discountedPurchase = transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            9,
            IntentType.PurchaseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["offerId"] = StarterShopCatalog.DallenRepairKitOfferId
            }));
        ExpectTrue(discountedPurchase.WasAccepted, "server accepts discounted shop purchase");
        ExpectEqual(localScripBeforeDiscountPurchase - 17, state.LocalScrip, "discounted shop purchase debits final price");
        ExpectEqual("18", discountedPurchase.Event.Data["basePrice"], "discounted purchase event reports base price");
        ExpectEqual("17", discountedPurchase.Event.Data["price"], "discounted purchase event reports final price");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { discountedPurchase.Event }).Contains("base 18"),
            "HUD formats discounted shop purchases");
        state.AddItem("peer_stand_in", StarterItems.WhoopieCushion);
        ExpectTrue(state.SetPlayerTeam("peer_stand_in", "ad-hoc-posse"), "players can hold temporary team status");
        var peerKarmaBreak = transferServer.ProcessIntent(new ServerIntent(
            "peer_stand_in",
            1,
            IntentType.KarmaBreak,
            new System.Collections.Generic.Dictionary<string, string>()));
        ExpectTrue(peerKarmaBreak.WasAccepted, "server accepts peer Karma Break intent");
        ExpectFalse(state.HasItem("peer_stand_in", StarterItems.WhoopieCushionId), "Karma Break drains loose inventory");
        ExpectFalse(state.Players["peer_stand_in"].HasTeam, "Karma Break clears temporary team status");
        var peerRespawnPosition = state.Players["peer_stand_in"].Position;
        ExpectTrue(peerRespawnPosition.DistanceSquaredTo(TilePosition.Origin) >= 144, "explicit Karma Break respawns peer away from its death location");
        ExpectTrue(
            transferServer.WorldItems.Values.Any(entity => entity.EntityId.StartsWith("drop_peer_stand_in") && entity.Item.Id == StarterItems.WhoopieCushionId),
            "Karma Break drops loose inventory as world items");
        ExpectTrue(peerKarmaBreak.Event.Data["droppedItemCount"] != "0", "Karma Break event reports dropped items");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { peerKarmaBreak.Event }).Contains($"respawned at {peerRespawnPosition.X},{peerRespawnPosition.Y}"),
            "HUD formats explicit Karma Break outcome from server event data");
        var peerDropId = transferServer.WorldItems.Values
            .First(entity => entity.EntityId.StartsWith("drop_peer_stand_in") && entity.Item.Id == StarterItems.WhoopieCushionId)
            .EntityId;
        var karmaBeforeDropPickup = state.LocalKarma.Score;
        var dropPickup = transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            10,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = peerDropId
            }));
        ExpectTrue(dropPickup.WasAccepted, "server accepts pickup of another player's death drop");
        ExpectTrue(state.HasItem(GameState.LocalPlayerId, StarterItems.WhoopieCushionId), "death drop pickup enters inventory");
        ExpectTrue(state.LocalKarma.Score < karmaBeforeDropPickup, "claiming another player's death drop descends karma");
        ExpectEqual("peer_stand_in", dropPickup.Event.Data["dropOwnerId"], "death drop pickup event reports owner");
        ExpectEqual("Stranded Player", dropPickup.Event.Data["dropOwnerName"], "death drop pickup event reports owner name");
        ExpectFalse(transferServer.GetInterestFor(GameState.LocalPlayerId).VisibleEntityIds.Contains(peerDropId), "picked up death drop leaves interest set");
        state.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(0, 0));
        state.SetPlayerPosition("peer_stand_in", new TilePosition(1, 0));
        var karmaBeforeReturningDrop = state.LocalKarma.Score;
        var returnedDrop = transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            11,
            IntentType.TransferItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["targetId"] = "peer_stand_in",
                ["itemId"] = StarterItems.WhoopieCushionId,
                ["mode"] = "gift"
            }));
        ExpectTrue(returnedDrop.WasAccepted, "server accepts returning another player's Karma Break drop");
        ExpectTrue(state.LocalKarma.Score > karmaBeforeReturningDrop, "returning another player's Karma Break drop ascends giver karma");
        ExpectEqual("True", returnedDrop.Event.Data["returnedDrop"], "returned drop transfer event is marked");
        ExpectEqual("Stranded Player", returnedDrop.Event.Data["dropOwnerName"], "returned drop transfer event reports owner name");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { returnedDrop.Event }).Contains("returned Whoopie Cushion from Stranded Player's Karma Break drop"),
            "HUD formats returned Karma Break drops");
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
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { requestDuel.Event }).Contains("requested duel_"),
            "HUD formats duel request events");
        var acceptDuel = duelServer.ProcessIntent(new ServerIntent(
            "peer_stand_in",
            1,
            IntentType.AcceptDuel,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["challengerId"] = GameState.LocalPlayerId
            }));
        ExpectTrue(acceptDuel.WasAccepted, "server accepts matching duel acceptance");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { acceptDuel.Event }).Contains("Status: Active"),
            "HUD formats duel acceptance events");
        ExpectTrue(state.Duels.IsActive(GameState.LocalPlayerId, "peer_stand_in"), "accepted duel becomes active");
        state.SetPlayerPosition("rival_renegade", new TilePosition(80, 80));
        ExpectTrue(
            duelServer.CreateInterestSnapshot(GameState.LocalPlayerId).Duels.Any(duel => duel.Status == DuelStatus.Active),
            "interest snapshot includes visible active duel state");
        ExpectFalse(
            duelServer.CreateInterestSnapshot("rival_renegade").Duels.Any(),
            "interest snapshot hides distant duel state");
        ExpectFalse(
            duelServer.CreateInterestSnapshot("rival_renegade").ShopOffers.Any(),
            "interest snapshot hides distant shop offers");
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
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { serverMove.Event }).Contains("moved to 3,4"),
            "HUD formats movement events");
        var localInterest = server.GetInterestFor(GameState.LocalPlayerId);
        ExpectTrue(localInterest.VisiblePlayerIds.Contains("peer_stand_in"), "interest area includes nearby players");
        ExpectFalse(localInterest.VisiblePlayerIds.Contains("rival_paragon"), "interest area excludes distant players");
        ExpectTrue(localInterest.VisibleEntityIds.Contains("pickup_practice_stick"), "interest area includes nearby pickup entities");
        ExpectTrue(localInterest.VisibleStructureIds.Contains("structure_greenhouse_standard"), "interest area includes nearby structures");
        ExpectTrue(localInterest.VisibleStructureIds.Contains("structure_greenhouse_planter"), "interest area includes greenhouse prop structures");

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
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { serverHelp.Event }).Contains("Help Peer"),
            "HUD formats karma shift events");
        ExpectEqual(2, server.EventLog.Count, "server records accepted move and karma intent events");
        var interestSnapshot = server.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectEqual(2, interestSnapshot.Players.Count, "interest snapshot includes self and nearby players");
        ExpectEqual(MatchStatus.Running, interestSnapshot.Match.Status, "interest snapshot includes match status");
        ExpectFalse(interestSnapshot.SyncHint.IsDelta, "full interest snapshot reports non-delta sync hint");
        ExpectEqual(0L, interestSnapshot.SyncHint.AfterTick, "full interest snapshot records zero after-tick");
        ExpectEqual(2, interestSnapshot.SyncHint.ServerEventCount, "interest snapshot sync hint counts visible server events");
        ExpectTrue(interestSnapshot.Players.Any(player => player.Id == GameState.LocalPlayerId), "interest snapshot includes local player");
        ExpectTrue(interestSnapshot.Players.Any(player => player.Id == "peer_stand_in"), "interest snapshot includes nearby peer");
        ExpectFalse(interestSnapshot.Players.Any(player => player.Id == "rival_paragon"), "interest snapshot excludes distant rival");
        ExpectTrue(interestSnapshot.Npcs.Any(npc => npc.Id == StarterNpcs.Mara.Id), "interest snapshot includes visible NPCs");
        ExpectTrue(interestSnapshot.Npcs.Any(npc => npc.Id == StarterNpcs.Dallen.Id), "interest snapshot includes visible vendor NPCs");
        ExpectTrue(interestSnapshot.ShopOffers.Any(offer => offer.VendorNpcId == StarterNpcs.Dallen.Id), "interest snapshot includes visible vendor offers");
        ExpectTrue(interestSnapshot.Structures.Any(structure => structure.StructureId == StructureArtCatalog.Get(StructureSpriteKind.GreenhouseStandard).Id), "interest snapshot includes visible structures");
        ExpectTrue(interestSnapshot.Structures.Count >= 3, "interest snapshot includes starter greenhouse structure set");
        ExpectTrue(interestSnapshot.Structures.All(structure => structure.WidthPx > 0 && structure.HeightPx > 0), "structure snapshots include render footprint");
        ExpectTrue(interestSnapshot.Structures.Any(structure => structure.IsInteractable && structure.InteractionPrompt.Contains("inspect")), "interest snapshot includes structure interaction prompt");
        ExpectTrue(interestSnapshot.Structures.Any(structure => structure.EntityId == "structure_greenhouse_standard" && structure.Integrity == 75 && structure.Condition == "stable"), "structure snapshots expose integrity state");
        var greenhouseInteract = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            3,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "structure_greenhouse_standard",
                ["action"] = "inspect"
            }));
        ExpectTrue(greenhouseInteract.WasAccepted, "server accepts nearby structure inspection");
        ExpectEqual(StructureArtCatalog.Get(StructureSpriteKind.GreenhouseStandard).Id, greenhouseInteract.Event.Data["structureId"], "structure interaction event reports structure id");
        ExpectEqual("inspect", greenhouseInteract.Event.Data["action"], "structure inspection event reports action");
        ExpectTrue(state.WorldEvents.Events.Any(worldEvent => worldEvent.Type == WorldEventType.Structure), "structure interaction records world event");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { greenhouseInteract.Event }).Contains("inspected Greenhouse"),
            "HUD formats structure inspection events");
        ExpectFalse(server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            4,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "structure_greenhouse_standard",
                ["action"] = "repair"
            })).WasAccepted, "server rejects structure repair without a tool");
        state.AddItem(StarterItems.MultiTool);
        var scripBeforeStructureRepair = state.LocalScrip;
        var structureRepair = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            5,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "structure_greenhouse_standard",
                ["action"] = "repair"
            }));
        ExpectTrue(structureRepair.WasAccepted, "server accepts structure repair with a tool");
        ExpectEqual("repair", structureRepair.Event.Data["action"], "structure repair event reports action");
        ExpectEqual("95", structureRepair.Event.Data["integrity"], "structure repair increases integrity");
        ExpectEqual("4", structureRepair.Event.Data["scripReward"], "structure repair pays bounty from repaired integrity");
        ExpectEqual(scripBeforeStructureRepair + 4, state.LocalScrip, "structure repair bounty credits actor wallet");
        ExpectEqual("4", structureRepair.Event.Data["factionDelta"], "structure repair reports civic faction gain");
        ExpectEqual(4, state.Factions.GetReputation(StarterFactions.CivicRepairGuildId, GameState.LocalPlayerId), "structure repair improves civic repair reputation");
        ExpectTrue(int.Parse(structureRepair.Event.Data["karmaAmount"]) > 0, "structure repair ascends actor karma");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { structureRepair.Event }).Contains("+4 scrip"),
            "HUD formats structure repair bounty");
        var structureSabotage = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            6,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "structure_greenhouse_standard",
                ["action"] = "sabotage"
            }));
        ExpectTrue(structureSabotage.WasAccepted, "server accepts structure sabotage");
        ExpectEqual("sabotage", structureSabotage.Event.Data["action"], "structure sabotage event reports action");
        ExpectEqual("70", structureSabotage.Event.Data["integrity"], "structure sabotage decreases integrity");
        ExpectEqual("-6", structureSabotage.Event.Data["factionDelta"], "structure sabotage reports civic faction loss");
        ExpectEqual(-2, state.Factions.GetReputation(StarterFactions.CivicRepairGuildId, GameState.LocalPlayerId), "structure sabotage damages civic repair reputation");
        ExpectTrue(int.Parse(structureSabotage.Event.Data["karmaAmount"]) < 0, "structure sabotage descends actor karma");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { structureSabotage.Event }).Contains("sabotaged Greenhouse"),
            "HUD formats structure sabotage events");
        var repairedStructureSnapshot = server.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(repairedStructureSnapshot.Structures.Any(structure => structure.EntityId == "structure_greenhouse_standard" && structure.Integrity == 70), "structure snapshot reflects latest integrity");
        server.SetTileMap(generatedA.TileMap);
        var mapChunkSnapshot = server.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(mapChunkSnapshot.MapChunks.Any(chunk => chunk.Tiles.Any(tile => tile.FloorId == WorldTileIds.ClinicFloor)), "interest snapshot includes nearby map chunk tiles");
        ExpectEqual(mapChunkSnapshot.MapChunks.Count, mapChunkSnapshot.SyncHint.VisibleMapChunkCount, "interest snapshot sync hint counts visible map chunks");
        ExpectTrue(mapChunkSnapshot.SyncHint.VisibleMapRevision != 0, "interest snapshot sync hint carries visible map revision checksum");
        ExpectTrue(interestSnapshot.Dialogues.Any(dialogue => dialogue.NpcId == StarterNpcs.Mara.Id), "interest snapshot includes visible NPC dialogue");
        ExpectTrue(
            interestSnapshot.Dialogues.Any(dialogue => dialogue.Choices.Any(choice => choice.Id == "help_filters")),
            "interest snapshot includes server-approved dialogue choices");
        ExpectTrue(interestSnapshot.Quests.Any(quest => quest.Id == StarterQuests.MaraClinicFiltersId), "interest snapshot includes visible NPC quests");
        ExpectTrue(interestSnapshot.Quests.Any(quest => quest.Id == StarterQuests.MaraClinicFiltersId && quest.ScripReward == 12), "interest snapshot includes quest scrip reward");
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
            7,
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
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { serverPlace.Event }).Contains("placed Deflated Balloon at 4,4"),
            "HUD formats placed object events");
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
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { peerPickupPlaced.Event }).Contains("picked up Deflated Balloon"),
            "HUD formats placed object pickups");

        var serverPickup = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            8,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "pickup_practice_stick"
            }));
        ExpectTrue(serverPickup.WasAccepted, "server accepts nearby pickup intent");
        ExpectTrue(state.HasItem(GameState.LocalPlayerId, StarterItems.PracticeStickId), "server pickup adds item to player inventory");
        ExpectFalse(server.GetInterestFor(GameState.LocalPlayerId).VisibleEntityIds.Contains("pickup_practice_stick"), "picked up entity leaves interest set");
        ExpectFalse(server.CreateInterestSnapshot(GameState.LocalPlayerId).WorldItems.Any(entity => entity.EntityId == "pickup_practice_stick"), "picked up entity leaves interest snapshot");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { serverPickup.Event }).Contains("picked up Practice Stick"),
            "HUD formats world item pickups");
        ExpectFalse(server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            9,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "pickup_practice_stick"
            })).WasAccepted, "server rejects duplicate pickup intent");

        state.AddItem(StarterItems.RepairKit);
        var repairKitCountBeforeUse = state.Inventory.Count(item => item.Id == StarterItems.RepairKitId);
        state.DamagePlayer(GameState.LocalPlayerId, "peer_stand_in", 40, "repair kit smoke test");
        var peerHealthBeforeRepair = state.Players["peer_stand_in"].Health;
        var serverRepair = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            10,
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = StarterItems.RepairKitId,
                ["targetId"] = "peer_stand_in"
            }));
        ExpectTrue(serverRepair.WasAccepted, "server accepts repair kit use intent");
        ExpectEqual(peerHealthBeforeRepair + 25, state.Players["peer_stand_in"].Health, "repair kit heals nearby target");
        ExpectEqual(repairKitCountBeforeUse - 1, state.Inventory.Count(item => item.Id == StarterItems.RepairKitId), "repair kit use consumes one item");
        ExpectTrue(serverRepair.Event.EventId.Contains("item_used"), "repair kit use emits item event");
        ExpectEqual(state.Players["peer_stand_in"].Health.ToString(), serverRepair.Event.Data["targetHealth"], "repair kit event reports target health");

        var serverEquip = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            11,
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = StarterItems.PracticeStickId
            }));
        ExpectTrue(serverEquip.WasAccepted, "server accepts equippable item intent");
        ExpectEqual(10, state.LocalPlayer.AttackPower, "server item intent equips weapon");
        ExpectTrue(serverEquip.Event.EventId.Contains("item_equipped"), "server item intent emits equipment event");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { serverEquip.Event }).Contains("equipped Practice Stick"),
            "HUD formats equipment events");
        ExpectFalse(server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            12,
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = "missing_item"
            })).WasAccepted, "server rejects unknown item intent");

        var peerHealthBeforeAttack = state.Players["peer_stand_in"].Health;
        var serverAttack = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            13,
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
        ExpectTrue(postAttackSnapshot.ServerEvents.Any(serverEvent => serverEvent.EventId.Contains("structure_interacted")), "interest snapshot includes visible structure events");
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
            13,
            IntentType.KarmaAction,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["action"] = PrototypeActions.AttackPeerId
            }));
        ExpectFalse(staleIntent.WasAccepted, "server rejects duplicate sequence intent");
        ExpectTrue(state.LocalKarma.Score > 0, "rejected stale intent does not mutate karma");
        var deltaInterestSnapshot = server.CreateInterestSnapshot(GameState.LocalPlayerId, afterTick: 2);
        ExpectEqual(13, deltaInterestSnapshot.ServerEvents.Count, "interest snapshot can return visible events after a tick");
        ExpectTrue(deltaInterestSnapshot.SyncHint.IsDelta, "delta interest snapshot reports delta sync hint");
        ExpectEqual(2L, deltaInterestSnapshot.SyncHint.AfterTick, "delta interest snapshot records requested after-tick");
        ExpectEqual(deltaInterestSnapshot.ServerEvents.Count, deltaInterestSnapshot.SyncHint.ServerEventCount, "delta sync hint counts returned server events");
        ExpectEqual(deltaInterestSnapshot.WorldEvents.Count, deltaInterestSnapshot.SyncHint.WorldEventCount, "delta sync hint counts returned world events");
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
            14,
            IntentType.StartDialogue,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Mara.Id
            }));
        ExpectTrue(startDialogue.WasAccepted, "server accepts visible NPC dialogue intent");
        ExpectTrue(startDialogue.Event.Data["choiceIds"].Contains("help_filters"), "dialogue event includes approved choice ids");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { startDialogue.Event }).Contains("talking with Mara Venn"),
            "HUD formats dialogue start events");
        var karmaBeforeDialogueChoice = state.LocalKarma.Score;
        var selectDialogueChoice = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            15,
            IntentType.SelectDialogueChoice,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Mara.Id,
                ["choiceId"] = "help_filters"
            }));
        ExpectTrue(selectDialogueChoice.WasAccepted, "server accepts approved dialogue choice intent");
        ExpectTrue(state.LocalKarma.Score > karmaBeforeDialogueChoice, "dialogue choice applies authoritative karma action");
        ExpectTrue(selectDialogueChoice.Event.EventId.Contains("dialogue_choice_selected"), "dialogue choice emits server event");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { selectDialogueChoice.Event }).Contains("Repair the filters"),
            "HUD formats dialogue choice events");
        ExpectFalse(server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            16,
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
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { serverStartEntanglement.Event }).Contains("Romantic entanglement with Mara Venn"),
            "HUD formats entanglement start events");
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
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { serverExposeEntanglement.Event }).Contains("between Mara Venn and Dallen Venn"),
            "HUD formats entanglement exposure events");
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
        ExpectTrue(snapshot.Players.Any(player => player.Id == GameState.LocalPlayerId && player.Scrip == state.LocalScrip), "snapshot captures player scrip");
        ExpectTrue(snapshot.Summary.Contains("players"), "snapshot has readable summary");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"leaderboard\""), "snapshot JSON includes leaderboard");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"standing\""), "snapshot JSON includes player standing");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"tileX\""), "snapshot JSON includes tile position");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"inventory\""), "snapshot JSON includes per-player inventory");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"scrip\""), "snapshot JSON includes player scrip");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"statusEffects\""), "snapshot JSON includes player status effects");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"duels\""), "snapshot JSON includes duels");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"scripReward\""), "snapshot JSON includes quest scrip rewards");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"karmaProgress\""), "snapshot JSON includes karma progress");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"players\""), "snapshot can be exported as JSON debug text");

        var largeServer = new AuthoritativeWorldServer(state, "large-test-world", ServerConfig.Large100Player);
        var largeJoin = largeServer.JoinPlayer("large_extra_player", "Large Extra Player");
        ExpectTrue(largeJoin.WasAccepted, "large server profile accepts extra player slots");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { largeServer.EventLog[^1] }).Contains("Large Extra Player joined"),
            "HUD formats player join events");
        var pingResponse = AuthoritativeNetworkProtocol.Handle(
            largeServer,
            NetworkClientMessage.Ping("msg_ping", GameState.LocalPlayerId));
        ExpectEqual(NetworkServerMessageType.Pong, pingResponse.Type, "network protocol responds to ping messages");
        ExpectEqual("msg_ping", pingResponse.CorrelationId, "network protocol preserves message correlation ids");
        var pingResponseJson = NetworkProtocolJson.WriteServer(pingResponse);
        ExpectTrue(pingResponseJson.Contains("\"Type\":\"Pong\""), "network protocol JSON writes readable server message types");
        ExpectEqual(
            NetworkServerMessageType.Pong,
            NetworkProtocolJson.ReadServer(pingResponseJson).Type,
            "network protocol JSON round-trips server messages");

        var snapshotResponse = AuthoritativeNetworkProtocol.Handle(
            largeServer,
            NetworkClientMessage.RequestSnapshot("msg_snapshot", GameState.LocalPlayerId, afterTick: 2));
        ExpectEqual(NetworkServerMessageType.Snapshot, snapshotResponse.Type, "network protocol returns interest snapshots");
        ExpectTrue(snapshotResponse.Snapshot.SyncHint.IsDelta, "network protocol snapshot request preserves delta cursor");

        var protocolMove = AuthoritativeNetworkProtocol.Handle(
            largeServer,
            NetworkClientMessage.SendIntent(
                "msg_move",
                new ServerIntent(
                    GameState.LocalPlayerId,
                    1,
                    IntentType.Move,
                    new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["x"] = "7",
                        ["y"] = "8"
                    })));
        var protocolMoveJson = NetworkProtocolJson.WriteClient(NetworkClientMessage.SendIntent(
            "msg_move_json",
            new ServerIntent(
                GameState.LocalPlayerId,
                20,
                IntentType.Move,
                new System.Collections.Generic.Dictionary<string, string>
                {
                    ["x"] = "9",
                    ["y"] = "10"
                })));
        var protocolMoveFromJson = NetworkProtocolJson.ReadClient(protocolMoveJson);
        ExpectEqual(NetworkClientMessageType.Intent, protocolMoveFromJson.Type, "network protocol JSON round-trips client message types");
        ExpectEqual(IntentType.Move, protocolMoveFromJson.Intent.Type, "network protocol JSON round-trips intent types");
        ExpectEqual("9", protocolMoveFromJson.Intent.Payload["x"], "network protocol JSON round-trips intent payloads");
        ExpectEqual(NetworkServerMessageType.IntentResult, protocolMove.Type, "network protocol handles sequenced player intents");
        ExpectTrue(protocolMove.IntentResult.WasAccepted, "network protocol returns accepted intent result");
        ExpectEqual(7, state.LocalPlayer.Position.X, "network protocol accepted intent mutates authoritative state");
        ExpectTrue(protocolMove.Snapshot.Players.Any(player => player.Id == GameState.LocalPlayerId), "network protocol intent response includes follow-up snapshot");

        var malformedResponse = AuthoritativeNetworkProtocol.Handle(
            largeServer,
            new NetworkClientMessage("msg_bad", GameState.LocalPlayerId, NetworkClientMessageType.Intent, string.Empty, 0, null));
        ExpectEqual(NetworkServerMessageType.Error, malformedResponse.Type, "network protocol rejects malformed intent messages");

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

    private static MapChunkSnapshot ToMapChunkSnapshot(GeneratedTileChunk chunk)
    {
        return new MapChunkSnapshot(
            $"{chunk.Coordinate.X}:{chunk.Coordinate.Y}",
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

    private static int CountTransparentPixels(Image image)
    {
        var transparentPixels = 0;
        for (var y = 0; y < image.GetHeight(); y++)
        {
            for (var x = 0; x < image.GetWidth(); x++)
            {
                if (image.GetPixel(x, y).A <= 0.01f)
                {
                    transparentPixels++;
                }
            }
        }

        return transparentPixels;
    }

    private static int CountPixelDifferences(Image image, Vector2I firstFrame, Vector2I secondFrame)
    {
        var differences = 0;
        for (var y = 0; y < CharacterSheetLayout.StandardFrameSize; y++)
        {
            for (var x = 0; x < CharacterSheetLayout.StandardFrameSize; x++)
            {
                var first = image.GetPixel(
                    (firstFrame.X * CharacterSheetLayout.StandardFrameSize) + x,
                    (firstFrame.Y * CharacterSheetLayout.StandardFrameSize) + y);
                var second = image.GetPixel(
                    (secondFrame.X * CharacterSheetLayout.StandardFrameSize) + x,
                    (secondFrame.Y * CharacterSheetLayout.StandardFrameSize) + y);
                if (!first.IsEqualApprox(second))
                {
                    differences++;
                }
            }
        }

        return differences;
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
