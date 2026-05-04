using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using Karma.Art;
using Karma.Audio;
using Karma.Core;
using Karma.Data;
using Karma.Generation;
using Karma.Net;
using Karma.Npc;
using Karma.Player;
using Karma.Quests;
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
        ExpectEqual("res://scenes/MainMenu.tscn", ProjectSettings.GetSetting("application/run/main_scene").AsString(), "project boots into the main menu scene");
        ExpectTrue(ResourceLoader.Exists("res://scenes/MainMenu.tscn"), "main menu prototype scene exists");
        ExpectTrue(ResourceLoader.Exists(MainMenuController.GameplayScenePath), "main menu start target gameplay scene exists");
        ExpectTrue(FileAccess.FileExists("res://assets/audio/music/main_menu_theme_placeholder.wav"), "main menu placeholder theme asset exists");
        ExpectTrue(FileAccess.FileExists(MainMenuController.MenuThemePath), "main menu medieval music asset exists");
        ExpectTrue(MainMenuController.LoadMenuThemeStream() is not null, "main menu loads the medieval music stream");
        var menuScene = ResourceLoader.Load<PackedScene>("res://scenes/MainMenu.tscn");
        var menuInstance = menuScene.Instantiate<Control>();
        AddChild(menuInstance);
        ExpectTrue(menuInstance.GetNodeOrNull<Button>("Root/MenuPanel/MenuMargin/MenuButtons/StartButton") is not null, "main menu exposes a start game button");
        ExpectTrue(menuInstance.GetNodeOrNull<Button>("Root/MenuPanel/MenuMargin/MenuButtons/OptionsButton") is not null, "main menu exposes an options button");
        ExpectTrue(menuInstance.GetNodeOrNull<AudioStreamPlayer>("MenuThemePlayer") is not null, "main menu includes a placeholder theme music player");
        ExpectTrue(
            menuInstance.GetNode<PanelContainer>("Root/MenuPanel").GetThemeStylebox("panel") is StyleBoxTexture,
            "main menu panel uses the selected fantasy panel frame");
        ExpectTrue(menuInstance.GetNodeOrNull<Control>("Root/OptionsPanel") is not null, "main menu includes an options panel prototype");
        ExpectTrue(
            menuInstance.GetNode<PanelContainer>("Root/OptionsPanel").GetThemeStylebox("panel") is StyleBoxTexture,
            "options panel uses the selected fantasy panel frame");
        ExpectTrue(
            menuInstance.GetNode<PanelContainer>("Root/CreditsPanel").GetThemeStylebox("panel") is StyleBoxTexture,
            "credits panel uses the selected fantasy panel frame");
        ExpectTrue(menuInstance.GetNodeOrNull<OptionButton>("Root/OptionsPanel/PanelMargin/OptionsContent/VideoGrid/ResolutionOption") is not null, "options menu includes resolution selection");
        ExpectTrue(menuInstance.GetNodeOrNull<Button>("Root/OptionsPanel/PanelMargin/OptionsContent/VideoGrid/DetectResolutionButton") is not null, "options menu includes display resolution detection");
        ExpectTrue(menuInstance.GetNodeOrNull<HSlider>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/MasterVolumeSlider") is not null, "options menu includes audio volume sliders");
        ExpectTrue(menuInstance.GetNodeOrNull<HSlider>("Root/OptionsPanel/PanelMargin/OptionsContent/AudioGrid/AmbientVolumeSlider") is not null, "options menu exposes the four-bus Ambient slider");
        ExpectTrue(menuInstance.GetNodeOrNull<Button>("Root/OptionsPanel/PanelMargin/OptionsContent/OptionsActions/ApplyOptionsButton") is not null, "options menu includes apply/save action");
        menuInstance.QueueFree();

        var hudProbe = new HudController();
        AddChild(hudProbe);
        ExpectTrue(hudProbe.GetNodeOrNull<PanelContainer>("HudRoot/EscapeMenuPanel") is not null, "gameplay HUD includes a non-pausing Escape menu overlay");
        ExpectTrue(hudProbe.GetNodeOrNull<Button>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/ResumeButton") is not null, "Escape menu includes resume action");
        ExpectTrue(hudProbe.GetNodeOrNull<Button>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/OptionsButton") is not null, "Escape menu includes options action");
        ExpectTrue(hudProbe.GetNodeOrNull<Button>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearanceButton") is not null, "Escape menu includes appearance action");
        ExpectTrue(hudProbe.GetNodeOrNull<PanelContainer>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel") is not null, "Escape menu includes appearance selection panel");
        ExpectTrue(hudProbe.GetNodeOrNull<Label>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel/AppearanceMargin/AppearanceContent/AppearanceSkinLabel") is not null, "appearance panel shows current skin label");
        ExpectTrue(hudProbe.GetNodeOrNull<Label>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel/AppearanceMargin/AppearanceContent/AppearanceHairLabel") is not null, "appearance panel shows current hair label");
        ExpectTrue(hudProbe.GetNodeOrNull<Label>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel/AppearanceMargin/AppearanceContent/AppearanceOutfitLabel") is not null, "appearance panel shows current outfit label");
        ExpectTrue(hudProbe.GetNodeOrNull<Label>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel/AppearanceMargin/AppearanceContent/AppearancePantsLabel") is not null, "appearance panel shows current pants label");
        ExpectTrue(hudProbe.GetNodeOrNull<Label>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel/AppearanceMargin/AppearanceContent/AppearanceShirtLabel") is not null, "appearance panel shows current shirt label");
        ExpectTrue(hudProbe.GetNodeOrNull<Label>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel/AppearanceMargin/AppearanceContent/AppearancePreviewLabel") is not null, "appearance panel reserves preview copy");
        ExpectTrue(hudProbe.GetNodeOrNull<Button>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel/AppearanceMargin/AppearanceContent/CycleSkinButton") is not null, "appearance panel includes skin cycling action");
        ExpectTrue(hudProbe.GetNodeOrNull<Button>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel/AppearanceMargin/AppearanceContent/CycleHairButton") is not null, "appearance panel includes hair cycling action");
        ExpectTrue(hudProbe.GetNodeOrNull<Button>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel/AppearanceMargin/AppearanceContent/CycleOutfitButton") is not null, "appearance panel includes outfit cycling action");
        ExpectTrue(hudProbe.GetNodeOrNull<Button>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel/AppearanceMargin/AppearanceContent/CyclePantsButton") is not null, "appearance panel includes pants cycling action");
        ExpectTrue(hudProbe.GetNodeOrNull<Button>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/AppearancePanel/AppearanceMargin/AppearanceContent/CycleShirtButton") is not null, "appearance panel includes shirt cycling action");
        ExpectTrue(hudProbe.GetNodeOrNull<Button>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/MainMenuButton") is not null, "Escape menu includes main menu action");
        ExpectTrue(hudProbe.GetNodeOrNull<PanelContainer>("HudRoot/DeveloperPanel") is not null, "gameplay HUD includes tilde developer overlay");
        ExpectTrue(
            hudProbe.GetNode<PanelContainer>("HudRoot/DeveloperPanel").GetThemeStylebox("panel") is StyleBoxTexture,
            "medieval HUD panels use the selected fantasy panel frame");
        ExpectTrue(HudController.UsesMedievalPanelFrameStyle(UiPaletteRegistry.MedievalThemeId), "medieval theme opts into the fantasy panel frame");
        ExpectTrue(hudProbe.GetNodeOrNull<Label>("HudRoot/DeveloperPanel/DeveloperMargin/DeveloperOverlayContent/DeveloperOverlayLabel") is not null, "developer overlay includes detailed character label");
        ExpectTrue(hudProbe.GetNodeOrNull<PanelContainer>("HudRoot/DeveloperPanel/DeveloperMargin/DeveloperOverlayContent/MedievalUiArtPreviewPanel") is not null, "developer overlay includes medieval UI art preview panel");
        var medievalPanelPreview = hudProbe.GetNodeOrNull<TextureRect>("HudRoot/DeveloperPanel/DeveloperMargin/DeveloperOverlayContent/MedievalUiArtPreviewPanel/MedievalUiArtPreviewMargin/MedievalUiArtPreviewContent/MedievalUiArtPreviewRow/PanelPreview/PanelPreviewFrame/PanelTexture");
        ExpectTrue(medievalPanelPreview is not null, "developer overlay previews a medieval panel frame texture");
        ExpectTrue(medievalPanelPreview?.Texture is not null, "medieval panel preview loads its raw PNG texture");
        foreach (var asset in HudController.GetMedievalUiArtPreviewAssets())
        {
            ExpectTrue(FileAccess.FileExists(asset.TexturePath), $"medieval UI preview asset exists: {asset.Label}");
        }
        ExpectTrue(hudProbe.GetNodeOrNull<PanelContainer>("HudRoot/ChatInputPanel") is not null, "gameplay HUD includes local chat input panel");
        ExpectTrue(hudProbe.GetNodeOrNull<LineEdit>("HudRoot/ChatInputPanel/LocalChatInput") is not null, "gameplay HUD includes local chat text entry");
        ExpectFalse(hudProbe.GetNode<PanelContainer>("HudRoot/ChatInputPanel").Visible, "local chat input starts closed");
        hudProbe.OpenLocalChatInput();
        ExpectTrue(hudProbe.GetNode<PanelContainer>("HudRoot/ChatInputPanel").Visible, "local chat input can open from HUD");
        hudProbe.CloseLocalChatInput();
        ExpectFalse(hudProbe.GetNode<PanelContainer>("HudRoot/ChatInputPanel").Visible, "local chat input can close without sending");
        ExpectEqual("hello station", HudController.NormalizeLocalChatInput(" hello\nstation  "), "local chat input normalizes whitespace");
        ExpectTrue(HudController.FormatAppearanceSummary(PlayerAppearanceSelection.Default).Contains("Medium skin"), "appearance summary formats selected layers");
        ExpectTrue(HudController.FormatAppearanceSummary(PlayerAppearanceSelection.Default).Contains("Blue pants"), "appearance summary formats pants layer");
        ExpectTrue(HudController.FormatAppearanceSummary(PlayerAppearanceSelection.Default).Contains("Black shirt"), "appearance summary formats shirt layer");
        ExpectEqual("Skin: Medium", HudController.FormatAppearanceDetailLine("Skin", PlayerAppearanceSelection.Default.SkinLayerId), "appearance panel formats current skin detail line");
        ExpectEqual("Pants: Blue", HudController.FormatAppearanceDetailLine("Pants", PlayerAppearanceSelection.Default.PantsLayerId), "appearance panel formats current pants detail line");
        ExpectEqual("Shirt: Black", HudController.FormatAppearanceDetailLine("Shirt", PlayerAppearanceSelection.Default.ShirtLayerId), "appearance panel formats current shirt detail line");
        ExpectEqual("Held tool: none", HudController.FormatAppearanceDetailLine("Held tool", string.Empty), "appearance panel formats empty held tool detail line");
        ExpectEqual("skin_deep_32x64", HudController.BuildAppearanceCyclePayload("skin", PlayerAppearanceSelection.Default)["skinLayerId"], "appearance panel builds skin cycle server payload");
        ExpectEqual("hair_short_blond_32x64", HudController.BuildAppearanceCyclePayload("hair", PlayerAppearanceSelection.Default)["hairLayerId"], "appearance panel builds hair cycle server payload");
        ExpectEqual("hair_short_copper_32x64", HudController.BuildAppearanceCyclePayload("hair", PlayerAppearanceSelection.Default with { HairLayerId = "hair_short_blond_32x64" })["hairLayerId"], "appearance panel cycles through extra hair test layers");
        ExpectEqual("outfit_settler_32x64", HudController.BuildAppearanceCyclePayload("outfit", PlayerAppearanceSelection.Default)["outfitLayerId"], "appearance panel builds outfit cycle server payload");
        ExpectEqual("outfit_medic_32x64", HudController.BuildAppearanceCyclePayload("outfit", PlayerAppearanceSelection.Default with { OutfitLayerId = "outfit_settler_32x64" })["outfitLayerId"], "appearance panel cycles through extra outfit test layers");
        ExpectEqual("", HudController.BuildAppearanceCyclePayload("pants", PlayerAppearanceSelection.Default)["pantsLayerId"], "appearance panel builds pants cycle server payload");
        ExpectEqual("", HudController.BuildAppearanceCyclePayload("shirt", PlayerAppearanceSelection.Default)["shirtLayerId"], "appearance panel builds shirt cycle server payload");
        hudProbe.ToggleDeveloperOverlay();
        ExpectTrue(hudProbe.GetNode<PanelContainer>("HudRoot/DeveloperPanel").Visible, "tilde developer overlay can be toggled visible");
        ExpectEqual(0, HudController.WrapDeveloperPageIndex(4), "developer overlay page index wraps forward");
        ExpectEqual(3, HudController.WrapDeveloperPageIndex(-1), "developer overlay page index wraps backward");
        ExpectTrue(HudController.FormatDeveloperOverlay(null, "Perf: test", 2).Contains("Tab cycles pages"), "developer overlay empty state explains page controls");
        ExpectFalse(GetTree().Paused, "Escape menu prototype does not pause the running tree");
        ExpectTrue(
            hudProbe.GetNodeOrNull<Button>("HudRoot/MatchSummaryPanel/MatchSummaryContent/ReturnToMainMenuButton") is not null,
            "match summary panel exposes a return-to-main-menu button");
        hudProbe.QueueFree();

        var state = GetNode<GameState>("/root/GameState");
        ExpectTrue(LpcPlayerAppearanceRegistry.BundleExists(state.LocalPlayer.LpcBundleId), "local player gets a generated LPC bundle");
        ExpectTrue(LpcPlayerAppearanceRegistry.BundleExists(state.Players["peer_stand_in"].LpcBundleId), "peer stand-in gets a generated LPC bundle");
        state.TriggerKarmaBreak();
        var localSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        ExpectTrue(localSession is not null, "prototype server session autoload is available");
        ExpectTrue(localSession.LastLocalSnapshot.Summary.Contains("visible"), "prototype server session exposes local interest snapshot");
        var localPlayerSnapshot = localSession.LastLocalSnapshot.Players.First(player => player.Id == localSession.LastLocalSnapshot.PlayerId);
        ExpectTrue(LpcPlayerAppearanceRegistry.BundleExists(localPlayerSnapshot.LpcBundleId), "interest snapshot exposes the local player's generated LPC bundle");
        ExpectFalse(WorldRoot.ShouldRenderRemotePlayer(localSession.LastLocalSnapshot, localPlayerSnapshot), "world renderer does not duplicate the local player avatar");
        ExpectTrue(WorldRoot.ShouldRenderRemotePlayer(localSession.LastLocalSnapshot, localPlayerSnapshot with { Id = "remote_player_preview", DisplayName = "Remote Preview" }), "world renderer can draw dynamic remote player avatars from snapshots");
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
        var chatState = new GameState();
        chatState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        chatState.SetPlayerPosition("peer_stand_in", new TilePosition(3, 0));
        chatState.SetPlayerPosition("rival_paragon", new TilePosition(30, 0));
        var chatServer = new AuthoritativeWorldServer(chatState, "chat-test-world");
        var localChat = chatServer.ProcessIntent(new ServerIntent(
            "peer_stand_in",
            1,
            IntentType.SendLocalChat,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["text"] = "  anyone need a hand?\n"
            }));
        ExpectTrue(localChat.WasAccepted, "server accepts local chat intents");
        var localChatSnapshot = chatServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(localChatSnapshot.LocalChatMessages.Any(message => message.Text == "anyone need a hand?"), "interest snapshot exposes nearby local chat");
        var nearbyChat = localChatSnapshot.LocalChatMessages.First(message => message.SpeakerId == "peer_stand_in");
        ExpectEqual(3, nearbyChat.DistanceTiles, "local chat snapshots include speaker distance");
        ExpectEqual(1f, nearbyChat.Volume, "local chat is full volume inside clear radius");
        var farChatSnapshot = chatServer.CreateInterestSnapshot("rival_paragon");
        ExpectFalse(farChatSnapshot.LocalChatMessages.Any(message => message.SpeakerId == "peer_stand_in"), "interest snapshot hides local chat outside audible radius");
        ExpectEqual(1f, AuthoritativeWorldServer.CalculateLocalChatVolume(AuthoritativeWorldServer.LocalChatClearRadiusTiles), "local chat falloff stays full at clear radius");
        ExpectEqual(0f, AuthoritativeWorldServer.CalculateLocalChatVolume(AuthoritativeWorldServer.LocalChatMaxRadiusTiles), "local chat falloff reaches silence at max radius");
        ExpectTrue(HudController.FormatLocalChatSummary(localChatSnapshot.LocalChatMessages).Contains("Stranded Player"), "HUD formats local chat speaker summaries");
        ExpectTrue(WorldRoot.IsChatBubbleFresh(localChatSnapshot, nearbyChat), "fresh local chat messages can render as world bubbles");
        ExpectTrue(WorldRoot.FormatChatBubbleText(nearbyChat).Contains("anyone need a hand?"), "world chat bubble formats nearby chat text");
        chatServer.AdvanceIdleTicks(WorldRoot.LocalChatBubbleVisibleTicks + 1);
        var staleChatSnapshot = chatServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectFalse(WorldRoot.IsChatBubbleFresh(staleChatSnapshot, staleChatSnapshot.LocalChatMessages.First()), "old local chat messages stop rendering as bubbles");
        chatServer.AdvanceIdleTicks(AuthoritativeWorldServer.LocalChatRetainTicks + 1);
        ExpectEqual(0, chatServer.CreateInterestSnapshot(GameState.LocalPlayerId).LocalChatMessages.Count, "server prunes expired local chat history");
        for (var i = 0; i < AuthoritativeWorldServer.LocalChatMaxRetainedMessages + 5; i++)
        {
            var retainedChat = chatServer.ProcessIntent(new ServerIntent(
                "peer_stand_in",
                2 + i,
                IntentType.SendLocalChat,
                new System.Collections.Generic.Dictionary<string, string>
                {
                    ["text"] = $"message {i}"
                }));
            ExpectTrue(retainedChat.WasAccepted, "server accepts retained local chat pruning test messages");
        }
        ExpectEqual(AuthoritativeWorldServer.LocalChatMaxRetainedMessages, chatServer.LocalChatLog.Count, "server caps retained local chat history");
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
            PrototypeCharacterSprite.WalkRightAnimation,
            PrototypeCharacterSprite.ResolveAnimationName(new Vector2(1f, -1f)),
            "native character sprite resolves diagonal movement to the east/west walk animation");
        ExpectEqual(
            CharacterFacingDirection.Left,
            PrototypeCharacterSprite.ToFacingDirection(new Vector2(-1f, 1f)),
            "native character sprite resolves diagonal facing to the east/west profile");
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
        var forceImageLoadAtlasPath = FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath)
            ? PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath
            : PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath;
        if (FileAccess.FileExists(forceImageLoadAtlasPath))
        {
            ExpectTrue(
                AtlasTextureLoader.Load(forceImageLoadAtlasPath, forceImageLoad: true) is not null,
                "atlas texture loader can force raw image loading for generated transparent runtime sheets");
        }
        if (FileAccess.FileExists(PrototypeSpriteCatalog.LayeredPlayerPreviewEightDirectionAtlasPath))
        {
            ExpectTrue(
                AtlasTextureLoader.Load(PrototypeSpriteCatalog.LayeredPlayerPreviewEightDirectionAtlasPath, forceImageLoad: true) is not null,
                "atlas texture loader can force raw image loading for optional layered player preview sheets");
        }
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
        var nativePlayerV2ManifestPath = PlayerV2LayerManifest.DefaultManifestPath;
        ExpectTrue(FileAccess.FileExists(nativePlayerV2ManifestPath), "native 32x64 player v2 layered manifest exists");
        using (var nativePlayerV2Manifest = JsonDocument.Parse(FileAccess.GetFileAsString(nativePlayerV2ManifestPath)))
        {
            var manifestRoot = nativePlayerV2Manifest.RootElement;
            ExpectEqual("karma.player_v2.layers_32x64.v1", manifestRoot.GetProperty("schema").GetString(), "native player v2 manifest declares 32x64 layer schema");
            ExpectEqual(32, manifestRoot.GetProperty("frameWidth").GetInt32(), "native player v2 manifest declares 32px frame width");
            ExpectEqual(64, manifestRoot.GetProperty("frameHeight").GetInt32(), "native player v2 manifest declares 64px frame height");
            ExpectEqual(8, manifestRoot.GetProperty("columns").GetInt32(), "native player v2 manifest declares eight direction columns");
            ExpectEqual(4, manifestRoot.GetProperty("rows").GetInt32(), "native player v2 manifest declares four animation rows");
            ExpectEqual(19, manifestRoot.GetProperty("layers").GetArrayLength(), "native player v2 manifest exposes base, skins, hair, outfit, boots, pants/shirt and overlay layers");
            ExpectEqual(7, manifestRoot.GetProperty("previewStack").GetArrayLength(), "native player v2 manifest preview stack composes a playable layered character");
            ExpectTrue(
                manifestRoot.GetProperty("layers").EnumerateArray().Any(layer => layer.GetProperty("id").GetString() == "skin_light_32x64"),
                "native player v2 manifest exposes swappable light skin layer");
            ExpectTrue(
                manifestRoot.GetProperty("layers").EnumerateArray().Any(layer => layer.GetProperty("id").GetString() == "skin_deep_32x64"),
                "native player v2 manifest exposes swappable deep skin layer");
            ExpectTrue(
                manifestRoot.GetProperty("layers").EnumerateArray().Any(layer => layer.GetProperty("id").GetString() == "hair_short_blond_32x64"),
                "native player v2 manifest exposes swappable blond hair layer");
            ExpectTrue(
                manifestRoot.GetProperty("layers").EnumerateArray().Any(layer => layer.GetProperty("id").GetString() == "hair_short_copper_32x64"),
                "native player v2 manifest exposes swappable copper hair layer");
            ExpectTrue(
                manifestRoot.GetProperty("layers").EnumerateArray().Any(layer => layer.GetProperty("id").GetString() == "outfit_settler_32x64"),
                "native player v2 manifest exposes swappable settler outfit layer");
            ExpectTrue(
                manifestRoot.GetProperty("layers").EnumerateArray().Any(layer => layer.GetProperty("id").GetString() == "outfit_medic_32x64"),
                "native player v2 manifest exposes swappable medic outfit layer");
            ExpectTrue(
                manifestRoot.GetProperty("layers").EnumerateArray().Any(layer => layer.GetProperty("id").GetString() == "boots_utility_32x64"),
                "native player v2 manifest exposes boots equipment layer");
            ExpectTrue(
                manifestRoot.GetProperty("layers").EnumerateArray().Any(layer => layer.GetProperty("id").GetString() == "boots_black_32x64"),
                "native player v2 manifest exposes black boots equipment layer");
            ExpectTrue(
                manifestRoot.GetProperty("layers").EnumerateArray().Any(layer => layer.GetProperty("id").GetString() == "backpack_daypack_32x64"),
                "native player v2 manifest exposes backpack overlay layer");
            ExpectTrue(
                manifestRoot.GetProperty("layers").EnumerateArray().Any(layer => layer.GetProperty("id").GetString() == "tool_multitool_32x64"),
                "native player v2 manifest exposes held tool overlay layer");
            ExpectTrue(
                manifestRoot.GetProperty("layers").EnumerateArray().Any(layer => layer.GetProperty("id").GetString() == "weapon_practice_baton_32x64"),
                "native player v2 manifest exposes weapon overlay layer");
        }
        var playerV2LayerManifest = PlayerV2LayerManifest.LoadDefault();
        ExpectEqual("karma.player_v2.layers_32x64.v1", playerV2LayerManifest.Schema, "player v2 layer manifest loader reads native 32x64 schema");
        ExpectEqual(32, playerV2LayerManifest.FrameWidth, "player v2 layer manifest loader reads rectangular frame width");
        ExpectEqual(64, playerV2LayerManifest.FrameHeight, "player v2 layer manifest loader reads rectangular frame height");
        ExpectEqual(3, playerV2LayerManifest.Layers.Count(layer => layer.Slot == "skin"), "player v2 layer manifest loader exposes skin variants");
        ExpectEqual(4, playerV2LayerManifest.Layers.Count(layer => layer.Slot == "hair"), "player v2 layer manifest loader exposes hair variants");
        ExpectEqual(4, playerV2LayerManifest.Layers.Count(layer => layer.Slot == "outfit"), "player v2 layer manifest loader exposes outfit variants");
        ExpectEqual(2, playerV2LayerManifest.Layers.Count(layer => layer.Slot == "boots"), "player v2 layer manifest loader exposes boots equipment variants");
        ExpectEqual(1, playerV2LayerManifest.Layers.Count(layer => layer.Slot == "pants"), "player v2 layer manifest loader exposes pants clothing variants");
        ExpectEqual(1, playerV2LayerManifest.Layers.Count(layer => layer.Slot == "shirt"), "player v2 layer manifest loader exposes shirt clothing variants");
        ExpectEqual(1, playerV2LayerManifest.Layers.Count(layer => layer.Slot == "backpack"), "player v2 layer manifest loader exposes backpack overlay variants");
        ExpectEqual(1, playerV2LayerManifest.Layers.Count(layer => layer.Slot == "held_tool"), "player v2 layer manifest loader exposes held tool overlay variants");
        ExpectEqual(1, playerV2LayerManifest.Layers.Count(layer => layer.Slot == "weapon"), "player v2 layer manifest loader exposes weapon overlay variants");
        var existingPlayerV2Layers = playerV2LayerManifest.Layers
            .Where(layer => FileAccess.FileExists(playerV2LayerManifest.ResolveLayerPath(layer)))
            .Select(layer => layer.Id)
            .ToHashSet(StringComparer.Ordinal);
        ExpectTrue(existingPlayerV2Layers.Contains("boots_black_32x64"), "player v2 layer manifest keeps the active black boots layer available");
        var defaultPlayerV2Appearance = playerV2LayerManifest.CreateDefaultAppearance();
        ExpectTrue(
            playerV2LayerManifest.GetLayerStack(defaultPlayerV2Appearance).SequenceEqual(playerV2LayerManifest.PreviewStack),
            "player v2 default appearance matches manifest preview stack");
        var lightSkinSelection = PlayerAppearanceSelection.Default with { SkinLayerId = "skin_light_32x64" };
        var lightSkinPlayerV2Appearance = playerV2LayerManifest.CreateAppearance(lightSkinSelection);
        ExpectEqual("skin_light_32x64", lightSkinPlayerV2Appearance.GetLayerIdForSlot("skin"), "player v2 appearance can override skin layer");
        ExpectFalse(
            playerV2LayerManifest.GetLayerStack(lightSkinPlayerV2Appearance).SequenceEqual(playerV2LayerManifest.PreviewStack),
            "player v2 custom appearance produces a different layer stack");
        var previewStackAssetsExist = playerV2LayerManifest.PreviewStack.All(layerId =>
            playerV2LayerManifest.Layers.Any(layer => layer.Id == layerId && FileAccess.FileExists(playerV2LayerManifest.ResolveLayerPath(layer))));
        if (previewStackAssetsExist && FileAccess.FileExists(playerV2LayerManifest.CompositePath))
        {
            var recomposedPlayerV2Preview = playerV2LayerManifest.ComposePreviewStack();
            var savedPlayerV2Preview = Image.LoadFromFile(playerV2LayerManifest.CompositePath);
            ExpectEqual(0, CountImagePixelDifferences(savedPlayerV2Preview, recomposedPlayerV2Preview), "player v2 layer compositor reproduces saved preview stack");
            if (existingPlayerV2Layers.Contains("skin_light_32x64"))
            {
                var lightSkinPlayerV2Preview = playerV2LayerManifest.Compose(lightSkinPlayerV2Appearance);
                ExpectTrue(CountImagePixelDifferences(recomposedPlayerV2Preview, lightSkinPlayerV2Preview) > 0, "player v2 appearance compositor changes output when skin changes");
            }

            if (existingPlayerV2Layers.Contains("hair_short_copper_32x64"))
            {
                var copperHairPlayerV2Appearance = playerV2LayerManifest.CreateAppearance(PlayerAppearanceSelection.Default with { HairLayerId = "hair_short_copper_32x64" });
                ExpectTrue(CountImagePixelDifferences(recomposedPlayerV2Preview, playerV2LayerManifest.Compose(copperHairPlayerV2Appearance)) > 0, "player v2 appearance compositor changes output when hair changes");
            }

            if (existingPlayerV2Layers.Contains("outfit_medic_32x64"))
            {
                var medicOutfitPlayerV2Appearance = playerV2LayerManifest.CreateAppearance(PlayerAppearanceSelection.Default with { OutfitLayerId = "outfit_medic_32x64" });
                ExpectTrue(CountImagePixelDifferences(recomposedPlayerV2Preview, playerV2LayerManifest.Compose(medicOutfitPlayerV2Appearance)) > 0, "player v2 appearance compositor changes output when outfit changes");
            }

            if (existingPlayerV2Layers.Contains("tool_multitool_32x64"))
            {
                var toolPlayerV2Appearance = playerV2LayerManifest.CreateAppearance(PlayerAppearanceSelection.Default with { HeldToolLayerId = "tool_multitool_32x64" });
                ExpectTrue(playerV2LayerManifest.GetLayerStack(toolPlayerV2Appearance).Contains("tool_multitool_32x64"), "player v2 appearance can include held tool overlay layer");
                ExpectTrue(CountImagePixelDifferences(recomposedPlayerV2Preview, playerV2LayerManifest.Compose(toolPlayerV2Appearance)) > 0, "player v2 held tool overlay changes composed output");
            }
        }
        var defaultPlayerDefinition = PrototypeSpriteCatalog.Get(PrototypeSpriteKind.Player);
        const string testCompositeRoot = "user://karma_test/player_v2/composites";
        if (previewStackAssetsExist && existingPlayerV2Layers.Contains("skin_light_32x64"))
        {
            var lightSkinPlayerV2Preview = playerV2LayerManifest.Compose(lightSkinPlayerV2Appearance);
            var lightSkinCompositePath = playerV2LayerManifest.ExportAppearanceComposite(lightSkinSelection, testCompositeRoot);
            ExpectTrue(FileAccess.FileExists(lightSkinCompositePath), "player v2 appearance compositor exports selected appearance composite");
            ExpectTrue(lightSkinCompositePath.Contains("skin_light_32x64"), "player v2 appearance composite path names selected skin layer");
            ExpectEqual(0, CountImagePixelDifferences(Image.LoadFromFile(lightSkinCompositePath), lightSkinPlayerV2Preview), "player v2 exported appearance matches composed image");
            var lightSkinPlayerDefinition = PrototypeCharacterSprite.WithAtlasPath(defaultPlayerDefinition, lightSkinCompositePath);
            var exportedByCharacterSprite = PrototypeCharacterSprite.ExportPlayerAppearanceAtlas(lightSkinSelection, testCompositeRoot);
            ExpectEqual(lightSkinCompositePath, exportedByCharacterSprite, "player character sprite resolves selected appearance atlas paths");
            ExpectEqual(lightSkinCompositePath, lightSkinPlayerDefinition.AtlasPath, "player character sprite can target an appearance composite atlas");
            ExpectEqual(defaultPlayerDefinition.Animations.Count, lightSkinPlayerDefinition.Animations.Count, "appearance composite atlas keeps player animation contract");
            ExpectTrue(
                PrototypeCharacterSprite.CreateSpriteFrames(AtlasTextureLoader.Load(lightSkinCompositePath, forceImageLoad: true), lightSkinPlayerDefinition)
                    .HasAnimation(PrototypeCharacterSprite.WalkRightAnimation),
                "appearance composite atlas creates walk animations for runtime sprites");
        }
        var expectedPlayerAtlasPath = FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath)
            ? PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath
            : FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2TrialImportedAtlasPath)
            ? PrototypeSpriteCatalog.PlayerV2TrialImportedAtlasPath
            : FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath)
            ? PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath
            : FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2Model32x64AtlasPath)
            ? PrototypeSpriteCatalog.PlayerV2Model32x64AtlasPath
            : FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath)
                ? PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath
                : FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath)
                ? PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath
                : FileAccess.FileExists(PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath)
                    ? PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath
                    : FileAccess.FileExists(PrototypeSpriteCatalog.LayeredPlayerPreviewEightDirectionAtlasPath)
                        ? PrototypeSpriteCatalog.LayeredPlayerPreviewEightDirectionAtlasPath
                        : FileAccess.FileExists(PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath)
                            ? PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath
                            : PrototypeSpriteCatalog.EngineerPlayerAtlasPath;
        var expectedPlayerSize = expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath ||
                                 expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath ||
                                 expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2TrialImportedAtlasPath ||
                                 expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64AtlasPath
            ? new Vector2(32f, 64f)
            : expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath ||
              expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath ||
              expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath
                ? new Vector2(64f, 64f)
            : expectedPlayerAtlasPath == PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath ||
              expectedPlayerAtlasPath == PrototypeSpriteCatalog.LayeredPlayerPreviewEightDirectionAtlasPath
                ? new Vector2(32f, 32f)
                : new Vector2(30f, 40f);
        if (expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath ||
            expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath ||
            expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2TrialImportedAtlasPath ||
            expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64AtlasPath ||
            expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath ||
            expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath ||
            expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath ||
            expectedPlayerAtlasPath == PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath ||
            expectedPlayerAtlasPath == PrototypeSpriteCatalog.LayeredPlayerPreviewEightDirectionAtlasPath)
        {
            var engineerImage = Image.LoadFromFile(expectedPlayerAtlasPath);
            var expectedSheetSize = expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath ||
                                    expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath ||
                                    expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2TrialImportedAtlasPath ||
                                    expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64AtlasPath
                ? new Vector2I(256, 256)
                : expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath ||
                  expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath ||
                  expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath
                    ? new Vector2I(512, 256)
                    : new Vector2I(256, 288);
            ExpectEqual(expectedSheetSize.X, engineerImage.GetWidth(), "active 8-direction runtime sheet has expected width");
            ExpectEqual(expectedSheetSize.Y, engineerImage.GetHeight(), "active 8-direction runtime sheet has expected height");
            ExpectTrue(CountTransparentPixels(engineerImage) > 0, "active 8-direction runtime sheet keeps transparent background pixels");
            var expectedFrameSize = expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath ||
                                    expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath ||
                                    expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2TrialImportedAtlasPath ||
                                    expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64AtlasPath
                ? new Vector2I(32, 64)
                : expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath ||
                  expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath ||
                  expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath
                    ? new Vector2I(64, 64)
                    : new Vector2I(CharacterSheetLayout.StandardFrameSize, CharacterSheetLayout.StandardFrameSize);
            if (expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath ||
                expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath ||
                expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath)
            {
                ExpectTrue(
                    CountPixelDifferences(engineerImage, new Vector2I(3, 0), new Vector2I(2, 0), expectedFrameSize) > 0,
                    "active 64px preview sheet keeps at least one diagonal distinct from side-facing frames");
            }
            else
            {
                ExpectTrue(
                    CountPixelDifferences(engineerImage, new Vector2I(1, 0), new Vector2I(2, 0), expectedFrameSize) > 0,
                    "active 8-direction runtime sheet gives front-right a distinct placeholder frame");
                ExpectTrue(
                    CountPixelDifferences(engineerImage, new Vector2I(3, 0), new Vector2I(2, 0), expectedFrameSize) > 0,
                    "active 8-direction runtime sheet gives back-right a distinct placeholder frame");
            }
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
        ExpectFalse(
            PlayerController.ShouldSnapToAuthoritativePosition(new Vector2(0f, 0f), new Vector2(32f, 0f), new Vector2(30f, 0f)),
            "player controller preserves predicted local movement during tile-authority updates");
        ExpectTrue(
            PlayerController.ShouldSnapToAuthoritativePosition(new Vector2(0f, 0f), new Vector2(128f, 0f), new Vector2(0f, 0f)),
            "player controller still snaps when prediction does not explain the authority correction");
        ExpectEqual("Health: 75/100", HudController.FormatHealth(75, 100), "health label formats current and maximum health");
        ExpectEqual(75f, HudController.CalculateHealthPercent(75, 100), "health bar percent follows authoritative health");
        ExpectEqual(0f, HudController.CalculateHealthPercent(-5, 100), "health bar percent clamps below zero");
        ExpectEqual(100f, HudController.CalculateHealthPercent(125, 100), "health bar percent clamps above maximum");
        ExpectEqual("Ammo: 6/12", HudController.FormatAmmo(6, 12), "ammo label formats current and max magazine");
        ExpectEqual("Ammo: 0/12 (reload)", HudController.FormatAmmo(0, 12), "ammo label flags empty magazine for reload");
        ExpectEqual("Ammo: --", HudController.FormatAmmo(0, 0), "ammo label shows -- when no magazine is configured");
        ExpectEqual("Stamina: 80/100", HudController.FormatCombatStamina(80, 100), "combat stamina label formats current and max");
        ExpectEqual("Stamina: 0/100", HudController.FormatCombatStamina(-5, 100), "combat stamina label clamps below zero");
        ExpectEqual("Hunger: 100/100", HudController.FormatHunger(100, 100), "hunger label formats full pool");
        ExpectEqual("Hunger: 50/100 (peckish)", HudController.FormatHunger(50, 100), "hunger label flags peckish at 50%");
        ExpectEqual("Hunger: 25/100 (hungry)", HudController.FormatHunger(25, 100), "hunger label flags hungry at 25%");
        ExpectEqual("Hunger: 0/100 (starving)", HudController.FormatHunger(0, 100), "hunger label flags starving at empty");

        // Pause Options panel: percent → dB and linear → dB conversions.
        ExpectEqual(-80f, HudController.PercentToDb(0.0),
            "pause master volume: 0% silences (returns -80 dB)");
        ExpectTrue(Math.Abs(HudController.PercentToDb(100.0) - 0f) < 0.01f,
            "pause master volume: 100% maps to 0 dB (unity)");
        ExpectTrue(HudController.PercentToDb(50.0) < 0f && HudController.PercentToDb(50.0) > -10f,
            "pause master volume: 50% lands between 0 and -10 dB");
        ExpectEqual(-80f, HudController.LinearToDb(0.0),
            "pause linear-to-dB silences at 0");
        ExpectTrue(Math.Abs(HudController.LinearToDb(1.0) - 0f) < 0.01f,
            "pause linear-to-dB at 1.0 maps to unity");

        // AudioSettings: four-bus mixer state + ConfigFile round-trip.
        ExpectEqual("Master", Karma.Audio.AudioSettings.MasterBusName,
            "AudioSettings exposes Master bus name constant");
        ExpectEqual("Music", Karma.Audio.AudioSettings.MusicBusName,
            "AudioSettings exposes Music bus name constant");
        ExpectEqual("SFX", Karma.Audio.AudioSettings.SfxBusName,
            "AudioSettings exposes SFX bus name constant");
        ExpectEqual("Ambient", Karma.Audio.AudioSettings.AmbientBusName,
            "AudioSettings exposes Ambient bus name constant");
        ExpectEqual(Karma.Audio.AudioSettings.PercentToDb(50.0), HudController.PercentToDb(50.0),
            "HudController.PercentToDb wraps AudioSettings.PercentToDb");
        ExpectEqual(100.0, Karma.Audio.AudioSettings.ClampPercent(150.0),
            "AudioSettings clamps over-100 percentages");
        ExpectEqual(0.0, Karma.Audio.AudioSettings.ClampPercent(-5.0),
            "AudioSettings clamps negative percentages");
        var roundTripConfig = new ConfigFile();
        Karma.Audio.AudioSettings.MasterVolume = 42;
        Karma.Audio.AudioSettings.MusicVolume = 17;
        Karma.Audio.AudioSettings.SfxVolume = 91;
        Karma.Audio.AudioSettings.AmbientVolume = 8;
        Karma.Audio.AudioSettings.SaveToConfig(roundTripConfig);
        Karma.Audio.AudioSettings.MasterVolume = 0;
        Karma.Audio.AudioSettings.MusicVolume = 0;
        Karma.Audio.AudioSettings.SfxVolume = 0;
        Karma.Audio.AudioSettings.AmbientVolume = 0;
        Karma.Audio.AudioSettings.LoadFromConfig(roundTripConfig);
        ExpectEqual(42.0, Karma.Audio.AudioSettings.MasterVolume,
            "AudioSettings round-trips master volume through ConfigFile");
        ExpectEqual(17.0, Karma.Audio.AudioSettings.MusicVolume,
            "AudioSettings round-trips music volume through ConfigFile");
        ExpectEqual(91.0, Karma.Audio.AudioSettings.SfxVolume,
            "AudioSettings round-trips sfx volume through ConfigFile");
        ExpectEqual(8.0, Karma.Audio.AudioSettings.AmbientVolume,
            "AudioSettings round-trips ambient volume through ConfigFile");
        var emptyConfig = new ConfigFile();
        Karma.Audio.AudioSettings.MasterVolume = 0;
        Karma.Audio.AudioSettings.AmbientVolume = 0;
        Karma.Audio.AudioSettings.LoadFromConfig(emptyConfig);
        ExpectEqual(Karma.Audio.AudioSettings.DefaultMasterVolume, Karma.Audio.AudioSettings.MasterVolume,
            "AudioSettings reverts to default master volume when key is missing");
        ExpectEqual(Karma.Audio.AudioSettings.DefaultAmbientVolume, Karma.Audio.AudioSettings.AmbientVolume,
            "AudioSettings reverts to default ambient volume when key is missing");

        // Pause menu: each of the four mixer rows builds a labeled slider.
        ExpectTrue(hudProbe.GetNodeOrNull<HSlider>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/EscapeOptionsPanel/EscapeOptionsMargin/EscapeOptionsContent/MasterVolumeRow/MasterVolumeSlider") is not null,
            "pause options panel includes Master volume slider");
        ExpectTrue(hudProbe.GetNodeOrNull<HSlider>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/EscapeOptionsPanel/EscapeOptionsMargin/EscapeOptionsContent/MusicVolumeRow/MusicVolumeSlider") is not null,
            "pause options panel includes Music volume slider");
        ExpectTrue(hudProbe.GetNodeOrNull<HSlider>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/EscapeOptionsPanel/EscapeOptionsMargin/EscapeOptionsContent/EffectsVolumeRow/EffectsVolumeSlider") is not null,
            "pause options panel includes Effects volume slider");
        ExpectTrue(hudProbe.GetNodeOrNull<HSlider>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/EscapeOptionsPanel/EscapeOptionsMargin/EscapeOptionsContent/AmbientVolumeRow/AmbientVolumeSlider") is not null,
            "pause options panel includes Ambient volume slider");
        ExpectTrue(hudProbe.GetNodeOrNull<CheckButton>("HudRoot/EscapeMenuPanel/EscapeMenuMargin/EscapeMenuContent/EscapeOptionsPanel/EscapeOptionsMargin/EscapeOptionsContent/CarryStateToggle") is not null,
            "pause options panel includes carry-state toggle");

        const string carryStateSmokePath = "user://carry_state_smoke.json";
        var carryPreferenceState = new GameState();
        carryPreferenceState.SetCarryStateIntoNextRound(true, carryStateSmokePath);
        var carryPreferenceLoad = new GameState();
        ExpectTrue(carryPreferenceLoad.LoadCarryStatePreference(carryStateSmokePath),
            "carry-state preference loads from JSON");
        ExpectTrue(carryPreferenceLoad.CarryStateIntoNextRound,
            "carry-state preference round-trips enabled value");
        carryPreferenceState.SetCarryStateIntoNextRound(false, carryStateSmokePath);
        ExpectTrue(carryPreferenceLoad.LoadCarryStatePreference(carryStateSmokePath),
            "carry-state preference reloads disabled JSON");
        ExpectFalse(carryPreferenceLoad.CarryStateIntoNextRound,
            "carry-state preference round-trips disabled value");

        var carryRoundState = new GameState();
        carryRoundState.RegisterPlayer(GameState.LocalPlayerId, "Carry Tester");
        carryRoundState.Players[GameState.LocalPlayerId].ApplyKarma(18);
        carryRoundState.Relationships.Apply(StarterNpcs.Mara.Id, GameState.LocalPlayerId, 22);
        carryRoundState.Factions.Apply(StarterFactions.FreeSettlersId, GameState.LocalPlayerId, 33);
        carryRoundState.SetCarryStateIntoNextRound(true, carryStateSmokePath);
        carryRoundState.ResetForNewMatch();
        ExpectEqual(18, carryRoundState.LocalKarma.Score,
            "carry-state reset preserves local karma when enabled");
        ExpectEqual(22, carryRoundState.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId),
            "carry-state reset preserves relationships when enabled");
        ExpectEqual(33, carryRoundState.Factions.GetReputation(StarterFactions.FreeSettlersId, GameState.LocalPlayerId),
            "carry-state reset preserves faction reputation when enabled");

        // Karma-break impact flash trigger.
        var noFlashTick = HudController.FindKarmaBreakTriggerTick(
            System.Array.Empty<ServerEvent>(), "p_a", -1);
        ExpectEqual(-1L, noFlashTick, "karma-break flash returns -1 when no events");

        var unrelated = new[]
        {
            new ServerEvent("player_attacked", "world", 5, string.Empty,
                new Dictionary<string, string> { ["playerId"] = "p_a", ["targetId"] = "p_b" })
        };
        ExpectEqual(-1L,
            HudController.FindKarmaBreakTriggerTick(unrelated, "p_a", -1),
            "karma-break flash ignores non-break events");

        var foreignBreak = new[]
        {
            new ServerEvent("karma_break", "world", 7, string.Empty,
                new Dictionary<string, string> { ["playerId"] = "p_other" })
        };
        ExpectEqual(-1L,
            HudController.FindKarmaBreakTriggerTick(foreignBreak, "p_a", -1),
            "karma-break flash ignores breaks on other players");

        var localBreak = new[]
        {
            new ServerEvent("karma_break", "world", 11, string.Empty,
                new Dictionary<string, string> { ["playerId"] = "p_a" })
        };
        ExpectEqual(11L,
            HudController.FindKarmaBreakTriggerTick(localBreak, "p_a", -1),
            "karma-break flash triggers on local-player break");
        ExpectEqual(-1L,
            HudController.FindKarmaBreakTriggerTick(localBreak, "p_a", 11),
            "karma-break flash does not re-trigger on the same event");

        var localRespawn = new[]
        {
            new ServerEvent("player_respawned", "world", 14, string.Empty,
                new Dictionary<string, string> { ["playerId"] = "p_a" })
        };
        ExpectEqual(14L,
            HudController.FindKarmaBreakTriggerTick(localRespawn, "p_a", -1),
            "karma-break flash also triggers on player_respawned for the local player");

        // Contraband detection flash trigger.
        ExpectEqual(-1L,
            HudController.FindContrabandFlashTriggerTick(System.Array.Empty<ServerEvent>(), "p_a", -1),
            "contraband flash returns -1 when no events");
        var foreignContraband = new[]
        {
            new ServerEvent("contraband_detected", "world", 9, string.Empty,
                new Dictionary<string, string> { ["playerId"] = "p_other" })
        };
        ExpectEqual(-1L,
            HudController.FindContrabandFlashTriggerTick(foreignContraband, "p_a", -1),
            "contraband flash ignores events for other players");
        var localContraband = new[]
        {
            new ServerEvent("contraband_detected", "world", 23, string.Empty,
                new Dictionary<string, string> { ["playerId"] = "p_a" })
        };
        ExpectEqual(23L,
            HudController.FindContrabandFlashTriggerTick(localContraband, "p_a", -1),
            "contraband flash triggers on contraband_detected for the local player");
        ExpectEqual(-1L,
            HudController.FindContrabandFlashTriggerTick(localContraband, "p_a", 23),
            "contraband flash does not re-trigger on the same event tick");

        // Wraith trail VFX: gated on SpeedModifier != 1f.
        ExpectFalse(
            WorldRoot.IsWraithTrailActive(null),
            "wraith trail inactive for null player snapshot");
        var wraithBaseSnap = new PlayerSnapshot(
            "p_a", "Normal", 0, "Drifter", 0, "0/0", LeaderboardRole.None, 0, 0, 100, 100, 0,
            PlayerAppearanceSelection.Default,
            System.Array.Empty<string>(),
            new Dictionary<EquipmentSlot, string>(),
            System.Array.Empty<string>());
        ExpectFalse(
            WorldRoot.IsWraithTrailActive(wraithBaseSnap),
            "wraith trail inactive when SpeedModifier == 1f");
        var wraithActiveSnap = wraithBaseSnap with { SpeedModifier = 0.7f };
        ExpectTrue(
            WorldRoot.IsWraithTrailActive(wraithActiveSnap),
            "wraith trail active when SpeedModifier diverges from 1f");

        // Wanted poster overlay: gated on StatusEffects containing "Wanted" or a bounty entry.
        ExpectFalse(
            WorldRoot.IsWantedOverlayActive(null),
            "wanted overlay inactive for null snapshot");
        ExpectFalse(
            WorldRoot.IsWantedOverlayActive(wraithBaseSnap),
            "wanted overlay inactive when StatusEffects is empty");
        var wantedSnap = wraithBaseSnap with { StatusEffects = new[] { "Wanted" } };
        ExpectTrue(
            WorldRoot.IsWantedOverlayActive(wantedSnap),
            "wanted overlay active when StatusEffects includes Wanted");
        var bountySnap = wraithBaseSnap with { StatusEffects = new[] { "Bounty: 50" } };
        ExpectTrue(
            WorldRoot.IsWantedOverlayActive(bountySnap),
            "wanted overlay active when StatusEffects includes a bounty entry");

        // Audio event catalog: built-in mappings + runtime override + substring fallback.
        Karma.Audio.AudioEventCatalog.Reset();
        ExpectTrue(
            Karma.Audio.AudioEventCatalog.Resolve("karma_break").EndsWith("karma_break_stinger.wav"),
            "audio catalog resolves built-in karma_break clip");
        ExpectEqual(string.Empty,
            Karma.Audio.AudioEventCatalog.Resolve("totally_unknown_event"),
            "audio catalog returns empty string for unknown event");
        Karma.Audio.AudioEventCatalog.Register("karma_break", "res://test/override.wav");
        ExpectEqual("res://test/override.wav",
            Karma.Audio.AudioEventCatalog.Resolve("karma_break"),
            "runtime override beats built-in clip");
        ExpectTrue(
            Karma.Audio.AudioEventCatalog.Resolve("player_attacked_with_pistol")
                .EndsWith("hit_thud.wav"),
            "audio catalog falls back to substring match for compound event ids");
        Karma.Audio.AudioEventCatalog.Reset();
        ExpectTrue(
            Karma.Audio.AudioEventCatalog.Resolve("karma_break").EndsWith("karma_break_stinger.wav"),
            "audio catalog Reset clears runtime overrides");
        // Locomotion + body-cue ids registered.
        ExpectTrue(
            Karma.Audio.AudioEventCatalog.Resolve("footstep_dirt").EndsWith("footstep_dirt.wav"),
            "audio catalog resolves footstep_dirt cue");
        ExpectTrue(
            Karma.Audio.AudioEventCatalog.Resolve("grunt_pain").EndsWith("grunt_pain.wav"),
            "audio catalog resolves grunt_pain cue");
        ExpectTrue(
            Karma.Audio.AudioEventCatalog.Resolve("sword_swing").EndsWith("sword_swing.wav"),
            "audio catalog resolves sword_swing cue");
        ExpectTrue(
            Karma.Audio.AudioEventCatalog.Resolve("world:1:item_purchased").EndsWith("purchase_chime.wav"),
            "audio catalog resolves shop purchase events");
        ExpectTrue(
            Karma.Audio.AudioEventCatalog.Resolve("world:1:item_picked_up").EndsWith("interact_pop.wav"),
            "audio catalog resolves pickup events");
        ExpectTrue(
            Karma.Audio.AudioEventCatalog.Resolve("world:1:dialogue_started").EndsWith("interact_pop.wav"),
            "audio catalog resolves dialogue events");
        ExpectTrue(
            Karma.Audio.AudioEventCatalog.Resolve("world:1:player_mounted").EndsWith("footstep_wood.wav"),
            "audio catalog resolves mount events");
        ExpectTrue(
            Karma.Audio.AudioEventCatalog.Resolve("world:1:item_used_heal").EndsWith("clinic_revive_chime.wav"),
            "audio catalog resolves healing item use events");
        foreach (var cue in Karma.Audio.AudioEventCatalog.All)
        {
            ExpectTrue(FileAccess.FileExists(cue.Value),
                $"registered audio clip exists for {cue.Key}");
        }
        foreach (var equippable in StarterItems.All.Where(item => item.Slot != EquipmentSlot.None))
        {
            var cueId = Karma.Audio.AudioEventCatalog.EquipmentCueIdForItem(equippable);
            var cuePath = Karma.Audio.AudioEventCatalog.ResolveEquipmentCue(equippable);
            ExpectTrue(!string.IsNullOrWhiteSpace(cueId),
                $"audio catalog assigns an equipment cue id for {equippable.Id}");
            ExpectTrue(!string.IsNullOrWhiteSpace(cuePath),
                $"audio catalog resolves an equipment cue for {equippable.Id}");
            ExpectTrue(FileAccess.FileExists(cuePath),
                $"equipment cue clip exists for {equippable.Id}");
        }
        // Generated SFX files actually present on disk so playback resolves.
        ExpectTrue(
            FileAccess.FileExists("res://assets/audio/sfx/footstep_dirt.wav"),
            "footstep_dirt.wav baked under assets/audio/sfx");
        ExpectTrue(
            FileAccess.FileExists("res://assets/audio/sfx/grunt_pain.wav"),
            "grunt_pain.wav baked under assets/audio/sfx");
        ExpectTrue(
            FileAccess.FileExists("res://assets/audio/sfx/sword_swing.wav"),
            "sword_swing.wav baked under assets/audio/sfx");
        ExpectTrue(
            FileAccess.FileExists("res://assets/audio/sfx/karma_break_stinger.wav"),
            "karma_break_stinger.wav baked under assets/audio/sfx");

        // Voice bark catalog: per-slot path resolution + event-id mapping.
        Karma.Audio.VoiceBarkCatalog.Reset();
        ExpectEqual("res://assets/audio/voice/voice1/ouch.ogg",
            Karma.Audio.VoiceBarkCatalog.Resolve(Karma.Audio.VoiceSlot.Voice1, Karma.Audio.VoiceBarkCatalog.Ouch),
            "voice bark catalog resolves Voice1 ouch path");
        ExpectEqual("res://assets/audio/voice/voice2/ready.ogg",
            Karma.Audio.VoiceBarkCatalog.Resolve(Karma.Audio.VoiceSlot.Voice2, Karma.Audio.VoiceBarkCatalog.Ready),
            "voice bark catalog resolves Voice2 ready path");
        ExpectEqual("res://assets/audio/voice/voice3/laugh.ogg",
            Karma.Audio.VoiceBarkCatalog.Resolve(Karma.Audio.VoiceSlot.Voice3, Karma.Audio.VoiceBarkCatalog.Laugh),
            "voice bark catalog resolves Voice3 laugh path");
        ExpectEqual(string.Empty,
            Karma.Audio.VoiceBarkCatalog.Resolve(Karma.Audio.VoiceSlot.Voice1, ""),
            "voice bark catalog returns empty for blank bark id");
        Karma.Audio.VoiceBarkCatalog.Register(Karma.Audio.VoiceSlot.Voice1, Karma.Audio.VoiceBarkCatalog.Ouch, "res://test/voice_override.ogg");
        ExpectEqual("res://test/voice_override.ogg",
            Karma.Audio.VoiceBarkCatalog.Resolve(Karma.Audio.VoiceSlot.Voice1, Karma.Audio.VoiceBarkCatalog.Ouch),
            "voice bark runtime override beats built-in path");
        Karma.Audio.VoiceBarkCatalog.Reset();
        ExpectEqual("res://assets/audio/voice/voice1/ouch.ogg",
            Karma.Audio.VoiceBarkCatalog.Resolve(Karma.Audio.VoiceSlot.Voice1, Karma.Audio.VoiceBarkCatalog.Ouch),
            "voice bark Reset clears runtime overrides");
        ExpectEqual(Karma.Audio.VoiceBarkCatalog.Ouch,
            Karma.Audio.VoiceBarkCatalog.BarkForEventId("karma_break"),
            "voice bark catalog maps karma_break event to ouch bark");
        ExpectEqual(Karma.Audio.VoiceBarkCatalog.Ready,
            Karma.Audio.VoiceBarkCatalog.BarkForEventId("match_started"),
            "voice bark catalog maps match_started event to ready bark");
        ExpectEqual(Karma.Audio.VoiceBarkCatalog.Laugh,
            Karma.Audio.VoiceBarkCatalog.BarkForEventId("posse_formed"),
            "voice bark catalog maps posse_formed event to laugh bark");
        ExpectEqual(Karma.Audio.VoiceBarkCatalog.Laugh,
            Karma.Audio.VoiceBarkCatalog.BarkForEventId("posse_accepted"),
            "voice bark catalog maps posse_accepted event to laugh bark");
        ExpectEqual(string.Empty,
            Karma.Audio.VoiceBarkCatalog.BarkForEventId("totally_unrelated_event"),
            "voice bark catalog returns empty for unmapped events");

        // Audio falloff registry: defaults + per-event overrides + reset.
        Karma.Audio.AudioFalloffRegistry.Reset();
        var defaultProfile = Karma.Audio.AudioFalloffRegistry.Resolve("totally_unmapped_event");
        ExpectEqual(8f, defaultProfile.MaxDistanceTiles,
            "audio falloff defaults to 8-tile max distance");
        ExpectEqual(5000f, defaultProfile.AttenuationCutoffHz,
            "audio falloff defaults to 5kHz attenuation cutoff");
        ExpectEqual(16f, Karma.Audio.AudioFalloffRegistry.Resolve("karma_break").MaxDistanceTiles,
            "karma_break carries to 16 tiles");
        ExpectEqual(4f, Karma.Audio.AudioFalloffRegistry.Resolve("purchase_complete").MaxDistanceTiles,
            "purchase_complete only carries to 4 tiles");
        ExpectEqual(8f, Karma.Audio.AudioFalloffRegistry.Resolve("door_opened").MaxDistanceTiles,
            "door_opened uses the 8-tile mid-range profile");
        ExpectEqual(8f, Karma.Audio.AudioFalloffRegistry.Resolve("door_opened_chapel").MaxDistanceTiles,
            "compound event id inherits door_opened profile via substring fallback");
        Karma.Audio.AudioFalloffRegistry.Register(
            "purchase_complete",
            new Karma.Audio.AudioFalloffProfile(2f, 3000f));
        ExpectEqual(2f, Karma.Audio.AudioFalloffRegistry.Resolve("purchase_complete").MaxDistanceTiles,
            "runtime override beats built-in falloff profile");
        Karma.Audio.AudioFalloffRegistry.Reset();
        ExpectEqual(4f, Karma.Audio.AudioFalloffRegistry.Resolve("purchase_complete").MaxDistanceTiles,
            "reset restores built-in falloff profile");
        var doorWorldPos = Karma.Audio.PositionalAudioPlayer.TileCenterToWorld(3, 4);
        ExpectEqual(112f, doorWorldPos.X, "TileCenterToWorld places X at the tile center (3.5 * 32)");
        ExpectEqual(144f, doorWorldPos.Y, "TileCenterToWorld places Y at the tile center (4.5 * 32)");

        // Ambient bed manager: outdoor → interior → outdoor transitions.
        ExpectEqual("outdoor",
            Karma.Audio.AmbientBedManager.CategoryToBedId(""),
            "blank category resolves to the outdoor bed");
        ExpectEqual("outdoor",
            Karma.Audio.AmbientBedManager.CategoryToBedId("dungeon"),
            "unknown category falls back to the outdoor bed");
        ExpectEqual("tavern",
            Karma.Audio.AmbientBedManager.CategoryToBedId("tavern"),
            "tavern category maps to the tavern bed");
        ExpectEqual("chapel",
            Karma.Audio.AmbientBedManager.CategoryToBedId("chapel"),
            "chapel category maps to the chapel bed");
        var tavernStructure = new WorldStructureSnapshot(
            "tavern_001", "tavern", "The Crooked Lantern", "tavern",
            10, 10, 64, 64, false, "", 100, "ok");
        var structures = new[] { tavernStructure };
        ExpectEqual("outdoor",
            Karma.Audio.AmbientBedManager.PickBedIdFromInterior("", structures),
            "ambient bed picker returns 'outdoor' when player is not inside");
        ExpectEqual("tavern",
            Karma.Audio.AmbientBedManager.PickBedIdFromInterior("tavern_001", structures),
            "ambient bed picker returns 'tavern' when player is inside the tavern");
        ExpectEqual("outdoor",
            Karma.Audio.AmbientBedManager.PickBedIdFromInterior("", structures),
            "ambient bed picker drops back to 'outdoor' when player exits");
        ExpectEqual("outdoor",
            Karma.Audio.AmbientBedManager.PickBedIdFromInterior("missing_id", structures),
            "ambient bed picker returns 'outdoor' when the structure id isn't visible");
        ExpectEqual("res://assets/audio/ambient/tavern.ogg",
            Karma.Audio.AmbientBedManager.ResolveBedPath("tavern"),
            "ambient bed path resolver builds the canonical path");

        // PlayerState carries a voice slot so per-character casting can drive bark playback.
        var voiceState = new PlayerState("voice_player", "Voice Player");
        ExpectEqual(Karma.Audio.VoiceSlot.Voice1, voiceState.VoiceSlot,
            "PlayerState defaults to VoiceSlot.Voice1");
        voiceState.SetVoiceSlot(Karma.Audio.VoiceSlot.Voice3);
        ExpectEqual(Karma.Audio.VoiceSlot.Voice3, voiceState.VoiceSlot,
            "PlayerState exposes a VoiceSlot setter");

        // Music playlist discovery: walks the music directory in alphabetical
        // order, skips the main-menu placeholder, accepts mp3 / ogg / wav.
        ExpectTrue(
            Karma.Audio.PrototypeMusicPlayer.IsPlayableAudioFile("track.mp3"),
            "music player accepts mp3 files");
        ExpectTrue(
            Karma.Audio.PrototypeMusicPlayer.IsPlayableAudioFile("track.ogg"),
            "music player accepts ogg files");
        ExpectTrue(
            Karma.Audio.PrototypeMusicPlayer.IsPlayableAudioFile("track.wav"),
            "music player accepts wav files");
        ExpectFalse(
            Karma.Audio.PrototypeMusicPlayer.IsPlayableAudioFile("track.import"),
            "music player rejects .import sidecar files");
        ExpectFalse(
            Karma.Audio.PrototypeMusicPlayer.IsPlayableAudioFile("README.md"),
            "music player rejects non-audio files");
        var musicFiles = Karma.Audio.PrototypeMusicPlayer.ListPlayableFiles(
            Karma.Audio.PrototypeMusicPlayer.MusicDirectory);
        ExpectTrue(
            musicFiles.Contains(Karma.Audio.PrototypeMusicPlayer.TravellingOnMedievalFileName),
            "music player playlist includes the verified medieval track");
        ExpectFalse(
            musicFiles.Contains(Karma.Audio.PrototypeMusicPlayer.MenuPlaceholderFileName),
            "music player playlist excludes the main-menu placeholder asset");
        ExpectTrue(
            Karma.Audio.PrototypeMusicPlayer.LoadPlayableAudio(Karma.Audio.PrototypeMusicPlayer.MusicDirectory + musicFiles.First()) is not null,
            "music player loads a raw medieval MP3 file");
        var orderedMusicFiles = musicFiles.OrderBy(name => name, StringComparer.Ordinal).ToList();
        ExpectTrue(
            musicFiles.SequenceEqual(orderedMusicFiles),
            "music player returns playlist files in alphabetical order so playback is deterministic");

        var lpcPlayerBundles = LpcPlayerAppearanceRegistry.ListBundleIds();
        ExpectTrue(lpcPlayerBundles.Count > 0, "LPC player appearance registry discovers generated bundles");
        var pickedLocalLpc = LpcPlayerAppearanceRegistry.PickBundleId("prototype", GameState.LocalPlayerId);
        ExpectTrue(LpcPlayerAppearanceRegistry.BundleExists(pickedLocalLpc), "LPC player appearance picker returns an existing bundle");
        ExpectEqual(
            pickedLocalLpc,
            LpcPlayerAppearanceRegistry.PickBundleId("prototype", GameState.LocalPlayerId),
            "LPC player appearance picker is deterministic for the same world/player");
        var equippedStick = new Dictionary<EquipmentSlot, string>
        {
            [EquipmentSlot.MainHand] = StarterItems.PracticeStickId
        };
        ExpectTrue(
            LpcPlayerEquipmentComposer.EquipmentSignature(equippedStick).Contains(StarterItems.PracticeStickId),
            "LPC player equipment signature includes equipped item ids");
        var equippedAtlas = LpcPlayerEquipmentComposer.ComposeEquippedAtlas(pickedLocalLpc, equippedStick);
        ExpectTrue(
            !string.IsNullOrWhiteSpace(equippedAtlas) && FileAccess.FileExists(equippedAtlas),
            "LPC player equipment composer emits a cached atlas for visible equipped items");
        ExpectEqual(
            LpcPlayerAppearanceRegistry.BuildAtlasPath(pickedLocalLpc),
            LpcPlayerEquipmentComposer.ComposeEquippedAtlas(pickedLocalLpc, new Dictionary<EquipmentSlot, string>()),
            "LPC player equipment composer returns the base atlas when no visible gear is equipped");

        // Pants and shirt appearance layers (manifest + cycler + payload).
        var pantsShirtManifest = PlayerV2LayerManifest.LoadDefault();
        ExpectTrue(
            pantsShirtManifest.LayerOrder.Contains("pants"),
            "player v2 manifest layerOrder includes pants slot");
        ExpectTrue(
            pantsShirtManifest.LayerOrder.Contains("shirt"),
            "player v2 manifest layerOrder includes shirt slot");
        ExpectTrue(
            pantsShirtManifest.Layers.Any(l => l.Id == "pants_blue_32x64" && l.Slot == "pants"),
            "manifest registers pants_blue_32x64 in pants slot");
        ExpectTrue(
            pantsShirtManifest.Layers.Any(l => l.Id == "shirt_black_32x64" && l.Slot == "shirt"),
            "manifest registers shirt_black_32x64 in shirt slot");

        ExpectEqual("pants_blue_32x64", PlayerController.CyclePantsLayerId(""),
            "pants cycler advances from empty to blue pants");
        ExpectEqual("", PlayerController.CyclePantsLayerId("pants_blue_32x64"),
            "pants cycler returns to empty (let outfit show)");
        ExpectEqual("shirt_black_32x64", PlayerController.CycleShirtLayerId(""),
            "shirt cycler advances from empty to black shirt");
        ExpectEqual("", PlayerController.CycleShirtLayerId("shirt_black_32x64"),
            "shirt cycler returns to empty (let outfit show)");

        var emptyPantsShirt = PlayerAppearanceSelection.Default with
        {
            PantsLayerId = string.Empty,
            ShirtLayerId = string.Empty
        };
        var pantsPayload = HudController.BuildAppearanceCyclePayload(
            "pants", emptyPantsShirt);
        ExpectEqual("pants_blue_32x64", pantsPayload["pantsLayerId"],
            "appearance pants payload routes through CyclePantsLayerId");
        var shirtPayload = HudController.BuildAppearanceCyclePayload(
            "shirt", emptyPantsShirt);
        ExpectEqual("shirt_black_32x64", shirtPayload["shirtLayerId"],
            "appearance shirt payload routes through CycleShirtLayerId");

        var psSelection = PlayerAppearanceSelection.Default with
        {
            PantsLayerId = "pants_blue_32x64",
            ShirtLayerId = "shirt_black_32x64"
        };
        var psSlots = psSelection.ToLayerIdsBySlot();
        ExpectEqual("pants_blue_32x64", psSlots["pants"],
            "appearance selection includes pants slot when set");
        ExpectEqual("shirt_black_32x64", psSlots["shirt"],
            "appearance selection includes shirt slot when set");
        ExpectEqual(
            "Combat: none | You ATK:10 DEF:3 | Status: none",
            HudController.FormatCombatLine("Combat: none", 10, 3, System.Array.Empty<string>()),
            "combat HUD line clears status effects when none are active");
        ExpectEqual(
            "Combat: hit | You ATK:10 DEF:3 | Status: Attack Cooldown (2)",
            HudController.FormatCombatLine("Combat: hit", 10, 3, new[] { "Attack Cooldown (2)" }),
            "combat HUD line includes active status effects");
        var statusStrip = HudController.FormatStatusStrip(new[] { "Poisoned", "Burning", "Dirty", "Attack Cooldown (2)" });
        ExpectEqual(4, statusStrip.Count,
            "FormatStatusStrip renders one entry per active status");
        ExpectTrue(statusStrip.Select(entry => entry.Status).SequenceEqual(new[] { "Attack Cooldown (2)", "Burning", "Dirty", "Poisoned" }),
            "FormatStatusStrip sorts entries by normalized status id");
        var medievalPalette = UiPaletteRegistry.Get("medieval");
        var medievalStatusStrip = HudController.FormatStatusStrip(new[] { "Poisoned", "Burning", "Dirty" }, medievalPalette);
        ExpectEqual(medievalPalette.Danger, medievalStatusStrip.First(entry => entry.Status == "Burning").Color,
            "medieval status strip uses palette danger for burning");
        ExpectEqual(medievalPalette.Success, medievalStatusStrip.First(entry => entry.Status == "Poisoned").Color,
            "medieval status strip uses palette success for poisoned");
        ExpectEqual(medievalPalette.DimText, medievalStatusStrip.First(entry => entry.Status == "Dirty").Color,
            "medieval status strip uses palette dim text for dirty");
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
        var combatLog = HudController.FormatCombatLog(new[]
        {
            new ServerEvent("world:1:player_attacked", "world", 1, "Alice struck Bob.", new Dictionary<string, string>()),
            new ServerEvent("world:2:item_used", "world", 2, "Alice used a tincture.", new Dictionary<string, string>()),
            new ServerEvent("world:3:posse_chat", "world", 3, "Posse message.", new Dictionary<string, string>()),
            new ServerEvent("world:4:bounty_claimed", "world", 4, "Bounty claimed.", new Dictionary<string, string>()),
            new ServerEvent("world:5:item_repaired", "world", 5, "Sword repaired.", new Dictionary<string, string>())
        });
        ExpectTrue(combatLog.Contains("-- Combat Log --"), "combat log formatter emits a header");
        ExpectTrue(combatLog.Contains("[1]"), "combat log formatter includes event ticks");
        ExpectTrue(combatLog.Contains("item_used"), "combat log formatter includes icon chip names");
        ExpectTrue(combatLog.Contains("Sword repaired."), "combat log formatter includes event summaries");
        const string firstRunSmokePath = "user://first_run_smoke.json";
        ExpectTrue(HudController.FormatFirstRunTutorialText().Contains("Welcome to Karma"),
            "first-run tutorial text introduces Karma");
        ExpectTrue(HudController.MarkFirstRunTutorialSeen(firstRunSmokePath),
            "first-run tutorial marker can be written");
        ExpectTrue(HudController.HasSeenFirstRunTutorial(firstRunSmokePath),
            "first-run tutorial marker can be read");
        ExpectEqual(new Color(0.95686275f, 0.9019608f, 0.78039217f), UiPaletteRegistry.Get("medieval").PanelBackground,
            "UiPaletteRegistry returns the medieval parchment palette");
        ExpectEqual(new Color(0.12156863f, 0.22745098f, 0.16862746f), UiPaletteRegistry.Get("boarding_school").PanelBackground,
            "UiPaletteRegistry returns the boarding-school green palette");
        ExpectEqual(UiPaletteRegistry.Get("western_sci_fi"), UiPaletteRegistry.Get("unknown_theme"),
            "UiPaletteRegistry falls back gracefully for unknown themes");
        const string saveSmokePath = "user://prototype_save_smoke.json";
        var saveState = new GameState();
        saveState.RegisterPlayer(GameState.LocalPlayerId, "Saver");
        saveState.AddScrip(GameState.LocalPlayerId, 17);
        saveState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(9, 8));
        saveState.AddItem(GameState.LocalPlayerId, StarterItems.RepairKit);
        saveState.AddItem(GameState.LocalPlayerId, StarterItems.WorkVest);
        saveState.EquipPlayer(GameState.LocalPlayerId, StarterItems.WorkVestId);
        saveState.ApplyLocalShift(PrototypeActions.HelpPeer());
        ExpectTrue(saveState.SaveLocalPlayer(saveSmokePath),
            "SaveLocalPlayer writes a prototype save file");
        var loadState = new GameState();
        ExpectTrue(loadState.LoadLocalPlayer(saveSmokePath),
            "LoadLocalPlayer reads a prototype save file");
        ExpectEqual(saveState.LocalPlayer.Karma.Score, loadState.LocalPlayer.Karma.Score,
            "local save restores karma score");
        ExpectEqual(saveState.LocalPlayer.Scrip, loadState.LocalPlayer.Scrip,
            "local save restores scrip");
        ExpectTrue(loadState.LocalPlayer.Position == new TilePosition(9, 8),
            "local save restores tile position");
        ExpectTrue(loadState.LocalPlayer.Inventory.Any(item => item.Id == StarterItems.RepairKitId),
            "local save restores inventory item ids");
        ExpectTrue(loadState.LocalPlayer.Equipment.TryGetValue(EquipmentSlot.Body, out var restoredVest) &&
                   restoredVest.Id == StarterItems.WorkVestId,
            "local save restores equipped item ids");
        var scripBeforeEnsureAfterLoad = loadState.LocalPlayer.Scrip;
        loadState.HasItem(GameState.LocalPlayerId, StarterItems.RepairKitId);
        ExpectEqual(scripBeforeEnsureAfterLoad, loadState.LocalPlayer.Scrip,
            "loaded local save does not receive duplicate starter scrip during EnsurePrototypePlayers");
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
        ExpectTrue(peerPrompt.Contains("V/B/N - Cycle prototype skin/hair/outfit layers"), "peer prompt explains prototype appearance shortcut");
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
        ExpectEqual("Meri Brindle • Tavernkeeper • Village Freeholders",
            HudController.FormatNpcTooltip("Meri Brindle", "Tavernkeeper", "Village Freeholders"),
            "NPC tooltip formatter renders name, role, and faction");
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
        var nervePerks = new[] { new KarmaPerk(PerkCatalog.AbyssalNerveId, "Abyssal Nerve", PerkPath.Descension, 35, "test") };
        ExpectEqual(22.5f, PlayerController.CalculateEffectiveStaminaRecovery(18f, beaconPerks), "Beacon Aura improves stamina recovery");
        ExpectTrue(Mathf.Abs(PlayerController.CalculateEffectiveSprintCost(24f, nervePerks) - 20.4f) < 0.01f, "Renegade Nerve reduces sprint stamina cost");
        var matchServer = new AuthoritativeWorldServer(state, "match-test-world");
        ExpectEqual(MatchStatus.Lobby, matchServer.Match.Status, "new server match starts in lobby");
        ExpectEqual(30 * 60, matchServer.Match.RemainingSeconds, "new server match starts with full duration remaining");
        // Ready up all connected players to start the match
        var matchReadySeq = 1;
        foreach (var matchPid in matchServer.ConnectedPlayerIds)
            matchServer.ProcessIntent(new ServerIntent(matchPid, matchReadySeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        ExpectEqual(MatchStatus.Running, matchServer.Match.Status, "match transitions to Running once all players are ready");
        ExpectEqual("rival_paragon", matchServer.Match.CurrentSaintId, "running match snapshot exposes current Saint leader");
        ExpectEqual("rival_renegade", matchServer.Match.CurrentScourgeId, "running match snapshot exposes current Scourge leader");
        ExpectEqual(MatchPhase.Dawn, AuthoritativeWorldServer.CalculateMatchPhase(0, 5),
            "match phase starts at Dawn");
        ExpectEqual(MatchPhase.Morning, AuthoritativeWorldServer.CalculateMatchPhase(5, 5),
            "match phase advances at one phase interval");
        ExpectEqual(MatchPhase.Night, AuthoritativeWorldServer.CalculateMatchPhase(25, 5),
            "match phase reaches Night at the sixth interval");
        ExpectEqual(MatchPhase.Dawn, AuthoritativeWorldServer.CalculateMatchPhase(30, 5),
            "match phase wraps back to Dawn after Night");
        ExpectTrue(matchServer.Match.Summary.Contains("Phase Dawn"),
            "running match HUD summary includes the current phase");
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
        // Use seq=10 (safely higher than ReadyUp seqs 1-4)
        var postMatchScoreIntent = matchServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            10,
            IntentType.KarmaAction,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["action"] = PrototypeActions.HelpPeerId
            }));
        ExpectFalse(postMatchScoreIntent.WasAccepted, "finished match rejects score-changing intents");
        ExpectEqual(karmaBeforePostMatchIntent, state.LocalKarma.Score, "rejected post-match score intent does not mutate karma");
        var postMatchMove = matchServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            11,
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
        ExpectEqual("item_used_heal", selfRepair.Event.Data["audioCue"], "repair kit use event carries healing audio cue");
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
        ExpectEqual("item_used_food", rationUse.Event.Data["audioCue"], "ration use event carries food audio cue");
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
        ExpectTrue(graceState.Players["grace_target"].IsDown, "lethal attack downs the target");
        ExpectEqual("True", lethalGraceAttack.Event.Data["downed"], "player_downed event carries downed flag");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { lethalGraceAttack.Event }).Contains("downed"),
            "HUD formats lethal attack as downed event");
        graceServer.AdvanceIdleTicks(AuthoritativeWorldServer.DownedCountdownTicks);
        var graceRespawnPosition = graceState.Players["grace_target"].Position;
        ExpectTrue(graceRespawnPosition.DistanceSquaredTo(TilePosition.Origin) >= 144, "downed player respawns away from the death pile after countdown");
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
        var graceDropSnapshot = graceServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        var graceDropSnapshotItem = graceDropSnapshot.WorldItems.First(entity => entity.EntityId == karmaBreakDrop.EntityId);
        ExpectTrue(graceDropSnapshotItem.DropOwnerExpiresTick > graceDropSnapshot.Tick,
            "interest snapshot exposes Karma Break drop ownership expiry");
        ExpectTrue(
            HudController.FormatDeathPileOwnershipPrompt(graceDropSnapshot).Contains("Grace Target's drop ownership expires"),
            "HUD formats death-pile ownership countdown while standing on another player's pile");
        ExpectTrue(WorldRoot.DeathPileOwnershipTint(graceDropSnapshotItem, GameState.LocalPlayerId, graceDropSnapshot.Tick).A > 0f,
            "world item renderer tints active death-pile ownership for other-player drops");

        var expiryState = new GameState();
        expiryState.RegisterPlayer(GameState.LocalPlayerId, "Expiry Local");
        expiryState.RegisterPlayer("expiry_owner", "Expiry Owner");
        expiryState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        var expiryServer = new AuthoritativeWorldServer(expiryState, "death-pile-expiry-test");
        expiryServer.SeedWorldItem("expiry_owned_drop", StarterItems.RepairKit, TilePosition.Origin, "expiry_owner", "Expiry Owner");
        var expiryBefore = expiryServer.CreateInterestSnapshot(GameState.LocalPlayerId).WorldItems.First(item => item.EntityId == "expiry_owned_drop");
        ExpectTrue(WorldRoot.IsDeathPileOwnershipActive(expiryBefore, expiryServer.Tick),
            "death-pile ownership starts active");
        expiryServer.AdvanceIdleTicks(AuthoritativeWorldServer.DeathPileGracePeriodTicks);
        var expiryAfter = expiryServer.CreateInterestSnapshot(GameState.LocalPlayerId).WorldItems.First(item => item.EntityId == "expiry_owned_drop");
        ExpectEqual(string.Empty, expiryAfter.DropOwnerId,
            "death-pile ownership clears after grace period");
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
        // The boarding-school theme is no longer the default (medieval is); pin
        // this test block to a boarding-school config explicitly so its art /
        // tile-zone assertions still validate the boarding-school theme path.
        var boardingSchoolPrototypeConfig = WorldConfig.CreatePrototype() with
        {
            Seed = new WorldSeed(8675309, "Boarding School Prototype", "boarding_school"),
        };
        var generatedA = WorldGenerator.Generate(boardingSchoolPrototypeConfig);
        var generatedB = WorldGenerator.Generate(boardingSchoolPrototypeConfig);
        ExpectEqual(generatedA.Summary, generatedB.Summary, "world generation is deterministic for the same seed");

        // ── Step 17: Road/path generation ────────────────────────────────────────
        // Spanning path graph connecting all station locations (Prim's MST).
        var locationIds = new System.Collections.Generic.HashSet<string>(generatedA.Locations.Select(l => l.Id));
        ExpectEqual(generatedA.Locations.Count - 1, generatedA.PathEdges.Count,
            "path graph has exactly N-1 edges for N locations (spanning tree)");
        ExpectTrue(generatedA.PathEdges.All(edge => locationIds.Contains(edge.FromLocationId) && locationIds.Contains(edge.ToLocationId)),
            "all path edge endpoints reference known locations");
        ExpectTrue(generatedA.PathEdges.All(edge => edge.FromLocationId != edge.ToLocationId),
            "path edges do not self-loop");
        // Verify every location is reachable (spanning tree covers all nodes)
        var reachable = new System.Collections.Generic.HashSet<string> { generatedA.PathEdges[0].FromLocationId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var edge in generatedA.PathEdges)
            {
                if (reachable.Contains(edge.FromLocationId) && reachable.Add(edge.ToLocationId)) changed = true;
                if (reachable.Contains(edge.ToLocationId) && reachable.Add(edge.FromLocationId)) changed = true;
            }
        }
        ExpectEqual(generatedA.Locations.Count, reachable.Count, "path graph connects all locations");
        ExpectTrue(generatedA.PathEdges.All(edge =>
            generatedA.Locations.Any(l => l.Id == edge.FromLocationId && l.X == edge.FromX && l.Y == edge.FromY) &&
            generatedA.Locations.Any(l => l.Id == edge.ToLocationId && l.X == edge.ToX && l.Y == edge.ToY)),
            "path edge coordinates match location positions");
        ExpectEqual(generatedA.PathEdges.Count, generatedB.PathEdges.Count,
            "path graph generation is deterministic");

        // ── Step 18: Path-aware world rendering ───────────────────────────────────
        // Path edges are stamped onto the tile map as PathDust road tiles.
        var roadTiles = generatedA.TileMap.Tiles.Where(t => t.ZoneId == "road_path").ToArray();
        ExpectTrue(roadTiles.Length > 0, "tile map contains road_path tiles from path edges");
        ExpectTrue(roadTiles.All(t => t.FloorId == WorldTileIds.PathDust),
            "road_path tiles use PathDust floor");
        var roadTilesB = generatedB.TileMap.Tiles.Where(t => t.ZoneId == "road_path").ToArray();
        ExpectEqual(roadTiles.Length, roadTilesB.Length, "road tile placement is deterministic");
        // Every road_path tile must be on a ground tile type (not a station floor)
        ExpectTrue(roadTiles.All(t => t.ZoneId == "road_path"),
            "road tiles are tagged with road_path zone");

        ExpectEqual(
            generatedA.Config.WidthTiles * generatedA.Config.HeightTiles,
            generatedA.TileMap.Tiles.Count,
            "world generation creates a logical tile for every coordinate");
        ExpectEqual(3, generatedA.TileMap.ChunkColumns, "prototype tile map stays compact around the boarding school building models");
        ExpectEqual(3, generatedA.TileMap.ChunkRows, "prototype tile map stays compact around the boarding school building models");
        ExpectEqual(
            new GeneratedChunkCoordinate(0, 0),
            generatedA.TileMap.GetChunkCoordinateForTile(3, 3),
            "tile map resolves tile coordinates to chunk coordinates");
        ExpectEqual(32 * 32, generatedA.TileMap.GetChunk(new GeneratedChunkCoordinate(0, 0)).Tiles.Count, "tile map can materialize one chunk");
        ExpectEqual(4, generatedA.TileMap.GetChunksAround(16, 16, radiusChunks: 1).Count, "tile map can query nearby chunks on the prototype map");
        var artSet = ThemeArtRegistry.GetForTheme(generatedA.Theme);
        ExpectEqual(
            1,
            artSet.GetTile(WorldTileIds.ClinicFloor).AtlasTileSizePixels,
            "theme art registry records pixel atlas source scale");
        ExpectEqual(
            new Rect2(160, 0, 32, 32),
            artSet.GetTile(WorldTileIds.ClinicFloor).SourceRegion,
            "theme art registry can calculate boarding school atlas source regions");
        ExpectTrue(artSet.GetTile(WorldTileIds.ClinicFloor).HasAtlasRegion, "theme art registry enables mapped boarding school atlas regions");
        ExpectEqual(
            ThemeArtRegistry.BoardingSchoolGrassAtlasPath,
            artSet.GetTile(WorldTileIds.ClinicFloor).AtlasPath,
            "prototype uses boarding school grass tiles for mapped floors");
        ExpectEqual(
            ThemeArtRegistry.BoardingSchoolPropsAtlasPath,
            artSet.GetTile(WorldTileIds.WallMetal).AtlasPath,
            "prototype uses boarding school props for structures");
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
        var alternateMap = WorldGenerator.Generate(WorldConfig.FromServerConfig(
            "renderer-alt-test",
            new WorldSeed(868, "Renderer Alt", "western-sci-fi"),
            ServerConfig.Prototype4Player));
        renderer.SetChunks(
            new[] { ToMapChunkSnapshot(alternateMap.TileMap.GetChunk(new GeneratedChunkCoordinate(0, 0))) },
            artSet);
        ExpectEqual(1, renderer.LoadedChunkCount, "tile renderer keeps the compact visible chunk loaded");
        ExpectEqual(1, renderer.LastUpdatedChunkCount, "tile renderer records compact chunk replacement when its revision changes");
        var boardingSchoolGrassTileIds = new[]
        {
            WorldTileIds.GroundScrub,
            WorldTileIds.GroundDust,
            WorldTileIds.PathDust,
            WorldTileIds.ClinicFloor,
            WorldTileIds.MarketFloor,
            WorldTileIds.WorkshopFloor,
            WorldTileIds.DuelRingFloor
        }.ToHashSet();
        ExpectTrue(generatedA.TileMap.Tiles.All(tile => boardingSchoolGrassTileIds.Contains(tile.FloorId)), "boarding school prototype covers the map in grass tile variants");
        ExpectTrue(generatedA.TileMap.Tiles.Select(tile => tile.FloorId).Distinct().Count() > 1, "boarding school prototype varies the grass tiles across the map");
        ExpectTrue(generatedA.TileMap.Tiles.All(tile => string.IsNullOrWhiteSpace(tile.StructureId)), "boarding school prototype keeps tile structures out of the grass showcase map");
        ExpectTrue(generatedA.TileMap.Tiles.All(tile => tile.ZoneId == "boarding_school_grass" || tile.ZoneId == "road_path"), "boarding school prototype labels tiles as grass or road zone");
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
        ExpectTrue(
            generatedA.Locations.All(location => !string.IsNullOrWhiteSpace(location.InteriorId) && !string.IsNullOrWhiteSpace(location.InteriorKind)),
            "generated station locations declare future interior hooks");
        ExpectTrue(
            generatedA.Locations.Any(location => location.Role == "clinic" && location.InteriorKind == "clinic"),
            "clinic station declares a clinic interior kind");
        ExpectTrue(
            generatedA.Locations.Any(location => location.Role == "workshop" && location.InteriorKind == "workshop"),
            "workshop station declares a workshop interior kind");
        var generatedContentState = new GameState();
        generatedContentState.RegisterPlayer(GameState.LocalPlayerId, "Generated Content Tester");
        var generatedContentServer = new AuthoritativeWorldServer(generatedContentState, "generated-content-test");
        generatedContentServer.SeedGeneratedWorldContent(generatedA);
        // Starter set: 3 greenhouse + 12 sliced-prop structures (clinic bed/cabinet/door,
        // shop shelves/counter/door, workbench/toolbox, 2 boards, supply pad, generator).
        const int starterStructureCount = 15;
        ExpectEqual(starterStructureCount + generatedA.Locations.Count + generatedA.StructurePlacements.Count, generatedContentServer.WorldStructures.Count, "server seeds generated station markers and generated structures alongside starter structures");
        var firstGeneratedLocation = generatedA.Locations[0];
        generatedContentState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(firstGeneratedLocation.X, firstGeneratedLocation.Y));
        var generatedStationSnapshot = generatedContentServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        var generatedStation = generatedStationSnapshot.Structures.FirstOrDefault(structure => structure.Name == firstGeneratedLocation.Name && structure.Category == "station");
        ExpectTrue(generatedStation is not null, "interest snapshot exposes generated station markers near the player");
        ExpectTrue(generatedStation?.InteractionPrompt.Contains(firstGeneratedLocation.KarmaHook) == true, "generated station marker prompt exposes its karma hook");
        ExpectTrue(generatedStation?.InteractionPrompt.Contains(firstGeneratedLocation.InteriorKind) == true, "generated station marker prompt exposes its future interior hook");
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
        var repairedStationSnapshot = generatedContentServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(
            repairedStationSnapshot.Structures.Any(structure => structure.EntityId == generatedStation?.EntityId && structure.InteractionPrompt.Contains("Station state: stabilized")),
            "generated structure repair stabilizes the linked station marker state");
        generatedContentState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        var stabilizedRespawn = generatedContentServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            73,
            IntentType.KarmaBreak,
            new System.Collections.Generic.Dictionary<string, string>()));
        ExpectTrue(stabilizedRespawn.WasAccepted, "server accepts Karma Break near a stabilized generated station");
        ExpectEqual(new TilePosition(firstGeneratedLocation.X, firstGeneratedLocation.Y), generatedContentState.Players[GameState.LocalPlayerId].Position, "context-aware respawn prefers safe stabilized station markers");
        var sabotageGeneratedStructure = generatedContentServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            74,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = firstGeneratedStructurePlacement.StructureId,
                ["action"] = "sabotage"
            }));
        ExpectTrue(sabotageGeneratedStructure.WasAccepted, "server accepts sabotage on generated station structures");
        ExpectEqual(StarterFactions.ToId(firstGeneratedStructurePlacement.SuggestedFaction), sabotageGeneratedStructure.Event.Data["factionId"], "generated structure sabotage affects the station suggested faction");
        ExpectTrue(int.Parse(sabotageGeneratedStructure.Event.Data["integrity"]) < int.Parse(repairGeneratedStructure.Event.Data["integrity"]), "generated structure sabotage damages integrity");
        var sabotagedStationSnapshot = generatedContentServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(
            sabotagedStationSnapshot.Structures.Any(structure => structure.EntityId == generatedStation?.EntityId && structure.InteractionPrompt.Contains("Station state: compromised")),
            "generated structure sabotage compromises the linked station marker state");
        var seededGeneratedNpcCount = generatedA.Npcs.Count(npc => npc.Id != StarterNpcs.Mara.Id && npc.Id != StarterNpcs.Dallen.Id);
        ExpectEqual(2 + seededGeneratedNpcCount, generatedContentServer.Npcs.Count, "server seeds generated NPC placements without duplicating starter NPCs");
        var firstGeneratedNpcPlacement = generatedA.NpcPlacements.First(placement =>
            placement.NpcId != StarterNpcs.Mara.Id &&
            placement.NpcId != StarterNpcs.Dallen.Id &&
            generatedA.Locations.Any(loc => loc.Id == placement.LocationId &&
                loc.Role is not "workshop" and not "clinic" and not "market" and not "notice-board"));
        var npcLinkedStructure = generatedA.StructurePlacements.First(placement => placement.LocationId == firstGeneratedNpcPlacement.LocationId);
        generatedContentState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(npcLinkedStructure.X, npcLinkedStructure.Y));
        var compromiseNpcStation = generatedContentServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            75,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = npcLinkedStructure.StructureId,
                ["action"] = "sabotage"
            }));
        ExpectTrue(compromiseNpcStation.WasAccepted, "server accepts sabotage on generated NPC linked station structure");
        generatedContentState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(firstGeneratedNpcPlacement.X, firstGeneratedNpcPlacement.Y));
        var generatedNpcSnapshot = generatedContentServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(
            generatedNpcSnapshot.Npcs.Any(npc => npc.Id == firstGeneratedNpcPlacement.NpcId && npc.TileX == firstGeneratedNpcPlacement.X && npc.TileY == firstGeneratedNpcPlacement.Y),
            "interest snapshot exposes seeded generated NPCs near the player");
        ExpectTrue(
            generatedNpcSnapshot.Dialogues.Any(dialogue => dialogue.NpcId == firstGeneratedNpcPlacement.NpcId && dialogue.Prompt.Contains("currently compromised")),
            "generated NPC dialogue reflects compromised station state");
        ExpectTrue(
            generatedNpcSnapshot.Dialogues.Any(dialogue => dialogue.NpcId == firstGeneratedNpcPlacement.NpcId && dialogue.Choices.Any(choice => choice.Id == "assist_need" && choice.Label.Contains("Emergency help"))),
            "generated NPC dialogue adjusts assist choices for compromised stations");
        ExpectTrue(
            generatedNpcSnapshot.Quests.Any(quest => generatedA.Quests.Any(definition => definition.Id == quest.Id && definition.GiverNpcId == firstGeneratedNpcPlacement.NpcId)),
            "interest snapshot exposes generated station quests for nearby generated NPCs");
        var firstGeneratedQuest = generatedA.Quests.First(quest => quest.GiverNpcId == firstGeneratedNpcPlacement.NpcId);
        ExpectTrue(
            generatedNpcSnapshot.Quests.Any(quest => quest.Id == firstGeneratedQuest.Id && quest.ScripReward == Math.Max(0, firstGeneratedQuest.ScripReward - 2)),
            "compromised station state reduces visible generated quest scrip reward");
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
        ExpectEqual(Math.Max(0, firstGeneratedQuest.ScripReward - 2).ToString(), completeGeneratedQuest.Event.Data["scripReward"], "generated quest completion reports station-state adjusted reward");
        ExpectEqual("-2", completeGeneratedQuest.Event.Data["stationStateBonus"], "generated quest completion reports compromised station penalty");
        ExpectTrue(generatedContentServer.WorldItems.Count >= generatedA.OddityPlacements.Count, "server seeds generated oddity placements as world items");
        var firstOddityPlacement = generatedA.OddityPlacements[0];
        generatedContentState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(firstOddityPlacement.X, firstOddityPlacement.Y));
        var generatedContentSnapshot = generatedContentServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(
            generatedContentSnapshot.WorldItems.Any(item => item.ItemId == firstOddityPlacement.ItemId && item.TileX == firstOddityPlacement.X && item.TileY == firstOddityPlacement.Y),
            "interest snapshot exposes seeded generated oddities near the player");
        ExpectTrue(artSet.Tiles.ContainsKey(WorldTileIds.ClinicFloor), "theme art registry maps clinic floor tile id");
        ExpectEqual(
            ThemeArtRegistry.BoardingSchoolPropsAtlasPath,
            artSet.GetTile(WorldTileIds.DoorAirlock).AtlasPath,
            "theme art registry maps DoorAirlock to boarding school props atlas");
        var playerSprite = PrototypeSpriteCatalog.Get(PrototypeSpriteKind.Player);
        var whoopieSprite = PrototypeSpriteCatalog.Get(PrototypeSpriteKind.WhoopieCushion);
        ExpectEqual("Player", playerSprite.DisplayName, "prototype sprite catalog names player model");
        ExpectTrue(playerSprite.Layers.Count >= 8, "prototype player sprite has layered pixel art");
        ExpectEqual(expectedPlayerAtlasPath, playerSprite.AtlasPath, "prototype player sprite records active engineer atlas path");
        ExpectTrue(playerSprite.HasAtlasRegion, "prototype player sprite can use character atlas art");
        var expectedPlayerWalkFrameCount = expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath ||
                                           expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath ||
                                           expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2TrialImportedAtlasPath ||
                                           expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64AtlasPath ||
                                           expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath ||
                                           expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath
            ? 3
            : expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath
                ? 2
                : expectedPlayerAtlasPath == PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath ||
                  expectedPlayerAtlasPath == PrototypeSpriteCatalog.LayeredPlayerPreviewEightDirectionAtlasPath
                    ? 7
                    : 4;
        ExpectEqual(
            expectedPlayerWalkFrameCount,
            playerSprite.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkDownAnimation).Frames.Count,
            "prototype player sprite maps multi-frame walk animations");
        var expectedWalkRightFinalFrame = expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath ||
                                          expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath ||
                                          expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2TrialImportedAtlasPath ||
                                          expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64AtlasPath
            ? new Rect2(2f * 32f, 3f * 64f, 32f, 64f)
            : expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath ||
              expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath
                ? new Rect2(2f * 64f, 3f * 64f, 64f, 64f)
            : expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath
                ? new Rect2(128f, 192f, 64f, 64f)
                : expectedPlayerAtlasPath == PrototypeSpriteCatalog.EngineerPlayerEightDirectionAtlasPath ||
                  expectedPlayerAtlasPath == PrototypeSpriteCatalog.LayeredPlayerPreviewEightDirectionAtlasPath
                    ? new Rect2(64f, 64f, 32f, 32f)
                    : new Rect2(963f, 860f, 120f, 190f);
        ExpectEqual(
            expectedWalkRightFinalFrame,
            playerSprite.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkRightAnimation).Frames[^1],
            "prototype player sprite maps the active engineer sheet walk-right row");
        if (expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2RealBaseBlackBootsAtlasPath ||
            expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2LayeredPreview32x64AtlasPath ||
            expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2TrialImportedAtlasPath ||
            expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64AtlasPath)
        {
            ExpectEqual(
                new Rect2(3f * 32f, 1f * 64f, 32f, 64f),
                playerSprite.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkUpRightAnimation).Frames[0],
                "32x64 player sprite maps up-right movement to the true up-right column");
            ExpectEqual(
                new Rect2(5f * 32f, 3f * 64f, 32f, 64f),
                playerSprite.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkUpLeftAnimation).Frames[^1],
                "32x64 player sprite maps up-left movement to the true up-left column");
        }
        else if (expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Model32x64RuntimeAtlasPath ||
                 expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2KnightReferenceAtlasPath)
        {
            ExpectEqual(
                new Rect2(3f * 64f, 1f * 64f, 64f, 64f),
                playerSprite.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkUpRightAnimation).Frames[0],
                "knight reference player sprite maps up-right movement to the true up-right column");
            ExpectEqual(
                new Rect2(5f * 64f, 3f * 64f, 64f, 64f),
                playerSprite.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkUpLeftAnimation).Frames[^1],
                "knight reference player sprite maps up-left movement to the true up-left column");
        }
        else if (expectedPlayerAtlasPath == PrototypeSpriteCatalog.PlayerV2Engineer64PreviewAtlasPath)
        {
            ExpectEqual(
                3,
                playerSprite.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkRightAnimation).Frames.Count,
                "64px preview player sprite uses strict walk-strip stepping for right movement");
            ExpectEqual(
                new Rect2(3f * 64f, 1f * 64f, 64f, 64f),
                playerSprite.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkUpRightAnimation).Frames[0],
                "64px preview player sprite uses strict up-right walk-strip frames");
            ExpectEqual(
                new Rect2(5f * 64f, 3f * 64f, 64f, 64f),
                playerSprite.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkUpLeftAnimation).Frames[^1],
                "64px preview player sprite uses mirrored strict up-left walk-strip frames");
        }
        ExpectEqual(PrototypeSpriteKind.Dallen, PrototypeSpriteCatalog.GetKindForNpc(StarterNpcs.Dallen.Id), "prototype sprite catalog maps Dallen NPC visuals");
        ExpectTrue(PrototypeSpriteCatalog.Get(PrototypeSpriteKind.Dallen).HasAtlasRegion, "prototype Dallen sprite can use character atlas art");
        var pixellabTrialNpc = PrototypeSpriteCatalog.Get(PrototypeSpriteKind.PixellabTrialNpc);
        ExpectEqual(PrototypeSpriteCatalog.PixellabTrialNpcRuntimeAtlasPath, pixellabTrialNpc.AtlasPath, "prototype PixelLab trial NPC records imported runtime atlas path");
        ExpectEqual(new Vector2(48f, 48f), pixellabTrialNpc.Size, "prototype PixelLab trial NPC scales its 64px padded cells down to match the 32x64 player body height");
        ExpectEqual(3, pixellabTrialNpc.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkRightAnimation).Frames.Count, "prototype PixelLab trial NPC uses the runtime sheet's three walk rows");
        ExpectEqual(new Rect2(2f * 64f, 1f * 64f, 64f, 64f), pixellabTrialNpc.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkRightAnimation).Frames[0], "prototype PixelLab trial NPC maps right walk to runtime direction column");
        var pixellabWalkDown = pixellabTrialNpc.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkDownAnimation);
        ExpectEqual(4, pixellabWalkDown.Frames.Count, "prototype PixelLab trial NPC uses the actual north/south step art with a mirrored middle frame");
        ExpectEqual(new Rect2(0f, 1f * 64f, 64f, 64f), pixellabWalkDown.Frames[0], "prototype PixelLab trial NPC south walk starts on walk row 1, not idle");
        ExpectEqual(new Rect2(0f, 3f * 64f, 64f, 64f), pixellabWalkDown.Frames[2], "prototype PixelLab trial NPC south walk includes the third vertical step frame");
        ExpectEqual(4f, pixellabWalkDown.Speed, "prototype PixelLab trial NPC slows weaker vertical walk frames while still cycling step art");
        ExpectEqual(CharacterSheetLayout.StandardWalkAnimationSpeed, pixellabTrialNpc.Animations.First(animation => animation.Name == PrototypeCharacterSprite.WalkRightAnimation).Speed, "prototype PixelLab trial NPC keeps horizontal walk frames snappy");
        ExpectTrue(PrototypeWanderingNpc.CalculatePatrolTarget(0.0, Vector2.Zero, 80f, horizontalOnly: false).DistanceTo(new Vector2(-80f, -28f)) < 0.01f, "prototype PixelLab trial NPC walker starts on its full patrol route");
        ExpectTrue(PrototypeWanderingNpc.CalculatePatrolTarget(5.0, Vector2.Zero, 80f, horizontalOnly: false).Y > -28f, "prototype PixelLab trial NPC walker includes vertical movement again");
        ExpectEqual(0f, PrototypeWanderingNpc.CalculatePatrolTarget(2.5, Vector2.Zero, 80f).Y, "prototype PixelLab trial NPC still supports horizontal-only patrols for review");
        ExpectTrue(ServerNpcObject.FormatPrompt("Dallen Venn", "Trader", "Village Freeholders").Contains("Faction: Village Freeholders"), "server NPC prompt formats faction");
        var vendorPrompt = ServerNpcObject.FormatVendorPrompt(
            "Dallen Venn",
            "Trader",
            "Village Freeholders",
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
        ExpectTrue(
            PrototypeSpriteCatalog.Get(PrototypeSpriteKind.RepairKit).AtlasPath.EndsWith("repair_kit_case.png"),
            "prototype repair kit can use polished Gemini art");
        ExpectEqual(PrototypeSpriteCatalog.ItemAtlasPath, PrototypeSpriteCatalog.Get(PrototypeSpriteKind.RationPack).AtlasPath, "prototype support item sprite records item atlas path");
        ExpectTrue(PrototypeSpriteCatalog.Get(PrototypeSpriteKind.PortableTerminal).HasAtlasRegion, "prototype utility item sprite can use utility atlas art");
        ExpectTrue(
            PrototypeSpriteCatalog.Get(PrototypeSpriteKind.PortableTerminal).AtlasPath.EndsWith("portable_terminal.png"),
            "prototype terminal can use polished Gemini art");
        ExpectEqual(PrototypeSpriteCatalog.WeaponAtlasPath, PrototypeSpriteCatalog.Get(PrototypeSpriteKind.StunBaton).AtlasPath, "prototype weapon sprite records weapon atlas path");
        ExpectTrue(PrototypeSpriteCatalog.Get(PrototypeSpriteKind.Rifle27).HasAtlasRegion, "prototype weapon sprite can use weapon atlas art");
        ExpectEqual(PrototypeSpriteCatalog.ToolAtlasPath, PrototypeSpriteCatalog.Get(PrototypeSpriteKind.MultiTool).AtlasPath, "prototype tool sprite records tool atlas path");
        ExpectTrue(PrototypeSpriteCatalog.Get(PrototypeSpriteKind.PortableShield).HasAtlasRegion, "prototype tool sprite can use tool atlas art");
        ExpectEqual(42, StarterItems.All.Count, "starter item catalog exposes all prototype items");
        ExpectEqual(
            StarterItems.All.Count,
            StarterItems.All.Select(item => item.Id).Distinct().Count(),
            "starter item catalog ids are unique");
        ExpectEqual(ItemRarity.Common, StarterItems.RepairKit.Rarity,
            "non-contraband starter items default to Common rarity");
        ExpectEqual(ItemRarity.Contraband, StarterItems.ContrabandPackage.Rarity,
            "contraband starter items derive Contraband rarity from IsContraband");
        ExpectEqual(100, StarterItems.PracticeStick.Durability,
            "starter equipment defaults to full durability");
        ExpectFalse(StarterItems.PracticeStick.IsBroken,
            "full-durability starter equipment is not broken");
        ExpectEqual(new Color(0.78f, 0.78f, 0.78f), HudController.InventoryTintForRarity(ItemRarity.Common),
            "inventory HUD tints Common items gray");
        ExpectEqual(new Color(1f, 0.36f, 0.34f), HudController.InventoryTintForRarity(ItemRarity.Contraband),
            "inventory HUD tints Contraband items red");
        foreach (var starterItem in StarterItems.All)
        {
            ExpectTrue(StarterItems.TryGetById(starterItem.Id, out _), $"starter item catalog can resolve {starterItem.Id}");
            ExpectTrue(
                PrototypeSpriteCatalog.Get(PrototypeSpriteCatalog.GetKindForItem(starterItem.Id)).HasAtlasRegion,
                $"starter item {starterItem.Id} has mapped prototype art");
        }

        ItemArtRegistry.ResetCache();
        var medievalItemIcons = ItemArtRegistry.ListThemeIcons("medieval");
        foreach (var icon in medievalItemIcons)
        {
            var resolvedIcon = ItemArtRegistry.Get("medieval", icon.ItemId);
            ExpectTrue(resolvedIcon.HasIcon, $"item art registry resolves medieval icon {icon.ItemId}");
            ExpectEqual(icon.IconPath, resolvedIcon.IconPath, $"item art registry records medieval icon path for {icon.ItemId}");
            ExpectTrue(FileAccess.FileExists(resolvedIcon.IconPath), $"item art registry path exists for {icon.ItemId}");
        }
        ExpectFalse(ItemArtRegistry.Get("medieval", "missing_test_item_icon").HasIcon, "item art registry reports missing theme icons for atlas fallback");
        if (FileAccess.FileExists("res://assets/art/themes/medieval/items/flamethrower.png"))
        {
            ExpectTrue(ItemArtRegistry.Get("medieval", StarterItems.FlameThrowerId).HasIcon, "item art registry resolves compact medieval icon aliases");
        }

        ExpectEqual(PrototypeSpriteKind.BackpackBrown, PrototypeSpriteCatalog.GetKindForItem(StarterItems.BackpackBrownId), "prototype sprite catalog maps backpack art");
        ExpectEqual(PrototypeSpriteKind.BallisticRound, PrototypeSpriteCatalog.GetKindForItem(StarterItems.BallisticRoundId), "prototype sprite catalog maps ballistic round art");
        ExpectEqual(PrototypeSpriteKind.EnergyCell, PrototypeSpriteCatalog.GetKindForItem(StarterItems.EnergyCellId), "prototype sprite catalog maps energy cell art");
        ExpectEqual(PrototypeSpriteKind.StimSpike, PrototypeSpriteCatalog.GetKindForItem(StarterItems.StimSpikeId), "prototype sprite catalog maps stim spike art");
        ExpectEqual(PrototypeSpriteKind.DownerHaze, PrototypeSpriteCatalog.GetKindForItem(StarterItems.DownerHazeId), "prototype sprite catalog maps downer haze art");
        ExpectEqual(PrototypeSpriteKind.TremorTab, PrototypeSpriteCatalog.GetKindForItem(StarterItems.TremorTabId), "prototype sprite catalog maps tremor tab art");

        ExpectEqual(new Vector2(500f, 180f), WorldRoot.CalculateCatalogShowcasePosition(0), "catalog showcase starts inside the compact prototype map");
        ExpectEqual(new Vector2(500f + (6 * 48f), 180f), WorldRoot.CalculateCatalogShowcasePosition(6), "catalog showcase fills a row before wrapping");
        ExpectEqual(new Vector2(500f, 228f), WorldRoot.CalculateCatalogShowcasePosition(7), "catalog showcase wraps to the next row");
        ExpectEqual(new Vector2(500f, 420f), WorldRoot.CalculateStructureShowcasePosition(0), "structure showcase starts inside the compact prototype map");
        ExpectEqual(new Vector2(500f + (4 * 58f), 420f), WorldRoot.CalculateStructureShowcasePosition(4), "structure showcase fills a row before wrapping");
        ExpectEqual(new Vector2(500f, 478f), WorldRoot.CalculateStructureShowcasePosition(5), "structure showcase wraps to the next row");
        ExpectEqual(new Vector2(14f * 32f, 22f * 32f), WorldRoot.CalculateBoardingSchoolBuildingPosition(0), "boarding school building showcase starts on the smaller grass map");
        ExpectEqual(new Vector2(70f * 32f, 62f * 32f), WorldRoot.CalculateBoardingSchoolBuildingPosition(7), "boarding school building showcase fits the library on the smaller grass map");
        var mainHallCollisionSize = WorldRoot.CalculateBoardingSchoolBuildingCollisionSize(StructureArtCatalog.Get(StructureSpriteKind.BoardingSchoolMainHall));
        ExpectTrue(mainHallCollisionSize.X > 32f && mainHallCollisionSize.X < 768f * 0.6f, "boarding school buildings expose tight physics collision footprints");
        ExpectTrue(mainHallCollisionSize.Y >= 24f && mainHallCollisionSize.Y <= 56f, "boarding school buildings keep shallow collision depth for stairs and doors");
        ExpectEqual(new Vector2(0f, -mainHallCollisionSize.Y * 0.35f), WorldRoot.CalculateBoardingSchoolBuildingCollisionOffset(StructureArtCatalog.Get(StructureSpriteKind.BoardingSchoolMainHall)), "boarding school building collisions sit close to the lower wall footprint");
        ExpectEqual(new Vector2(5f * 32f, 14f * 32f), WorldRoot.CalculateBoardingSchoolPropPosition(0), "boarding school props are placed on the grass map");
        ExpectEqual(new Vector2(57f * 32f, 52f * 32f), WorldRoot.CalculateBoardingSchoolPropPosition(15), "boarding school prop showcase places every props atlas entry");
        var stoneBenchCollisionSize = WorldRoot.CalculateBoardingSchoolPropCollisionSize(StructureArtCatalog.Get(StructureSpriteKind.BoardingSchoolStoneBench));
        ExpectTrue(stoneBenchCollisionSize.X > 16f && stoneBenchCollisionSize.Y >= 12f, "boarding school props expose compact collision footprints");
        ExpectEqual(new Vector2(6f * 32f, 25f * 32f), WorldRoot.CalculateBoardingSchoolTreePosition(0), "boarding school trees are placed on the grass map");
        ExpectEqual(new Vector2(42f * 32f, 61f * 32f), WorldRoot.CalculateBoardingSchoolTreePosition(15), "boarding school tree showcase places every trees atlas entry");
        var oakCollisionSize = WorldRoot.CalculateBoardingSchoolTreeCollisionSize(StructureArtCatalog.Get(StructureSpriteKind.BoardingSchoolCourtyardOak));
        ExpectTrue(oakCollisionSize.X > 14f && oakCollisionSize.Y >= 12f, "boarding school trees expose compact trunk collision footprints");
        ExpectEqual(new Vector2(7f * 32f, 8f * 32f), WorldRoot.CalculateBoardingSchoolFlowerPosition(0), "boarding school flower details are scattered on the grass map");
        ExpectTrue(WorldRoot.CalculateBoardingSchoolFlowerPosition(23).X >= 0f, "boarding school flower detail placement handles the full scatter set");
        ExpectTrue(TopDownDepth.TileLayerZ + 1 < TopDownDepth.CalculateZIndex(0f), "flower details render as background below actors and props");
        var greenhouse = StructureArtCatalog.Get(StructureSpriteKind.GreenhouseStandard);
        ExpectEqual(StructureArtCatalog.GreenhouseAtlasPath, greenhouse.AtlasPath, "structure catalog records greenhouse atlas path");
        ExpectTrue(greenhouse.HasAtlasRegion, "structure catalog maps greenhouse atlas art");
        ExpectTrue(StructureArtCatalog.All.ContainsKey(StructureSpriteKind.GreenhouseDamaged), "structure catalog maps greenhouse variants");
        var cargoCrateStructure = StructureArtCatalog.Get(StructureSpriteKind.CargoCrate);
        ExpectTrue(cargoCrateStructure.AtlasPath.EndsWith("cargo_crate.png"), "structure catalog exposes polished Gemini station props");
        ExpectEqual(new Rect2(0f, 0f, 128f, 128f), cargoCrateStructure.AtlasRegion, "polished Gemini station props use their full source image");
        var blueCrystalStructure = StructureArtCatalog.Get(StructureSpriteKind.BlueCrystalShard);
        ExpectTrue(blueCrystalStructure.AtlasPath.EndsWith("blue_crystal_shard.png"), "structure catalog exposes polished Gemini natural props");
        ExpectEqual("natural_prop", blueCrystalStructure.Category, "natural prop structures are categorized for preview placement");
        var mainHallStructure = StructureArtCatalog.Get(StructureSpriteKind.BoardingSchoolMainHall);
        ExpectEqual(StructureArtCatalog.BoardingSchoolBuildingsAtlasPath, mainHallStructure.AtlasPath, "structure catalog brings in the boarding school building atlas");
        ExpectEqual(new Rect2(0f, 0f, 768f, 576f), mainHallStructure.AtlasRegion, "structure catalog maps the boarding school main hall model");
        var stoneBenchStructure = StructureArtCatalog.Get(StructureSpriteKind.BoardingSchoolStoneBench);
        ExpectEqual(StructureArtCatalog.BoardingSchoolPropsAtlasPath, stoneBenchStructure.AtlasPath, "structure catalog brings in the boarding school props atlas");
        ExpectEqual(new Rect2(256f, 0f, 112f, 64f), stoneBenchStructure.AtlasRegion, "structure catalog maps props atlas entries");
        var courtyardOakStructure = StructureArtCatalog.Get(StructureSpriteKind.BoardingSchoolCourtyardOak);
        ExpectEqual(StructureArtCatalog.BoardingSchoolTreesAtlasPath, courtyardOakStructure.AtlasPath, "structure catalog brings in the boarding school trees atlas");
        ExpectEqual(new Rect2(0f, 0f, 256f, 320f), courtyardOakStructure.AtlasRegion, "structure catalog maps trees atlas entries");
        var grassFlowers = StructureArtCatalog.Get(StructureSpriteKind.BoardingSchoolGrassFlowersA);
        ExpectEqual(StructureArtCatalog.BoardingSchoolGrassAtlasPath, grassFlowers.AtlasPath, "structure catalog reuses flower details from the grass tileset");
        ExpectEqual(new Rect2(0f, 0f, 32f, 32f), grassFlowers.AtlasRegion, "structure catalog maps flower details from grass tile regions");
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
        ExpectEqual(10, generatedA.Npcs.Count, "prototype target players generate focused small-world NPC population");
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
        ExpectEqual("Paragon 2", state.LocalKarma.TierName, "high positive karma gains infinite Paragon rank");
        ExpectEqual("Progress: 0/100 toward Paragon 3", state.LocalKarma.RankProgress.Summary, "high positive karma shows progress toward next Paragon rank");
        ExpectTrue(state.LocalPerks.Any(perk => perk.Name == "Paragon 2"), "repeat Paragon ranks unlock repeat ascension perks");
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
        ExpectTrue(state.Factions.GetReputation(StarterFactions.FreeSettlersId, GameState.LocalPlayerId) > 0, "helping Mara improves Village Freeholders faction reputation");
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
        ExpectTrue(state.DamagePlayer(GameState.LocalPlayerId, "peer_stand_in", 100, "test lethal strike"), "lethal damage downs the player");
        ExpectTrue(state.Players["peer_stand_in"].IsDown, "lethal damage marks player as downed");
        state.TriggerKarmaBreak("peer_stand_in");
        ExpectEqual(100, state.Players["peer_stand_in"].Health, "Karma Break restores downed player's health");
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
        ExpectEqual("Renegade 2", state.LocalKarma.TierName, "low negative karma gains infinite Renegade rank");
        ExpectEqual("Progress: 54/100 toward Renegade 3", state.LocalKarma.RankProgress.Summary, "low negative karma shows progress toward next Renegade rank");
        ExpectTrue(state.LocalPerks.Any(perk => perk.Name == "Renegade 2"), "repeat Renegade ranks unlock repeat descension perks");
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
        state.LocalPlayer.ResetCleanliness();
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
        var shopPurchasePrice = int.Parse(shopPurchase.Event.Data["price"]);
        ExpectEqual(localScripBeforePurchase - shopPurchasePrice, state.LocalScrip, "shop purchase debits scrip");
        ExpectTrue(state.HasItem(GameState.LocalPlayerId, StarterItems.WhoopieCushionId), "shop purchase adds item to inventory");
        ExpectTrue(shopPurchase.Event.EventId.Contains("item_purchased"), "shop purchase emits server event");
        ExpectEqual("item_purchased", shopPurchase.Event.Data["audioCue"], "shop purchase event carries audio cue");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { shopPurchase.Event }).Contains("bought Whoopie Cushion"),
            "HUD formats shop purchases");
        var shopSnapshot = transferServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectTrue(
            shopSnapshot.ShopOffers.Any(offer => offer.OfferId == StarterShopCatalog.DallenWhoopieCushionOfferId && offer.Price == shopPurchasePrice),
            "interest snapshot includes nearby shop offers");
        ExpectFalse(transferServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            8,
            IntentType.PurchaseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["offerId"] = StarterShopCatalog.DallenWorkVestOfferId
            })).WasAccepted, "server rejects shop purchase without enough scrip");
        state.LocalPlayer.ResetCleanliness();
        if (state.LocalKarma.Score < 20)
        {
            state.LocalPlayer.ApplyKarma(20 - state.LocalKarma.Score);
        }
        var dallenOpinion = state.Relationships.GetOpinion(StarterNpcs.Dallen.Id, GameState.LocalPlayerId);
        if (dallenOpinion != 0)
        {
            state.Relationships.Apply(StarterNpcs.Dallen.Id, GameState.LocalPlayerId, -dallenOpinion);
        }
        state.AddScrip(GameState.LocalPlayerId, 20);
        var discountedShopSnapshot = transferServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        var discountedRepairKitOffer = discountedShopSnapshot.ShopOffers.First(offer => offer.OfferId == StarterShopCatalog.DallenRepairKitOfferId);
        ExpectTrue(
            discountedRepairKitOffer.Price < StarterShopCatalog.Offers.First(offer => offer.Id == StarterShopCatalog.DallenRepairKitOfferId).Price,
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
        ExpectEqual(localScripBeforeDiscountPurchase - discountedRepairKitOffer.Price, state.LocalScrip, "discounted shop purchase debits final price");
        ExpectEqual("18", discountedPurchase.Event.Data["basePrice"], "discounted purchase event reports base price");
        ExpectEqual(discountedRepairKitOffer.Price.ToString(), discountedPurchase.Event.Data["price"], "discounted purchase event reports final price");
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
        ExpectTrue(server.EventLog.Count >= 2, "server records accepted move and karma intent events");
        var interestSnapshot = server.CreateInterestSnapshot(GameState.LocalPlayerId);
        ExpectEqual(2, interestSnapshot.Players.Count, "interest snapshot includes self and nearby players");
        ExpectEqual(MatchStatus.Lobby, interestSnapshot.Match.Status, "interest snapshot includes match status (Lobby before ready-up)");
        ExpectFalse(interestSnapshot.SyncHint.IsDelta, "full interest snapshot reports non-delta sync hint");
        ExpectEqual(0L, interestSnapshot.SyncHint.AfterTick, "full interest snapshot records zero after-tick");
        ExpectTrue(interestSnapshot.SyncHint.ServerEventCount >= 2, "interest snapshot sync hint counts visible server events");
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
        ExpectTrue(ServerStructureObject.FormatStructurePrompt("Press E to inspect Greenhouse.").Contains("L - Enter"), "structure prompt advertises building entry controls");
        var structureEnter = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            4,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "structure_greenhouse_standard",
                ["action"] = "enter"
            }));
        ExpectTrue(structureEnter.WasAccepted, "server accepts structure entry placeholder");
        ExpectEqual("inside", structureEnter.Event.Data["entryState"], "structure entry event reports inside state");
        ExpectTrue(server.CreateInterestSnapshot(GameState.LocalPlayerId).Players.First(player => player.Id == GameState.LocalPlayerId).StatusEffects.Contains("Inside: Greenhouse"), "structure entry appears as player status effect");
        var structureExit = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            5,
            IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "structure_greenhouse_standard",
                ["action"] = "exit"
            }));
        ExpectTrue(structureExit.WasAccepted, "server accepts structure exit placeholder");
        ExpectEqual("outside", structureExit.Event.Data["entryState"], "structure exit event reports outside state");
        ExpectFalse(server.CreateInterestSnapshot(GameState.LocalPlayerId).Players.First(player => player.Id == GameState.LocalPlayerId).StatusEffects.Contains("Inside: Greenhouse"), "structure exit clears inside status effect");
        ExpectFalse(server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            6,
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
            7,
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
            8,
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
            9,
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
            10,
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
            11,
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
            12,
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = StarterItems.PracticeStickId
            }));
        ExpectTrue(serverEquip.WasAccepted, "server accepts equippable item intent");
        ExpectEqual(10, state.LocalPlayer.AttackPower, "server item intent equips weapon");
        ExpectTrue(serverEquip.Event.EventId.Contains("item_equipped"), "server item intent emits equipment event");
        ExpectEqual(
            Karma.Audio.AudioEventCatalog.EquipmentCueIdForItem(StarterItems.PracticeStick),
            serverEquip.Event.Data["audioCue"],
            "server equipment events include an item-specific audio cue");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { serverEquip.Event }).Contains("equipped Practice Stick"),
            "HUD formats equipment events");
        ExpectFalse(server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            13,
            IntentType.UseItem,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["itemId"] = "missing_item"
            })).WasAccepted, "server rejects unknown item intent");

        var peerHealthBeforeAttack = state.Players["peer_stand_in"].Health;
        var serverAttack = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            14,
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
        ExpectFalse(distantFilteredSnapshot.ServerEvents.Any(serverEvent => serverEvent.Data.GetValueOrDefault("playerId") == "rival_renegade"), "interest snapshot hides distant server events");
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
        ExpectTrue(deltaInterestSnapshot.ServerEvents.Count >= 15, "interest snapshot can return visible events after a tick");
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
            15,
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
        ExpectEqual(StarterNpcs.Mara.Name, startDialogue.Event.Data["npcName"], "dialogue start event includes NPC name for TTS");
        ExpectTrue(startDialogue.Event.Data["npcPrompt"].Contains(StarterNpcs.Mara.Need), "dialogue start event includes speakable NPC prompt");
        ExpectEqual("npc_dialogue_prompt", startDialogue.Event.Data["ttsType"], "dialogue start event marks TTS payload type");
        var karmaBeforeDialogueChoice = state.LocalKarma.Score;
        var selectDialogueChoice = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            16,
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
        ExpectTrue(selectDialogueChoice.Event.Data["npcResponse"].Length > 0, "dialogue choice event includes NPC spoken response");
        ExpectEqual("npc_dialogue_response", selectDialogueChoice.Event.Data["ttsType"], "dialogue choice event marks TTS payload type");
        ExpectTrue(
            NpcTextToSpeechController.BuildUtterance("Mara Venn", "  Needs   filters.\nNow.  ") == "Needs filters. Now.",
            "NPC TTS utterance formatting collapses whitespace without prefixing speaker");
        ExpectFalse(server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            17,
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
        ExpectTrue(snapshot.Players.All(player => player.Appearance == PlayerAppearanceSelection.Default), "snapshot captures default player appearance selections");
        var serverSetAppearance = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            1000,
            IntentType.SetAppearance,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["skinLayerId"] = "skin_light_32x64"
            }));
        ExpectTrue(serverSetAppearance.WasAccepted, "server accepts player appearance selection intents");
        ExpectEqual("skin_light_32x64", state.LocalPlayer.Appearance.SkinLayerId, "server appearance intent updates authoritative player state");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { serverSetAppearance.Event }).Contains("Light skin"),
            "HUD formats player appearance change events");
        ExpectEqual("skin_light_32x64", server.CreateInterestSnapshot(GameState.LocalPlayerId).Players.Single(player => player.Id == GameState.LocalPlayerId).Appearance.SkinLayerId, "interest snapshots expose updated player appearance selections");
        var serverRejectsInvalidAppearance = server.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            1001,
            IntentType.SetAppearance,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["skinLayerId"] = "skin_not_real"
            }));
        ExpectFalse(serverRejectsInvalidAppearance.WasAccepted, "server rejects unknown player appearance layers");
        ExpectEqual("skin_deep_32x64", PlayerController.CycleSkinLayerId("skin_medium_32x64"), "player controller cycles skin appearance layers");
        ExpectEqual("hair_short_blond_32x64", PlayerController.CycleHairLayerId("hair_short_dark_32x64"), "player controller cycles hair appearance layers");
        ExpectEqual("outfit_settler_32x64", PlayerController.CycleOutfitLayerId("outfit_engineer_32x64"), "player controller cycles outfit appearance layers");
        var customAppearancePlayer = state.RegisterPlayer("appearance-test", "Appearance Tester");
        customAppearancePlayer.SetAppearance(PlayerAppearanceSelection.Default with { SkinLayerId = "skin_deep_32x64" });
        var customAppearanceSnapshot = SnapshotBuilder.PlayersFrom(
            new[] { customAppearancePlayer },
            new LeaderboardStanding(string.Empty, string.Empty, 0, string.Empty, string.Empty, 0)).Single();
        ExpectEqual("skin_deep_32x64", customAppearanceSnapshot.Appearance.SkinLayerId, "snapshot captures custom player skin layer selection");
        ExpectTrue(snapshot.Summary.Contains("players"), "snapshot has readable summary");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"leaderboard\""), "snapshot JSON includes leaderboard");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"standing\""), "snapshot JSON includes player standing");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"tileX\""), "snapshot JSON includes tile position");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"inventory\""), "snapshot JSON includes per-player inventory");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"scrip\""), "snapshot JSON includes player scrip");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"statusEffects\""), "snapshot JSON includes player status effects");
        ExpectTrue(SnapshotJson.Write(snapshot).Contains("\"appearance\""), "snapshot JSON includes player appearance selection");
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

        var multiStepQuestState = new GameState();
        multiStepQuestState.RegisterPlayer(GameState.LocalPlayerId, "Quest Tester");
        multiStepQuestState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        var stepA = new QuestStep(
            "find_tool",
            "Pick up a repair kit.",
            new QuestStepCondition(QuestStepConditionKind.HoldItem, StarterItems.RepairKitId),
            new[] { "helpful" },
            ScripReward: 2);
        var stepB = new QuestStep(
            "report_to_mara",
            "Report back to Mara.",
            new QuestStepCondition(QuestStepConditionKind.NearNpc, StarterNpcs.Mara.Id),
            new[] { "generous" },
            ScripReward: 0);
        var multiStepDef = new QuestDefinition(
            "multi_step_smoke_test",
            "Two-Step Test",
            StarterNpcs.Mara.Id,
            "A two-step smoke test quest.",
            System.Array.Empty<string>(),
            PrototypeActions.HelpMaraId,
            ScripReward: 10,
            Steps: new[] { stepA, stepB });
        multiStepQuestState.Quests.AddDefinition(multiStepDef);
        var multiStepServer = new AuthoritativeWorldServer(multiStepQuestState, "multi-step-quest-test");
        ExpectEqual(2, multiStepQuestState.Quests.Get("multi_step_smoke_test").Definition.Steps.Count, "multi-step quest definition exposes step list");
        ExpectTrue(multiStepQuestState.Quests.Get("multi_step_smoke_test").IsMultiStep, "multi-step quest reports IsMultiStep");
        ExpectFalse(multiStepQuestState.Quests.Get("multi_step_smoke_test").AllStepsDone, "incomplete multi-step quest reports steps remaining");
        var multiStepSnapshot = multiStepServer.CreateInterestSnapshot(GameState.LocalPlayerId);
        var multiStepQuestSnap = multiStepSnapshot.Quests.FirstOrDefault(q => q.Id == "multi_step_smoke_test");
        ExpectTrue(multiStepQuestSnap is not null, "multi-step quest appears in interest snapshot");
        ExpectEqual(2, multiStepQuestSnap?.TotalSteps ?? -1, "interest snapshot exposes total step count");
        ExpectEqual(0, multiStepQuestSnap?.CurrentStep ?? -1, "interest snapshot exposes current step index");
        ExpectTrue(multiStepQuestSnap?.CurrentStepDescription.Contains("repair kit") == true, "interest snapshot exposes current step description");
        var startMultiStepQuest = multiStepServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            100,
            IntentType.StartQuest,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "multi_step_smoke_test" }));
        ExpectTrue(startMultiStepQuest.WasAccepted, "server accepts multi-step quest start near giver");
        var earlyComplete = multiStepServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            101,
            IntentType.CompleteQuest,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "multi_step_smoke_test" }));
        ExpectFalse(earlyComplete.WasAccepted, "server rejects CompleteQuest when multi-step quest has unfinished steps");
        var advanceWithoutItem = multiStepServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            102,
            IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "multi_step_smoke_test" }));
        ExpectFalse(advanceWithoutItem.WasAccepted, "server rejects AdvanceQuestStep when HoldItem condition is not met");
        multiStepQuestState.AddItem(GameState.LocalPlayerId, StarterItems.RepairKit);
        var advanceStepA = multiStepServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            103,
            IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "multi_step_smoke_test" }));
        ExpectTrue(advanceStepA.WasAccepted, "server accepts AdvanceQuestStep when HoldItem condition is met");
        ExpectEqual("find_tool", advanceStepA.Event.Data["stepId"], "step advance event records the completed step id");
        ExpectEqual(1, multiStepQuestState.Quests.Get("multi_step_smoke_test").CurrentStepIndex, "step index advances after accepted step");
        ExpectFalse(multiStepQuestState.Quests.Get("multi_step_smoke_test").AllStepsDone, "quest still has remaining steps after step A");
        var advanceStepB = multiStepServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            104,
            IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "multi_step_smoke_test" }));
        ExpectTrue(advanceStepB.WasAccepted, "server accepts AdvanceQuestStep for NearNpc condition when player is at origin near Mara");
        ExpectEqual("True", advanceStepB.Event.Data["allStepsDone"], "step advance event signals when all steps are complete");
        ExpectTrue(multiStepQuestState.Quests.Get("multi_step_smoke_test").AllStepsDone, "quest reports AllStepsDone after final step");
        var completeMultiStepQuest = multiStepServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId,
            105,
            IntentType.CompleteQuest,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "multi_step_smoke_test" }));
        ExpectTrue(completeMultiStepQuest.WasAccepted, "server accepts CompleteQuest after all multi-step quest steps are done");
        ExpectEqual(QuestStatus.Completed, multiStepQuestState.Quests.Get("multi_step_smoke_test").Status, "multi-step quest marks Completed after successful turn-in");

        var repairMissionDef = QuestModuleRegistry.Repair.CreateQuest(new QuestCreationContext(
            "repair_smoke_test", "workshop_smoke_location", "Fix the Workshop",
            "workshop", StarterNpcs.Mara.Id, 20, Array.Empty<QuestPlacementInfo>()));
        ExpectEqual(3, repairMissionDef.Steps.Count, "repair mission quest has three steps");
        ExpectEqual(QuestStepConditionKind.NearStructureCategory, repairMissionDef.Steps[0].Condition.Kind, "repair mission step 1 requires nearness to structure category");
        ExpectEqual("workshop", repairMissionDef.Steps[0].Condition.TargetId, "repair mission step 1 targets the correct structure role");
        ExpectEqual(QuestStepConditionKind.HoldRepairTool, repairMissionDef.Steps[1].Condition.Kind, "repair mission step 2 requires holding a repair tool");
        ExpectEqual(QuestStepConditionKind.NearStructureCategory, repairMissionDef.Steps[2].Condition.Kind, "repair mission step 3 requires returning to structure");
        var repairState = new GameState();
        repairState.RegisterPlayer(GameState.LocalPlayerId, "Repair Tester");
        repairState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        repairState.Quests.AddDefinition(repairMissionDef);
        var repairServer = new AuthoritativeWorldServer(repairState, "repair-mission-test");
        repairServer.SeedWorldStructure(
            "workshop_smoke_fixture",
            "Smoke Workshop",
            "workshop",
            TilePosition.Origin,
            integrity: 60);
        var startRepairMission = repairServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 1, IntentType.StartQuest,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "repair_smoke_test" }));
        ExpectTrue(startRepairMission.WasAccepted, "server accepts repair mission quest start");
        var repairStep1 = repairServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 2, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "repair_smoke_test" }));
        ExpectTrue(repairStep1.WasAccepted, "repair mission step 1 passes when player is near the workshop structure");
        var repairStep2NoTool = repairServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 3, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "repair_smoke_test" }));
        ExpectFalse(repairStep2NoTool.WasAccepted, "repair mission step 2 is rejected without a repair tool");
        repairState.AddItem(GameState.LocalPlayerId, StarterItems.MultiTool);
        var repairStep2 = repairServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 4, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "repair_smoke_test" }));
        ExpectTrue(repairStep2.WasAccepted, "repair mission step 2 passes when player holds a repair tool");
        var repairStep3 = repairServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 5, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "repair_smoke_test" }));
        ExpectTrue(repairStep3.WasAccepted, "repair mission step 3 passes when player returns to workshop structure");
        var completeRepairMission = repairServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 6, IntentType.CompleteQuest,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "repair_smoke_test" }));
        ExpectTrue(completeRepairMission.WasAccepted, "server accepts repair mission completion after all steps done");
        ExpectTrue(
            generatedA.Quests.Any(quest => quest.Steps is { Count: 3 } && quest.Steps[1].Condition.Kind == QuestStepConditionKind.HoldRepairTool),
            "world generator produces repair mission quests for workshop/clinic stations");

        var deliveryDef = QuestModuleRegistry.Delivery.CreateQuest(new QuestCreationContext(
            "delivery_smoke_test", "market_smoke_location", "Supply Run",
            "market", StarterNpcs.Mara.Id, 18, Array.Empty<QuestPlacementInfo>()));
        ExpectEqual(3, deliveryDef.Steps.Count, "delivery quest has three steps");
        ExpectEqual(QuestStepConditionKind.NearStructureCategory, deliveryDef.Steps[0].Condition.Kind, "delivery step 1 requires nearness to source station");
        ExpectEqual("market", deliveryDef.Steps[0].Condition.TargetId, "delivery step 1 targets source role");
        ExpectEqual(QuestStepConditionKind.HoldItem, deliveryDef.Steps[1].Condition.Kind, "delivery step 2 requires holding the delivery item");
        ExpectEqual(StarterItems.FilterCoreId, deliveryDef.Steps[1].Condition.TargetId, "delivery step 2 checks for the correct item");
        ExpectEqual(QuestStepConditionKind.NearStructureCategory, deliveryDef.Steps[2].Condition.Kind, "delivery step 3 requires nearness to destination station");
        ExpectEqual("clinic", deliveryDef.Steps[2].Condition.TargetId, "delivery step 3 targets destination role");
        ExpectTrue(deliveryDef.RequiredItemIds.Contains(StarterItems.FilterCoreId), "delivery quest consumes the delivery item on completion");
        var deliveryState = new GameState();
        deliveryState.RegisterPlayer(GameState.LocalPlayerId, "Delivery Tester");
        deliveryState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        deliveryState.Quests.AddDefinition(deliveryDef);
        var deliveryServer = new AuthoritativeWorldServer(deliveryState, "delivery-quest-test");
        deliveryServer.SeedWorldStructure("market_smoke", "Smoke Market", "market", TilePosition.Origin);
        deliveryServer.SeedWorldStructure("clinic_smoke", "Smoke Clinic", "clinic", TilePosition.Origin);
        var startDelivery = deliveryServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 1, IntentType.StartQuest,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "delivery_smoke_test" }));
        ExpectTrue(startDelivery.WasAccepted, "server accepts delivery quest start near giver");
        var deliveryStep1 = deliveryServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 2, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "delivery_smoke_test" }));
        ExpectTrue(deliveryStep1.WasAccepted, "delivery step 1 passes when player is near source station");
        var deliveryStep2NoItem = deliveryServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 3, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "delivery_smoke_test" }));
        ExpectFalse(deliveryStep2NoItem.WasAccepted, "delivery step 2 is rejected without the delivery item");
        deliveryState.AddItem(GameState.LocalPlayerId, StarterItems.FilterCore);
        var deliveryStep2 = deliveryServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 4, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "delivery_smoke_test" }));
        ExpectTrue(deliveryStep2.WasAccepted, "delivery step 2 passes when player holds the delivery item");
        var deliveryStep3 = deliveryServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 5, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "delivery_smoke_test" }));
        ExpectTrue(deliveryStep3.WasAccepted, "delivery step 3 passes when player is near destination station");
        var completeDelivery = deliveryServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 6, IntentType.CompleteQuest,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "delivery_smoke_test" }));
        ExpectTrue(completeDelivery.WasAccepted, "server accepts delivery quest completion after all steps done and item consumed");
        ExpectFalse(deliveryState.HasItem(GameState.LocalPlayerId, StarterItems.FilterCoreId), "delivery quest completion consumes the delivery item");
        ExpectTrue(
            generatedA.Quests.Any(quest => quest.Steps is { Count: 3 } && quest.Steps[2].Condition.TargetId == "clinic"),
            "world generator produces delivery quests for market stations targeting clinic");

        // ── Step 4: Rumor Quest ──────────────────────────────────────────────
        var rumorDef = QuestModuleRegistry.Rumor.CreateQuest(new QuestCreationContext(
            "rumor_smoke_test", "notice_board_smoke", "The Hidden Ledger",
            "notice-board", StarterNpcs.Mara.Id, 16,
            new[] { new QuestPlacementInfo(StarterNpcs.Dallen.Id, "dallen_location") }));
        ExpectEqual(2, rumorDef.Steps.Count, "rumor quest has two steps");
        ExpectEqual(QuestStepConditionKind.NearStructureCategory, rumorDef.Steps[0].Condition.Kind, "rumor quest step 1 requires nearness to notice-board structure");
        ExpectEqual("notice-board", rumorDef.Steps[0].Condition.TargetId, "rumor quest step 1 targets notice-board category");
        ExpectEqual(QuestStepConditionKind.NearNpc, rumorDef.Steps[1].Condition.Kind, "rumor quest step 2 requires finding the target NPC");
        ExpectEqual(StarterNpcs.Dallen.Id, rumorDef.Steps[1].Condition.TargetId, "rumor quest step 2 targets the named subject");
        ExpectTrue(rumorDef.CompletionActionId.StartsWith("rumor_resolve:"), "rumor quest uses rumor_resolve completion action");

        var rumorState = new GameState();
        rumorState.RegisterPlayer(GameState.LocalPlayerId, "Rumor Tester");
        rumorState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        rumorState.Quests.AddDefinition(rumorDef);
        var rumorServer = new AuthoritativeWorldServer(rumorState, "rumor-quest-test");
        rumorServer.SeedWorldStructure("noticeboard_smoke_fixture", "Smoke Notice Board", "notice-board", TilePosition.Origin);

        var startRumorQuest = rumorServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 1, IntentType.StartQuest,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "rumor_smoke_test" }));
        ExpectTrue(startRumorQuest.WasAccepted, "server accepts rumor quest start near giver");

        var rumorStep1 = rumorServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 2, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "rumor_smoke_test" }));
        ExpectTrue(rumorStep1.WasAccepted, "rumor quest step 1 passes when player is near notice-board structure");

        rumorState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(50, 50));
        var rumorStep2NoNpc = rumorServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 3, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "rumor_smoke_test" }));
        ExpectFalse(rumorStep2NoNpc.WasAccepted, "rumor quest step 2 is rejected when target NPC is not nearby");

        rumorState.SetPlayerPosition(GameState.LocalPlayerId, rumorServer.GetNpcPosition(StarterNpcs.Dallen.Id));
        var rumorStep2 = rumorServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 4, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "rumor_smoke_test" }));
        ExpectTrue(rumorStep2.WasAccepted, "rumor quest step 2 passes when player is near the target NPC");

        rumorState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        var karmaBeforeExpose = rumorState.Players[GameState.LocalPlayerId].Karma.Score;
        var exposeRumor = rumorServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 5, IntentType.CompleteQuest,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["questId"] = "rumor_smoke_test",
                ["choice"] = "expose"
            }));
        ExpectTrue(exposeRumor.WasAccepted, "server accepts rumor quest completion with expose choice");
        ExpectEqual("expose", exposeRumor.Event.Data["rumorChoice"], "quest completed event records expose choice");
        ExpectTrue(rumorState.Players[GameState.LocalPlayerId].Karma.Score > karmaBeforeExpose, "expose choice ascends player karma");

        var rumorState2 = new GameState();
        rumorState2.RegisterPlayer(GameState.LocalPlayerId, "Rumor Tester 2");
        rumorState2.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        var rumorDef2 = QuestModuleRegistry.Rumor.CreateQuest(new QuestCreationContext(
            "rumor_bury_test", "notice_board_smoke2", "Hidden Ledger Bury",
            "notice-board", StarterNpcs.Mara.Id, 16,
            new[] { new QuestPlacementInfo(StarterNpcs.Dallen.Id, "dallen_location2") }));
        rumorState2.Quests.AddDefinition(rumorDef2);
        var rumorServer2 = new AuthoritativeWorldServer(rumorState2, "rumor-bury-test");
        rumorServer2.SeedWorldStructure("noticeboard_smoke_fixture2", "Smoke Notice Board 2", "notice-board", TilePosition.Origin);
        rumorServer2.ProcessIntent(new ServerIntent(GameState.LocalPlayerId, 1, IntentType.StartQuest,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "rumor_bury_test" }));
        rumorServer2.ProcessIntent(new ServerIntent(GameState.LocalPlayerId, 2, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "rumor_bury_test" }));
        rumorState2.SetPlayerPosition(GameState.LocalPlayerId, rumorServer2.GetNpcPosition(StarterNpcs.Dallen.Id));
        rumorServer2.ProcessIntent(new ServerIntent(GameState.LocalPlayerId, 3, IntentType.AdvanceQuestStep,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "rumor_bury_test" }));
        rumorState2.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        var karmaBeforeBury = rumorState2.Players[GameState.LocalPlayerId].Karma.Score;
        var buryRumor = rumorServer2.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 4, IntentType.CompleteQuest,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["questId"] = "rumor_bury_test",
                ["choice"] = "bury"
            }));
        ExpectTrue(buryRumor.WasAccepted, "server accepts rumor quest completion with bury choice");
        ExpectEqual("bury", buryRumor.Event.Data["rumorChoice"], "quest completed event records bury choice");
        ExpectTrue(rumorState2.Players[GameState.LocalPlayerId].Karma.Score > karmaBeforeBury, "bury choice ascends player karma");
        ExpectTrue(rumorState2.Players[GameState.LocalPlayerId].Karma.Score >= rumorState.Players[GameState.LocalPlayerId].Karma.Score,
            "bury choice grants at least as much karma as expose (mercy > boldness)");

        ExpectTrue(
            generatedA.Quests.Any(quest => quest.CompletionActionId.StartsWith("rumor_resolve:") && quest.Steps?.Count == 2),
            "world generator produces rumor quests for notice-board stations");

        // ── Step 5: Paragon Favor perk ────────────────────────────────────────────
        var paragonState = new GameState();
        paragonState.RegisterPlayer(GameState.LocalPlayerId, "Paragon Tester");
        paragonState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);

        for (var pi = 0; pi < 3; pi++)
        {
            paragonState.ApplyShift(GameState.LocalPlayerId,
                new KarmaAction(GameState.LocalPlayerId, "none", new[] { "protective" }, "perk build", 4));
        }

        var paragonKarma = paragonState.Players[GameState.LocalPlayerId].Karma.Score;
        ExpectTrue(paragonKarma >= 50, $"paragon test state reaches karma >= 50 (got {paragonKarma})");

        var paragonPerks = PerkCatalog.GetForPlayer(
            paragonState.Players[GameState.LocalPlayerId],
            paragonState.GetLeaderboardStanding());
        ExpectTrue(paragonPerks.Any(p => p.Id == PerkCatalog.ExaltedFavorId),
            "Paragon Favor perk activates at karma >= 50");

        var paragonDiscountPct = ShopPricing.CalculateDiscountPercent(
            paragonState.Players[GameState.LocalPlayerId],
            paragonState.GetLeaderboardStanding());
        ExpectEqual(ShopPricing.ExaltedFavorDiscountPercent, paragonDiscountPct,
            "Paragon Favor grants 25% shop discount");

        var testOffer = StarterShopCatalog.Offers.First(o => o.VendorNpcId == StarterNpcs.Dallen.Id);
        var paragonShopPrice = ShopPricing.CalculatePrice(
            testOffer,
            paragonState.Players[GameState.LocalPlayerId],
            paragonState.GetLeaderboardStanding());
        ExpectTrue(paragonShopPrice < testOffer.Price, "Paragon Favor reduces shop purchase price");
        ExpectEqual(-10, ShopPricing.CalculateRelationshipModifierPercent(25),
            "friendly vendor relationship grants a shop discount modifier");
        ExpectEqual(25, ShopPricing.CalculateRelationshipModifierPercent(-55),
            "hostile vendor relationship applies a shop price surcharge");
        ExpectEqual(18, ShopPricing.ApplySignedModifier(20, -10),
            "signed shop modifier applies friendly discounts");
        ExpectEqual(25, ShopPricing.ApplySignedModifier(20, 25),
            "signed shop modifier applies hostile surcharges");
        ExpectTrue(
            HudController.FormatShopPricingTooltip(new ShopOfferSnapshot(
                "tooltip_friendly",
                StarterNpcs.Dallen.Id,
                StarterItems.RepairKitId,
                "Repair Kit",
                ItemCategory.Tool,
                18,
                "scrip",
                BasePrice: 20,
                PricingBreakdown: "Garrick: -10% (Friendly). Net price: 18 scrip (base 20)."))
                .Contains("-10% (Friendly)"),
            "shop pricing tooltip formatter surfaces friendly relationship discounts");
        ExpectTrue(
            HudController.FormatShopPricingTooltip(new ShopOfferSnapshot(
                "tooltip_hostile",
                StarterNpcs.Dallen.Id,
                StarterItems.RepairKitId,
                "Repair Kit",
                ItemCategory.Tool,
                25,
                "scrip",
                BasePrice: 20,
                PricingBreakdown: "Garrick: +25% (Hostile). Net price: 25 scrip (base 20)."))
                .Contains("+25% (Hostile)"),
            "shop pricing tooltip formatter surfaces hostile relationship surcharges");
        ExpectTrue(
            HudController.FormatShopPricingTooltip(new ShopOfferSnapshot(
                "tooltip_base",
                StarterNpcs.Dallen.Id,
                StarterItems.RepairKitId,
                "Repair Kit",
                ItemCategory.Tool,
                20,
                "scrip",
                BasePrice: 20))
                .Contains("Net price: 20 scrip"),
            "shop pricing tooltip formatter falls back to base and net prices");

        var paragonFlatQuestDef = new QuestDefinition(
            "paragon_bonus_test",
            "Paragon Test Quest",
            StarterNpcs.Mara.Id,
            "A simple quest to verify Paragon bonus scrip.",
            System.Array.Empty<string>(),
            Core.PrototypeActions.HelpMaraId,
            ScripReward: 10);
        paragonState.Quests.AddDefinition(paragonFlatQuestDef);
        var paragonServer = new AuthoritativeWorldServer(paragonState, "paragon-favor-test");
        paragonServer.ProcessIntent(new ServerIntent(GameState.LocalPlayerId, 1, IntentType.StartQuest,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "paragon_bonus_test" }));

        var scripBeforeParagonQuest = paragonState.Players[GameState.LocalPlayerId].Scrip;
        var completeParagonQuest = paragonServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 2, IntentType.CompleteQuest,
            new System.Collections.Generic.Dictionary<string, string> { ["questId"] = "paragon_bonus_test" }));
        ExpectTrue(completeParagonQuest.WasAccepted, "Paragon player can complete a quest");
        var paragonQuestBonus = int.Parse(completeParagonQuest.Event.Data["paragonQuestBonus"]);
        ExpectTrue(paragonQuestBonus > 0, "quest completion event records a positive Paragon scrip bonus");
        ExpectEqual(Math.Max(1, 10 / 5), paragonQuestBonus,
            "Paragon quest bonus is 20% of the base reward (minimum 1)");
        ExpectTrue(paragonState.Players[GameState.LocalPlayerId].Scrip >= scripBeforeParagonQuest + 10 + paragonQuestBonus,
            "Paragon player's scrip includes both base reward and Paragon bonus");

        var scripBeforeParagonDialogue = paragonState.Players[GameState.LocalPlayerId].Scrip;
        var paragonDialogue = paragonServer.ProcessIntent(new ServerIntent(
            GameState.LocalPlayerId, 3, IntentType.SelectDialogueChoice,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Mara.Id,
                ["choiceId"] = "help_filters"
            }));
        ExpectTrue(paragonDialogue.WasAccepted, "Paragon player can select a helpful NPC dialogue choice");
        ExpectEqual("1", paragonDialogue.Event.Data["paragonGift"],
            "helpful NPC dialogue choice grants +1 scrip gift to Paragon player");
        ExpectTrue(paragonState.Players[GameState.LocalPlayerId].Scrip > scripBeforeParagonDialogue,
            "Paragon player's scrip increases from NPC cooperation gift");

        // ── Step 6: Abyssal Mark perk ────────────────────────────────────────────
        var abyssalState = new GameState();
        abyssalState.RegisterPlayer(GameState.LocalPlayerId, "Abyssal Tester");
        abyssalState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);

        // betrayal(-8 karma) * BaseMagnitude 4 → Clamp(-32, -20, 20) = -20 per action; 5 × -20 = -100
        for (var ai = 0; ai < 5; ai++)
        {
            abyssalState.ApplyShift(GameState.LocalPlayerId,
                new KarmaAction(GameState.LocalPlayerId, "peer_stand_in", new[] { "betrayal" }, "abyssal build", 4));
        }

        var abyssalKarma = abyssalState.Players[GameState.LocalPlayerId].Karma.Score;
        ExpectTrue(abyssalKarma <= -100, $"abyssal test state reaches karma <= -100 (got {abyssalKarma})");

        var abyssalPerks = PerkCatalog.GetForPlayer(
            abyssalState.Players[GameState.LocalPlayerId],
            abyssalState.GetLeaderboardStanding());
        ExpectTrue(abyssalPerks.Any(p => p.Id == PerkCatalog.RenegadeMarkId),
            "Abyssal Mark perk activates at karma <= -100");

        var abyssalDiscountPct = ShopPricing.CalculateDiscountPercent(
            abyssalState.Players[GameState.LocalPlayerId],
            abyssalState.GetLeaderboardStanding());
        ExpectEqual(ShopPricing.RenegadeMarkDiscountPercent, abyssalDiscountPct,
            "Abyssal Mark grants 50% shop discount");

        var abyssalTestOffer = StarterShopCatalog.Offers.First(o => o.VendorNpcId == StarterNpcs.Dallen.Id);
        var abyssalShopPrice = ShopPricing.CalculatePrice(
            abyssalTestOffer,
            abyssalState.Players[GameState.LocalPlayerId],
            abyssalState.GetLeaderboardStanding());
        ExpectTrue(abyssalShopPrice < abyssalTestOffer.Price, "Abyssal Mark reduces shop purchase price");

        // MockMaraWithBalloon: harmful(-6) + humiliating(-5) + selfish(-4) = -15 base relationship delta
        // AbyssalMark 10%: ceil(-15 * 0.1) = ceil(-1.5) = -1
        var maraOpinionBeforeAbyssal = abyssalState.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId);
        abyssalState.ApplyShift(GameState.LocalPlayerId, PrototypeActions.MockMaraWithBalloon());
        var maraOpinionAfterAbyssal = abyssalState.Relationships.GetOpinion(StarterNpcs.Mara.Id, GameState.LocalPlayerId);
        ExpectTrue(maraOpinionAfterAbyssal > maraOpinionBeforeAbyssal - 15,
            "Abyssal Mark reduces NPC fear reaction damage (well below raw -15)");
        ExpectEqual(maraOpinionBeforeAbyssal - 1, maraOpinionAfterAbyssal,
            "Abyssal Mark reduces fear reaction to 10% of base damage");

        // ── Step 7: Posse formation ───────────────────────────────────────────────
        var posseState = new GameState();
        var posseServer = new AuthoritativeWorldServer(posseState, "posse-test");
        posseServer.JoinPlayer("alpha", "Alpha");
        posseServer.JoinPlayer("beta", "Beta");
        posseState.SetPlayerPosition("alpha", new TilePosition(10, 10));
        posseState.SetPlayerPosition("beta", new TilePosition(11, 10));

        // InvitePosse: alpha → beta
        var inviteResult = posseServer.ProcessIntent(new ServerIntent(
            "alpha", 1, IntentType.InvitePosse,
            new System.Collections.Generic.Dictionary<string, string> { ["targetPlayerId"] = "beta" }));
        ExpectTrue(inviteResult.WasAccepted, "InvitePosse is accepted");
        ExpectTrue(inviteResult.Event.EventId.Contains("posse_invite_sent"), "InvitePosse emits invite event");
        ExpectTrue(posseState.Players["alpha"].HasTeam, "inviter joins their own posse on invite");
        ExpectFalse(posseState.Players["beta"].HasTeam, "invitee is not yet in the posse before accepting");

        // Self-invite rejected
        var selfInvite = posseServer.ProcessIntent(new ServerIntent(
            "alpha", 2, IntentType.InvitePosse,
            new System.Collections.Generic.Dictionary<string, string> { ["targetPlayerId"] = "alpha" }));
        ExpectFalse(selfInvite.WasAccepted, "InvitePosse targeting self is rejected");

        // AcceptPosse: beta accepts
        var acceptResult = posseServer.ProcessIntent(new ServerIntent(
            "beta", 1, IntentType.AcceptPosse,
            new System.Collections.Generic.Dictionary<string, string>()));
        ExpectTrue(acceptResult.WasAccepted, "AcceptPosse is accepted");
        ExpectTrue(acceptResult.Event.EventId.Contains("posse_accepted"), "AcceptPosse emits accepted event");
        ExpectTrue(posseState.Players["beta"].HasTeam, "beta is in a posse after accepting");
        ExpectEqual(posseState.Players["alpha"].TeamId, posseState.Players["beta"].TeamId,
            "both players share the same posse after acceptance");
        var posseFormationSnapshot = posseServer.CreateInterestSnapshot("alpha");
        var alphaPosseSnapshot = posseFormationSnapshot.Players.First(player => player.Id == "alpha");
        var betaPosseSnapshot = posseFormationSnapshot.Players.First(player => player.Id == "beta");
        ExpectTrue(!string.IsNullOrWhiteSpace(alphaPosseSnapshot.PosseName), "posse name is generated on formation");
        ExpectEqual(alphaPosseSnapshot.PosseName, betaPosseSnapshot.PosseName,
            "posse name sticks across member snapshots");
        ExpectEqual("alpha", alphaPosseSnapshot.PosseLeaderId, "inviter becomes the initial posse leader");

        var leadershipTransfer = posseServer.ProcessIntent(new ServerIntent(
            "alpha", 3, IntentType.TransferPosseLeadership,
            new System.Collections.Generic.Dictionary<string, string> { ["targetPlayerId"] = "beta" }));
        ExpectTrue(leadershipTransfer.WasAccepted, "TransferPosseLeadership is accepted for the current leader");
        var transferredSnapshot = posseServer.CreateInterestSnapshot("beta");
        ExpectEqual("beta", transferredSnapshot.Players.First(player => player.Id == "beta").PosseLeaderId,
            "posse leadership transfer is reflected in snapshots");

        // AcceptPosse with no pending invite rejected
        var noInviteAccept = posseServer.ProcessIntent(new ServerIntent(
            "beta", 2, IntentType.AcceptPosse,
            new System.Collections.Generic.Dictionary<string, string>()));
        ExpectFalse(noInviteAccept.WasAccepted, "AcceptPosse without pending invite is rejected");

        // LeavePosse: beta leaves (alpha stays)
        var leaveResult = posseServer.ProcessIntent(new ServerIntent(
            "beta", 3, IntentType.LeavePosse,
            new System.Collections.Generic.Dictionary<string, string>()));
        ExpectTrue(leaveResult.WasAccepted, "LeavePosse is accepted");
        ExpectTrue(leaveResult.Event.EventId.Contains("posse_member_left"), "LeavePosse with remaining members emits member-left event");
        ExpectFalse(posseState.Players["beta"].HasTeam, "beta has no posse after leaving");
        ExpectTrue(posseState.Players["alpha"].HasTeam, "alpha still has a posse after beta leaves");

        // LeavePosse: alpha leaves (posse dissolves)
        var dissolveResult = posseServer.ProcessIntent(new ServerIntent(
            "alpha", 4, IntentType.LeavePosse,
            new System.Collections.Generic.Dictionary<string, string>()));
        ExpectTrue(dissolveResult.WasAccepted, "LeavePosse by last member is accepted");
        ExpectTrue(dissolveResult.Event.EventId.Contains("posse_disbanded"), "last member leaving emits disbanded event");
        ExpectFalse(posseState.Players["alpha"].HasTeam, "alpha has no posse after dissolving");

        // LeavePosse when not in a posse rejected
        var notInPosse = posseServer.ProcessIntent(new ServerIntent(
            "alpha", 5, IntentType.LeavePosse,
            new System.Collections.Generic.Dictionary<string, string>()));
        ExpectFalse(notInPosse.WasAccepted, "LeavePosse when not in a posse is rejected");

        // ── Step 8: Posse HUD panel ───────────────────────────────────────────────
        var hudProbePosse = new HudController();
        AddChild(hudProbePosse);
        ExpectTrue(hudProbePosse.GetNodeOrNull<PanelContainer>("HudRoot/PossePanel") is not null,
            "gameplay HUD includes a Posse panel node");

        var noPosse = new PlayerSnapshot("p1", "Alice", 25, "Trusted", 1, "prog",
            LeaderboardRole.None, 0, 0, 100, 100, 50,
            PlayerAppearanceSelection.Default,
            System.Array.Empty<string>(),
            new System.Collections.Generic.Dictionary<EquipmentSlot, string>(),
            System.Array.Empty<string>(),
            "");
        ExpectTrue(HudController.FormatPossePanel(new[] { noPosse }, "p1").Contains("not in a posse"),
            "FormatPossePanel shows not-in-posse message when PosseId is empty");

        var posseP1 = new PlayerSnapshot("p1", "Alice", 40, "Helpful", 1, "prog",
            LeaderboardRole.None, 0, 0, 90, 100, 60,
            PlayerAppearanceSelection.Default,
            System.Array.Empty<string>(),
            new System.Collections.Generic.Dictionary<EquipmentSlot, string>(),
            System.Array.Empty<string>(),
            "posse_p1",
            PosseName: "Amber Wolves",
            PosseLeaderId: "p1");
        var posseP2 = new PlayerSnapshot("p2", "Bob", -12, "Outlaw", 1, "prog",
            LeaderboardRole.None, 0, 0, 75, 100, 20,
            PlayerAppearanceSelection.Default,
            System.Array.Empty<string>(),
            new System.Collections.Generic.Dictionary<EquipmentSlot, string>(),
            System.Array.Empty<string>(),
            "posse_p1",
            PosseName: "Amber Wolves",
            PosseLeaderId: "p1");
        var possePanel = HudController.FormatPossePanel(new[] { posseP1, posseP2 }, "p1");
        ExpectTrue(possePanel.Contains("Amber Wolves"), "FormatPossePanel surfaces the posse name");
        ExpectTrue(possePanel.Contains("Alice"), "FormatPossePanel lists member Alice");
        ExpectTrue(possePanel.Contains("Bob"), "FormatPossePanel lists member Bob");
        ExpectTrue(possePanel.Contains("90/100"), "FormatPossePanel shows member health");
        ExpectTrue(possePanel.Contains("(you)"), "FormatPossePanel marks local player");
        ExpectTrue(possePanel.Contains("(leader)"), "FormatPossePanel marks the posse leader");
        ExpectTrue(possePanel.Contains("+40"), "FormatPossePanel shows member karma");

        var posseInviteEvent = new ServerEvent("world:1:posse_invite_sent", "world", 1,
            "Alpha invited Beta.", new System.Collections.Generic.Dictionary<string, string>
            { ["inviterId"] = "alpha", ["targetId"] = "beta" });
        ExpectTrue(HudController.FormatLatestServerEvent(new[] { posseInviteEvent }).Contains("invited"),
            "HUD formats posse_invite_sent event");

        var posseDisbandedEvent = new ServerEvent("world:2:posse_disbanded", "world", 2,
            "Posse dissolved.", new System.Collections.Generic.Dictionary<string, string>());
        ExpectTrue(HudController.FormatLatestServerEvent(new[] { posseDisbandedEvent }).Contains("disbanded"),
            "HUD formats posse_disbanded event");
        hudProbePosse.QueueFree();

        // ── Step 9: Saint/Scourge NPC behavior ───────────────────────────────────
        var saintState = new GameState();
        saintState.RegisterPlayer(GameState.LocalPlayerId, "Saint Tester");
        saintState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        // Reach Saint standing: positive karma, sole player
        saintState.ApplyShift(GameState.LocalPlayerId,
            new KarmaAction(GameState.LocalPlayerId, "peer_stand_in", new[] { "helpful" }, "saint build", 4));
        var saintServer = new AuthoritativeWorldServer(saintState, "saint-test");
        saintState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(3, 4));

        var saintDialogue = saintServer.GetDialogueFor(GameState.LocalPlayerId, StarterNpcs.Mara.Id);
        ExpectTrue(saintDialogue.Prompt.Contains("Saint"), "Saint player sees a special NPC greeting");
        ExpectTrue(saintDialogue.Choices.Any(c => c.Id == "saint_bless"),
            "Saint player receives saint_bless dialogue choice");

        var saintDiscount = ShopPricing.CalculateDiscountPercent(
            saintState.Players[GameState.LocalPlayerId],
            saintState.GetLeaderboardStanding());
        ExpectTrue(saintDiscount >= ShopPricing.SaintCommunityDiscountPercent,
            "Saint standing grants community discount");

        var scourgeState = new GameState();
        scourgeState.RegisterPlayer(GameState.LocalPlayerId, "Scourge Tester");
        scourgeState.SetPlayerPosition(GameState.LocalPlayerId, TilePosition.Origin);
        // Reach Scourge standing: negative karma, sole player
        scourgeState.ApplyShift(GameState.LocalPlayerId,
            new KarmaAction(GameState.LocalPlayerId, "peer_stand_in", new[] { "harmful" }, "scourge build", 4));
        var scourgeServer = new AuthoritativeWorldServer(scourgeState, "scourge-test");
        scourgeState.SetPlayerPosition(GameState.LocalPlayerId, new TilePosition(3, 4));

        var scourgeDialogue = scourgeServer.GetDialogueFor(GameState.LocalPlayerId, StarterNpcs.Mara.Id);
        ExpectTrue(scourgeDialogue.Prompt.Contains("Scourge"), "Scourge player sees a wary NPC greeting");
        ExpectTrue(scourgeDialogue.Choices.Any(c => c.Id == "scourge_tribute"),
            "Scourge player receives scourge_tribute dialogue choice");

        // ── Step 12: Combat heat tracking ────────────────────────────────────────
        var heatState = new GameState();
        heatState.RegisterPlayer("heat_attacker", "Attacker");
        heatState.RegisterPlayer("heat_victim", "Victim");
        heatState.SetPlayerPosition("heat_attacker", TilePosition.Origin);
        heatState.SetPlayerPosition("heat_victim", TilePosition.Origin);
        var heatServer = new AuthoritativeWorldServer(heatState, "heat-test");
        var heatChunk = heatServer.GetChunkForTile(TilePosition.Origin);
        ExpectFalse(heatServer.IsChunkHot(heatChunk.ChunkX, heatChunk.ChunkY),
            "chunk starts cool before any combat");
        ExpectEqual(0, heatServer.GetChunkHeat(heatChunk.ChunkX, heatChunk.ChunkY),
            "chunk heat starts at zero");

        heatServer.ProcessIntent(new ServerIntent(
            "heat_attacker", 1, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "heat_victim" }));
        ExpectTrue(heatServer.IsChunkHot(heatChunk.ChunkX, heatChunk.ChunkY),
            "chunk is hot after an attack");
        ExpectEqual(AuthoritativeWorldServer.CombatHeatPerAttack,
            heatServer.GetChunkHeat(heatChunk.ChunkX, heatChunk.ChunkY),
            "chunk heat equals CombatHeatPerAttack after one attack");

        heatServer.AdvanceIdleTicks(AuthoritativeWorldServer.CombatHeatPerAttack);
        ExpectFalse(heatServer.IsChunkHot(heatChunk.ChunkX, heatChunk.ChunkY),
            "chunk cools down after enough ticks pass");
        ExpectEqual(0, heatServer.GetChunkHeat(heatChunk.ChunkX, heatChunk.ChunkY),
            "chunk heat decays to zero after full decay period");

        // ── Step 13: Smarter respawn placement ───────────────────────────────────
        // Use IDs starting with 'a' so they sort before prototype players and are seeded as connected
        var respawnState = new GameState();
        respawnState.RegisterPlayer("aa_attacker", "Attacker");
        respawnState.RegisterPlayer("aa_victim", "Victim");
        respawnState.SetPlayerPosition("aa_attacker", TilePosition.Origin);
        respawnState.SetPlayerPosition("aa_victim", TilePosition.Origin);
        var respawnServer = new AuthoritativeWorldServer(respawnState, "respawn-test");

        // Three attacks (with cooldown intervals) kill the 100-HP victim (35 dmg each)
        respawnServer.ProcessIntent(new ServerIntent(
            "aa_attacker", 1, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "aa_victim" }));
        respawnServer.AdvanceIdleTicks(AuthoritativeWorldServer.CombatHeatPerAttack / 4);
        respawnServer.ProcessIntent(new ServerIntent(
            "aa_attacker", 2, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "aa_victim" }));
        respawnServer.AdvanceIdleTicks(AuthoritativeWorldServer.CombatHeatPerAttack / 4);
        respawnServer.ProcessIntent(new ServerIntent(
            "aa_attacker", 3, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "aa_victim" }));

        var hotChunk = respawnServer.GetChunkForTile(TilePosition.Origin);
        ExpectTrue(respawnServer.IsChunkHot(hotChunk.ChunkX, hotChunk.ChunkY),
            "origin chunk remains hot when victim is downed there");

        // Advance past downed countdown so victim is auto-respawned
        respawnServer.AdvanceIdleTicks(AuthoritativeWorldServer.DownedCountdownTicks);

        var victimRespawnPos = respawnState.Players["aa_victim"].Position;
        var victimRespawnChunk = respawnServer.GetChunkForTile(victimRespawnPos);
        var respawnedInHotChunk = victimRespawnChunk.ChunkX == hotChunk.ChunkX &&
                                  victimRespawnChunk.ChunkY == hotChunk.ChunkY;
        ExpectFalse(respawnedInHotChunk,
            "downed player auto-respawns outside the hot combat chunk when a cool chunk is available");

        var coolFarChunk = respawnServer.GetChunkForTile(new TilePosition(40, 40));
        ExpectFalse(respawnServer.IsChunkHot(coolFarChunk.ChunkX, coolFarChunk.ChunkY),
            "undisturbed far chunk stays cool for respawn preference");

        // ── Step 14: Downed state ─────────────────────────────────────────────────
        var downedState = new GameState();
        downedState.RegisterPlayer("ab_attacker", "Striker");
        downedState.RegisterPlayer("ab_victim", "Target");
        downedState.SetPlayerPosition("ab_attacker", TilePosition.Origin);
        downedState.SetPlayerPosition("ab_victim", TilePosition.Origin);
        var downedServer = new AuthoritativeWorldServer(downedState, "downed-test");

        // Three attacks to reduce victim to 0 HP (35 dmg × 3 = 105 > 100 HP)
        downedServer.ProcessIntent(new ServerIntent(
            "ab_attacker", 1, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ab_victim" }));
        downedServer.AdvanceIdleTicks(AuthoritativeWorldServer.CombatHeatPerAttack / 4);
        downedServer.ProcessIntent(new ServerIntent(
            "ab_attacker", 2, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ab_victim" }));
        downedServer.AdvanceIdleTicks(AuthoritativeWorldServer.CombatHeatPerAttack / 4);
        var downKill = downedServer.ProcessIntent(new ServerIntent(
            "ab_attacker", 3, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ab_victim" }));

        ExpectTrue(downKill.WasAccepted, "lethal attack is accepted");
        ExpectTrue(downKill.Event.EventId.Contains("player_downed"), "lethal attack emits player_downed event");
        ExpectTrue(downedState.Players["ab_victim"].IsDown, "victim is marked as downed after lethal hit");
        ExpectEqual(0, downedState.Players["ab_victim"].Health, "downed victim has 0 HP");
        ExpectTrue(downedState.Players["ab_victim"].IsAlive, "downed victim is still alive (not yet respawned)");

        var victimSnapshot = downedServer.CreateInterestSnapshot("ab_victim");
        ExpectTrue(victimSnapshot.Players.Any(p => p.Id == "ab_victim" && p.StatusEffects.Any(s => s.StartsWith("Downed"))),
            "downed player shows Downed status in snapshot");

        var downedChat = downedServer.ProcessIntent(new ServerIntent(
            "ab_victim", 1, IntentType.SendLocalChat,
            new System.Collections.Generic.Dictionary<string, string> { ["text"] = "help me" }));
        ExpectTrue(downedChat.WasAccepted, "downed player can still send local chat");

        var downedMove = downedServer.ProcessIntent(new ServerIntent(
            "ab_victim", 2, IntentType.Move,
            new System.Collections.Generic.Dictionary<string, string> { ["x"] = "5", ["y"] = "5" }));
        ExpectFalse(downedMove.WasAccepted, "downed player cannot move");

        // Advance past countdown; victim should auto-respawn
        downedServer.AdvanceIdleTicks(AuthoritativeWorldServer.DownedCountdownTicks + 1);
        ExpectFalse(downedState.Players["ab_victim"].IsDown, "victim is no longer downed after countdown");
        ExpectEqual(downedState.Players["ab_victim"].MaxHealth, downedState.Players["ab_victim"].Health,
            "auto-respawned victim has full health");

        // ── Step 15: Rescue intent ────────────────────────────────────────────────
        var rescueState = new GameState();
        rescueState.RegisterPlayer("ac_rescuer", "Hero");
        rescueState.RegisterPlayer("ac_victim", "Victim");
        rescueState.SetPlayerPosition("ac_rescuer", TilePosition.Origin);
        rescueState.SetPlayerPosition("ac_victim", TilePosition.Origin);
        var rescueServer = new AuthoritativeWorldServer(rescueState, "rescue-test");

        // Down the victim (three attacks from rescuer)
        rescueServer.ProcessIntent(new ServerIntent(
            "ac_rescuer", 1, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ac_victim" }));
        rescueServer.AdvanceIdleTicks(5);
        rescueServer.ProcessIntent(new ServerIntent(
            "ac_rescuer", 2, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ac_victim" }));
        rescueServer.AdvanceIdleTicks(5);
        rescueServer.ProcessIntent(new ServerIntent(
            "ac_rescuer", 3, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ac_victim" }));
        ExpectTrue(rescueState.Players["ac_victim"].IsDown, "rescue test: victim is downed before rescue");

        // Rescue rejects self-rescue
        var selfRescue = rescueServer.ProcessIntent(new ServerIntent(
            "ac_rescuer", 4, IntentType.Rescue,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ac_rescuer" }));
        ExpectFalse(selfRescue.WasAccepted, "rescue rejected for self-rescue");

        // Rescue rejects non-downed target (local_player is connected but not downed)
        var notDownedRescue = rescueServer.ProcessIntent(new ServerIntent(
            "ac_rescuer", 5, IntentType.Rescue,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = GameState.LocalPlayerId }));
        ExpectFalse(notDownedRescue.WasAccepted, "rescue rejected when target is not downed");

        // Rescue rejects out-of-range
        var farRescueState = new GameState();
        farRescueState.RegisterPlayer("ac_far_rescuer", "Far Hero");
        farRescueState.RegisterPlayer("ac_far_victim", "Far Victim");
        farRescueState.SetPlayerPosition("ac_far_rescuer", TilePosition.Origin);
        farRescueState.SetPlayerPosition("ac_far_victim", TilePosition.Origin);
        var farRescueServer = new AuthoritativeWorldServer(farRescueState, "far-rescue-test");
        farRescueServer.ProcessIntent(new ServerIntent(
            "ac_far_rescuer", 1, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ac_far_victim" }));
        farRescueServer.AdvanceIdleTicks(5);
        farRescueServer.ProcessIntent(new ServerIntent(
            "ac_far_rescuer", 2, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ac_far_victim" }));
        farRescueServer.AdvanceIdleTicks(5);
        farRescueServer.ProcessIntent(new ServerIntent(
            "ac_far_rescuer", 3, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ac_far_victim" }));
        farRescueState.SetPlayerPosition("ac_far_rescuer", new TilePosition(999, 999));
        var outOfRangeRescue = farRescueServer.ProcessIntent(new ServerIntent(
            "ac_far_rescuer", 4, IntentType.Rescue,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ac_far_victim" }));
        ExpectFalse(outOfRangeRescue.WasAccepted, "rescue rejected when rescuer is out of range");

        // Successful rescue
        var rescuerKarmaBefore = rescueState.Players["ac_rescuer"].Karma.Score;
        var rescueResult = rescueServer.ProcessIntent(new ServerIntent(
            "ac_rescuer", 6, IntentType.Rescue,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ac_victim" }));
        ExpectTrue(rescueResult.WasAccepted, "rescue intent accepted when rescuer is near downed player");
        ExpectFalse(rescueState.Players["ac_victim"].IsDown, "rescue clears downed state");
        ExpectEqual(AuthoritativeWorldServer.RescueHealAmount, rescueState.Players["ac_victim"].Health,
            "rescue heals victim to RescueHealAmount");
        ExpectTrue(rescueState.Players["ac_rescuer"].Karma.Score > rescuerKarmaBefore,
            "rescue ascends rescuer karma");
        ExpectTrue(rescueResult.Event.EventId.Contains("player_rescued"),
            "rescue emits player_rescued event");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { rescueResult.Event }).Contains("rescued"),
            "HUD formats rescue event");

        // Rescued player can move again
        var rescuedMove = rescueServer.ProcessIntent(new ServerIntent(
            "ac_victim", 1, IntentType.Move,
            new System.Collections.Generic.Dictionary<string, string> { ["x"] = "3", ["y"] = "3" }));
        ExpectTrue(rescuedMove.WasAccepted, "rescued player can move after rescue");

        // Second rescue rejected (not downed anymore)
        var doubleRescue = rescueServer.ProcessIntent(new ServerIntent(
            "ac_rescuer", 7, IntentType.Rescue,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ac_victim" }));
        ExpectFalse(doubleRescue.WasAccepted, "rescue rejected when target is no longer downed");

        // ── Step 19: Mount/vehicle entity model ───────────────────────────────────
        // Starter mounts are seeded at known positions; interest snapshot exposes
        // nearby mounts with speed modifier, parked state, and occupancy.
        var mountState = new GameState();
        mountState.RegisterPlayer("ah_rider", "Rider");
        mountState.SetPlayerPosition("ah_rider", TilePosition.Origin);
        var mountServer = new AuthoritativeWorldServer(mountState, "mount-test");

        // Server seeds starter mounts
        ExpectTrue(mountServer.Mounts.Count >= 2, "server seeds at least two starter mounts");
        ExpectTrue(mountServer.Mounts.ContainsKey("mount_hover_1"), "server seeds Hover Scooter mount");
        ExpectTrue(mountServer.Mounts.ContainsKey("mount_cargo_1"), "server seeds Cargo Crawler mount");

        // Mounts have correct model fields
        var hoverScooter = mountServer.Mounts["mount_hover_1"];
        ExpectTrue(hoverScooter.SpeedModifier > 1.0f, "hover scooter has speed bonus");
        ExpectTrue(hoverScooter.IsParked, "hover scooter starts parked");
        ExpectTrue(string.IsNullOrWhiteSpace(hoverScooter.OccupantPlayerId), "hover scooter starts unoccupied");

        // Interest snapshot includes mounts within radius (both starter mounts are at (12,8) and (15,12),
        // within 24-tile interest radius from Origin)
        mountState.SetPlayerPosition("ah_rider", TilePosition.Origin);
        var mountSnapshot = mountServer.CreateInterestSnapshot("ah_rider");
        ExpectTrue(mountSnapshot.Mounts.Count >= 2, "interest snapshot exposes nearby mounts");
        var snapHover = mountSnapshot.Mounts.FirstOrDefault(m => m.EntityId == "mount_hover_1");
        ExpectTrue(snapHover is not null, "interest snapshot includes hover scooter");
        ExpectEqual(12, snapHover?.TileX ?? -1, "hover scooter snapshot has correct X");
        ExpectEqual(8, snapHover?.TileY ?? -1, "hover scooter snapshot has correct Y");
        ExpectTrue(snapHover?.IsParked == true, "hover scooter snapshot reports parked");
        ExpectTrue(string.IsNullOrWhiteSpace(snapHover?.OccupantPlayerId), "hover scooter snapshot reports no occupant");

        // Mount out of interest radius is not included
        mountState.SetPlayerPosition("ah_rider", new TilePosition(50, 50));
        var farMountSnapshot = mountServer.CreateInterestSnapshot("ah_rider");
        ExpectEqual(0, farMountSnapshot.Mounts.Count, "mounts beyond interest radius are not included in snapshot");

        // ── Step 20: Mount/dismount intents + karma hooks ─────────────────────────
        // Players can mount parked unoccupied vehicles within interest radius.
        // Karma nudges on both mount and dismount (bonus near a station).
        var rideState = new GameState();
        rideState.RegisterPlayer("ai_rider", "Ace");
        rideState.RegisterPlayer("ai_rider2", "Bee");
        rideState.SetPlayerPosition("ai_rider", TilePosition.Origin);
        rideState.SetPlayerPosition("ai_rider2", TilePosition.Origin);
        var rideServer = new AuthoritativeWorldServer(rideState, "ride-test");

        // Hover Scooter at (12,8) is within 24-tile radius from Origin
        var mountIntent = new ServerIntent("ai_rider", 1, IntentType.Mount,
            new Dictionary<string, string> { ["mountId"] = "mount_hover_1" });
        var mountResult = rideServer.ProcessIntent(mountIntent);
        ExpectTrue(mountResult.WasAccepted, "Mount intent accepted when player is near a parked mount");
        ExpectFalse(rideServer.Mounts["mount_hover_1"].IsParked, "mount is no longer parked after mount intent");
        ExpectEqual("ai_rider", rideServer.Mounts["mount_hover_1"].OccupantPlayerId, "mount records occupant after mount intent");

        // Snap after mount shows occupant
        var afterMountSnap = rideServer.CreateInterestSnapshot("ai_rider");
        var snapAfterMount = afterMountSnap.Mounts.FirstOrDefault(m => m.EntityId == "mount_hover_1");
        ExpectTrue(snapAfterMount is not null, "interest snapshot includes mount after mount intent");
        ExpectEqual("ai_rider", snapAfterMount?.OccupantPlayerId ?? "", "snapshot reports occupant after mount");

        // Mount bag transfer persists on the mount across dismount/remount.
        rideState.AddItem("ai_rider", StarterItems.RationPack);
        var stashBag = rideServer.ProcessIntent(new ServerIntent("ai_rider", 2, IntentType.MountBagTransfer,
            new Dictionary<string, string>
            {
                ["mountId"] = "mount_hover_1",
                ["itemId"] = StarterItems.RationPackId,
                ["mode"] = "stash"
            }));
        ExpectTrue(stashBag.WasAccepted, "MountBagTransfer stashes a held item");
        ExpectFalse(rideState.Players["ai_rider"].Inventory.Any(i => i.Id == StarterItems.RationPackId),
            "stashed mount bag item leaves player inventory");
        ExpectTrue(rideServer.Mounts["mount_hover_1"].BagItemIds.Contains(StarterItems.RationPackId),
            "stashed item persists in mount bag state");
        var mountBagHud = HudController.FormatMountBag(rideServer.CreateInterestSnapshot("ai_rider").Mounts.First(m => m.EntityId == "mount_hover_1"));
        ExpectTrue(mountBagHud.Contains("Ration Pack x1"), "HUD formats mount bag contents");

        // HUD formats player_mounted event
        var mountedHud = HudController.FormatLatestServerEvent(new[] { mountResult.Event });
        ExpectTrue(mountedHud.Contains("Ace") && mountedHud.Contains("Hover Scooter"), "HUD formats player_mounted event with rider name and mount name");

        // Karma ascends on mount
        var karmaAfterMount = rideState.Players["ai_rider"].Karma.Score;
        ExpectTrue(karmaAfterMount > 0, "mounting ascends rider karma");

        // Double mount is rejected
        var doubleMountIntent = new ServerIntent("ai_rider", 3, IntentType.Mount,
            new Dictionary<string, string> { ["mountId"] = "mount_hover_1" });
        ExpectFalse(rideServer.ProcessIntent(doubleMountIntent).WasAccepted, "mount rejected when player is already mounted");

        // Occupied mount rejected for a second rider
        var occupiedMountIntent = new ServerIntent("ai_rider2", 1, IntentType.Mount,
            new Dictionary<string, string> { ["mountId"] = "mount_hover_1" });
        ExpectFalse(rideServer.ProcessIntent(occupiedMountIntent).WasAccepted, "mount rejected when vehicle is already occupied");

        // Dismount
        var dismountIntent = new ServerIntent("ai_rider", 4, IntentType.Dismount,
            new Dictionary<string, string>());
        var dismountResult = rideServer.ProcessIntent(dismountIntent);
        ExpectTrue(dismountResult.WasAccepted, "Dismount intent accepted when player is mounted");
        ExpectTrue(rideServer.Mounts["mount_hover_1"].IsParked, "mount is parked again after dismount");
        ExpectTrue(string.IsNullOrWhiteSpace(rideServer.Mounts["mount_hover_1"].OccupantPlayerId), "mount has no occupant after dismount");
        ExpectTrue(rideServer.Mounts["mount_hover_1"].BagItemIds.Contains(StarterItems.RationPackId),
            "mount bag contents persist after dismount");

        var remountResult = rideServer.ProcessIntent(new ServerIntent("ai_rider", 5, IntentType.Mount,
            new Dictionary<string, string> { ["mountId"] = "mount_hover_1" }));
        ExpectTrue(remountResult.WasAccepted, "rider can remount after dismount");
        var takeBag = rideServer.ProcessIntent(new ServerIntent("ai_rider", 6, IntentType.MountBagTransfer,
            new Dictionary<string, string>
            {
                ["mountId"] = "mount_hover_1",
                ["itemId"] = StarterItems.RationPackId,
                ["mode"] = "take"
            }));
        ExpectTrue(takeBag.WasAccepted, "MountBagTransfer takes an item from the bag");
        ExpectTrue(rideState.Players["ai_rider"].Inventory.Any(i => i.Id == StarterItems.RationPackId),
            "taken mount bag item enters player inventory");
        rideServer.ProcessIntent(new ServerIntent("ai_rider", 7, IntentType.Dismount,
            new Dictionary<string, string>()));

        // HUD formats player_dismounted event
        var dismountedHud = HudController.FormatLatestServerEvent(new[] { dismountResult.Event });
        ExpectTrue(dismountedHud.Contains("Ace") && dismountedHud.Contains("Hover Scooter"), "HUD formats player_dismounted event with rider name and mount name");

        // Dismount while not mounted is rejected
        var badDismountIntent = new ServerIntent("ai_rider", 8, IntentType.Dismount,
            new Dictionary<string, string>());
        ExpectFalse(rideServer.ProcessIntent(badDismountIntent).WasAccepted, "dismount rejected when player is not mounted");

        // Mount out of range is rejected
        rideState.SetPlayerPosition("ai_rider", new TilePosition(50, 50));
        var farMountIntent = new ServerIntent("ai_rider", 9, IntentType.Mount,
            new Dictionary<string, string> { ["mountId"] = "mount_hover_1" });
        ExpectFalse(rideServer.ProcessIntent(farMountIntent).WasAccepted, "mount rejected when mount is out of interest radius");

        // ── Step 21: Karma watermark tracking ────────────────────────────────────
        // KarmaPeak and KarmaFloor track the highest and lowest karma scores
        // reached during the match. They survive Karma Break (match-scoped, not
        // life-scoped). Surfaced in PlayerSnapshot.
        var wmState = new GameState();
        wmState.RegisterPlayer("aj_hero", "Hero");
        wmState.RegisterPlayer("aj_villain", "Villain");

        // Watermarks start at 0
        ExpectEqual(0, wmState.Players["aj_hero"].Karma.KarmaPeak, "karma peak starts at 0");
        ExpectEqual(0, wmState.Players["aj_hero"].Karma.KarmaFloor, "karma floor starts at 0");

        // Positive shifts update peak
        wmState.ApplyShift("aj_hero", new KarmaAction("aj_hero", "aj_hero", new[] { "helpful" }, "good deed"));
        var peakAfterGood = wmState.Players["aj_hero"].Karma.KarmaPeak;
        ExpectTrue(peakAfterGood > 0, "karma peak updates after positive shift");
        ExpectEqual(0, wmState.Players["aj_hero"].Karma.KarmaFloor, "karma floor unchanged after positive shift");

        // Negative shifts update floor
        wmState.ApplyShift("aj_villain", new KarmaAction("aj_villain", "aj_hero", new[] { "harmful" }, "bad deed"));
        ExpectTrue(wmState.Players["aj_villain"].Karma.KarmaFloor < 0, "karma floor updates after negative shift");
        ExpectEqual(0, wmState.Players["aj_villain"].Karma.KarmaPeak, "karma peak unchanged after negative shift");

        // Peak tracks maximum reached
        wmState.ApplyShift("aj_hero", new KarmaAction("aj_hero", "aj_hero", new[] { "helpful", "generous" }, "great deed"));
        var peakAfterGreat = wmState.Players["aj_hero"].Karma.KarmaPeak;
        ExpectTrue(peakAfterGreat > peakAfterGood, "karma peak advances when new high is reached");

        // Going back down does not lower the peak
        wmState.ApplyShift("aj_hero", new KarmaAction("aj_hero", "aj_hero", new[] { "harmful" }, "slip up"));
        ExpectEqual(peakAfterGreat, wmState.Players["aj_hero"].Karma.KarmaPeak, "karma peak does not decrease when score drops");

        // Going below zero tracks floor
        wmState.ApplyShift("aj_hero", new KarmaAction("aj_hero", "aj_hero", new[] { "violent", "harmful", "betrayal" }, "betrayal"));
        var floorAfterBetrayal = wmState.Players["aj_hero"].Karma.KarmaFloor;
        ExpectTrue(floorAfterBetrayal < 0, "karma floor tracks negative minimum");

        // Karma Break resets score but preserves watermarks
        var scoreBeforeBreak = wmState.Players["aj_hero"].Karma.Score;
        wmState.Players["aj_hero"].KarmaBreak();
        ExpectEqual(0, wmState.Players["aj_hero"].Karma.Score, "karma score resets to 0 on Karma Break");
        ExpectEqual(peakAfterGreat, wmState.Players["aj_hero"].Karma.KarmaPeak, "karma peak survives Karma Break");
        ExpectEqual(floorAfterBetrayal, wmState.Players["aj_hero"].Karma.KarmaFloor, "karma floor survives Karma Break");

        // Snapshot surfaces watermarks
        var wmServer = new AuthoritativeWorldServer(wmState, "watermark-test");
        var wmSnap = wmServer.CreateInterestSnapshot("aj_hero");
        var heroSnap = wmSnap.Players.FirstOrDefault(p => p.Id == "aj_hero");
        ExpectTrue(heroSnap is not null, "watermark snapshot includes player");
        ExpectEqual(peakAfterGreat, heroSnap?.KarmaPeak ?? -1, "snapshot KarmaPeak matches tracked peak");
        ExpectEqual(floorAfterBetrayal, heroSnap?.KarmaFloor ?? 1, "snapshot KarmaFloor matches tracked floor");

        // ── Step 22: Karma title-change broadcast ─────────────────────────────────
        // Server emits saint_title_changed / scourge_title_changed events when the
        // leaderboard leadership changes after a karma shift.
        var tcState = new GameState();
        tcState.RegisterPlayer("ak_alpha", "Alpha");
        tcState.RegisterPlayer("ak_beta", "Beta");
        tcState.RegisterPlayer("ak_gamma", "Gamma");
        // Pre-seed prototype rivals at -1 so EnsurePrototypePlayers does not set them to
        // +8/-8, leaving Saint and Scourge titles vacant at the start of this test.
        tcState.RegisterPlayer("rival_paragon", "Helpful Rival").ApplyKarma(-1);
        tcState.RegisterPlayer("rival_renegade", "Shady Rival").ApplyKarma(-1);
        var tcServer = new AuthoritativeWorldServer(tcState, "title-test");
        var eventsBefore = tcServer.EventLog.Count;

        // Alpha helps Mara (+7 karma) — overtakes rivals at -1, claims the vacant Saint title
        tcServer.ProcessIntent(new ServerIntent("ak_alpha", 1, IntentType.KarmaAction,
            new Dictionary<string, string> { ["action"] = PrototypeActions.HelpMaraId }));
        var saintEvent = tcServer.EventLog.Skip(eventsBefore).FirstOrDefault(e => e.EventId.Contains("saint_title_changed"));
        ExpectTrue(saintEvent is not null, "saint_title_changed event emitted when a player becomes Saint");
        ExpectEqual("ak_alpha", saintEvent?.Data.GetValueOrDefault("newHolderId", ""), "saint_title_changed identifies new holder");
        ExpectTrue(string.IsNullOrEmpty(saintEvent?.Data.GetValueOrDefault("previousHolderId", "X")), "saint_title_changed shows empty previousHolderId on first acquisition");

        // HUD formats saint_title_changed
        if (saintEvent is not null)
        {
            var saintHud = HudController.FormatLatestServerEvent(new[] { saintEvent });
            ExpectTrue(saintHud.Contains("Alpha") && saintHud.Contains("Saint"), "HUD formats saint_title_changed event");
        }

        // Beta helps a peer (+12 karma) — eclipses Alpha's +7 to claim Saint
        var eventsBeforeEclipse = tcServer.EventLog.Count;
        tcServer.ProcessIntent(new ServerIntent("ak_beta", 2, IntentType.KarmaAction,
            new Dictionary<string, string> { ["action"] = PrototypeActions.HelpPeerId }));
        var eclipseEvent = tcServer.EventLog.Skip(eventsBeforeEclipse).FirstOrDefault(e => e.EventId.Contains("saint_title_changed"));
        ExpectTrue(eclipseEvent is not null, "saint_title_changed fires when Saint title changes hands");
        ExpectEqual("ak_beta", eclipseEvent?.Data.GetValueOrDefault("newHolderId", ""), "eclipsing saint_title_changed identifies new holder");
        ExpectEqual("ak_alpha", eclipseEvent?.Data.GetValueOrDefault("previousHolderId", ""), "eclipsing saint_title_changed records previous holder");

        // Gamma steals from Mara (-10 karma) — descends below rivals at -1 to claim Scourge
        var eventsBeforeScourge = tcServer.EventLog.Count;
        tcServer.ProcessIntent(new ServerIntent("ak_gamma", 3, IntentType.KarmaAction,
            new Dictionary<string, string> { ["action"] = PrototypeActions.StealFromMaraId }));
        var scourgeEvent = tcServer.EventLog.Skip(eventsBeforeScourge).FirstOrDefault(e => e.EventId.Contains("scourge_title_changed"));
        ExpectTrue(scourgeEvent is not null, "scourge_title_changed event emitted when a player becomes Scourge");
        ExpectEqual("ak_gamma", scourgeEvent?.Data.GetValueOrDefault("newHolderId", ""), "scourge_title_changed identifies new holder");

        // HUD formats scourge_title_changed
        if (scourgeEvent is not null)
        {
            var scourgeHud = HudController.FormatLatestServerEvent(new[] { scourgeEvent });
            ExpectTrue(scourgeHud.Contains("Gamma") && scourgeHud.Contains("Scourge"), "HUD formats scourge_title_changed event");
        }

        // ── Step 23: Match end summary snapshot ───────────────────────────────────
        // After the match timer expires, CreateInterestSnapshot includes a
        // MatchSummarySnapshot with final standings and per-player stats.
        var msState = new GameState();
        msState.RegisterPlayer("al_hero", "Hero");
        msState.RegisterPlayer("al_villain", "Villain");
        msState.SetPlayerPosition("al_hero", TilePosition.Origin);
        msState.SetPlayerPosition("al_villain", TilePosition.Origin);
        var msServer = new AuthoritativeWorldServer(msState, "summary-test",
            new ServerConfig(MaxPlayers: 4, TargetPlayers: 4, Scale: WorldScale.Small,
                TickRate: 20, InterestRadiusTiles: 24, CombatRangeTiles: 2,
                ChunkSizeTiles: 32, MatchDurationSeconds: 10));

        // Build some stats: Hero helps Mara (+7 karma), Villain steals (-10)
        msServer.ProcessIntent(new ServerIntent("al_hero", 1, IntentType.KarmaAction,
            new Dictionary<string, string> { ["action"] = PrototypeActions.HelpMaraId }));
        msServer.ProcessIntent(new ServerIntent("al_villain", 2, IntentType.KarmaAction,
            new Dictionary<string, string> { ["action"] = PrototypeActions.StealFromMaraId }));

        // Match still in lobby/pre-start — summary is null
        var runningSnap = msServer.CreateInterestSnapshot("al_hero");
        ExpectTrue(runningSnap.MatchSummary is null, "MatchSummary is null before match ends");

        // Ready up all connected players to start the match, then advance past duration to finish
        var msSeq = 100;
        foreach (var msConnPid in msServer.ConnectedPlayerIds)
            msServer.ProcessIntent(new ServerIntent(msConnPid, msSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        msServer.AdvanceMatchTime(15);
        var summarySnap = msServer.CreateInterestSnapshot("al_hero");
        ExpectTrue(summarySnap.MatchSummary is not null, "MatchSummary is present after match ends");
        ExpectEqual(MatchStatus.Finished, summarySnap.Match.Status, "match status is Finished in summary snapshot");

        var msSummary = summarySnap.MatchSummary;
        ExpectTrue(msSummary.Players.Any(p => p.Id == "al_hero"), "MatchSummary includes connected players");
        var heroEntry = msSummary.Players.FirstOrDefault(p => p.Id == "al_hero");
        ExpectEqual(7, heroEntry?.FinalKarma ?? -1, "MatchSummary records final karma");
        ExpectEqual(7, heroEntry?.KarmaPeak ?? -1, "MatchSummary records karma peak");
        ExpectTrue((heroEntry?.KarmaFloor ?? 1) <= 7, "MatchSummary records karma floor");
        ExpectTrue(msSummary.Highlights.TryGetValue("al_hero", out var heroHighlights),
            "MatchSummary includes per-player highlights");
        ExpectEqual(7, heroHighlights?.MostKarmaGained ?? -1,
            "MatchSummary highlights record most karma gained from peak");
        var villainEntry = msSummary.Players.FirstOrDefault(p => p.Id == "al_villain");
        ExpectTrue(villainEntry is not null, "MatchSummary includes villain player");
        ExpectTrue((villainEntry?.FinalKarma ?? 0) < 0, "MatchSummary records villain negative karma");
        ExpectTrue(msSummary.Highlights.TryGetValue("al_villain", out var villainHighlights) &&
                   villainHighlights.MostKarmaLost > 0,
            "MatchSummary highlights record most karma lost from floor");

        // Kill counter
        msState.SetPlayerPosition("al_hero", TilePosition.Origin);
        msState.SetPlayerPosition("al_villain", TilePosition.Origin);
        var msKillServer = new AuthoritativeWorldServer(msState, "kill-test",
            new ServerConfig(MaxPlayers: 4, TargetPlayers: 4, Scale: WorldScale.Small,
                TickRate: 20, InterestRadiusTiles: 24, CombatRangeTiles: 2,
                ChunkSizeTiles: 32, MatchDurationSeconds: 5));
        var msKillSeq = 1;
        foreach (var msKillPid in msKillServer.ConnectedPlayerIds)
            msKillServer.ProcessIntent(new ServerIntent(msKillPid, msKillSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        for (var seq = msKillSeq; seq < msKillSeq + 5; seq++)
        {
            msKillServer.ProcessIntent(new ServerIntent("al_hero", seq, IntentType.Attack,
                new Dictionary<string, string> { ["targetId"] = "al_villain" }));
            msKillServer.AdvanceIdleTicks(3);
        }
        msKillServer.AdvanceMatchTime(10);
        var killSummary = msKillServer.CreateInterestSnapshot("al_hero").MatchSummary;
        var killHeroEntry = killSummary?.Players.FirstOrDefault(p => p.Id == "al_hero");
        ExpectTrue((killHeroEntry?.Kills ?? 0) >= 1, "MatchSummary counts kills when attacker downs a player");
        ExpectTrue(killSummary?.Highlights["al_hero"].LongestSpree >= 1,
            "MatchSummary highlights include kill spree count");

        // HUD formatter
        var hudSummary = HudController.FormatMatchSummary(msSummary);
        ExpectTrue(hudSummary.Contains("Match Over"), "FormatMatchSummary shows match-over header");
        ExpectTrue(hudSummary.Contains("Hero"), "FormatMatchSummary includes player names");
        ExpectTrue(hudSummary.Contains("highlights"), "FormatMatchSummary includes highlight rows");
        ExpectTrue(HudController.FormatMatchSummary(null).Contains("progress"), "FormatMatchSummary handles null gracefully");

        // ── Step 24: Warden perk + IssueWanted intent ─────────────────────────────
        // Players with karma ≥ 150 earn the Warden perk and can mark others Wanted.
        // Downing a Wanted player clears the warrant and awards karma to the attacker.
        var wardenCheckPlayer = new PlayerState("warden_check", "Warden Check");
        wardenCheckPlayer.ApplyKarma(PerkCatalog.WardenThreshold);
        ExpectTrue(PerkCatalog.GetForPlayer(
            wardenCheckPlayer,
            new LeaderboardStanding("warden_check", "Warden Check", PerkCatalog.WardenThreshold, "", "", 0))
            .Any(p => p.Id == PerkCatalog.WardenId), "Warden perk awarded at karma ≥ 150");
        var subWardenPlayer = new PlayerState("sub_check", "Sub Check");
        subWardenPlayer.ApplyKarma(PerkCatalog.WardenThreshold - 1);
        ExpectFalse(PerkCatalog.GetForPlayer(
            subWardenPlayer,
            new LeaderboardStanding("sub_check", "Sub Check", PerkCatalog.WardenThreshold - 1, "", "", 0))
            .Any(p => p.Id == PerkCatalog.WardenId), "Warden perk not awarded below karma 150");

        var wdState = new GameState();
        wdState.RegisterPlayer("am_warden", "Lawbringer");
        wdState.RegisterPlayer("am_target", "Outlaw");
        wdState.SetPlayerPosition("am_warden", TilePosition.Origin);
        wdState.SetPlayerPosition("am_target", TilePosition.Origin);
        // Give Warden player enough karma for the perk
        wdState.Players["am_warden"].ApplyKarma(PerkCatalog.WardenThreshold);
        var wdServer = new AuthoritativeWorldServer(wdState, "warden-test");

        // Reject without perk
        var noWarden = new GameState();
        noWarden.RegisterPlayer("am_poor", "Poor");
        noWarden.RegisterPlayer("am_villain", "Villain");
        noWarden.SetPlayerPosition("am_poor", TilePosition.Origin);
        noWarden.SetPlayerPosition("am_villain", TilePosition.Origin);
        var noWardenServer = new AuthoritativeWorldServer(noWarden, "no-warden");
        var rejectedWanted = noWardenServer.ProcessIntent(new ServerIntent("am_poor", 1, IntentType.IssueWanted,
            new Dictionary<string, string> { ["targetId"] = "am_villain" }));
        ExpectFalse(rejectedWanted.WasAccepted, "IssueWanted rejected without Warden perk");

        // Accept with perk
        var issuedWanted = wdServer.ProcessIntent(new ServerIntent("am_warden", 1, IntentType.IssueWanted,
            new Dictionary<string, string> { ["targetId"] = "am_target" }));
        ExpectTrue(issuedWanted.WasAccepted, "IssueWanted accepted by Warden-perk holder");

        // Status effect visible in snapshot
        var wdSnap = wdServer.CreateInterestSnapshot("am_warden");
        var targetSnap = wdSnap.Players.FirstOrDefault(p => p.Id == "am_target");
        ExpectTrue(targetSnap?.StatusEffects.Any(s => s.Contains("Wanted")) == true, "Wanted player has Wanted status effect");

        // Duplicate Wanted rejected
        var dupWanted = wdServer.ProcessIntent(new ServerIntent("am_warden", 2, IntentType.IssueWanted,
            new Dictionary<string, string> { ["targetId"] = "am_target" }));
        ExpectFalse(dupWanted.WasAccepted, "IssueWanted rejected when target is already Wanted");

        // Downing the Wanted player clears warrant and rewards karma
        var wardenKarmaBefore = wdState.Players["am_warden"].Karma.Score;
        for (var wdSeq = 3; wdSeq <= 8; wdSeq++)
        {
            wdServer.ProcessIntent(new ServerIntent("am_warden", wdSeq, IntentType.Attack,
                new Dictionary<string, string> { ["targetId"] = "am_target" }));
            wdServer.AdvanceIdleTicks(3);
        }
        var wdAfterSnap = wdServer.CreateInterestSnapshot("am_warden");
        var targetAfterSnap = wdAfterSnap.Players.FirstOrDefault(p => p.Id == "am_target");
        ExpectFalse(targetAfterSnap?.StatusEffects.Any(s => s.Contains("Wanted")) == true, "Wanted status cleared after target is downed");
        var wantedBountyEvent = wdServer.EventLog.FirstOrDefault(e => e.EventId.Contains("wanted_bounty_claimed"));
        ExpectTrue(wantedBountyEvent is not null, "wanted_bounty_claimed event fires when Wanted player is downed");
        ExpectEqual(
            AuthoritativeWorldServer.WantedKarmaReward.ToString(),
            wantedBountyEvent?.Data.GetValueOrDefault("karmaReward", ""),
            "Warden gains karma for downing a Wanted player");

        // Self-warrant rejected
        var selfWanted = wdServer.ProcessIntent(new ServerIntent("am_warden", 9, IntentType.IssueWanted,
            new Dictionary<string, string> { ["targetId"] = "am_warden" }));
        ExpectFalse(selfWanted.WasAccepted, "IssueWanted rejected when targeting self");

        // ── Step 25: Wraith perk speed modifier ───────────────────────────────────
        // Players at karma ≤ -150 earn Wraith Surge. When HP ≤ 30%, their snapshot
        // SpeedModifier is 1.5; at higher HP or without the perk, it is 1.0.
        var wrcState = new GameState();
        wrcState.RegisterPlayer("an_wraith", "Shade");
        wrcState.RegisterPlayer("an_attacker", "Hunter");
        wrcState.SetPlayerPosition("an_wraith", TilePosition.Origin);
        wrcState.SetPlayerPosition("an_attacker", TilePosition.Origin);
        wrcState.Players["an_wraith"].ApplyKarma(-PerkCatalog.WraithThreshold);
        var wrcServer = new AuthoritativeWorldServer(wrcState, "wraith-test");

        // Full HP — no speed bonus even with perk
        var wrFullSnap = wrcServer.CreateInterestSnapshot("an_wraith");
        var wrSelf = wrFullSnap.Players.FirstOrDefault(p => p.Id == "an_wraith");
        ExpectTrue(wrcState.Players["an_wraith"].Karma.Score <= -PerkCatalog.WraithThreshold, "Wraith perk player has required karma");
        ExpectEqual(1f, wrSelf?.SpeedModifier ?? 0f, "SpeedModifier is 1.0 at full HP even with Wraith perk");

        // Damage wraith to ≤ 30% HP
        var wrMaxHp = wrcState.Players["an_wraith"].MaxHealth;
        var lowHpTarget = (int)(wrMaxHp * PerkCatalog.WraithLowHpPercent);
        wrcState.Players["an_wraith"].ApplyDamage(wrMaxHp - lowHpTarget);
        var wrLowSnap = wrcServer.CreateInterestSnapshot("an_wraith");
        var wrLowSelf = wrLowSnap.Players.FirstOrDefault(p => p.Id == "an_wraith");
        ExpectTrue(wrcState.Players["an_wraith"].Health <= lowHpTarget, "Wraith player HP is at or below 30%");
        ExpectEqual(PerkCatalog.WraithSpeedModifier, wrLowSelf?.SpeedModifier ?? 0f, "SpeedModifier is 1.5 at ≤ 30% HP with Wraith perk");

        // Normal player at low HP has no boost
        var normalPlayer = new PlayerState("normal_check", "Normal");
        normalPlayer.ApplyDamage(normalPlayer.MaxHealth - lowHpTarget);
        var normalSnap = SnapshotBuilder.CalculateSpeedModifier(
            normalPlayer, new LeaderboardStanding("normal_check", "Normal", 0, "", "", 0));
        ExpectEqual(1f, normalSnap, "SpeedModifier is 1.0 for non-Wraith player even at low HP");

        // ── Step 26: Bounty system ─────────────────────────────────────────────────
        // Players whose karma falls below -50 accrue a scrip bounty equal to the
        // overage (e.g. karma -60 → bounty 10).  Downing them transfers the bounty
        // scrip to the scorer.  A Karma Break clears the bounty.

        var bqState = new GameState();
        bqState.RegisterPlayer("ao_hunter", "Hunter");
        bqState.RegisterPlayer("ao_outlaw", "Outlaw");
        bqState.RegisterPlayer("ao_bystander", "Bystander");
        bqState.SetPlayerPosition("ao_hunter", TilePosition.Origin);
        bqState.SetPlayerPosition("ao_outlaw", TilePosition.Origin);
        bqState.SetPlayerPosition("ao_bystander", TilePosition.Origin);
        var bqServer = new AuthoritativeWorldServer(bqState, "bounty-test");

        // No bounty yet — karma is 0
        ExpectEqual(0, bqServer.GetBounty("ao_outlaw"), "No bounty at zero karma");

        // Descend outlaw to -50 exactly — no bounty yet (threshold is < -50)
        bqState.Players["ao_outlaw"].ApplyKarma(AuthoritativeWorldServer.BountyKarmaThreshold);
        bqServer.ProcessIntent(new ServerIntent("ao_outlaw", 1, IntentType.KarmaAction,
            new Dictionary<string, string> { ["action"] = PrototypeActions.StealFromMaraId }));
        var bountyAfterThreshold = bqServer.GetBounty("ao_outlaw");
        ExpectTrue(bqState.Players["ao_outlaw"].Karma.Score < AuthoritativeWorldServer.BountyKarmaThreshold,
            "Outlaw karma is below threshold after steal");
        ExpectTrue(bountyAfterThreshold > 0, "Bounty accrues when karma drops below threshold");

        // Bounty should equal |karma| - 50
        var expectedBounty = -(bqState.Players["ao_outlaw"].Karma.Score - AuthoritativeWorldServer.BountyKarmaThreshold);
        ExpectEqual(expectedBounty, bountyAfterThreshold, "Bounty equals karma overage below threshold");

        // Down the outlaw — hunter claims the bounty scrip
        var hunterScripBefore = bqState.Players["ao_hunter"].Scrip;
        bqServer.AdvanceIdleTicks(5);
        bqServer.ProcessIntent(new ServerIntent("ao_hunter", 2, IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "ao_outlaw" }));
        bqServer.AdvanceIdleTicks(5);
        bqServer.ProcessIntent(new ServerIntent("ao_hunter", 3, IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "ao_outlaw" }));
        bqServer.AdvanceIdleTicks(5);
        bqServer.ProcessIntent(new ServerIntent("ao_hunter", 4, IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "ao_outlaw" }));
        var outlawDowned = bqState.Players["ao_outlaw"].Health <= 0;
        ExpectTrue(outlawDowned, "Outlaw was downed");

        if (outlawDowned)
        {
            var bountyClaimed = bqServer.EventLog.Any(e => e.EventId.Contains("bounty_claimed") &&
                e.Data.GetValueOrDefault("targetId") == "ao_outlaw");
            ExpectTrue(bountyClaimed, "bounty_claimed event fires when bounty outlaw is downed");
            var hunterScripAfter = bqState.Players["ao_hunter"].Scrip;
            ExpectTrue(hunterScripAfter > hunterScripBefore, "Hunter receives scrip bounty for downing outlaw");
            ExpectEqual(0, bqServer.GetBounty("ao_outlaw"), "Bounty cleared after it is claimed");
        }

        // Karma Break clears any remaining bounty
        var bqBreakState = new GameState();
        bqBreakState.RegisterPlayer("ao_fugitive", "Fugitive");
        bqBreakState.SetPlayerPosition("ao_fugitive", TilePosition.Origin);
        bqBreakState.Players["ao_fugitive"].ApplyKarma(AuthoritativeWorldServer.BountyKarmaThreshold - 20);
        var bqBreakServer = new AuthoritativeWorldServer(bqBreakState, "bounty-break-test");
        bqBreakServer.ProcessIntent(new ServerIntent("ao_fugitive", 1, IntentType.KarmaAction,
            new Dictionary<string, string> { ["action"] = PrototypeActions.StealFromMaraId }));
        ExpectTrue(bqBreakServer.GetBounty("ao_fugitive") > 0, "Fugitive has bounty before Karma Break");
        bqBreakServer.ProcessIntent(new ServerIntent("ao_fugitive", 2, IntentType.KarmaBreak,
            new Dictionary<string, string>()));
        ExpectEqual(0, bqBreakServer.GetBounty("ao_fugitive"), "Bounty cleared after Karma Break");

        // Bounty status effect appears in snapshot
        var bqSnapState = new GameState();
        bqSnapState.RegisterPlayer("ao_marked", "Marked");
        bqSnapState.SetPlayerPosition("ao_marked", TilePosition.Origin);
        bqSnapState.Players["ao_marked"].ApplyKarma(AuthoritativeWorldServer.BountyKarmaThreshold - 10);
        var bqSnapServer = new AuthoritativeWorldServer(bqSnapState, "bounty-snap-test");
        bqSnapServer.ProcessIntent(new ServerIntent("ao_marked", 1, IntentType.KarmaAction,
            new Dictionary<string, string> { ["action"] = PrototypeActions.StealFromMaraId }));
        var bqSnap = bqSnapServer.CreateInterestSnapshot("ao_marked");
        var markedSnapshot = bqSnap.Players.FirstOrDefault(p => p.Id == "ao_marked");
        var hasBountyStatus = markedSnapshot?.StatusEffects.Any(s => s.StartsWith("Bounty:")) == true;
        ExpectTrue(hasBountyStatus, "Bounty status effect appears in player snapshot");

        // ── Step 27: Player status effects model ──────────────────────────────────
        // ApplyStatus/ClearStatus/HasStatus let the server attach typed statuses
        // to players.  They appear in PlayerSnapshot.StatusEffects and are wiped
        // on Karma Break.

        var pssState = new GameState();
        pssState.RegisterPlayer("ap_target", "Target");
        pssState.SetPlayerPosition("ap_target", TilePosition.Origin);
        var pssServer = new AuthoritativeWorldServer(pssState, "status-test");

        ExpectTrue(!pssServer.HasStatus("ap_target", PlayerStatusKind.Poisoned), "No Poisoned status initially");

        pssServer.ApplyStatus("ap_target", PlayerStatusKind.Poisoned);
        ExpectTrue(pssServer.HasStatus("ap_target", PlayerStatusKind.Poisoned), "Poisoned status active after ApplyStatus");

        var pssSnap = pssServer.CreateInterestSnapshot("ap_target");
        var psPlayer = pssSnap.Players.FirstOrDefault(p => p.Id == "ap_target");
        ExpectTrue(psPlayer?.StatusEffects.Contains("Poisoned") == true,
            "Poisoned appears in PlayerSnapshot.StatusEffects");

        pssServer.ClearStatus("ap_target", PlayerStatusKind.Poisoned);
        ExpectTrue(!pssServer.HasStatus("ap_target", PlayerStatusKind.Poisoned), "Poisoned cleared after ClearStatus");

        var pssSnapAfterClear = pssServer.CreateInterestSnapshot("ap_target");
        var psPlayerAfterClear = pssSnapAfterClear.Players.FirstOrDefault(p => p.Id == "ap_target");
        ExpectTrue(psPlayerAfterClear?.StatusEffects.Contains("Poisoned") != true,
            "Poisoned absent from snapshot after ClearStatus");

        // Multiple statuses can coexist
        pssServer.ApplyStatus("ap_target", PlayerStatusKind.Poisoned);
        pssServer.ApplyStatus("ap_target", PlayerStatusKind.Burning);
        ExpectTrue(pssServer.HasStatus("ap_target", PlayerStatusKind.Poisoned) &&
                   pssServer.HasStatus("ap_target", PlayerStatusKind.Burning),
            "Multiple statuses can be active simultaneously");

        // Karma Break clears all persistent statuses
        pssServer.ProcessIntent(new ServerIntent("ap_target", 1, IntentType.KarmaBreak,
            new Dictionary<string, string>()));
        ExpectTrue(!pssServer.HasStatus("ap_target", PlayerStatusKind.Poisoned),
            "Poisoned cleared after Karma Break");
        ExpectTrue(!pssServer.HasStatus("ap_target", PlayerStatusKind.Burning),
            "Burning cleared after Karma Break");

        // ── Step 28: Contraband item tag ───────────────────────────────────────────
        // Items with IsContraband=true cause karma decay each tick when the holder
        // is near a law-aligned NPC.  Players far from law NPCs are unaffected.

        // Verify the model flag
        ExpectTrue(StarterItems.ContrabandPackage.IsContraband, "ContrabandPackage has IsContraband=true");
        ExpectTrue(!StarterItems.RationPack.IsContraband, "RationPack is not contraband");

        // Player at Mara's position (3,4) — within interest radius
        var cbState = new GameState();
        cbState.RegisterPlayer("aq_carrier", "Carrier");
        cbState.SetPlayerPosition("aq_carrier", new TilePosition(3, 4));
        cbState.AddItem("aq_carrier", StarterItems.ContrabandPackage);
        var cbServer = new AuthoritativeWorldServer(cbState, "contraband-test");

        var karmaBefore = cbState.Players["aq_carrier"].Karma.Score;
        cbServer.AdvanceIdleTicks(1);
        var karmaAfter = cbState.Players["aq_carrier"].Karma.Score;
        ExpectTrue(karmaAfter < karmaBefore,
            "Carrying contraband near law NPC causes karma decay each tick");

        var detectedEvent = cbServer.EventLog.Any(e => e.EventId.Contains("contraband_detected") &&
            e.Data.GetValueOrDefault("playerId") == "aq_carrier");
        ExpectTrue(detectedEvent, "contraband_detected event fires when decay triggers");

        // Player far from any NPC — no decay
        var cbFarState = new GameState();
        cbFarState.RegisterPlayer("aq_farcarrier", "FarCarrier");
        cbFarState.SetPlayerPosition("aq_farcarrier", new TilePosition(60, 60));
        cbFarState.AddItem("aq_farcarrier", StarterItems.ContrabandPackage);
        var cbFarServer = new AuthoritativeWorldServer(cbFarState, "contraband-far-test");

        var karmaFarBefore = cbFarState.Players["aq_farcarrier"].Karma.Score;
        cbFarServer.AdvanceIdleTicks(1);
        var karmaFarAfter = cbFarState.Players["aq_farcarrier"].Karma.Score;
        ExpectEqual(karmaFarBefore, karmaFarAfter,
            "Carrying contraband far from law NPCs causes no karma decay");

        // ── Step 29: Lobby / ready-up flow ────────────────────────────────────────
        // Match starts in Lobby.  AdvanceMatchTime does nothing until a quorum of
        // connected players has sent ReadyUp; after quorum the match transitions to
        // Running and the timer advances normally.

        var lobState = new GameState();
        lobState.RegisterPlayer("ar_p1", "P1");
        lobState.RegisterPlayer("ar_p2", "P2");
        lobState.SetPlayerPosition("ar_p1", TilePosition.Origin);
        lobState.SetPlayerPosition("ar_p2", TilePosition.Origin);
        var lobServer = new AuthoritativeWorldServer(lobState, "lobby-test",
            new ServerConfig(MaxPlayers: 2, TargetPlayers: 2, Scale: WorldScale.Small,
                TickRate: 20, InterestRadiusTiles: 24, CombatRangeTiles: 2,
                ChunkSizeTiles: 32, MatchDurationSeconds: 60));

        ExpectEqual(MatchStatus.Lobby, lobServer.Match.Status, "new server starts in Lobby");

        // Timer does not advance in Lobby
        lobServer.AdvanceMatchTime(10);
        ExpectEqual(60, lobServer.Match.RemainingSeconds, "timer does not count down while in Lobby");

        // One player readies up — still Lobby (quorum not met)
        var readyResult = lobServer.ProcessIntent(new ServerIntent("ar_p1", 1, IntentType.ReadyUp, new Dictionary<string, string>()));
        ExpectTrue(readyResult.WasAccepted, "ReadyUp intent is accepted in Lobby");
        ExpectEqual(MatchStatus.Lobby, lobServer.Match.Status, "match stays in Lobby until all players are ready");
        ExpectTrue(lobServer.IsReady("ar_p1"), "ReadyUp marks player as ready");
        ExpectTrue(!lobServer.IsReady("ar_p2"), "player who has not readied up is not ready");
        ExpectEqual(1, lobServer.ReadyCount, "ReadyCount reflects players who have sent ReadyUp");

        // Duplicate ReadyUp is rejected
        var dupeReady = lobServer.ProcessIntent(new ServerIntent("ar_p1", 2, IntentType.ReadyUp, new Dictionary<string, string>()));
        ExpectTrue(!dupeReady.WasAccepted, "duplicate ReadyUp is rejected");

        // Second player readies up — quorum reached, match starts
        lobServer.ProcessIntent(new ServerIntent("ar_p2", 3, IntentType.ReadyUp, new Dictionary<string, string>()));
        ExpectEqual(MatchStatus.Running, lobServer.Match.Status, "match transitions to Running once all players ready");
        ExpectTrue(lobServer.EventLog.Any(e => e.EventId.Contains("match_started")),
            "match_started event fires when quorum is reached");

        // Timer now advances normally
        lobServer.AdvanceMatchTime(10);
        ExpectEqual(50, lobServer.Match.RemainingSeconds, "timer counts down when match is Running");

        // ReadyUp rejected after match started
        var postStartReady = lobServer.ProcessIntent(new ServerIntent("ar_p2", 4, IntentType.ReadyUp, new Dictionary<string, string>()));
        ExpectTrue(!postStartReady.WasAccepted, "ReadyUp rejected after match has started");

        // ── Step 30: Supply drop world event ──────────────────────────────────────
        // ScheduleSupplyDrop seeds a world item at a broadcast location.  The first
        // player to pick it up emits supply_drop_claimed.  The cache expires and is
        // removed if not claimed within the expiry window.

        var sdState = new GameState();
        sdState.RegisterPlayer("as_player", "Treasure Hunter");
        sdState.SetPlayerPosition("as_player", TilePosition.Origin);
        var sdServer = new AuthoritativeWorldServer(sdState, "supply-drop-test");
        var sdReadySeq = 1;
        foreach (var sdPid in sdServer.ConnectedPlayerIds)
            sdServer.ProcessIntent(new ServerIntent(sdPid, sdReadySeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        var sdDropPos = new TilePosition(2, 2);
        var sdEntityId = sdServer.ScheduleSupplyDrop(sdDropPos, StarterItems.MediPatch, expiryTicks: 50);
        ExpectTrue(!string.IsNullOrEmpty(sdEntityId), "ScheduleSupplyDrop returns a non-empty entity ID");
        ExpectTrue(sdServer.WorldItems.ContainsKey(sdEntityId), "supply drop world item is seeded");
        ExpectTrue(sdServer.EventLog.Any(e => e.EventId.Contains("supply_drop_spawned")),
            "supply_drop_spawned event emitted when drop is scheduled");

        // Global world event broadcast
        var sdWorldEvent = sdState.WorldEvents.Events.LastOrDefault(e => e.Type == WorldEventType.SupplyDrop);
        ExpectTrue(sdWorldEvent is not null, "supply drop emits a global SupplyDrop world event");
        ExpectTrue(sdWorldEvent?.IsGlobal == true, "supply drop world event is global (visible to all)");

        // Player claims the drop
        sdState.SetPlayerPosition("as_player", sdDropPos);
        var claimResult = sdServer.ProcessIntent(new ServerIntent("as_player", sdReadySeq++, IntentType.Interact,
            new Dictionary<string, string> { ["entityId"] = sdEntityId }));
        ExpectTrue(claimResult.WasAccepted, "player can claim a supply drop by interacting with it");
        ExpectTrue(sdServer.EventLog.Any(e => e.EventId.Contains("supply_drop_claimed") &&
            e.Data.GetValueOrDefault("playerId") == "as_player"),
            "supply_drop_claimed event fires when player picks up supply drop");
        ExpectTrue(sdState.Players["as_player"].Inventory.Any(i => i.Id == StarterItems.MediPatchId),
            "claimed supply drop item is added to player inventory");

        // Expiry: unclaimed supply drop is removed after timeout
        var sdExpireState = new GameState();
        sdExpireState.RegisterPlayer("as_bystander", "Bystander");
        sdExpireState.SetPlayerPosition("as_bystander", new TilePosition(60, 60));
        var sdExpireServer = new AuthoritativeWorldServer(sdExpireState, "supply-drop-expire-test");
        var sdExpireId = sdExpireServer.ScheduleSupplyDrop(TilePosition.Origin, StarterItems.DataChip, expiryTicks: 5);
        sdExpireServer.AdvanceIdleTicks(6);
        ExpectTrue(!sdExpireServer.WorldItems.ContainsKey(sdExpireId) ||
                   !sdExpireServer.WorldItems[sdExpireId].IsAvailable,
            "expired supply drop is removed from world items");
        ExpectTrue(sdExpireServer.EventLog.Any(e => e.EventId.Contains("supply_drop_expired")),
            "supply_drop_expired event fires when drop times out");

        // ── Event playback prototype: updated match systems together ──────────────
        // This short narrative prototype plays several newer systems in sequence so
        // future slices can catch integration breaks, not just isolated unit failures:
        // lobby ready-up → Warden marks an outlaw Wanted → contraband ticks near law
        // NPCs → Wraith low-HP speed kicks in → supply drop is claimed → match summary.
        var protoConfig = new ServerConfig(
            MaxPlayers: 7,
            TargetPlayers: 7,
            Scale: WorldScale.Small,
            TickRate: 20,
            InterestRadiusTiles: 24,
            CombatRangeTiles: 2,
            ChunkSizeTiles: ServerConfig.DefaultChunkSizeTiles,
            MatchDurationSeconds: 12);
        var protoState = new GameState();
        protoState.RegisterPlayer("proto_warden", "Prototype Warden");
        protoState.RegisterPlayer("proto_outlaw", "Prototype Outlaw");
        protoState.RegisterPlayer("proto_wraith", "Prototype Wraith");
        protoState.SetPlayerPosition("proto_warden", TilePosition.Origin);
        protoState.SetPlayerPosition("proto_outlaw", new TilePosition(3, 4));
        protoState.SetPlayerPosition("proto_wraith", new TilePosition(1, 0));
        protoState.Players["proto_warden"].ApplyKarma(PerkCatalog.WardenThreshold);
        protoState.Players["proto_outlaw"].ApplyKarma(AuthoritativeWorldServer.BountyKarmaThreshold - 25);
        protoState.Players["proto_wraith"].ApplyKarma(-PerkCatalog.WraithThreshold);
        protoState.Players["proto_wraith"].ApplyDamage(70);
        protoState.AddItem("proto_outlaw", StarterItems.ContrabandPackage);

        var protoServer = new AuthoritativeWorldServer(protoState, "event-playback-prototype", protoConfig);
        var protoSeq = 1;
        foreach (var protoPid in protoServer.ConnectedPlayerIds)
        {
            var ready = protoServer.ProcessIntent(new ServerIntent(protoPid, protoSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
            ExpectTrue(ready.WasAccepted, $"event prototype ready-up accepted for {protoPid}");
        }

        ExpectEqual(MatchStatus.Running, protoServer.Match.Status, "event prototype starts match after all players ready");
        var protoWanted = protoServer.ProcessIntent(new ServerIntent("proto_warden", protoSeq++, IntentType.IssueWanted,
            new Dictionary<string, string> { ["targetId"] = "proto_outlaw" }));
        ExpectTrue(protoWanted.WasAccepted, "event prototype Warden can mark outlaw Wanted");
        protoServer.AdvanceIdleTicks(1);
        var protoDropId = protoServer.ScheduleSupplyDrop(new TilePosition(1, 0), StarterItems.DataChip, expiryTicks: 20);
        var protoClaim = protoServer.ProcessIntent(new ServerIntent("proto_wraith", protoSeq++, IntentType.Interact,
            new Dictionary<string, string> { ["entityId"] = protoDropId }));
        ExpectTrue(protoClaim.WasAccepted, "event prototype Wraith can claim supply drop");

        var protoSnapshot = protoServer.CreateInterestSnapshot("proto_warden");
        var protoOutlaw = protoSnapshot.Players.FirstOrDefault(player => player.Id == "proto_outlaw");
        var protoWraith = protoSnapshot.Players.FirstOrDefault(player => player.Id == "proto_wraith");
        ExpectTrue(protoOutlaw?.StatusEffects.Any(status => status.Contains("Wanted")) == true,
            "event prototype snapshot shows Wanted status");
        ExpectTrue(protoOutlaw?.StatusEffects.Any(status => status.StartsWith("Bounty:")) == true,
            "event prototype snapshot shows bounty status");
        ExpectEqual(PerkCatalog.WraithSpeedModifier, protoWraith?.SpeedModifier ?? 0f,
            "event prototype snapshot shows low-HP Wraith speed modifier");
        ExpectTrue(protoState.Players["proto_outlaw"].Karma.Score < AuthoritativeWorldServer.BountyKarmaThreshold - 25,
            "event prototype contraband decay applies near law NPC");
        ExpectTrue(protoState.Players["proto_wraith"].Inventory.Any(item => item.Id == StarterItems.DataChipId),
            "event prototype claimed supply drop reaches inventory");
        ExpectTrue(protoServer.EventLog.Any(e => e.EventId.Contains("match_started")),
            "event prototype emits match_started event");
        ExpectTrue(protoServer.EventLog.Any(e => e.EventId.Contains("player_wanted")),
            "event prototype emits player_wanted event");
        ExpectTrue(protoServer.EventLog.Any(e => e.EventId.Contains("contraband_detected")),
            "event prototype emits contraband_detected event");
        ExpectTrue(protoServer.EventLog.Any(e => e.EventId.Contains("supply_drop_claimed")),
            "event prototype emits supply_drop_claimed event");

        protoServer.AdvanceMatchTime(protoConfig.MatchDurationSeconds);
        var protoSummary = protoServer.CreateInterestSnapshot("proto_warden").MatchSummary;
        ExpectTrue(protoSummary?.Players.Any(player => player.Id == "proto_warden") == true,
            "event prototype produces match summary with Warden participant");
        ExpectTrue(protoSummary?.Players.Any(player => player.Id == "proto_outlaw") == true,
            "event prototype produces match summary with Outlaw participant");
        GD.Print($"Event playback prototype ran {protoServer.EventLog.Count} server events through {protoServer.Match.Status}.");

        // ── Step 31: NPC patrol routes ────────────────────────────────────────────
        // NPCs cycle through 2–3 tile waypoints at NpcPatrolTickInterval-tick cadence.
        // SetNpcPatrol places the NPC at waypoint[0]; each patrol step advances the
        // index and updates Position; snapshot reflects the new tile coordinates.

        var patrolState = new GameState();
        patrolState.RegisterPlayer("pt_observer", "Observer");
        patrolState.SetPlayerPosition("pt_observer", TilePosition.Origin);
        var patrolServer = new AuthoritativeWorldServer(patrolState, "patrol-test");
        foreach (var ptPid in patrolServer.ConnectedPlayerIds)
            patrolServer.ProcessIntent(new ServerIntent(ptPid, 1, IntentType.ReadyUp, new Dictionary<string, string>()));

        var ptWp0 = new TilePosition(2, 2);
        var ptWp1 = new TilePosition(5, 2);
        var ptWp2 = new TilePosition(5, 5);
        patrolServer.SetNpcPatrol(StarterNpcs.Mara.Id, new[] { ptWp0, ptWp1, ptWp2 });

        ExpectTrue(patrolServer.GetNpcPosition(StarterNpcs.Mara.Id) == ptWp0,
            "NPC starts at waypoint 0 after SetNpcPatrol");

        // Advance exactly one patrol interval → NPC steps to waypoint 1
        patrolServer.AdvanceIdleTicks(AuthoritativeWorldServer.NpcPatrolTickInterval);
        ExpectTrue(patrolServer.GetNpcPosition(StarterNpcs.Mara.Id) == ptWp1,
            "NPC moves to waypoint 1 after one patrol interval");

        // Advance another interval → waypoint 2
        patrolServer.AdvanceIdleTicks(AuthoritativeWorldServer.NpcPatrolTickInterval);
        ExpectTrue(patrolServer.GetNpcPosition(StarterNpcs.Mara.Id) == ptWp2,
            "NPC moves to waypoint 2 after second patrol interval");

        // Advance another interval → wraps back to waypoint 0
        patrolServer.AdvanceIdleTicks(AuthoritativeWorldServer.NpcPatrolTickInterval);
        ExpectTrue(patrolServer.GetNpcPosition(StarterNpcs.Mara.Id) == ptWp0,
            "NPC wraps back to waypoint 0 after cycling all waypoints");

        // Snapshot reflects the patrol position
        var ptSnapshot = patrolServer.CreateInterestSnapshot("pt_observer");
        var ptNpcSnap = ptSnapshot.Npcs.FirstOrDefault(n => n.Id == StarterNpcs.Mara.Id);
        ExpectTrue(ptNpcSnap is not null, "patrol NPC is included in interest snapshot");
        ExpectTrue(ptNpcSnap?.TileX == ptWp0.X && ptNpcSnap?.TileY == ptWp0.Y,
            "interest snapshot reflects current patrol position");

        // Default patrols seeded at world-gen: starter NPCs cycle through their
        // own waypoints unless overridden. Dallen ships with a 2-tile shop route
        // and Mara with a 3-tile clinic route.
        ExpectTrue(patrolServer.Npcs[StarterNpcs.Dallen.Id].PatrolWaypoints?.Count == 2,
            "Dallen ships with a default 2-tile patrol route");
        ExpectTrue(patrolServer.Npcs[StarterNpcs.Mara.Id].PatrolWaypoints?.Count == 3,
            "Mara ships with a default 3-tile patrol route");

        var driftConfig = new ServerConfig(
            MaxPlayers: 2,
            TargetPlayers: 1,
            Scale: WorldScale.Small,
            TickRate: 20,
            InterestRadiusTiles: 32,
            CombatRangeTiles: 2,
            ChunkSizeTiles: ServerConfig.DefaultChunkSizeTiles,
            MatchDurationSeconds: ServerConfig.DefaultMatchDurationSeconds,
            MatchPhaseDurationTicks: 5);
        var driftState = new GameState();
        driftState.RegisterPlayer("drift_observer", "Observer");
        driftState.SetPlayerPosition("drift_observer", new TilePosition(10, 0));
        var driftServer = new AuthoritativeWorldServer(driftState, "ambient-drift-test", driftConfig);
        driftServer.SeedWorldStructure("drift_smithy", "Smithy", "smithy", TilePosition.Origin);
        driftServer.SeedWorldStructure("drift_tavern", "Tavern", "tavern", new TilePosition(20, 0));
        var driftSmith = new NpcProfile(
            "drift_smith",
            "Drift Smith",
            "Village Blacksmith",
            "steady",
            "Village Freeholders",
            "a hot forge",
            "none",
            Array.Empty<string>(),
            Array.Empty<string>());
        driftServer.AdvanceIdleTicks(25);
        driftServer.SeedNpc(driftSmith, new TilePosition(20, 0));
        var smithyAnchor = TilePosition.Origin;
        var midnightDistance = driftServer.GetNpcPosition("drift_smith").DistanceSquaredTo(smithyAnchor);
        driftServer.AdvanceIdleTicks(15);
        var noonDistance = driftServer.GetNpcPosition("drift_smith").DistanceSquaredTo(smithyAnchor);
        ExpectEqual(MatchPhase.Noon, driftServer.CurrentMatchPhase,
            "ambient drift test reaches Noon after phase transition");
        ExpectTrue(noonDistance < midnightDistance,
            "blacksmith ambient drift moves closer to smithy at Noon than at midnight");

        // ── Step 32: Reputation decay ─────────────────────────────────────────────
        // Faction standings drift toward 0 by 1 per ReputationDecayTickInterval
        // ticks, then stop inside a +/-2 dead band.

        var repState = new GameState();
        repState.RegisterPlayer("rd_player", "Rep Player");
        repState.SetPlayerPosition("rd_player", TilePosition.Origin);
        var repConfig = new ServerConfig(
            MaxPlayers: 1,
            TargetPlayers: 1,
            Scale: WorldScale.Small,
            TickRate: 20,
            InterestRadiusTiles: 24,
            CombatRangeTiles: 2,
            ChunkSizeTiles: ServerConfig.DefaultChunkSizeTiles,
            MatchDurationSeconds: 60,
            ReputationDecayTickInterval: 5);
        var repServer = new AuthoritativeWorldServer(repState, "rep-decay-test", repConfig);
        foreach (var repPid in repServer.ConnectedPlayerIds)
            repServer.ProcessIntent(new ServerIntent(repPid, 1, IntentType.ReadyUp, new Dictionary<string, string>()));

        repState.Factions.Apply(StarterFactions.FreeSettlersId, "rd_player", 50);
        ExpectEqual(50, repState.Factions.GetReputation(StarterFactions.FreeSettlersId, "rd_player"),
            "reputation set to 10 before decay");

        repServer.AdvanceIdleTicks(5);
        ExpectEqual(49, repState.Factions.GetReputation(StarterFactions.FreeSettlersId, "rd_player"),
            "positive reputation decays by 1 per interval");

        repServer.AdvanceIdleTicks(500);
        ExpectEqual(2, repState.Factions.GetReputation(StarterFactions.FreeSettlersId, "rd_player"),
            "positive reputation decay stops at the +2 dead band");

        repState.Factions.Apply(StarterFactions.FreeSettlersId, "rd_player", -8);
        repServer.AdvanceIdleTicks(5);
        ExpectEqual(-5, repState.Factions.GetReputation(StarterFactions.FreeSettlersId, "rd_player"),
            "negative reputation decays toward 0 by 1 per interval");

        repServer.AdvanceIdleTicks(25);
        ExpectEqual(-2, repState.Factions.GetReputation(StarterFactions.FreeSettlersId, "rd_player"),
            "negative reputation decay stops at the -2 dead band");

        // ── Step 33: Faction store gating ────────────────────────────────────────
        // PurchaseItem is rejected when player reputation with RequiredFactionId is
        // below MinReputation.  ShopOfferSnapshot exposes both fields.

        var gateState = new GameState();
        gateState.RegisterPlayer("ag_buyer", "Ag Buyer");
        gateState.SetPlayerPosition("ag_buyer", new TilePosition(6, 4)); // near Dallen
        gateState.AddScrip("ag_buyer", 100);
        var gateServer = new AuthoritativeWorldServer(gateState, "shop-gate-test");
        foreach (var sgPid in gateServer.ConnectedPlayerIds)
            gateServer.ProcessIntent(new ServerIntent(sgPid, 1, IntentType.ReadyUp, new Dictionary<string, string>()));

        // Create a gated offer that requires reputation 20 with Village Freeholders
        var gatedOffer = new ShopOffer(
            "sg_gated_offer",
            StarterNpcs.Dallen.Id,
            StarterItems.MediPatchId,
            12,
            RequiredFactionId: StarterFactions.FreeSettlersId,
            MinReputation: 20);

        // Inject the offer into the catalog (register it via a shim on gateServer)
        // We test the snapshot MinReputation field using existing ungated Dallen offer
        var sgDallenSnap = gateServer.CreateInterestSnapshot("ag_buyer")
            .ShopOffers.FirstOrDefault(o => o.OfferId == StarterShopCatalog.DallenMediPatchOfferId);
        ExpectTrue(sgDallenSnap is not null, "shop offer appears in snapshot near vendor NPC");
        ExpectEqual(0, sgDallenSnap?.MinReputation ?? -1, "standard offer has MinReputation of 0");

        // Register the gated offer so PurchaseItem intents can reference it
        gateServer.SeedShopOffer(gatedOffer);

        var sgNoRepResult = gateServer.ProcessIntent(new ServerIntent("ag_buyer", 2, IntentType.PurchaseItem,
            new Dictionary<string, string> { ["offerId"] = gatedOffer.Id }));
        ExpectTrue(!sgNoRepResult.WasAccepted, "PurchaseItem rejected when player has no faction reputation");
        ExpectTrue(
            sgNoRepResult.Event.Data["reason"].Contains("Village Freeholders won't sell to you yet (need rep ≥ 20, you're at 0)"),
            "faction-gated purchase rejection names the faction and required reputation");
        ExpectTrue(
            HudController.FormatLatestServerEvent(new[] { sgNoRepResult.Event }).Contains("Village Freeholders won't sell"),
            "HUD rejected-intent text surfaces faction store denial");
        var gatedOfferSnapshot = new ShopOfferSnapshot(
            gatedOffer.Id,
            gatedOffer.VendorNpcId,
            gatedOffer.ItemId,
            StarterItems.MediPatch.Name,
            StarterItems.MediPatch.Category,
            gatedOffer.Price,
            gatedOffer.Currency,
            gatedOffer.RequiredFactionId,
            gatedOffer.MinReputation);
        ExpectTrue(HudController.IsShopOfferFactionLocked(gatedOfferSnapshot, 0), "HUD marks faction-gated shop rows locked below required reputation");
        ExpectFalse(HudController.IsShopOfferFactionLocked(gatedOfferSnapshot, 20), "HUD unlocks faction-gated shop rows at required reputation");
        ExpectEqual(
            "Village Freeholders won't sell to you yet (need rep ≥ 20, you're at 0)",
            HudController.FormatFactionStoreDenial(StarterFactions.FreeSettlersId, 20, 0),
            "HUD formats faction store denial prompt");

        gateState.Factions.Apply(StarterFactions.FreeSettlersId, "ag_buyer", 20);
        var sgWithRepResult = gateServer.ProcessIntent(new ServerIntent("ag_buyer", 3, IntentType.PurchaseItem,
            new Dictionary<string, string> { ["offerId"] = gatedOffer.Id }));
        ExpectTrue(sgWithRepResult.WasAccepted, "PurchaseItem accepted when player meets MinReputation");
        gateState.Relationships.Apply(StarterNpcs.Dallen.Id, "ag_buyer", 25);
        var friendlyShopOffer = gateServer.CreateInterestSnapshot("ag_buyer")
            .ShopOffers.First(offer => offer.OfferId == StarterShopCatalog.DallenRepairKitOfferId);
        ExpectTrue(friendlyShopOffer.PricingBreakdown.Contains("-10% (Friendly)"),
            "shop offer snapshot carries relationship pricing breakdown");
        ExpectTrue(friendlyShopOffer.Price < friendlyShopOffer.BasePrice,
            "friendly vendor relationship lowers the visible shop price");

        var cleanShopState = new GameState();
        cleanShopState.RegisterPlayer("ah_clean_buyer", "Clean Buyer");
        cleanShopState.RegisterPlayer("ai_dirty_buyer", "Dirty Buyer");
        cleanShopState.RegisterPlayer("aj_filthy_buyer", "Filthy Buyer");
        cleanShopState.SetPlayerPosition("ah_clean_buyer", new TilePosition(6, 4));
        cleanShopState.SetPlayerPosition("ai_dirty_buyer", new TilePosition(6, 4));
        cleanShopState.SetPlayerPosition("aj_filthy_buyer", new TilePosition(6, 4));
        cleanShopState.AddScrip("ah_clean_buyer", 100);
        cleanShopState.AddScrip("ai_dirty_buyer", 100);
        cleanShopState.AddScrip("aj_filthy_buyer", 100);
        cleanShopState.Players["ai_dirty_buyer"].SpendCleanliness(75);
        cleanShopState.Players["aj_filthy_buyer"].SpendCleanliness(100);
        var cleanShopServer = new AuthoritativeWorldServer(cleanShopState, "clean-shop-test");
        var cleanShopSeq = 1;
        foreach (var csPid in cleanShopServer.ConnectedPlayerIds)
            cleanShopServer.ProcessIntent(new ServerIntent(csPid, cleanShopSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        var cleanShopBuy = cleanShopServer.ProcessIntent(new ServerIntent("ah_clean_buyer", cleanShopSeq++,
            IntentType.PurchaseItem,
            new Dictionary<string, string> { ["offerId"] = StarterShopCatalog.DallenWhoopieCushionOfferId }));
        ExpectTrue(cleanShopBuy.WasAccepted,
            "Clean player can buy from a shopkeeper at normal price");
        ExpectEqual("7", cleanShopBuy.Event.Data["price"],
            "Clean shop purchase keeps the normal final price");

        var dirtyOffer = cleanShopServer.CreateInterestSnapshot("ai_dirty_buyer").ShopOffers
            .FirstOrDefault(offer => offer.OfferId == StarterShopCatalog.DallenWhoopieCushionOfferId);
        ExpectEqual(9, dirtyOffer?.Price ?? 0,
            "Dirty shop snapshot shows the cleanliness price markup");
        var dirtyScripBefore = cleanShopState.Players["ai_dirty_buyer"].Scrip;
        var dirtyShopBuy = cleanShopServer.ProcessIntent(new ServerIntent("ai_dirty_buyer", cleanShopSeq++,
            IntentType.PurchaseItem,
            new Dictionary<string, string> { ["offerId"] = StarterShopCatalog.DallenWhoopieCushionOfferId }));
        ExpectTrue(dirtyShopBuy.WasAccepted,
            "Dirty player can buy from a shopkeeper with a markup");
        ExpectEqual("9", dirtyShopBuy.Event.Data["price"],
            "Dirty shop purchase applies the cleanliness markup to final price");
        ExpectEqual(dirtyScripBefore - 9, cleanShopState.Players["ai_dirty_buyer"].Scrip,
            "Dirty shop purchase debits the marked-up cost");

        var filthyShopBuy = cleanShopServer.ProcessIntent(new ServerIntent("aj_filthy_buyer", cleanShopSeq++,
            IntentType.PurchaseItem,
            new Dictionary<string, string> { ["offerId"] = StarterShopCatalog.DallenWhoopieCushionOfferId }));
        ExpectFalse(filthyShopBuy.WasAccepted,
            "Filthy player is refused by shopkeepers");
        ExpectEqual("You reek. Wash, then come back.", filthyShopBuy.RejectionReason,
            "Filthy shop rejection explains cleanliness refusal");

        // ── Step 34: Station claim intent ─────────────────────────────────────────
        // A player in a posse can ClaimStation to flag a structure.  Posse members
        // earn ClaimScripPerTick passive scrip per AdvanceIdleTicks tick.  A second
        // claim by a different posse is rejected.

        var scState = new GameState();
        scState.RegisterPlayer("ac_leader", "Claim Leader");
        scState.RegisterPlayer("ac_member", "Claim Member");
        scState.RegisterPlayer("ac_rival", "Claim Rival");
        scState.SetPlayerPosition("ac_leader", TilePosition.Origin);
        scState.SetPlayerPosition("ac_member", TilePosition.Origin);
        scState.SetPlayerPosition("ac_rival", TilePosition.Origin);
        var scServer = new AuthoritativeWorldServer(scState, "station-claim-test");

        // Form posse: invite + accept
        var scSeq = 1;
        foreach (var scPid in scServer.ConnectedPlayerIds)
            scServer.ProcessIntent(new ServerIntent(scPid, scSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        scServer.ProcessIntent(new ServerIntent("ac_leader", scSeq++, IntentType.InvitePosse,
            new Dictionary<string, string> { ["targetPlayerId"] = "ac_member" }));
        scServer.ProcessIntent(new ServerIntent("ac_member", scSeq++, IntentType.AcceptPosse,
            new Dictionary<string, string>()));

        // Form rival single-member posse (self-invite won't work; make rival its own team by the invite path)
        // Just give rival its own posse by using a secondary path:
        // Actually rival can't invite themselves; test just verifies cross-posse rejection uses ac_leader's posse.
        // We'll skip forming rival's posse and just test no-posse rejection:
        var scNoposseResult = scServer.ProcessIntent(new ServerIntent("ac_rival", scSeq++, IntentType.ClaimStation,
            new Dictionary<string, string> { ["entityId"] = "clinic_npc_station" }));
        ExpectTrue(!scNoposseResult.WasAccepted, "ClaimStation rejected when player has no posse");

        // Seed a claimable structure and have the leader claim it
        scServer.SeedWorldStructure("sc_outpost", "Outpost", "outpost", TilePosition.Origin);
        var scClaimResult = scServer.ProcessIntent(new ServerIntent("ac_leader", scSeq++, IntentType.ClaimStation,
            new Dictionary<string, string> { ["entityId"] = "sc_outpost" }));
        ExpectTrue(scClaimResult.WasAccepted, "ClaimStation accepted for posse member near unclaimed structure");
        ExpectTrue(scServer.WorldStructures["sc_outpost"].ClaimingPosseId == scState.Players["ac_leader"].TeamId,
            "structure ClaimingPosseId is set to claiming posse");
        ExpectTrue(scServer.EventLog.Any(e => e.EventId.Contains("station_claimed")),
            "station_claimed event emitted on successful claim");

        // Passive scrip: advance ticks and verify posse members earned scrip
        var scripBefore = scState.Players["ac_leader"].Scrip;
        scServer.AdvanceIdleTicks(3);
        ExpectTrue(scState.Players["ac_leader"].Scrip == scripBefore + AuthoritativeWorldServer.ClaimScripPerTick * 3,
            "station owner earns ClaimScripPerTick scrip per idle tick");

        // Snapshot shows ClaimingPosseId
        var scSnapshot = scServer.CreateInterestSnapshot("ac_leader");
        var scStructureSnap = scSnapshot.Structures.FirstOrDefault(s => s.EntityId == "sc_outpost");
        ExpectTrue(scStructureSnap?.ClaimingPosseId == scState.Players["ac_leader"].TeamId,
            "interest snapshot includes ClaimingPosseId on claimed structure");

        // ── Step 35: Death trophy drop ────────────────────────────────────────────
        // When a player downs another and that player Karma Breaks, the scorer
        // receives a unique trophy item named after the victim ("X's Dog Tag").
        // The event "trophy_drop" is emitted at the moment of the Karma Break.

        var trState = new GameState();
        trState.RegisterPlayer("ab_hunter", "Hunter");
        trState.RegisterPlayer("ab_prey", "Prey");
        trState.SetPlayerPosition("ab_hunter", TilePosition.Origin);
        trState.SetPlayerPosition("ab_prey", TilePosition.Origin);
        var trServer = new AuthoritativeWorldServer(trState, "trophy-drop-test");
        var trSeq = 1;
        foreach (var trPid in trServer.ConnectedPlayerIds)
            trServer.ProcessIntent(new ServerIntent(trPid, trSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        // Hunter attacks Prey until Prey is downed
        while (!trState.Players["ab_prey"].IsDown)
        {
            trServer.ProcessIntent(new ServerIntent("ab_hunter", trSeq++, IntentType.Attack,
                new Dictionary<string, string> { ["targetId"] = "ab_prey" }));
        }
        ExpectTrue(trState.Players["ab_prey"].IsDown, "trophy test: Prey is downed before Karma Break");

        // Advance countdown to trigger Karma Break (FinalizeExpiredDownedPlayers)
        trServer.AdvanceIdleTicks(AuthoritativeWorldServer.DownedCountdownTicks + 1);
        ExpectTrue(trServer.EventLog.Any(e => e.EventId.Contains("trophy_drop")),
            "trophy_drop event fires when downed player Karma Breaks");
        ExpectTrue(trState.Players["ab_hunter"].Inventory.Any(item => item.Name.Contains("Prey")),
            "scorer receives a trophy item named after the victim");
        ExpectTrue(trState.Players["ab_hunter"].Inventory.Any(item => item.Name.Contains("Dog Tag")),
            "trophy item name includes Dog Tag");
        // Trophy id encodes victim id + tick so duplicate display names cannot
        // collide on the same id.
        var trophyItem = trState.Players["ab_hunter"].Inventory.First(item => item.Name.Contains("Dog Tag"));
        ExpectTrue(trophyItem.Id.Contains("ab_prey"),
            "trophy item id includes the victim's player id");
        ExpectTrue(trophyItem.Id.Length > "trophy_ab_prey_".Length,
            "trophy item id includes a tick suffix beyond just victim id");

        // ── Step 36: Crafting intent ──────────────────────────────────────────────
        // CraftItem consumes ingredients and produces a recipe output, validated
        // against a workshop structure in range.

        var crState = new GameState();
        crState.RegisterPlayer("aa_crafter", "Crafter");
        // Far from default starter workbench (which sits near (12,5)) so the
        // "no workshop in range" branch can be exercised cleanly.
        crState.SetPlayerPosition("aa_crafter", new TilePosition(120, 120));
        crState.AddItem("aa_crafter", StarterItems.MultiTool);
        crState.AddItem("aa_crafter", StarterItems.DataChip);
        var crServer = new AuthoritativeWorldServer(crState, "craft-test");
        var crSeq = 1;
        foreach (var crPid in crServer.ConnectedPlayerIds)
            crServer.ProcessIntent(new ServerIntent(crPid, crSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        // Without workshop nearby — rejected
        var crNoWorkshop = crServer.ProcessIntent(new ServerIntent("aa_crafter", crSeq++, IntentType.CraftItem,
            new Dictionary<string, string> { ["recipeId"] = StarterRecipes.RepairKitFromPartsId }));
        ExpectTrue(!crNoWorkshop.WasAccepted, "CraftItem rejected when no workshop is in range");

        // Seed a workshop structure adjacent to the player's far-away position
        crServer.SeedWorldStructure("cr_workshop", "Workbench", "workshop", new TilePosition(120, 120));

        // Without ingredients — first eat MultiTool to be missing it
        var crWithIngredients = crServer.ProcessIntent(new ServerIntent("aa_crafter", crSeq++, IntentType.CraftItem,
            new Dictionary<string, string> { ["recipeId"] = StarterRecipes.RepairKitFromPartsId }));
        ExpectTrue(crWithIngredients.WasAccepted, "CraftItem accepted near workshop with all ingredients");
        ExpectTrue(crState.Players["aa_crafter"].Inventory.Any(i => i.Id == StarterItems.RepairKitId),
            "crafted output item appears in player inventory");
        ExpectTrue(!crState.Players["aa_crafter"].Inventory.Any(i => i.Id == StarterItems.MultiToolId),
            "crafting consumes the MultiTool ingredient");
        ExpectTrue(!crState.Players["aa_crafter"].Inventory.Any(i => i.Id == StarterItems.DataChipId),
            "crafting consumes the DataChip ingredient");
        ExpectTrue(crServer.EventLog.Any(e => e.EventId.Contains("item_crafted")),
            "item_crafted event fires on successful craft");

        // Missing ingredients now (already consumed)
        var crNoIngredients = crServer.ProcessIntent(new ServerIntent("aa_crafter", crSeq++, IntentType.CraftItem,
            new Dictionary<string, string> { ["recipeId"] = StarterRecipes.RepairKitFromPartsId }));
        ExpectTrue(!crNoIngredients.WasAccepted, "CraftItem rejected when ingredients are missing");

        // Unknown recipe
        var crBadRecipe = crServer.ProcessIntent(new ServerIntent("aa_crafter", crSeq++, IntentType.CraftItem,
            new Dictionary<string, string> { ["recipeId"] = "nonexistent_recipe" }));
        ExpectTrue(!crBadRecipe.WasAccepted, "CraftItem rejected for unknown recipe id");

        // Recipe table coverage: verify the expanded recipe set includes the
        // new ammo / weapon / utility recipes and they all resolve.
        var recipeIds = new[]
        {
            StarterRecipes.BallisticRoundFromScrapId,
            StarterRecipes.EnergyCellFromPowerCellId,
            StarterRecipes.FlashlightFromTerminalId,
            StarterRecipes.StunBatonFromStickId,
            StarterRecipes.GrapplingHookFromToolsId,
            StarterRecipes.ContrabandPackageFromFlowerId,
            StarterRecipes.LongSwordFromIronId,
            StarterRecipes.ShortBowFromYewId,
            StarterRecipes.HealingTinctureFromHerbsId,
            StarterRecipes.LockpickSetFromWireId
        };
        foreach (var rid in recipeIds)
        {
            ExpectTrue(StarterRecipes.TryGet(rid, out var r),
                $"recipe {rid} is registered in StarterRecipes.All");
            ExpectTrue(r.IngredientItemIds.Count > 0,
                $"recipe {rid} has at least one ingredient");
            ExpectTrue(StarterItems.TryGetById(r.OutputItemId, out _),
                $"recipe {rid} output item resolves via StarterItems");
            foreach (var ingredient in r.IngredientItemIds)
                ExpectTrue(StarterItems.TryGetById(ingredient, out _),
                    $"recipe {rid} ingredient {ingredient} resolves via StarterItems");
        }
        ExpectTrue(StarterRecipes.All
                .First(recipe => recipe.Id == StarterRecipes.BallisticRoundFromScrapId)
                .IngredientItemIds.Contains(StarterItems.ApologyFlowerId),
            "medieval arrow recipe includes a feather stand-in ingredient");
        ExpectTrue(StarterRecipes.All
                .First(recipe => recipe.Id == StarterRecipes.FlashlightFromTerminalId)
                .IngredientItemIds.Contains(StarterItems.WorkVestId),
            "medieval torch recipe includes a cloth stand-in ingredient");
        ExpectEqual(12, StarterRecipes.All.Count,
            "starter recipe set has medieval base recipes plus 4 new entries");

        // Starter quest pack coverage: the expanded medieval quests keep their
        // giver ids tied to real themed NPC roster entries.
        var questTheme = ThemeDataCatalog.Get("medieval");
        var medievalQuestIds = new[]
        {
            StarterQuests.GarrickBladeOrderId,
            StarterQuests.MeriCellarStockId,
            StarterQuests.CaldenChapelTitheId,
            StarterQuests.WaceBarracksWatchId,
            StarterQuests.YsoltHerbalRemedyId
        };
        foreach (var questId in medievalQuestIds)
        {
            var quest = StarterQuests.All.First(definition => definition.Id == questId);
            ExpectTrue(questTheme.NpcRoster.ContainsKey(quest.GiverNpcId),
                $"medieval quest {questId} giver resolves to theme NPC roster");
            ExpectTrue(quest.RequiredItemIds.Count > 0,
                $"medieval quest {questId} requires at least one item");
            foreach (var requiredItemId in quest.RequiredItemIds)
                ExpectTrue(StarterItems.TryGetById(requiredItemId, out _),
                    $"medieval quest {questId} required item {requiredItemId} resolves via StarterItems");
        }
        ExpectEqual(6, StarterQuests.All.Count,
            "starter quest set has original clinic quest plus medieval quest pack");

        // ── Step 37: Posse shared quest module ────────────────────────────────────
        // PosseQuestModule produces a multi-step quest assigned to a posse.
        // AwardPosseQuestBonus pays PosseQuestBonusScrip to all connected members.

        var pqState = new GameState();
        pqState.RegisterPlayer("aa_lead", "Posse Lead");
        pqState.RegisterPlayer("ab_buddy", "Posse Buddy");
        pqState.SetPlayerPosition("aa_lead", TilePosition.Origin);
        pqState.SetPlayerPosition("ab_buddy", TilePosition.Origin);
        var pqServer = new AuthoritativeWorldServer(pqState, "posse-quest-test");
        var pqSeq = 1;
        foreach (var pqPid in pqServer.ConnectedPlayerIds)
            pqServer.ProcessIntent(new ServerIntent(pqPid, pqSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        // Form posse
        pqServer.ProcessIntent(new ServerIntent("aa_lead", pqSeq++, IntentType.InvitePosse,
            new Dictionary<string, string> { ["targetPlayerId"] = "ab_buddy" }));
        pqServer.ProcessIntent(new ServerIntent("ab_buddy", pqSeq++, IntentType.AcceptPosse,
            new Dictionary<string, string>()));

        // Cannot start a posse quest without a posse (use a third player not in any team)
        var pqOrphanResult = pqServer.StartPosseQuest("local_player", "pq_orphan_quest");
        ExpectTrue(string.IsNullOrEmpty(pqOrphanResult), "StartPosseQuest returns empty when player has no posse");

        // Start a posse quest as the lead
        var pqPosseId = pqServer.StartPosseQuest("aa_lead", "pq_test_quest");
        ExpectTrue(!string.IsNullOrEmpty(pqPosseId), "StartPosseQuest returns the posse id when player is in a posse");
        ExpectTrue(pqState.Quests.Quests.ContainsKey("pq_test_quest"),
            "posse quest is registered in GameState.Quests");
        ExpectEqual(pqPosseId, pqServer.GetPosseQuestOwner("pq_test_quest"),
            "posse quest owner is recorded");
        ExpectTrue(pqServer.EventLog.Any(e => e.EventId.Contains("posse_quest_started")),
            "posse_quest_started event fires when a posse quest is started");

        // Check: the quest definition has multi-step structure (two steps)
        var pqQuestDef = pqState.Quests.Get("pq_test_quest").Definition;
        ExpectTrue(pqQuestDef.Steps != null && pqQuestDef.Steps.Count == 2,
            "PosseQuestModule produces a 2-step quest");

        // Award bonus and verify each posse member gets scrip
        var pqLeadScripBefore = pqState.Players["aa_lead"].Scrip;
        var pqBuddyScripBefore = pqState.Players["ab_buddy"].Scrip;
        pqServer.AwardPosseQuestBonus("pq_test_quest");
        ExpectEqual(pqLeadScripBefore + AuthoritativeWorldServer.PosseQuestBonusScrip,
            pqState.Players["aa_lead"].Scrip,
            "posse quest bonus paid to leader");
        ExpectEqual(pqBuddyScripBefore + AuthoritativeWorldServer.PosseQuestBonusScrip,
            pqState.Players["ab_buddy"].Scrip,
            "posse quest bonus paid to member");
        ExpectTrue(pqServer.EventLog.Any(e => e.EventId.Contains("posse_quest_completed")),
            "posse_quest_completed event fires on bonus award");
        ExpectEqual(string.Empty, pqServer.GetPosseQuestOwner("pq_test_quest"),
            "posse quest owner is cleared after bonus is awarded");

        // StartPosseQuest intent path: a player in a posse can trigger a quest
        // via IntentType.StartPosseQuest. Without a posse, intent is rejected.
        var pqiSeq = 100;
        var pqiNoPosse = pqServer.ProcessIntent(new ServerIntent("local_player", pqiSeq++,
            IntentType.StartPosseQuest,
            new Dictionary<string, string> { ["questId"] = "pq_intent_quest_orphan" }));
        ExpectFalse(pqiNoPosse.WasAccepted, "StartPosseQuest intent rejected when player has no posse");

        var pqiOk = pqServer.ProcessIntent(new ServerIntent("aa_lead", pqiSeq++,
            IntentType.StartPosseQuest,
            new Dictionary<string, string> { ["questId"] = "pq_intent_quest_real" }));
        ExpectTrue(pqiOk.WasAccepted, "StartPosseQuest intent accepted for player in a posse");
        ExpectTrue(pqState.Quests.Quests.ContainsKey("pq_intent_quest_real"),
            "StartPosseQuest intent registers the quest in GameState.Quests");
        ExpectTrue(pqServer.EventLog.Any(e => e.EventId.Contains("player_started_posse_quest")),
            "player_started_posse_quest event fires on intent acceptance");

        var pqiDup = pqServer.ProcessIntent(new ServerIntent("aa_lead", pqiSeq++,
            IntentType.StartPosseQuest,
            new Dictionary<string, string> { ["questId"] = "pq_intent_quest_real" }));
        ExpectFalse(pqiDup.WasAccepted, "StartPosseQuest intent rejected when quest id already exists");

        var pqiNoId = pqServer.ProcessIntent(new ServerIntent("aa_lead", pqiSeq++,
            IntentType.StartPosseQuest,
            new Dictionary<string, string>()));
        ExpectFalse(pqiNoId.WasAccepted, "StartPosseQuest intent rejected when questId payload is missing");

        // ── Step 38: World tier zones (lawless) ───────────────────────────────────
        // Tiles can be marked lawless.  Attacks from a lawless attacker position
        // skip the karma-descent penalty.  Snapshot lists "Lawless Zone" status.

        var lzState = new GameState();
        lzState.RegisterPlayer("aa_outlaw", "Outlaw");
        lzState.RegisterPlayer("ab_target", "Target");
        var lawlessPos = new TilePosition(50, 50);
        lzState.SetPlayerPosition("aa_outlaw", lawlessPos);
        lzState.SetPlayerPosition("ab_target", lawlessPos);
        var lzServer = new AuthoritativeWorldServer(lzState, "lawless-test");
        var lzSeq = 1;
        foreach (var lzPid in lzServer.ConnectedPlayerIds)
            lzServer.ProcessIntent(new ServerIntent(lzPid, lzSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        // Mark the attacker's tile as lawless
        lzServer.MarkTileLawless(lawlessPos);
        ExpectTrue(lzServer.IsTileLawless(lawlessPos), "MarkTileLawless flags a tile as lawless");
        ExpectTrue(lzServer.IsPlayerInLawlessZone("aa_outlaw"),
            "IsPlayerInLawlessZone is true for player on lawless tile");

        var lzKarmaBefore = lzState.Players["aa_outlaw"].Karma.Score;
        lzServer.ProcessIntent(new ServerIntent("aa_outlaw", lzSeq++, IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "ab_target" }));
        ExpectEqual(lzKarmaBefore, lzState.Players["aa_outlaw"].Karma.Score,
            "attack from lawless zone does not decrease karma");

        var lzSnapshot = lzServer.CreateInterestSnapshot("aa_outlaw");
        var lzOutlawSnap = lzSnapshot.Players.FirstOrDefault(p => p.Id == "aa_outlaw");
        ExpectTrue(lzOutlawSnap?.StatusEffects.Any(s => s.Contains("Lawless Zone")) == true,
            "snapshot shows Lawless Zone status for player on lawless tile");

        // Move attacker out of lawless zone — penalty applies again
        lzState.SetPlayerPosition("aa_outlaw", new TilePosition(0, 0));
        lzState.SetPlayerPosition("ab_target", new TilePosition(0, 0));
        lzServer.AdvanceIdleTicks(AuthoritativeWorldServer.AttackCooldownTicks + 1);
        var lzKarmaAfterMove = lzState.Players["aa_outlaw"].Karma.Score;
        lzServer.ProcessIntent(new ServerIntent("aa_outlaw", lzSeq++, IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "ab_target" }));
        ExpectTrue(lzState.Players["aa_outlaw"].Karma.Score < lzKarmaAfterMove,
            "attack outside lawless zone applies normal karma penalty");

        // Lawless zone enter/exit toast: walking onto / off of a lawless tile
        // emits entered_lawless_zone / left_lawless_zone events.
        var ltState = new GameState();
        ltState.RegisterPlayer("aa_walker", "Walker");
        ltState.SetPlayerPosition("aa_walker", new TilePosition(0, 0));
        var ltServer = new AuthoritativeWorldServer(ltState, "lawless-toast-test");
        var ltSeq = 1;
        foreach (var ltPid in ltServer.ConnectedPlayerIds)
            ltServer.ProcessIntent(new ServerIntent(ltPid, ltSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        var ltLawlessTile = new TilePosition(40, 40);
        ltServer.MarkTileLawless(ltLawlessTile);

        // First crossing: enter the lawless tile -> entered event
        ltServer.ProcessIntent(new ServerIntent("aa_walker", ltSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "40", ["y"] = "40" }));
        ExpectTrue(ltServer.EventLog.Any(e => e.EventId.Contains("entered_lawless_zone")),
            "entered_lawless_zone event fires on first move into a lawless tile");

        // Walking on another lawless tile (not yet flagged) is a no-op for the toast
        ltServer.MarkTileLawless(new TilePosition(40, 41));
        ltServer.ProcessIntent(new ServerIntent("aa_walker", ltSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "40", ["y"] = "41" }));
        var enteredCount = ltServer.EventLog.Count(e => e.EventId.Contains("entered_lawless_zone"));
        ExpectEqual(1, enteredCount,
            "moving lawless-to-lawless does not emit a redundant entered_lawless_zone");

        // Step out: emits left event
        ltServer.ProcessIntent(new ServerIntent("aa_walker", ltSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "39", ["y"] = "39" }));
        ExpectTrue(ltServer.EventLog.Any(e => e.EventId.Contains("left_lawless_zone")),
            "left_lawless_zone event fires on stepping off a lawless tile");

        // ── Step 39: Fog of war ───────────────────────────────────────────────────
        // Server tracks visited chunks per player; CreateInterestSnapshot only
        // includes chunks the player has visited (via FogOfWarMinimumRevealRadius).

        var fwState = new GameState();
        fwState.RegisterPlayer("aa_explorer", "Explorer");
        fwState.SetPlayerPosition("aa_explorer", new TilePosition(5, 5));
        var fwServer = new AuthoritativeWorldServer(fwState, "fog-of-war-test");
        var fwWorld = WorldGenerator.Generate(WorldConfig.FromServerConfig(
            "fog-of-war-test",
            new WorldSeed(99, "Fog Test", "test"),
            ServerConfig.Prototype4Player));
        fwServer.SetTileMap(fwWorld.TileMap);
        var fwSeq = 1;
        foreach (var fwPid in fwServer.ConnectedPlayerIds)
            fwServer.ProcessIntent(new ServerIntent(fwPid, fwSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        // Take a snapshot — the player's current chunk should be marked visited
        var fwSnap1 = fwServer.CreateInterestSnapshot("aa_explorer");
        var fwVisited1Count = fwServer.GetVisitedChunks("aa_explorer").Count;
        var fwVisited1Snapshot = fwServer.GetVisitedChunks("aa_explorer").ToArray();
        ExpectTrue(fwVisited1Count > 0, "fog of war records visited chunks after first snapshot");

        // The visited set includes the player's chunk + reveal radius chunks
        var fwHere = fwServer.GetChunkForTile(new TilePosition(5, 5));
        ExpectTrue(fwVisited1Snapshot.Contains((fwHere.ChunkX, fwHere.ChunkY)),
            "player's current chunk is in the visited set");

        // Move player to a different chunk (within the small map's bounds)
        fwState.SetPlayerPosition("aa_explorer", new TilePosition(45, 45));
        fwServer.CreateInterestSnapshot("aa_explorer");
        var fwVisited2Count = fwServer.GetVisitedChunks("aa_explorer").Count;
        ExpectTrue(fwVisited2Count > fwVisited1Count,
            "moving to new area expands the visited chunks set");
        var fwVisited2 = fwServer.GetVisitedChunks("aa_explorer").ToArray();

        // Snapshot only includes visited chunks (not all chunks within radius)
        var fwSnap2 = fwServer.CreateInterestSnapshot("aa_explorer");
        ExpectTrue(fwSnap2.MapChunks.Count > 0, "fog-of-war snapshot still has at least the local chunk");
        var fwVisitedSet = fwVisited2.Select(v => $"{v.X}:{v.Y}").ToHashSet();
        ExpectTrue(fwSnap2.MapChunks.All(chunk => fwVisitedSet.Contains(chunk.ChunkKey)),
            "all chunks in fog-of-war snapshot have been visited");

        // ── Step 40: HUD minimap ──────────────────────────────────────────────────
        // FormatMinimap renders a small radar panel from the interest snapshot.
        // '@' marks the local player, 'P' = other players, 'N' = NPCs, 'S' = structures.

        var mmState = new GameState();
        mmState.RegisterPlayer("aa_observer", "Observer");
        mmState.RegisterPlayer("ab_friend", "Friend");
        mmState.SetPlayerPosition("aa_observer", new TilePosition(5, 5));
        mmState.SetPlayerPosition("ab_friend", new TilePosition(7, 6));
        var mmServer = new AuthoritativeWorldServer(mmState, "minimap-test");
        foreach (var mmPid in mmServer.ConnectedPlayerIds)
            mmServer.ProcessIntent(new ServerIntent(mmPid, 1, IntentType.ReadyUp, new Dictionary<string, string>()));
        mmServer.SeedWorldStructure("mm_clinic", "Clinic", "clinic", new TilePosition(4, 4));

        var mmSnapshot = mmServer.CreateInterestSnapshot("aa_observer");
        var mmMap = HudController.FormatMinimap(mmSnapshot, "aa_observer", radiusTiles: 5);

        ExpectTrue(mmMap.Contains('@'), "minimap shows local player marker '@'");
        ExpectTrue(mmMap.Contains('P'), "minimap shows other player marker 'P'");
        ExpectTrue(mmMap.Contains('N'), "minimap shows NPC marker 'N'");
        ExpectTrue(mmMap.Contains('S'), "minimap shows structure marker 'S'");

        // 11x11 grid (radiusTiles=5) means 11 rows separated by newlines = 10 newlines
        var mmRows = mmMap.Split('\n');
        ExpectEqual(11, mmRows.Length, "minimap renders (radius*2+1) rows");
        ExpectTrue(mmRows.All(r => r.Length == 11), "minimap rows are (radius*2+1) chars wide");

        // Unavailable case
        var mmEmpty = HudController.FormatMinimap(mmSnapshot, "nonexistent_player");
        ExpectTrue(mmEmpty.Contains("unavailable"), "minimap returns unavailable when local player is missing");

        // ── Shop UX upgrade: SellItem intent + browse/sell dialogue choices ───────
        // Vendor NPCs offer "Browse wares" and "Sell items" dialogue choices.
        // SellItem consumes an inventory item and pays the player back at 50% of
        // the matching shop offer price (or a flat fallback if not in catalog).

        var shState = new GameState();
        shState.RegisterPlayer("aa_seller", "Seller");
        shState.SetPlayerPosition("aa_seller", new TilePosition(6, 4)); // near Dallen
        shState.AddItem("aa_seller", StarterItems.RepairKit);
        shState.AddItem("aa_seller", StarterItems.WhoopieCushion);
        var shServer = new AuthoritativeWorldServer(shState, "shop-ux-test");
        var shSeq = 1;
        foreach (var shPid in shServer.ConnectedPlayerIds)
            shServer.ProcessIntent(new ServerIntent(shPid, shSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        var shScripBefore = shState.Players["aa_seller"].Scrip;
        var shSell1 = shServer.ProcessIntent(new ServerIntent("aa_seller", shSeq++, IntentType.SellItem,
            new Dictionary<string, string>
            {
                ["itemId"] = StarterItems.RepairKitId,
                ["vendorNpcId"] = StarterNpcs.Dallen.Id
            }));
        ExpectTrue(shSell1.WasAccepted, "SellItem accepted near vendor with matching item");
        ExpectTrue(!shState.Players["aa_seller"].Inventory.Any(i => i.Id == StarterItems.RepairKitId),
            "selling consumes the item from inventory");
        // Repair kit shop price is 18 → sell at 50% = 9
        ExpectEqual(shScripBefore + 9, shState.Players["aa_seller"].Scrip,
            "selling pays player 50% of the catalog price");
        ExpectTrue(shServer.EventLog.Any(e => e.EventId.Contains("item_sold")),
            "item_sold event fires on successful sale");

        // Sell unlisted item — uses fallback price
        var shScripBefore2 = shState.Players["aa_seller"].Scrip;
        var shSell2 = shServer.ProcessIntent(new ServerIntent("aa_seller", shSeq++, IntentType.SellItem,
            new Dictionary<string, string>
            {
                ["itemId"] = StarterItems.WhoopieCushionId,
                ["vendorNpcId"] = StarterNpcs.Dallen.Id
            }));
        ExpectTrue(shSell2.WasAccepted, "SellItem accepted for catalogued item");
        // Whoopie cushion shop price is 7 → sell at 50% = 3 (max of 1, 7*50/100=3)
        ExpectEqual(shScripBefore2 + 3, shState.Players["aa_seller"].Scrip,
            "low-priced sale rounds down to floor of 50%");

        // Sell rejected when player not near vendor
        shState.SetPlayerPosition("aa_seller", new TilePosition(60, 60));
        shState.AddItem("aa_seller", StarterItems.RationPack);
        var shFar = shServer.ProcessIntent(new ServerIntent("aa_seller", shSeq++, IntentType.SellItem,
            new Dictionary<string, string>
            {
                ["itemId"] = StarterItems.RationPackId,
                ["vendorNpcId"] = StarterNpcs.Dallen.Id
            }));
        ExpectTrue(!shFar.WasAccepted, "SellItem rejected when player is out of range");

        // Dialogue choices include browse/sell for vendor NPCs (Dallen has shop offers)
        var shDallenDialogue = shServer.GetDialogueFor("aa_seller", StarterNpcs.Dallen.Id);
        // (player is now far so this is just to confirm the choice list shape)
        var shAnyDallen = shDallenDialogue.Choices.Any(c => c.Id == "browse_wares");
        // Player is far — no choices may be returned. Test with closer position
        shState.SetPlayerPosition("aa_seller", new TilePosition(6, 4));
        var shCloseDialogue = shServer.GetDialogueFor("aa_seller", StarterNpcs.Dallen.Id);
        ExpectTrue(shCloseDialogue.Choices.Any(c => c.Id == "browse_wares"),
            "vendor NPC dialogue includes 'browse_wares' choice");
        ExpectTrue(shCloseDialogue.Choices.Any(c => c.Id == "sell_items"),
            "vendor NPC dialogue includes 'sell_items' choice");

        // HUD shop bubble formatting
        var shSnapshot = shServer.CreateInterestSnapshot("aa_seller");
        var shBubble = HudController.FormatShopBubble(shSnapshot.ShopOffers, StarterNpcs.Dallen.Id, 100);
        ExpectTrue(shBubble.Contains("Wares"), "FormatShopBubble shows the wares header");
        ExpectTrue(shBubble.Contains("Scrip:"), "FormatShopBubble shows the player's scrip");
        var shSellBubble = HudController.FormatSellBubble(shState.Players["aa_seller"].Inventory, 50);
        ExpectTrue(shSellBubble.Contains("Sell") || shSellBubble.Contains("Nothing"),
            "FormatSellBubble shows a sell or empty header");

        // Attack target feedback: HUD-side helper picks the nearest in-range
        // player and formats a target line for the local readout.
        var atfState = new GameState();
        atfState.RegisterPlayer("aa_observer_atf", "Watcher");
        atfState.RegisterPlayer("ab_close_atf", "Close");
        atfState.RegisterPlayer("ac_far_atf", "Far");
        atfState.SetPlayerPosition("aa_observer_atf", new TilePosition(5, 5));
        atfState.SetPlayerPosition("ab_close_atf", new TilePosition(6, 5));
        atfState.SetPlayerPosition("ac_far_atf", new TilePosition(50, 50));
        var atfServer = new AuthoritativeWorldServer(atfState, "attack-target-test");
        foreach (var atfPid in atfServer.ConnectedPlayerIds)
            atfServer.ProcessIntent(new ServerIntent(atfPid, 1, IntentType.ReadyUp, new Dictionary<string, string>()));

        var atfSnap = atfServer.CreateInterestSnapshot("aa_observer_atf");
        var atfTarget = HudController.FindAttackTarget(atfSnap, "aa_observer_atf", combatRangeTiles: 2);
        ExpectTrue(atfTarget?.Id == "ab_close_atf",
            "FindAttackTarget picks the nearest player within combat range");

        var atfFarTarget = HudController.FindAttackTarget(atfSnap, "aa_observer_atf", combatRangeTiles: 0);
        ExpectTrue(atfFarTarget is null,
            "FindAttackTarget returns null when no player is within combat range");

        var atfLine = HudController.FormatAttackTargetLine(atfSnap, "aa_observer_atf", combatRangeTiles: 2);
        ExpectTrue(atfLine.Contains("Close") && atfLine.Contains("HP"),
            "FormatAttackTargetLine names the target and shows HP");

        var atfNoneLine = HudController.FormatAttackTargetLine(atfSnap, "aa_observer_atf", combatRangeTiles: 0);
        ExpectTrue(atfNoneLine.Contains("none in range"),
            "FormatAttackTargetLine reports no target in range");

        // Fog-of-war client helper: chunks within interest radius that are
        // missing from the snapshot are returned as fog candidates.
        var fwClState = new GameState();
        fwClState.RegisterPlayer("aa_fwcl", "FwClient");
        fwClState.SetPlayerPosition("aa_fwcl", new TilePosition(5, 5));
        var fwClServer = new AuthoritativeWorldServer(fwClState, "fog-client-test");
        var fwClWorld = WorldGenerator.Generate(WorldConfig.FromServerConfig(
            "fog-client-test",
            new WorldSeed(1, "FogClient", "test"),
            ServerConfig.Prototype4Player));
        fwClServer.SetTileMap(fwClWorld.TileMap);
        foreach (var fwClPid in fwClServer.ConnectedPlayerIds)
            fwClServer.ProcessIntent(new ServerIntent(fwClPid, 1, IntentType.ReadyUp, new Dictionary<string, string>()));
        var fwClSnap = fwClServer.CreateInterestSnapshot("aa_fwcl");
        var fwClFog = WorldRoot.ComputeFogChunks(fwClSnap, interestRadiusChunks: 2, chunkSizeTiles: 32);
        // Player at chunk (0,0) with radius 2 covers chunks (-2..2)^2 = 25 chunks.
        // Visited chunks include the local + reveal radius (= 9); 25 - 9 = 16 fog candidates.
        ExpectTrue(fwClFog.Count > 0, "ComputeFogChunks returns at least one chunk when interest exceeds visited");
        // Each fog chunk key should NOT be in the snapshot's MapChunks
        var fwClVisibleKeys = fwClSnap.MapChunks.Select(c => c.ChunkKey).ToHashSet();
        foreach (var (cx, cy) in fwClFog)
            ExpectFalse(fwClVisibleKeys.Contains($"{cx}:{cy}"),
                $"fog chunk ({cx},{cy}) is not in the visible MapChunks set");

        // Building interior bounds: walking onto a door enters; movement clamps
        // to the building footprint; only door tiles can exit.
        var biState = new GameState();
        biState.RegisterPlayer("aa_visitor", "Visitor");
        biState.SetPlayerPosition("aa_visitor", TilePosition.Origin);
        var biServer = new AuthoritativeWorldServer(biState, "interior-test");
        var biSeq = 1;
        foreach (var biPid in biServer.ConnectedPlayerIds)
            biServer.ProcessIntent(new ServerIntent(biPid, biSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        biServer.SeedWorldStructure("bi_clinic", "Clinic", "clinic", new TilePosition(10, 10));
        // 4x4 interior at (10,10)-(13,13), door at (10,9) on the north edge
        var biInterior = new BuildingInterior(
            MinX: 10, MinY: 10, Width: 4, Height: 4,
            DoorTiles: new[] { new TilePosition(10, 9) });
        biServer.AssignBuildingInterior("bi_clinic", biInterior);

        // Walk to the door from outside
        var biEnter = biServer.ProcessIntent(new ServerIntent("aa_visitor", biSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "10", ["y"] = "9" }));
        ExpectTrue(biEnter.WasAccepted, "stepping onto a door tile is accepted");
        ExpectTrue(biEnter.Event?.Data.GetValueOrDefault("enteredInterior") == "bi_clinic",
            "stepping onto a door records enteredInterior in the move event");

        // Step into the interior
        var biStep = biServer.ProcessIntent(new ServerIntent("aa_visitor", biSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "11", ["y"] = "11" }));
        ExpectTrue(biStep.WasAccepted, "stepping inside the interior is accepted");

        // Try to step outside the bounds (not via door) — rejected
        var biEscape = biServer.ProcessIntent(new ServerIntent("aa_visitor", biSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "20", ["y"] = "20" }));
        ExpectFalse(biEscape.WasAccepted,
            "movement outside the interior bounds is rejected when not stepping on a door");

        // Step back onto the door — exits
        var biExit = biServer.ProcessIntent(new ServerIntent("aa_visitor", biSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "10", ["y"] = "9" }));
        ExpectTrue(biExit.WasAccepted, "stepping back onto the door from inside exits the interior");
        ExpectTrue(biExit.Event?.Data.GetValueOrDefault("exitedInterior") == "bi_clinic",
            "exiting via door records exitedInterior in the move event");

        // Now far-away move is accepted again (no longer inside)
        var biFreeMove = biServer.ProcessIntent(new ServerIntent("aa_visitor", biSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "20", ["y"] = "20" }));
        ExpectTrue(biFreeMove.WasAccepted, "moves outside any interior are unrestricted");

        // Interior snapshot scoping: outside viewer doesn't see inside player;
        // two players in the same interior see each other; players in different
        // interiors don't.
        var siState = new GameState();
        siState.RegisterPlayer("aa_inside1", "Inside1");
        siState.RegisterPlayer("ab_inside2", "Inside2");
        siState.RegisterPlayer("ac_outside", "Outside");
        siState.SetPlayerPosition("aa_inside1", TilePosition.Origin);
        siState.SetPlayerPosition("ab_inside2", TilePosition.Origin);
        siState.SetPlayerPosition("ac_outside", TilePosition.Origin);
        var siServer = new AuthoritativeWorldServer(siState, "interior-scope-test");
        var siSeq = 1;
        foreach (var siPid in siServer.ConnectedPlayerIds)
            siServer.ProcessIntent(new ServerIntent(siPid, siSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        // Two structures with interiors, doors at adjacent tiles
        siServer.SeedWorldStructure("si_clinic", "Clinic", "clinic", new TilePosition(2, 2));
        siServer.SeedWorldStructure("si_saloon", "Saloon", "saloon", new TilePosition(8, 8));
        siServer.AssignBuildingInterior("si_clinic", new BuildingInterior(2, 2, 2, 2, new[] { new TilePosition(2, 1) }));
        siServer.AssignBuildingInterior("si_saloon", new BuildingInterior(8, 8, 2, 2, new[] { new TilePosition(8, 7) }));
        // Send both inside players through their respective doors (auto-enter)
        siServer.ProcessIntent(new ServerIntent("aa_inside1", siSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "2", ["y"] = "1" }));
        siServer.ProcessIntent(new ServerIntent("aa_inside1", siSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "2", ["y"] = "2" }));
        siServer.ProcessIntent(new ServerIntent("ab_inside2", siSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "8", ["y"] = "7" }));
        siServer.ProcessIntent(new ServerIntent("ab_inside2", siSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "8", ["y"] = "8" }));

        // Outside viewer should NOT see inside players
        var siOutsideSnap = siServer.CreateInterestSnapshot("ac_outside");
        ExpectFalse(siOutsideSnap.Players.Any(p => p.Id == "aa_inside1"),
            "outside viewer cannot see player inside a structure");
        ExpectFalse(siOutsideSnap.Players.Any(p => p.Id == "ab_inside2"),
            "outside viewer cannot see player inside a different structure either");

        // Inside1 should see themself (with InsideStructureId set) but not Inside2
        var siInside1Snap = siServer.CreateInterestSnapshot("aa_inside1");
        var siInside1Self = siInside1Snap.Players.FirstOrDefault(p => p.Id == "aa_inside1");
        ExpectTrue(siInside1Self is not null, "inside player sees themself in their snapshot");
        ExpectEqual("si_clinic", siInside1Self?.InsideStructureId,
            "PlayerSnapshot.InsideStructureId reflects the structure the player is inside");
        ExpectFalse(siInside1Snap.Players.Any(p => p.Id == "ab_inside2"),
            "inside player cannot see another player inside a different structure");
        ExpectFalse(siInside1Snap.Players.Any(p => p.Id == "ac_outside"),
            "inside player cannot see outside players");

        // If we move outside back in via door, both should see each other
        siServer.ProcessIntent(new ServerIntent("ac_outside", siSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "2", ["y"] = "1" }));
        siServer.ProcessIntent(new ServerIntent("ac_outside", siSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "2", ["y"] = "3" }));
        var siInside1AfterJoin = siServer.CreateInterestSnapshot("aa_inside1");
        ExpectTrue(siInside1AfterJoin.Players.Any(p => p.Id == "ac_outside"),
            "two players inside the same interior can see each other");

        // PlayerSnapshot.InsideStructureId is empty for outside players in their own snapshot
        var siOutsideSnap2 = siServer.CreateInterestSnapshot("ab_inside2");
        var siInside2Self = siOutsideSnap2.Players.FirstOrDefault(p => p.Id == "ab_inside2");
        ExpectEqual("si_saloon", siInside2Self?.InsideStructureId,
            "second inside player's InsideStructureId reflects their own structure");

        // door_opened events fire on enter and exit with a direction tag
        ExpectTrue(siServer.EventLog.Any(e => e.EventId.Contains("door_opened") &&
                e.Data.GetValueOrDefault("direction") == "enter"),
            "door_opened event with direction=enter fires on entering an interior");
        // Force an exit and verify the matching event
        siServer.ProcessIntent(new ServerIntent("aa_inside1", siSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "2", ["y"] = "1" }));
        ExpectTrue(siServer.EventLog.Any(e => e.EventId.Contains("door_opened") &&
                e.Data.GetValueOrDefault("direction") == "exit"),
            "door_opened event with direction=exit fires on leaving an interior");

        // NPC residency: an NPC marked as a resident of a structure with their
        // tile inside that structure's bounds is hidden from outside viewers
        // and visible to viewers inside the same structure.
        var nrState = new GameState();
        nrState.RegisterPlayer("aa_inside_nr", "Inside");
        nrState.RegisterPlayer("ab_outside_nr", "Outside");
        nrState.SetPlayerPosition("aa_inside_nr", TilePosition.Origin);
        nrState.SetPlayerPosition("ab_outside_nr", TilePosition.Origin);
        var nrServer = new AuthoritativeWorldServer(nrState, "npc-residency-test");
        var nrSeq = 1;
        foreach (var nrPid in nrServer.ConnectedPlayerIds)
            nrServer.ProcessIntent(new ServerIntent(nrPid, nrSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        // Mara starts at (3,4) by default. Set up a clinic with bounds containing (3,4).
        nrServer.SeedWorldStructure("nr_clinic", "Clinic", "clinic", new TilePosition(2, 2));
        nrServer.AssignBuildingInterior("nr_clinic",
            new BuildingInterior(2, 2, 4, 4, new[] { new TilePosition(2, 1) }));
        nrServer.AssignNpcResidency(StarterNpcs.Mara.Id, "nr_clinic");
        // Outside player at origin can NOT see Mara
        var nrOutsideSnap = nrServer.CreateInterestSnapshot("ab_outside_nr");
        ExpectFalse(nrOutsideSnap.Npcs.Any(n => n.Id == StarterNpcs.Mara.Id),
            "outside viewer cannot see resident NPC inside their assigned structure");
        // Move inside player into the clinic via door (2,1)
        nrServer.ProcessIntent(new ServerIntent("aa_inside_nr", nrSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "2", ["y"] = "1" }));
        nrServer.ProcessIntent(new ServerIntent("aa_inside_nr", nrSeq++, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "3", ["y"] = "3" }));
        var nrInsideSnap = nrServer.CreateInterestSnapshot("aa_inside_nr");
        ExpectTrue(nrInsideSnap.Npcs.Any(n => n.Id == StarterNpcs.Mara.Id),
            "inside viewer sees resident NPC inside their structure");

        // Weapon resource costs: ranged needs ammo + reload, melee needs stamina.
        var gunState = new GameState();
        gunState.RegisterPlayer("aa_gunner", "Gunner");
        gunState.RegisterPlayer("ab_dummy", "Dummy");
        gunState.SetPlayerPosition("aa_gunner", TilePosition.Origin);
        gunState.SetPlayerPosition("ab_dummy", TilePosition.Origin);
        gunState.AddItem("aa_gunner", StarterItems.ElectroPistol);
        gunState.AddItem("aa_gunner", StarterItems.EnergyCell);
        var gunServer = new AuthoritativeWorldServer(gunState, "weapon-resource-test");
        var gunSeq = 1;
        foreach (var gPid in gunServer.ConnectedPlayerIds)
            gunServer.ProcessIntent(new ServerIntent(gPid, gunSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        // Equip pistol -> magazine starts empty (ranged weapons must reload first)
        gunServer.ProcessIntent(new ServerIntent("aa_gunner", gunSeq++, IntentType.UseItem,
            new Dictionary<string, string> { ["itemId"] = StarterItems.ElectroPistolId }));
        ExpectEqual(0, gunState.Players["aa_gunner"].CurrentAmmo,
            "equipping a ranged weapon starts with empty magazine");

        // Attack with empty mag -> rejected
        var gunEmptyAttack = gunServer.ProcessIntent(new ServerIntent("aa_gunner", gunSeq++, IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "ab_dummy" }));
        ExpectFalse(gunEmptyAttack.WasAccepted, "Attack rejected when ranged weapon magazine is empty");

        // Reload -> ammo full, ammo item consumed
        var gunReload = gunServer.ProcessIntent(new ServerIntent("aa_gunner", gunSeq++, IntentType.Reload,
            new Dictionary<string, string>()));
        ExpectTrue(gunReload.WasAccepted, "Reload accepted with ammo in inventory");
        ExpectEqual(StarterItems.ElectroPistol.MagazineSize, gunState.Players["aa_gunner"].CurrentAmmo,
            "Reload refills magazine to MagazineSize");
        ExpectFalse(gunState.Players["aa_gunner"].Inventory.Any(i => i.Id == StarterItems.EnergyCellId),
            "Reload consumes one ammo item from inventory");
        ExpectTrue(gunServer.EventLog.Any(e => e.EventId.Contains("weapon_reloaded")),
            "weapon_reloaded event fires on successful reload");
        ExpectEqual("weapon_reloaded", gunReload.Event.Data["audioCue"],
            "weapon reload event carries audio cue");

        // Attack succeeds now -> ammo decrements
        var gunAmmoBefore = gunState.Players["aa_gunner"].CurrentAmmo;
        gunServer.ProcessIntent(new ServerIntent("aa_gunner", gunSeq++, IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "ab_dummy" }));
        ExpectEqual(gunAmmoBefore - 1, gunState.Players["aa_gunner"].CurrentAmmo,
            "Successful ranged attack decrements ammo by 1");

        // Reload rejected when no ammo item in inventory
        var gunReloadFail = gunServer.ProcessIntent(new ServerIntent("aa_gunner", gunSeq++, IntentType.Reload,
            new Dictionary<string, string>()));
        ExpectFalse(gunReloadFail.WasAccepted, "Reload rejected when no matching ammo in inventory");

        // Melee path: equip stun baton, attack costs stamina
        var wrMeleeState = new GameState();
        wrMeleeState.RegisterPlayer("aa_brawler", "Brawler");
        wrMeleeState.RegisterPlayer("ab_punching", "Bag");
        wrMeleeState.SetPlayerPosition("aa_brawler", TilePosition.Origin);
        wrMeleeState.SetPlayerPosition("ab_punching", TilePosition.Origin);
        wrMeleeState.AddItem("aa_brawler", StarterItems.StunBaton);
        var wrMeleeServer = new AuthoritativeWorldServer(wrMeleeState, "weapon-melee-test");
        var wrMeleeSeq = 1;
        foreach (var wrPid in wrMeleeServer.ConnectedPlayerIds)
            wrMeleeServer.ProcessIntent(new ServerIntent(wrPid, wrMeleeSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        wrMeleeServer.ProcessIntent(new ServerIntent("aa_brawler", wrMeleeSeq++, IntentType.UseItem,
            new Dictionary<string, string> { ["itemId"] = StarterItems.StunBatonId }));

        var staminaBefore = wrMeleeState.Players["aa_brawler"].Stamina;
        var wrSwing = wrMeleeServer.ProcessIntent(new ServerIntent("aa_brawler", wrMeleeSeq++, IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "ab_punching" }));
        ExpectTrue(wrSwing.WasAccepted, "Melee attack accepted with full stamina");
        ExpectEqual(staminaBefore - StarterItems.StunBaton.StaminaCost,
            wrMeleeState.Players["aa_brawler"].Stamina,
            "Melee attack deducts the weapon's StaminaCost");

        // Reload rejected for melee weapon
        var wrMeleeReload = wrMeleeServer.ProcessIntent(new ServerIntent("aa_brawler", wrMeleeSeq++, IntentType.Reload,
            new Dictionary<string, string>()));
        ExpectFalse(wrMeleeReload.WasAccepted, "Reload rejected for melee weapon");

        // Attack rejected when stamina is too low
        wrMeleeState.Players["aa_brawler"].SpendStamina(95);
        var wrTired = wrMeleeServer.ProcessIntent(new ServerIntent("aa_brawler", wrMeleeSeq++, IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "ab_punching" }));
        ExpectFalse(wrTired.WasAccepted, "Melee attack rejected when stamina < StaminaCost");

        // Stamina regens during idle ticks
        var staminaAfterDrain = wrMeleeState.Players["aa_brawler"].Stamina;
        wrMeleeServer.AdvanceIdleTicks(5);
        ExpectTrue(wrMeleeState.Players["aa_brawler"].Stamina > staminaAfterDrain,
            "Stamina regenerates during AdvanceIdleTicks");

        // Equipment durability: equipped items preserve per-instance durability,
        // wear on attack, reject while broken, and can be repaired near a workshop.
        var durState = new GameState();
        durState.RegisterPlayer("aa_smith", "Smith");
        durState.RegisterPlayer("ab_target", "Target");
        durState.SetPlayerPosition("aa_smith", new TilePosition(12, 5));
        durState.SetPlayerPosition("ab_target", new TilePosition(12, 5));
        durState.AddItem("aa_smith", StarterItems.PracticeStick with { Durability = 1, MaxDurability = 100 });
        durState.AddScrip("aa_smith", 25);
        var durServer = new AuthoritativeWorldServer(durState, "durability-test");
        var durSeq = 1;
        foreach (var durPid in durServer.ConnectedPlayerIds)
            durServer.ProcessIntent(new ServerIntent(durPid, durSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        durServer.ProcessIntent(new ServerIntent("aa_smith", durSeq++, IntentType.UseItem,
            new Dictionary<string, string> { ["itemId"] = StarterItems.PracticeStickId }));

        var durAttack = durServer.ProcessIntent(new ServerIntent("aa_smith", durSeq++, IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "ab_target" }));
        ExpectTrue(durAttack.WasAccepted, "Attack accepted with a 1-durability weapon");
        ExpectEqual(0, durState.Players["aa_smith"].Equipment[EquipmentSlot.MainHand].Durability,
            "Successful attack wears weapon durability down to 0");

        durServer.AdvanceIdleTicks(AuthoritativeWorldServer.AttackCooldownTicks);
        var brokenAttack = durServer.ProcessIntent(new ServerIntent("aa_smith", durSeq++, IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "ab_target" }));
        ExpectFalse(brokenAttack.WasAccepted, "Attack rejected when equipped weapon is broken");

        var scripBeforeRepair = durState.Players["aa_smith"].Scrip;
        var repairResult = durServer.ProcessIntent(new ServerIntent("aa_smith", durSeq++, IntentType.RepairItem,
            new Dictionary<string, string> { ["slot"] = EquipmentSlot.MainHand.ToString() }));
        ExpectTrue(repairResult.WasAccepted, "RepairItem accepted near a workshop");
        ExpectEqual(100, durState.Players["aa_smith"].Equipment[EquipmentSlot.MainHand].Durability,
            "RepairItem restores equipped item durability");
        ExpectTrue(durState.Players["aa_smith"].Scrip < scripBeforeRepair,
            "RepairItem deducts scrip based on missing durability");
        ExpectEqual("100/100",
            HudController.FormatDurability(durState.Players["aa_smith"].Equipment, EquipmentSlot.MainHand),
            "HUD formats equipped weapon durability");

        // PlayerSnapshot now surfaces ammo/stamina/weapon kind for HUD readout.
        var gunSnaps = SnapshotBuilder.PlayersFrom(gunState.Players, new LeaderboardStanding(string.Empty, string.Empty, 0, string.Empty, string.Empty, 0));
        var gunnerSnap = gunSnaps.First(p => p.Id == "aa_gunner");
        ExpectEqual(WeaponKind.Ranged, gunnerSnap.EquippedWeaponKind,
            "PlayerSnapshot.EquippedWeaponKind reflects an equipped ranged weapon");
        ExpectTrue(gunnerSnap.MaxAmmo > 0,
            "PlayerSnapshot.MaxAmmo is non-zero when a ranged weapon is equipped");
        ExpectEqual(gunState.Players["aa_gunner"].CurrentAmmo, gunnerSnap.CurrentAmmo,
            "PlayerSnapshot.CurrentAmmo mirrors authoritative state");
        ExpectEqual(gunState.Players["aa_gunner"].Stamina, gunnerSnap.Stamina,
            "PlayerSnapshot.Stamina mirrors authoritative state");
        ExpectEqual(gunState.Players["aa_gunner"].MaxStamina, gunnerSnap.MaxStamina,
            "PlayerSnapshot.MaxStamina mirrors authoritative state");

        var meleeSnaps = SnapshotBuilder.PlayersFrom(wrMeleeState.Players, new LeaderboardStanding(string.Empty, string.Empty, 0, string.Empty, string.Empty, 0));
        var brawlerSnap = meleeSnaps.First(p => p.Id == "aa_brawler");
        ExpectEqual(WeaponKind.Melee, brawlerSnap.EquippedWeaponKind,
            "PlayerSnapshot.EquippedWeaponKind reflects an equipped melee weapon");
        ExpectEqual(0, brawlerSnap.MaxAmmo,
            "PlayerSnapshot.MaxAmmo is zero for melee equip");

        // Backpack equipment slot expands inventory capacity.
        var bpState = new GameState();
        bpState.RegisterPlayer("aa_porter", "Porter");
        ExpectEqual(StarterItems.BaseInventorySlots,
            bpState.Players["aa_porter"].MaxInventorySlots,
            "Player without a backpack uses BaseInventorySlots");
        var bpServer = new AuthoritativeWorldServer(bpState, "backpack-test");
        var bpSeq = 1;
        foreach (var bpPid in bpServer.ConnectedPlayerIds)
            bpServer.ProcessIntent(new ServerIntent(bpPid, bpSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        bpState.AddItem("aa_porter", StarterItems.BackpackBrown);
        bpServer.ProcessIntent(new ServerIntent("aa_porter", bpSeq++, IntentType.UseItem,
            new Dictionary<string, string> { ["itemId"] = StarterItems.BackpackBrownId }));
        ExpectEqual(StarterItems.BaseInventorySlots + StarterItems.BackpackBrownInventoryBoost,
            bpState.Players["aa_porter"].MaxInventorySlots,
            "Equipping a backpack adds InventoryBoost to MaxInventorySlots");
        var bpSnap = SnapshotBuilder.PlayersFrom(bpState.Players,
                new LeaderboardStanding(string.Empty, string.Empty, 0, string.Empty, string.Empty, 0))
            .First(p => p.Id == "aa_porter");
        ExpectEqual(bpState.Players["aa_porter"].MaxInventorySlots, bpSnap.MaxInventorySlots,
            "PlayerSnapshot.MaxInventorySlots mirrors authoritative state");

        // Hunger system: starts at 100, decays during idle ticks, food restores it.
        var hngState = new GameState();
        hngState.RegisterPlayer("aa_eater", "Eater");
        ExpectEqual(100, hngState.Players["aa_eater"].Hunger,
            "Player starts at full hunger (100)");
        var hngServer = new AuthoritativeWorldServer(hngState, "hunger-test");
        var hngSeq = 1;
        foreach (var hngPid in hngServer.ConnectedPlayerIds)
            hngServer.ProcessIntent(new ServerIntent(hngPid, hngSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));

        // Idle long enough for hunger to decay by at least 1.
        hngServer.AdvanceIdleTicks(AuthoritativeWorldServer.HungerDecayTickInterval);
        ExpectTrue(hngState.Players["aa_eater"].Hunger < 100,
            "Hunger decays after HungerDecayTickInterval idle ticks");

        // Drain hunger manually then eat ration — should restore.
        hngState.Players["aa_eater"].SpendHunger(60);
        var hungerBeforeFood = hngState.Players["aa_eater"].Hunger;
        hngState.AddItem("aa_eater", StarterItems.RationPack);
        hngServer.ProcessIntent(new ServerIntent("aa_eater", hngSeq++, IntentType.UseItem,
            new Dictionary<string, string> { ["itemId"] = StarterItems.RationPackId }));
        ExpectTrue(hngState.Players["aa_eater"].Hunger > hungerBeforeFood,
            "Eating a ration pack restores hunger");

        // Snapshot surfaces hunger.
        var hngSnap = SnapshotBuilder.PlayersFrom(hngState.Players,
                new LeaderboardStanding(string.Empty, string.Empty, 0, string.Empty, string.Empty, 0))
            .First(p => p.Id == "aa_eater");
        ExpectEqual(hngState.Players["aa_eater"].Hunger, hngSnap.Hunger,
            "PlayerSnapshot.Hunger mirrors authoritative state");
        ExpectEqual(100, hngSnap.MaxHunger,
            "PlayerSnapshot.MaxHunger surfaces the configured pool");

        // Hunger threshold triggers Hungry status; Starving + HP decay at 0.
        var hungryStatusState = new GameState();
        hungryStatusState.RegisterPlayer("aa_hungry", "Hungry");
        var hungryServer = new AuthoritativeWorldServer(hungryStatusState, "hungry-status-test");
        var hsSeq = 1;
        foreach (var pid in hungryServer.ConnectedPlayerIds)
            hungryServer.ProcessIntent(new ServerIntent(pid, hsSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        // Drain hunger below the Hungry threshold.
        hungryStatusState.Players["aa_hungry"].SpendHunger(80);
        var hungryStatuses = hungryServer.GetStatusEffectsFor(hungryStatusState.Players["aa_hungry"]);
        ExpectTrue(hungryStatuses.Contains("Hungry"),
            "Hungry status appears when hunger is at/below threshold");
        ExpectFalse(hungryStatuses.Contains("Starving"),
            "Starving does not appear before hunger reaches 0");

        // Drop hunger to 0; status flips to Starving and starvation deals damage on decay.
        hungryStatusState.Players["aa_hungry"].SpendHunger(100);
        var starvingStatuses = hungryServer.GetStatusEffectsFor(hungryStatusState.Players["aa_hungry"]);
        ExpectTrue(starvingStatuses.Contains("Starving"),
            "Starving status appears when hunger reaches 0");
        ExpectFalse(starvingStatuses.Contains("Hungry"),
            "Starving replaces Hungry once at 0");

        var hpBeforeStarve = hungryStatusState.Players["aa_hungry"].Health;
        hungryServer.AdvanceIdleTicks(AuthoritativeWorldServer.HungerDecayTickInterval);
        ExpectTrue(hungryStatusState.Players["aa_hungry"].Health < hpBeforeStarve,
            "Starving player loses HP on hunger decay tick");

        var hungerSpeedStanding = new LeaderboardStanding(string.Empty, string.Empty, 0, string.Empty, string.Empty, 0);
        var hungrySpeedPlayer = new PlayerState("aa_hungry_speed", "Hungry Speed");
        hungrySpeedPlayer.SpendHunger(75);
        var hungrySpeed = SnapshotBuilder.CalculateSpeedModifier(hungrySpeedPlayer, hungerSpeedStanding);
        var starvingSpeedPlayer = new PlayerState("aa_starving_speed", "Starving Speed");
        starvingSpeedPlayer.SpendHunger(100);
        var starvingSpeed = SnapshotBuilder.CalculateSpeedModifier(starvingSpeedPlayer, hungerSpeedStanding);
        var wraithSpeedPlayer = new PlayerState("aa_wraith_speed", "Wraith Speed");
        wraithSpeedPlayer.ApplyKarma(-PerkCatalog.WraithThreshold);
        wraithSpeedPlayer.ApplyDamage(80);
        var wraithSpeed = SnapshotBuilder.CalculateSpeedModifier(
            wraithSpeedPlayer,
            new LeaderboardStanding(string.Empty, string.Empty, 0, wraithSpeedPlayer.Id, wraithSpeedPlayer.DisplayName, wraithSpeedPlayer.Karma.Score));
        ExpectTrue(starvingSpeed < hungrySpeed,
            "Starving speed modifier is lower than Hungry");
        ExpectTrue(hungrySpeed < wraithSpeed,
            "Hungry speed modifier is lower than low-HP Wraith");
        ExpectEqual(0.85f, hungrySpeed,
            "Hungry speed modifier is 0.85");
        ExpectEqual(0.6f, starvingSpeed,
            "Starving speed modifier is 0.6");

        // Cleanliness/restroom mechanic: slower idle decay, combat drop, restroom reset.
        var cleanState = new GameState();
        cleanState.RegisterPlayer("aa_clean", "Clean");
        cleanState.SetPlayerPosition("aa_clean", TilePosition.Origin);
        var cleanServer = new AuthoritativeWorldServer(cleanState, "cleanliness-test");
        var cleanSeq = 1;
        foreach (var pid in cleanServer.ConnectedPlayerIds)
            cleanServer.ProcessIntent(new ServerIntent(pid, cleanSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        ExpectEqual(100, cleanState.Players["aa_clean"].Cleanliness,
            "Player starts at full cleanliness (100)");
        cleanServer.AdvanceIdleTicks(AuthoritativeWorldServer.CleanlinessDecayTickInterval);
        ExpectTrue(cleanState.Players["aa_clean"].Cleanliness < 100,
            "Cleanliness decays after CleanlinessDecayTickInterval idle ticks");
        cleanState.Players["aa_clean"].SpendCleanliness(100);
        var filthyStatuses = cleanServer.GetStatusEffectsFor(cleanState.Players["aa_clean"]);
        ExpectTrue(filthyStatuses.Contains("Filthy"),
            "Filthy status appears at zero cleanliness");
        cleanState.Players["aa_clean"].ResetCleanliness();
        cleanState.Players["aa_clean"].SpendCleanliness(75);
        var dirtyStatuses = cleanServer.GetStatusEffectsFor(cleanState.Players["aa_clean"]);
        ExpectTrue(dirtyStatuses.Contains("Dirty"),
            "Dirty status appears at/below cleanliness threshold");
        ExpectFalse(dirtyStatuses.Contains("Filthy"),
            "Dirty does not include Filthy before cleanliness reaches zero");

        var combatCleanState = new GameState();
        combatCleanState.RegisterPlayer("aa_clean_attacker", "Clean Attacker");
        combatCleanState.RegisterPlayer("aa_clean_target", "Clean Target");
        combatCleanState.SetPlayerPosition("aa_clean_attacker", TilePosition.Origin);
        combatCleanState.SetPlayerPosition("aa_clean_target", TilePosition.Origin);
        var combatCleanServer = new AuthoritativeWorldServer(combatCleanState, "combat-cleanliness-test");
        var combatCleanSeq = 1;
        foreach (var pid in combatCleanServer.ConnectedPlayerIds)
            combatCleanServer.ProcessIntent(new ServerIntent(pid, combatCleanSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        var attackerCleanBefore = combatCleanState.Players["aa_clean_attacker"].Cleanliness;
        var targetCleanBefore = combatCleanState.Players["aa_clean_target"].Cleanliness;
        var cleanAttack = combatCleanServer.ProcessIntent(new ServerIntent("aa_clean_attacker", combatCleanSeq++,
            IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "aa_clean_target" }));
        ExpectTrue(cleanAttack.WasAccepted,
            "combat cleanliness test attack is accepted");
        ExpectEqual(attackerCleanBefore - AuthoritativeWorldServer.CombatCleanlinessLoss,
            combatCleanState.Players["aa_clean_attacker"].Cleanliness,
            "Attacker cleanliness drops after a hit");
        ExpectEqual(targetCleanBefore - AuthoritativeWorldServer.CombatCleanlinessLoss,
            combatCleanState.Players["aa_clean_target"].Cleanliness,
            "Target cleanliness drops after a hit");

        cleanServer.SeedRestroomStructure("structure_test_restroom", TilePosition.Origin);
        var restroomUse = cleanServer.ProcessIntent(new ServerIntent("aa_clean", cleanSeq++,
            IntentType.Interact,
            new Dictionary<string, string>
            {
                ["entityId"] = "structure_test_restroom",
                ["action"] = "use"
            }));
        ExpectTrue(restroomUse.WasAccepted,
            "restroom use interaction is accepted");
        ExpectEqual(100, cleanState.Players["aa_clean"].Cleanliness,
            "Restroom use resets cleanliness to full");
        ExpectFalse(cleanServer.GetStatusEffectsFor(cleanState.Players["aa_clean"]).Any(status => status == "Dirty" || status == "Filthy"),
            "Restroom reset clears cleanliness-derived statuses");

        // Witness propagation: attack events carry nearby witnesses and scale
        // karma impact up toward full value when the act is public.
        var noWitnessState = new GameState();
        noWitnessState.RegisterPlayer("aa_shadow", "Shadow");
        noWitnessState.RegisterPlayer("aa_target_shadow", "Target Shadow");
        noWitnessState.SetPlayerPosition("aa_shadow", new TilePosition(100, 100));
        noWitnessState.SetPlayerPosition("aa_target_shadow", new TilePosition(100, 100));
        var noWitnessServer = new AuthoritativeWorldServer(noWitnessState, "no-witness-test");
        var noWitnessSeq = 1;
        foreach (var pid in noWitnessServer.ConnectedPlayerIds)
            noWitnessServer.ProcessIntent(new ServerIntent(pid, noWitnessSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        var noWitnessAttack = noWitnessServer.ProcessIntent(new ServerIntent("aa_shadow", noWitnessSeq++,
            IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "aa_target_shadow" }));
        ExpectTrue(noWitnessAttack.WasAccepted,
            "no-witness attack is accepted");
        ExpectEqual(0, noWitnessAttack.Event.Witnesses.Count,
            "attack far from players/NPCs records no witnesses");
        var noWitnessKarma = noWitnessState.Players["aa_shadow"].Karma.Score;

        var publicWitnessState = new GameState();
        publicWitnessState.RegisterPlayer("aa_public", "Public");
        publicWitnessState.RegisterPlayer("aa_target_public", "Target Public");
        publicWitnessState.SetPlayerPosition("aa_public", TilePosition.Origin);
        publicWitnessState.SetPlayerPosition("aa_target_public", TilePosition.Origin);
        var publicWitnessServer = new AuthoritativeWorldServer(publicWitnessState, "public-witness-test");
        for (var i = 0; i < 5; i++)
        {
            publicWitnessServer.SeedNpc(new NpcProfile(
                $"witness_npc_{i}",
                $"Witness {i}",
                "Witness",
                "watchful",
                "Village",
                string.Empty,
                string.Empty,
                System.Array.Empty<string>(),
                System.Array.Empty<string>()),
                TilePosition.Origin);
        }
        var publicWitnessSeq = 1;
        foreach (var pid in publicWitnessServer.ConnectedPlayerIds)
            publicWitnessServer.ProcessIntent(new ServerIntent(pid, publicWitnessSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        var publicAttack = publicWitnessServer.ProcessIntent(new ServerIntent("aa_public", publicWitnessSeq++,
            IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "aa_target_public" }));
        ExpectTrue(publicAttack.WasAccepted,
            "public attack is accepted");
        ExpectTrue(publicAttack.Event.Witnesses.Count >= 5,
            "public attack records nearby NPC witnesses");
        ExpectEqual(publicAttack.Event.Witnesses.Count.ToString(), publicAttack.Event.Data["witnessCount"],
            "attack event data mirrors witness count");
        ExpectTrue(publicWitnessState.Players["aa_public"].Karma.Score < noWitnessKarma,
            "attack near 5+ witnesses has a larger negative karma swing than an unwitnessed attack");

        var crimeState = new GameState();
        crimeState.RegisterPlayer("aa_criminal", "Criminal");
        crimeState.RegisterPlayer("aa_victim", "Victim");
        crimeState.SetPlayerPosition("aa_criminal", TilePosition.Origin);
        crimeState.SetPlayerPosition("aa_victim", TilePosition.Origin);
        var crimeServer = new AuthoritativeWorldServer(crimeState, "crime-report-test");
        var guardWitness = new NpcProfile(
            "guard_reporter",
            "Guard Reporter",
            "Guard",
            "stern",
            "Crown Garrison",
            "a clear report",
            "keeps a spare whistle",
            Array.Empty<string>(),
            Array.Empty<string>(),
            IsLawAligned: true,
            Tags: new[] { NpcRoleTags.LawAligned });
        var captainReceiver = new NpcProfile(
            "captain_receiver",
            "Captain Receiver",
            "Captain",
            "steady",
            "Crown Garrison",
            "reports",
            "none",
            Array.Empty<string>(),
            Array.Empty<string>(),
            IsLawAligned: true,
            Tags: new[] { NpcRoleTags.LawAligned });
        crimeServer.SeedNpc(guardWitness, new TilePosition(1, 0));
        crimeServer.SeedNpc(captainReceiver, new TilePosition(5, 0));
        var crimeSeq = 1;
        foreach (var pid in crimeServer.ConnectedPlayerIds)
            crimeServer.ProcessIntent(new ServerIntent(pid, crimeSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        var witnessedCrime = crimeServer.ProcessIntent(new ServerIntent("aa_criminal", crimeSeq++,
            IntentType.Attack,
            new Dictionary<string, string> { ["targetId"] = "aa_victim" }));
        ExpectTrue(witnessedCrime.WasAccepted,
            "witnessed crime attack is accepted");
        ExpectTrue(witnessedCrime.Event.Witnesses.Contains("guard_reporter"),
            "law-aligned NPC is recorded in the attack witness set");
        crimeServer.AdvanceIdleTicks(crimeServer.Config.CrimeReportDelayTicks);
        var crimeReport = crimeServer.EventLog.LastOrDefault(serverEvent => serverEvent.EventId.Contains("crime_reported"));
        ExpectTrue(crimeReport is not null,
            "crime_reported event fires after the configured report delay");
        ExpectEqual("guard_reporter", crimeReport?.Data["witnessNpcId"] ?? "",
            "crime_reported event cites the reporting witness NPC");
        ExpectEqual("player_attacked", crimeReport?.Data["sourceEventType"] ?? "",
            "crime_reported event links back to the attack event type");

        // Drug registry server wiring: a registered drug item applies an
        // on-use status, expires after its duration, then enters withdrawal
        // once cumulative exposure crosses the addiction threshold.
        var drugState = new GameState();
        drugState.RegisterPlayer("aa_doser", "Doser");
        var drugServer = new AuthoritativeWorldServer(drugState, "drug-status-test");
        var drugSeq = 1;
        foreach (var pid in drugServer.ConnectedPlayerIds)
            drugServer.ProcessIntent(new ServerIntent(pid, drugSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        drugState.Players["aa_doser"].SpendStamina(75);
        drugState.AddItem("aa_doser", StarterItems.StimSpike);
        drugState.AddItem("aa_doser", StarterItems.StimSpike);
        drugState.AddItem("aa_doser", StarterItems.StimSpike);

        for (var dose = 0; dose < 3; dose++)
        {
            var drugResult = drugServer.ProcessIntent(new ServerIntent("aa_doser", drugSeq++, IntentType.UseItem,
                new Dictionary<string, string> { ["itemId"] = StarterItems.StimSpikeId }));
            ExpectTrue(drugResult.WasAccepted, $"Stim Spike dose {dose + 1} is accepted by UseItem");
        }

        ExpectTrue(drugState.Players["aa_doser"].Stamina > 25,
            "Stim Spike applies its stamina delta on use");
        var drugOnUseStatuses = drugServer.GetStatusEffectsFor(drugState.Players["aa_doser"]);
        ExpectTrue(drugOnUseStatuses.Contains("Energised"),
            "Stim Spike on-use status appears after use");

        drugServer.AdvanceIdleTicks(600);
        var drugExpiredStatuses = drugServer.GetStatusEffectsFor(drugState.Players["aa_doser"]);
        ExpectFalse(drugExpiredStatuses.Contains("Energised"),
            "Stim Spike on-use status expires after DurationTicks");

        var staminaBeforeWithdrawal = drugState.Players["aa_doser"].Stamina;
        drugServer.AdvanceIdleTicks(1201);
        var drugWithdrawalStatuses = drugServer.GetStatusEffectsFor(drugState.Players["aa_doser"]);
        ExpectTrue(drugWithdrawalStatuses.Contains("Crashing"),
            "Stim Spike withdrawal status appears after addiction grace expires");
        ExpectTrue(drugState.Players["aa_doser"].Stamina < staminaBeforeWithdrawal,
            "Stim Spike withdrawal applies a stamina penalty during idle ticks");

        var downerState = new GameState();
        downerState.RegisterPlayer("aa_downer", "Downer");
        var downerServer = new AuthoritativeWorldServer(downerState, "downer-withdrawal-test");
        var downerSeq = 1;
        foreach (var pid in downerServer.ConnectedPlayerIds)
            downerServer.ProcessIntent(new ServerIntent(pid, downerSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        downerState.AddItem("aa_downer", StarterItems.DownerHaze);
        downerState.AddItem("aa_downer", StarterItems.DownerHaze);
        for (var dose = 0; dose < 2; dose++)
        {
            var downerResult = downerServer.ProcessIntent(new ServerIntent("aa_downer", downerSeq++, IntentType.UseItem,
                new Dictionary<string, string> { ["itemId"] = StarterItems.DownerHazeId }));
            ExpectTrue(downerResult.WasAccepted, $"Downer Haze dose {dose + 1} is accepted by UseItem");
        }

        var hpBeforeDownerWithdrawal = downerState.Players["aa_downer"].Health;
        downerServer.AdvanceIdleTicks(1501);
        var downerWithdrawalStatuses = downerServer.GetStatusEffectsFor(downerState.Players["aa_downer"]);
        ExpectTrue(downerWithdrawalStatuses.Contains("Twitchy"),
            "Downer Haze withdrawal status appears after addiction grace expires");
        ExpectTrue(downerState.Players["aa_downer"].Health < hpBeforeDownerWithdrawal,
            "Downer Haze withdrawal applies an HP penalty during idle ticks");

        // Loot drop table registry: built-in lookup, deterministic roll with seeded RNG,
        // qty range, runtime override, unknown table → empty result.
        LootTableCatalog.Reset();
        ExpectTrue(LootTableCatalog.TryGet(LootTableCatalog.SupplyDropCommonId, out var supplyTable),
            "loot catalog resolves built-in supply_drop_common table");
        ExpectTrue(supplyTable.Entries.Count > 0,
            "supply_drop_common table has at least one entry");
        ExpectFalse(LootTableCatalog.TryGet("totally_unknown_table", out _),
            "loot catalog returns false for unknown table id");
        ExpectEqual(0,
            LootTableCatalog.Roll("totally_unknown_table", new System.Random(1)).Count,
            "loot catalog roll on unknown table returns empty list");

        var seededRoll = LootTableCatalog.Roll(
            LootTableCatalog.SupplyDropCommonId, new System.Random(42));
        ExpectTrue(seededRoll.Count >= 1 && seededRoll.Count <= supplyTable.Rolls,
            "supply_drop_common roll yields between 1 and Rolls items with a seeded RNG");
        var seededRollAgain = LootTableCatalog.Roll(
            LootTableCatalog.SupplyDropCommonId, new System.Random(42));
        ExpectEqual(seededRoll.Count, seededRollAgain.Count,
            "loot rolls are deterministic across identical seeds");
        for (var i = 0; i < seededRoll.Count; i++)
        {
            ExpectEqual(seededRoll[i].ItemId, seededRollAgain[i].ItemId,
                $"loot roll item-id at index {i} matches across identical seeds");
            ExpectEqual(seededRoll[i].Quantity, seededRollAgain[i].Quantity,
                $"loot roll quantity at index {i} matches across identical seeds");
        }

        var ammoRoll = LootTableCatalog.Roll(
            LootTableCatalog.SupplyDropAmmoId, new System.Random(7));
        ExpectTrue(ammoRoll.All(r =>
                r.ItemId == StarterItems.BallisticRoundId
                || r.ItemId == StarterItems.EnergyCellId),
            "supply_drop_ammo only rolls ballistic / energy entries");
        ExpectTrue(ammoRoll.All(r => r.Quantity >= 2),
            "supply_drop_ammo respects MinQuantity ≥ 2");

        // Runtime override: register a single-entry table and confirm it resolves.
        LootTableCatalog.Register(new LootTable("test_override",
            new[] { new LootTableEntry("ration_pack", Weight: 1, MinQuantity: 5, MaxQuantity: 5) },
            Rolls: 1));
        var overrideRoll = LootTableCatalog.Roll("test_override", new System.Random(0));
        ExpectEqual(1, overrideRoll.Count, "registered override table rolls once");
        ExpectEqual("ration_pack", overrideRoll[0].ItemId,
            "registered override entry yields its item id");
        ExpectEqual(5, overrideRoll[0].Quantity,
            "registered override entry honours fixed quantity");
        LootTableCatalog.Reset();
        ExpectFalse(LootTableCatalog.TryGet("test_override", out _),
            "loot catalog Reset clears runtime overrides");

        // Dialogue tree DSL: built-in tree, traversal, choice-array adapter.
        DialogueRegistry.Reset();
        ExpectTrue(DialogueRegistry.TryGet(DialogueRegistry.MaraClinicTreeId, out var maraTree),
            "dialogue registry resolves built-in mara_clinic_default tree");
        ExpectTrue(maraTree.Root is not null,
            "mara_clinic_default tree exposes a Root node");
        ExpectEqual(3, maraTree.Root.Choices.Count,
            "mara_clinic_default root offers three choices");
        var acceptedNode = maraTree.Get("accepted");
        ExpectTrue(acceptedNode is not null && acceptedNode.Choices.Count == 1,
            "accepted node has a single closing choice");
        ExpectTrue(acceptedNode.Choices[0].Terminates,
            "accepted close choice terminates dialogue");
        ExpectFalse(DialogueRegistry.TryGet("totally_unknown_tree", out _),
            "dialogue registry returns false for unknown tree id");
        ExpectTrue(maraTree.Get("not_a_node") is null,
            "tree.Get returns null for unknown node id");

        var legacyChoices = DialogueRegistry.BuildChoiceArray(maraTree.Root);
        ExpectEqual(3, legacyChoices.Count,
            "BuildChoiceArray projects all root choices");
        var advanceChoice = legacyChoices.First(c => c.Id == "ask_about_supplies");
        ExpectEqual("dialogue_advance:supplies", advanceChoice.ActionId,
            "non-action choice projects next node into ActionId");
        var actionChoice = DialogueRegistry.BuildChoiceArray(maraTree.Get("accepted"))[0];
        ExpectEqual("help_mara", actionChoice.ActionId,
            "explicit ActionId is preserved by the adapter");
        ExpectTrue(DialogueRegistry.TryGet(DialogueRegistry.DallenShopkeeperTreeId, out var dallenTree),
            "dialogue registry resolves built-in dallen_shopkeeper_default tree");
        ExpectTrue(dallenTree.Root.Choices.Any(choice => choice.Id == "browse_wares") &&
                   dallenTree.Root.Choices.Any(choice => choice.Id == "ask_about_mara") &&
                   dallenTree.Root.Choices.Any(choice => choice.Id == "leave"),
            "Dallen tree branches between wares, Mara, and leave");

        // Runtime override: register a tiny tree and confirm it resolves.
        DialogueRegistry.Register(new DialogueTree(
            Id: "test_tree",
            RootNodeId: "start",
            Nodes: new Dictionary<string, DialogueNode>
            {
                ["start"] = new DialogueNode("start", "hi", new[]
                {
                    new DialogueChoice("ok", "ok", Terminates: true)
                })
            }));
        ExpectTrue(DialogueRegistry.TryGet("test_tree", out var customTree),
            "runtime-registered dialogue tree resolves");
        ExpectEqual("ok", customTree.Root.Choices[0].Id,
            "registered tree's root has the expected choice id");
        DialogueRegistry.Reset();
        ExpectFalse(DialogueRegistry.TryGet("test_tree", out _),
            "dialogue registry Reset clears runtime overrides");

        // NPC profile carries a DialogueTreeId field (currently empty for
        // legacy NPCs; the walker only engages once a tree is bound).
        ExpectEqual(string.Empty, StarterNpcs.Mara.DialogueTreeId,
            "Mara's DialogueTreeId is unset by default — legacy procedural choices win");
        ExpectEqual(DialogueRegistry.DallenShopkeeperTreeId, StarterNpcs.Dallen.DialogueTreeId,
            "Dallen is bound to the shopkeeper dialogue tree");
        // Verify the registry+walker pieces still work in isolation.
        ExpectTrue(DialogueRegistry.TryGet(DialogueRegistry.MaraClinicTreeId, out var registryTree),
            "registry resolves the built-in mara_clinic_default tree");
        ExpectTrue(registryTree.Root.Choices.Count > 0,
            "registered tree exposes branching choices at the root");

        var dallenState = new GameState();
        dallenState.RegisterPlayer("aa_dallen_talker", "Dallen Talker");
        var dallenServer = new AuthoritativeWorldServer(dallenState, "dallen-dialogue-test");
        dallenState.SetPlayerPosition("aa_dallen_talker", dallenServer.GetNpcPosition(StarterNpcs.Dallen.Id));
        var dallenSeq = 1;
        foreach (var pid in dallenServer.ConnectedPlayerIds)
            dallenServer.ProcessIntent(new ServerIntent(pid, dallenSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        var dallenDialogue = dallenServer.GetDialogueFor("aa_dallen_talker", StarterNpcs.Dallen.Id);
        ExpectTrue(dallenDialogue.Choices.Any(choice => choice.ActionId == "dialogue_advance:wares"),
            "Dallen root dialogue advances to wares node");
        ExpectTrue(dallenDialogue.Choices.Any(choice => choice.ActionId == "dialogue_advance:mara"),
            "Dallen root dialogue advances to Mara node");
        var dallenStart = dallenServer.ProcessIntent(new ServerIntent("aa_dallen_talker", dallenSeq++,
            IntentType.StartDialogue,
            new Dictionary<string, string> { ["npcId"] = StarterNpcs.Dallen.Id }));
        ExpectTrue(dallenStart.WasAccepted,
            "server starts Dallen tree dialogue");
        var dallenAskMara = dallenServer.ProcessIntent(new ServerIntent("aa_dallen_talker", dallenSeq++,
            IntentType.SelectDialogueChoice,
            new Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Dallen.Id,
                ["choiceId"] = "ask_about_mara"
            }));
        ExpectTrue(dallenAskMara.WasAccepted,
            "server advances Dallen dialogue to Mara branch");
        ExpectEqual("mara", dallenServer.GetActiveDialogueNodeId("aa_dallen_talker", StarterNpcs.Dallen.Id),
            "Dallen dialogue walker stores Mara node");
        var dallenBack = dallenServer.ProcessIntent(new ServerIntent("aa_dallen_talker", dallenSeq++,
            IntentType.SelectDialogueChoice,
            new Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Dallen.Id,
                ["choiceId"] = "back_to_root"
            }));
        ExpectTrue(dallenBack.WasAccepted,
            "server walks Dallen dialogue back to root");
        ExpectEqual("root", dallenServer.GetActiveDialogueNodeId("aa_dallen_talker", StarterNpcs.Dallen.Id),
            "Dallen dialogue walker returns to root");
        var dallenLeave = dallenServer.ProcessIntent(new ServerIntent("aa_dallen_talker", dallenSeq++,
            IntentType.SelectDialogueChoice,
            new Dictionary<string, string>
            {
                ["npcId"] = StarterNpcs.Dallen.Id,
                ["choiceId"] = "leave"
            }));
        ExpectTrue(dallenLeave.WasAccepted,
            "server closes Dallen dialogue from root leave choice");
        ExpectEqual(string.Empty, dallenServer.GetActiveDialogueNodeId("aa_dallen_talker", StarterNpcs.Dallen.Id),
            "Dallen dialogue close clears active node");

        // Medieval theme data: load roster/interactions, pick deterministic
        // appearance bundles, surface bundle ids, and inject greeting/gossip.
        var medievalTheme = ThemeDataCatalog.Get("medieval");
        ExpectTrue(medievalTheme.NpcRoster.Count >= 50,
            "medieval theme loader reads the NPC roster");
        var pickA = medievalTheme.PickAppearanceBundle("theme-world", "blacksmith_garrick");
        var pickB = medievalTheme.PickAppearanceBundle("theme-world", "blacksmith_garrick");
        ExpectEqual(pickA, pickB,
            "medieval theme appearance pick is deterministic for same world/npc");
        ExpectTrue(!string.IsNullOrWhiteSpace(pickA),
            "medieval theme appearance pick yields an LPC bundle id");

        DialogueRegistry.Register(new DialogueTree(
            Id: "theme_test_tree",
            RootNodeId: "root",
            Nodes: new Dictionary<string, DialogueNode>
            {
                ["root"] = new DialogueNode("root", "Theme root.", new[]
                {
                    new DialogueChoice("gossip", "Any gossip?", NextNodeId: "gossip"),
                    new DialogueChoice("close", "Leave.", Terminates: true)
                })
            }));
        var themeState = new GameState();
        themeState.RegisterPlayer("aa_theme_talker", "Theme Talker");
        themeState.SetPlayerPosition("aa_theme_talker", TilePosition.Origin);
        var themeServer = new AuthoritativeWorldServer(themeState, "theme-world");
        var themeNpc = new NpcProfile(
            "blacksmith_garrick",
            "Garrick the Smith",
            "Village Blacksmith",
            "gruff",
            "village_freeholders",
            "stolen tools",
            "knows a secret",
            System.Array.Empty<string>(),
            System.Array.Empty<string>(),
            DialogueTreeId: "theme_test_tree");
        themeServer.SeedNpc(themeNpc, TilePosition.Origin);
        var themeSnapshot = themeServer.CreateInterestSnapshot("aa_theme_talker");
        ExpectEqual(pickA,
            themeSnapshot.Npcs.First(npc => npc.Id == "blacksmith_garrick").LpcBundleId,
            "NpcSnapshot surfaces deterministic medieval LPC bundle id");
        var themeDialogue = themeServer.GetDialogueFor("aa_theme_talker", "blacksmith_garrick");
        ExpectTrue(themeDialogue.Prompt.Contains("Theme root.") && themeDialogue.Prompt.Contains('\n'),
            "theme greeting is prepended to bound NPC root dialogue");
        var themeGossip = themeServer.ProcessIntent(new ServerIntent("aa_theme_talker", 1,
            IntentType.SelectDialogueChoice,
            new Dictionary<string, string>
            {
                ["npcId"] = "blacksmith_garrick",
                ["choiceId"] = "gossip"
            }));
        ExpectTrue(themeGossip.WasAccepted,
            "theme gossip dialogue advance is accepted");
        var gossipDialogue = themeServer.GetDialogueFor("aa_theme_talker", "blacksmith_garrick");
        ExpectTrue(!gossipDialogue.Prompt.Contains("No gossip worth the candle"),
            "theme gossip uses relationship-driven template text");
        DialogueRegistry.Reset();

        // Supply drop materialised from a loot table.
        var lootDropState = new GameState();
        lootDropState.RegisterPlayer("aa_dropper", "Dropper");
        var lootDropServer = new AuthoritativeWorldServer(lootDropState, "loot-drop-test");
        var loot1 = lootDropServer.ScheduleSupplyDropFromTable(
            new TilePosition(50, 50),
            LootTableCatalog.SupplyDropAmmoId,
            seedSalt: 1);
        ExpectTrue(loot1.Count > 0,
            "ScheduleSupplyDropFromTable spawns at least one entity from supply_drop_ammo");
        ExpectTrue(loot1.All(id => lootDropServer.WorldItems.ContainsKey(id)),
            "spawned supply drop entities are registered in WorldItems");
        ExpectTrue(loot1.All(id =>
            {
                var ent = lootDropServer.WorldItems[id];
                return ent.Item.Id == StarterItems.BallisticRoundId
                    || ent.Item.Id == StarterItems.EnergyCellId;
            }),
            "supply_drop_ammo only spawns ballistic / energy items");
        ExpectTrue(lootDropServer.EventLog.Any(e =>
            e.EventId.Contains("supply_drop_spawned")),
            "supply_drop_spawned event fires for table-driven drops");

        // Unknown loot table → empty result, no entities created.
        var lootDropPreCount = lootDropServer.WorldItems.Count;
        var loot2 = lootDropServer.ScheduleSupplyDropFromTable(
            new TilePosition(60, 60), "totally_unknown_table");
        ExpectEqual(0, loot2.Count,
            "unknown loot table id yields no supply drop entities");
        ExpectEqual(lootDropPreCount, lootDropServer.WorldItems.Count,
            "unknown loot table does not mutate WorldItems");

        // Downed-player table drops are generated from downed_player_drops,
        // separate from legacy loose-inventory drops.
        var downedDropState = new GameState();
        downedDropState.RegisterPlayer("aa_downed_loot", "Downed Loot");
        var downedDropServer = new AuthoritativeWorldServer(downedDropState, "downed-loot-test");
        var downedSeq = 1;
        foreach (var pid in downedDropServer.ConnectedPlayerIds)
            downedDropServer.ProcessIntent(new ServerIntent(pid, downedSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        downedDropServer.ProcessIntent(new ServerIntent("aa_downed_loot", downedSeq++, IntentType.KarmaBreak,
            new Dictionary<string, string>()));
        var downedDropIds = downedDropServer.WorldItems.Values
            .Where(entity => entity.EntityId.StartsWith("downed_drop_aa_downed_loot"))
            .Select(entity => entity.Item.Id)
            .ToArray();
        ExpectTrue(downedDropIds.Length > 0,
            "Karma Break spawns downed-player loot table drops");
        var downedAllowed = LootTableCatalog.All[LootTableCatalog.DownedPlayerDropsId]
            .Entries.Select(entry => entry.ItemId)
            .ToHashSet();
        ExpectTrue(downedDropIds.All(downedAllowed.Contains),
            "downed-player table drops only items from downed_player_drops");

        // Scavengeable containers roll container_scavenge directly into inventory.
        var scavengeState = new GameState();
        scavengeState.RegisterPlayer("aa_scavenger", "Scavenger");
        scavengeState.SetPlayerPosition("aa_scavenger", TilePosition.Origin);
        var scavengeServer = new AuthoritativeWorldServer(scavengeState, "container-scavenge-test");
        scavengeServer.SeedWorldStructure(
            "structure_test_scavenge_chest",
            StructureArtCatalog.Get(StructureSpriteKind.LockedMetalChest).Id,
            TilePosition.Origin);
        var scavengeSeq = 1;
        foreach (var pid in scavengeServer.ConnectedPlayerIds)
            scavengeServer.ProcessIntent(new ServerIntent(pid, scavengeSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        var scavengeResult = scavengeServer.ProcessIntent(new ServerIntent("aa_scavenger", scavengeSeq++,
            IntentType.Interact,
            new Dictionary<string, string>
            {
                ["entityId"] = "structure_test_scavenge_chest",
                ["action"] = "scavenge"
            }));
        ExpectTrue(scavengeResult.WasAccepted,
            "container scavenge interaction is accepted");
        ExpectEqual(LootTableCatalog.ContainerScavengeId, scavengeResult.Event.Data["lootTableId"],
            "container scavenge event reports container_scavenge table");
        var scavengeAllowed = LootTableCatalog.All[LootTableCatalog.ContainerScavengeId]
            .Entries.Select(entry => entry.ItemId)
            .ToHashSet();
        ExpectTrue(scavengeState.Players["aa_scavenger"].Inventory.Count > 0,
            "container scavenge adds loot to inventory");
        ExpectTrue(scavengeState.Players["aa_scavenger"].Inventory.All(item => scavengeAllowed.Contains(item.Id)),
            "container scavenge samples only items from container_scavenge");
        var scavengeAgain = scavengeServer.ProcessIntent(new ServerIntent("aa_scavenger", scavengeSeq++,
            IntentType.Interact,
            new Dictionary<string, string>
            {
                ["entityId"] = "structure_test_scavenge_chest",
                ["action"] = "scavenge"
            }));
        ExpectFalse(scavengeAgain.WasAccepted,
            "container scavenge rejects repeat looting of the same container");

        // Match replay log: record a short intent stream and reconstruct the
        // same post-intent snapshot stream from the sidecar file.
        var replayPath = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"karma_replay_smoke_{System.Guid.NewGuid():N}.jsonl");
        var replayState = new GameState();
        replayState.RegisterPlayer("aa_replay", "Replay");
        var replayConfig = ServerConfig.Prototype4Player with
        {
            ReplayEnabled = true,
            ReplayPath = replayPath
        };
        var replayServer = new AuthoritativeWorldServer(replayState, "replay-test", replayConfig);
        var expectedReplaySnapshots = new List<ClientInterestSnapshot>();
        for (var i = 1; i <= 10; i++)
        {
            var replayIntent = new ServerIntent("aa_replay", i, IntentType.Move,
                new Dictionary<string, string>
                {
                    ["x"] = i.ToString(),
                    ["y"] = "0"
                });
            var replayResult = replayServer.ProcessIntent(replayIntent);
            ExpectTrue(replayResult.WasAccepted,
                $"replay test move {i} is accepted");
            expectedReplaySnapshots.Add(replayServer.CreateInterestSnapshot("aa_replay"));
        }
        var replayRows = MatchReplayLog.Load(replayPath);
        ExpectEqual(10, replayRows.Count,
            "Replay log records one row per processed intent");
        ExpectTrue(replayRows.All(row => row.WasAccepted),
            "Replay rows preserve intent acceptance");
        var reconstructedSnapshots = MatchReplayLog.ReconstructSnapshots(replayPath);
        ExpectEqual(10, reconstructedSnapshots.Count,
            "Replay loader reconstructs the snapshot stream");
        for (var i = 0; i < reconstructedSnapshots.Count; i++)
        {
            ExpectEqual(expectedReplaySnapshots[i].Tick, reconstructedSnapshots[i].Tick,
                $"Replay snapshot {i} preserves tick");
            ExpectEqual(expectedReplaySnapshots[i].Players.First(p => p.Id == "aa_replay").TileX,
                reconstructedSnapshots[i].Players.First(p => p.Id == "aa_replay").TileX,
                $"Replay snapshot {i} preserves player X");
            ExpectEqual(expectedReplaySnapshots[i].Players.First(p => p.Id == "aa_replay").TileY,
                reconstructedSnapshots[i].Players.First(p => p.Id == "aa_replay").TileY,
                $"Replay snapshot {i} preserves player Y");
        }
        if (System.IO.File.Exists(replayPath))
            System.IO.File.Delete(replayPath);

        // Inventory cap: pickup rejects when player is at MaxInventorySlots.
        var capState = new GameState();
        capState.RegisterPlayer("aa_hoarder", "Hoarder");
        capState.SetPlayerPosition("aa_hoarder", new TilePosition(10, 10));
        // Force the player up to MaxInventorySlots using the unchecked AddItem.
        for (var i = 0; i < StarterItems.BaseInventorySlots; i++)
            capState.Players["aa_hoarder"].AddItem(StarterItems.RationPack);
        ExpectFalse(capState.Players["aa_hoarder"].TryAddItem(StarterItems.MediPatch),
            "TryAddItem rejects when inventory is at MaxInventorySlots");

        var capServer = new AuthoritativeWorldServer(capState, "cap-test");
        var capSeq = 1;
        foreach (var pid in capServer.ConnectedPlayerIds)
            capServer.ProcessIntent(new ServerIntent(pid, capSeq++, IntentType.ReadyUp, new Dictionary<string, string>()));
        var dropId = capServer.ScheduleSupplyDrop(new TilePosition(10, 10), StarterItems.MediPatch);
        var fullPickup = capServer.ProcessIntent(new ServerIntent("aa_hoarder", capSeq++,
            IntentType.Interact,
            new Dictionary<string, string> { ["entityId"] = dropId }));
        ExpectFalse(fullPickup.WasAccepted,
            "Pickup rejected when player inventory is full");
        ExpectTrue(fullPickup.RejectionReason.Contains("Inventory is full"),
            "Pickup rejection mentions full inventory");

        // Equipping a backpack should expand cap and let the next pickup succeed.
        capState.Players["aa_hoarder"].AddItem(StarterItems.BackpackBrown);
        capServer.ProcessIntent(new ServerIntent("aa_hoarder", capSeq++, IntentType.UseItem,
            new Dictionary<string, string> { ["itemId"] = StarterItems.BackpackBrownId }));
        var dropId2 = capServer.ScheduleSupplyDrop(new TilePosition(10, 10), StarterItems.MediPatch);
        var roomyPickup = capServer.ProcessIntent(new ServerIntent("aa_hoarder", capSeq++,
            IntentType.Interact,
            new Dictionary<string, string> { ["entityId"] = dropId2 }));
        ExpectTrue(roomyPickup.WasAccepted,
            "Pickup accepted after backpack expands MaxInventorySlots");

        // Server-side dialogue walker: tree state machine in isolation.
        // Mara is intentionally NOT bound to the tree (legacy choices still
        // win for her), so end-to-end assertions go through the registry
        // and node traversal directly. `GetActiveDialogueNodeId` defaults
        // to empty for any (player, npc) pair that hasn't advanced.
        var dlgState = new GameState();
        dlgState.RegisterPlayer("aa_talker", "Talker");
        var dlgServer = new AuthoritativeWorldServer(dlgState, "dialogue-walker-test");
        ExpectEqual(string.Empty,
            dlgServer.GetActiveDialogueNodeId("aa_talker", "any_npc"),
            "GetActiveDialogueNodeId returns empty when no advance has occurred");

        // Tree node traversal end-to-end: root → advance choice → supplies node.
        if (DialogueRegistry.TryGet(DialogueRegistry.MaraClinicTreeId, out var walkerTree))
        {
            var rootChoices = DialogueRegistry.BuildChoiceArray(walkerTree.Root);
            var walkerAdvance = rootChoices.First(c => c.Id == "ask_about_supplies");
            ExpectTrue(walkerAdvance.ActionId.StartsWith("dialogue_advance:"),
                "branching choice projects ActionId as dialogue_advance:<node>");
            var supplies = walkerTree.Get("supplies");
            ExpectTrue(supplies is not null && supplies.Choices.Count >= 2,
                "supplies node has at least back-to-root + close choices");
            var closeChoice = walkerTree.Get("accepted").Choices[0];
            ExpectEqual("help_mara", closeChoice.ActionId,
                "accepted node's close choice carries its legacy ActionId through the tree");
        }

        // Sliced atlas props are registered in StructureArtCatalog and point at
        // the right sliced-PNG path so runtime can load them without a slicing
        // pass at game-startup.
        var slicedKinds = new[]
        {
            StructureSpriteKind.ClinicBed,
            StructureSpriteKind.MedicalCrate,
            StructureSpriteKind.SupplyDropParachute,
            StructureSpriteKind.ShopKiosk,
            StructureSpriteKind.AmmoCrateMetal,
            StructureSpriteKind.WantedMugShotFrame,
            StructureSpriteKind.JailBarredWindow,
            StructureSpriteKind.HandcuffsSilver,
            // Round 2 (this commit):
            StructureSpriteKind.GeneratorPristine,
            StructureSpriteKind.GeneratorWrecked,
            StructureSpriteKind.GreenhouseShattered,
            StructureSpriteKind.ElectricalBoxSparking,
            StructureSpriteKind.NoticeBoardCluttered,
            StructureSpriteKind.BedClean,
            StructureSpriteKind.SofaGrey,
            StructureSpriteKind.VendingMachine,
            StructureSpriteKind.MechanicalWorkbench,
            StructureSpriteKind.ElectronicsWorkshop,
            StructureSpriteKind.HydroponicsPlanterGrown,
            StructureSpriteKind.WoodChestClosed,
            StructureSpriteKind.OrnateChest,
            StructureSpriteKind.SafePadlock,
            StructureSpriteKind.ClinicDoor,
            StructureSpriteKind.AirlockDoorOpen,
            StructureSpriteKind.GateClosed,
            StructureSpriteKind.WindowBroken,
            StructureSpriteKind.Archway,
            // Round 3 (this commit):
            StructureSpriteKind.DuelPostSign,
            StructureSpriteKind.PosseBannerHanging,
            StructureSpriteKind.MailboxLitRed,
            StructureSpriteKind.GiftBoxBlue,
            StructureSpriteKind.ClinicExterior,
            StructureSpriteKind.JailBlockExterior,
            StructureSpriteKind.SupplyDropLandingPad,
            StructureSpriteKind.SafehouseTent,
            StructureSpriteKind.FireFlames,
            StructureSpriteKind.ToxicPoolGreen,
            StructureSpriteKind.AlertSirenRed,
            StructureSpriteKind.RumorBoardLabeled,
            StructureSpriteKind.DeliveryBoardLabeled,
            StructureSpriteKind.WagonSupplyWestern,
            StructureSpriteKind.PodSupplyScifi,
            StructureSpriteKind.FantasyShrine
        };
        foreach (var kind in slicedKinds)
        {
            ExpectTrue(StructureArtCatalog.All.ContainsKey(kind),
                $"StructureArtCatalog includes {kind}");
            var def = StructureArtCatalog.GetById(StructureArtCatalog.All[kind].Id);
            ExpectTrue(def.AtlasPath.Contains("/sliced/"),
                $"{kind} resolves to a sliced-atlas resource path");
            ExpectFalse(def.HasAtlasRegion,
                $"{kind} is a whole-PNG sliced prop (no atlas region required)");
        }

        // Event icon resolver: maps server event ids to sliced UI icon paths.
        ExpectEqual("supply_spawned",
            HudController.ResolveEventIconName("world1:42:supply_drop_spawned"),
            "ResolveEventIconName maps supply_drop_spawned to supply_spawned");
        ExpectEqual("clinic_revive",
            HudController.ResolveEventIconName("world1:42:clinic_revive"),
            "ResolveEventIconName maps clinic_revive");
        ExpectEqual("karma_break",
            HudController.ResolveEventIconName("world1:42:karma_break"),
            "ResolveEventIconName maps karma_break");
        ExpectEqual("bounty_claimed",
            HudController.ResolveEventIconName("world1:42:bounty_claimed"),
            "ResolveEventIconName maps bounty_claimed");
        ExpectEqual("ready_up",
            HudController.ResolveEventIconName("world1:0:player_ready"),
            "ResolveEventIconName maps player_ready to ready_up");
        ExpectEqual("danger_heat",
            HudController.ResolveEventIconName("world1:5:entered_lawless_zone"),
            "ResolveEventIconName maps entered_lawless_zone to danger_heat");
        ExpectEqual("interact_key_prompt",
            HudController.ResolveEventIconName("world1:9:door_opened"),
            "ResolveEventIconName maps door_opened to interact_key_prompt");
        ExpectEqual(string.Empty,
            HudController.ResolveEventIconName("world1:9:totally_unknown_event"),
            "ResolveEventIconName returns empty for unmapped events");
        ExpectTrue(HudController.ResolveEventIconPath("world1:42:supply_drop_spawned")
                .EndsWith("supply_spawned.png"),
            "ResolveEventIconPath returns the full sliced-atlas resource path");

        // FormatMatchSummary handles the no-summary case before the match ends.
        ExpectTrue(HudController.FormatMatchSummary(null).Contains("Match in progress"),
            "FormatMatchSummary returns 'Match in progress' when no summary is available");

        // Bounty leaderboard formatter parses bounty amounts from StatusEffects.
        ExpectEqual(25, HudController.ParseBountyAmount("Bounty: 25"),
            "ParseBountyAmount extracts the integer from a Bounty: N string");
        ExpectEqual(0, HudController.ParseBountyAmount("Wanted"),
            "ParseBountyAmount returns 0 for non-bounty status effects");

        // Quest module registry: built-in modules are registered at type-init,
        // Register is idempotent, and All exposes the live module list.
        ExpectTrue(QuestModuleRegistry.All.Contains(QuestModuleRegistry.Repair),
            "QuestModuleRegistry.All includes the Repair module");
        ExpectTrue(QuestModuleRegistry.All.Contains(QuestModuleRegistry.Posse),
            "QuestModuleRegistry.All includes the Posse module");
        var preCount = QuestModuleRegistry.All.Count;
        QuestModuleRegistry.Register(QuestModuleRegistry.Repair);
        ExpectEqual(preCount, QuestModuleRegistry.All.Count,
            "Re-registering an existing module is a no-op");
        ExpectTrue(QuestModuleRegistry.GetForCompletion(PosseQuestModule.PosseCompletionPrefix + "abc")
                == QuestModuleRegistry.Posse,
            "GetForCompletion resolves to the Posse module by completion prefix");

        // PlayerStatus model split: persistent statuses (ApplyStatus) are
        // surfaced separately from derived statuses (Downed, Lawless Zone,
        // Bounty: N, Inside: X, etc.) but both end up in the merged snapshot.
        var splitState = new GameState();
        splitState.RegisterPlayer("aa_split", "PSplit");
        splitState.SetPlayerPosition("aa_split", TilePosition.Origin);
        var splitServer = new AuthoritativeWorldServer(splitState, "status-split-test");
        foreach (var splitPid in splitServer.ConnectedPlayerIds)
            splitServer.ProcessIntent(new ServerIntent(splitPid, 1, IntentType.ReadyUp, new Dictionary<string, string>()));

        ExpectEqual(0, splitServer.GetPersistentStatuses("aa_split").Count,
            "persistent status set is empty by default");
        splitServer.ApplyStatus("aa_split", PlayerStatusKind.Poisoned);
        ExpectTrue(splitServer.GetPersistentStatuses("aa_split").Contains(PlayerStatusKind.Poisoned),
            "ApplyStatus adds the kind to the persistent set");
        splitServer.ClearStatus("aa_split", PlayerStatusKind.Poisoned);
        ExpectFalse(splitServer.GetPersistentStatuses("aa_split").Contains(PlayerStatusKind.Poisoned),
            "ClearStatus removes the kind from the persistent set");

        // Snapshot still surfaces both derived (Lawless Zone) and persistent
        // (Burning) entries in the same StatusEffects list.
        splitServer.MarkTileLawless(TilePosition.Origin);
        splitServer.ApplyStatus("aa_split", PlayerStatusKind.Burning);
        var splitSnap = splitServer.CreateInterestSnapshot("aa_split");
        var splitSelf = splitSnap.Players.First(p => p.Id == "aa_split");
        ExpectTrue(splitSelf.StatusEffects.Any(s => s == "Lawless Zone"),
            "snapshot includes derived 'Lawless Zone' status");
        ExpectTrue(splitSelf.StatusEffects.Any(s => s == "Burning"),
            "snapshot includes persistent 'Burning' status alongside derived");

        // Sequence guard exemptions: idempotent / unordered intents are not
        // gated by the monotonic sequence check.
        ExpectTrue(AuthoritativeWorldServer.IsSequenceExempt(IntentType.SendLocalChat),
            "SendLocalChat is exempt from strict sequence ordering");
        ExpectTrue(AuthoritativeWorldServer.IsSequenceExempt(IntentType.ReadyUp),
            "ReadyUp is exempt from strict sequence ordering");
        ExpectTrue(AuthoritativeWorldServer.IsSequenceExempt(IntentType.SelectDialogueChoice),
            "SelectDialogueChoice is exempt from strict sequence ordering");
        ExpectFalse(AuthoritativeWorldServer.IsSequenceExempt(IntentType.Move),
            "Move keeps strict sequence ordering");
        ExpectFalse(AuthoritativeWorldServer.IsSequenceExempt(IntentType.Attack),
            "Attack keeps strict sequence ordering (server cooldown is the spam guard)");

        // Behavior test: a chat intent with a stale sequence is still accepted.
        var sgState = new GameState();
        sgState.RegisterPlayer("aa_sgchat", "Chatter");
        sgState.SetPlayerPosition("aa_sgchat", TilePosition.Origin);
        var sgServer = new AuthoritativeWorldServer(sgState, "sequence-guard-test");
        foreach (var sgPid in sgServer.ConnectedPlayerIds)
            sgServer.ProcessIntent(new ServerIntent(sgPid, 50, IntentType.ReadyUp, new Dictionary<string, string>()));
        // Send a Move with high sequence, then a chat with a low sequence — chat
        // should still be accepted because chat is exempt from the guard.
        sgServer.ProcessIntent(new ServerIntent("aa_sgchat", 100, IntentType.Move,
            new Dictionary<string, string> { ["x"] = "1", ["y"] = "0" }));
        var sgChat = sgServer.ProcessIntent(new ServerIntent("aa_sgchat", 5, IntentType.SendLocalChat,
            new Dictionary<string, string> { ["text"] = "out of order chat" }));
        ExpectTrue(sgChat.WasAccepted,
            "out-of-order chat intent is still accepted under sequence-exempt rule");

        // Per-NPC vendor catalogues: SeedVendorCatalogue attaches offers to a
        // specific vendor; CreateVisibleShopOffers includes them; GetVendorCatalogue
        // surfaces the merged static + seeded list per vendor.
        var vcState = new GameState();
        vcState.RegisterPlayer("aa_vcbuyer", "Buyer");
        vcState.SetPlayerPosition("aa_vcbuyer", new TilePosition(6, 4));
        vcState.AddScrip("aa_vcbuyer", 200);
        var vcServer = new AuthoritativeWorldServer(vcState, "vendor-catalogue-test");
        foreach (var vcPid in vcServer.ConnectedPlayerIds)
            vcServer.ProcessIntent(new ServerIntent(vcPid, 1, IntentType.ReadyUp, new Dictionary<string, string>()));

        // Mara has no static offers in StarterShopCatalog. Seed a 2-offer catalogue
        // on her so she becomes a vendor without modifying the static catalog.
        vcServer.SeedVendorCatalogue(StarterNpcs.Mara.Id, new[]
        {
            new ShopOffer("vc_mara_patch", StarterNpcs.Mara.Id, StarterItems.MediPatchId, 8),
            new ShopOffer("vc_mara_kit", StarterNpcs.Mara.Id, StarterItems.RepairKitId, 18)
        });

        var vcMaraCatalogue = vcServer.GetVendorCatalogue(StarterNpcs.Mara.Id);
        ExpectEqual(2, vcMaraCatalogue.Count, "Mara's catalogue contains exactly the seeded offers");
        ExpectTrue(vcMaraCatalogue.All(o => o.VendorNpcId == StarterNpcs.Mara.Id),
            "all offers in Mara's catalogue point to her vendor id");

        // Move buyer next to Mara (3,4) and confirm seeded offers appear in snapshot
        vcState.SetPlayerPosition("aa_vcbuyer", new TilePosition(3, 4));
        var vcSnap = vcServer.CreateInterestSnapshot("aa_vcbuyer");
        var vcMaraOffers = vcSnap.ShopOffers.Where(o => o.VendorNpcId == StarterNpcs.Mara.Id).ToList();
        ExpectEqual(2, vcMaraOffers.Count, "snapshot includes seeded vendor catalogue offers");
        ExpectTrue(vcMaraOffers.Any(o => o.OfferId == "vc_mara_patch"),
            "seeded patch offer surfaces in snapshot");

        // Dallen still has his static catalog merged in
        var vcDallenCatalogue = vcServer.GetVendorCatalogue(StarterNpcs.Dallen.Id);
        ExpectTrue(vcDallenCatalogue.Count >= 5,
            "Dallen retains his static StarterShopCatalog offers");

        // Catalogues bound to a coercive vendor id: passing offers tagged with a
        // different vendor id is rebound to the target vendor on seed.
        vcServer.SeedVendorCatalogue(StarterNpcs.Mara.Id, new[]
        {
            new ShopOffer("vc_mara_template", "abstract_template_vendor", StarterItems.RationPackId, 5)
        });
        var rebound = vcServer.GetVendorCatalogue(StarterNpcs.Mara.Id)
            .First(o => o.Id == "vc_mara_template");
        ExpectEqual(StarterNpcs.Mara.Id, rebound.VendorNpcId,
            "SeedVendorCatalogue rebinds template offers to the target vendor id");

        // NPC role tags: starter NPCs ship with the right tag set so callers can
        // query function/alignment without string-contains checks on Role.
        ExpectTrue(StarterNpcs.Mara.HasTag(NpcRoleTags.Clinic),
            "Mara is tagged as a clinic NPC");
        ExpectTrue(StarterNpcs.Mara.HasTag(NpcRoleTags.LawAligned),
            "Mara is tagged as law-aligned");
        ExpectTrue(StarterNpcs.Dallen.HasTag(NpcRoleTags.Vendor),
            "Dallen is tagged as a vendor");
        ExpectTrue(StarterNpcs.Dallen.HasTag(NpcRoleTags.Clinic),
            "Dallen is tagged as a clinic NPC (he shares the clinic location)");
        ExpectFalse(StarterNpcs.Mara.HasTag(NpcRoleTags.Dealer),
            "Mara is not tagged as a dealer");

        // Synthetic player-list test: explicit Bounty status effects exercise
        // the formatter's sort + filter + top-N behavior.
        var blFakePlayers = new List<PlayerSnapshot>
        {
            new("aa_low", "LowBounty", 0, "Unmarked", 0, "", LeaderboardRole.None,
                0, 0, 100, 100, 0, PlayerAppearanceSelection.Default,
                System.Array.Empty<string>(), new Dictionary<EquipmentSlot, string>(),
                new[] { "Bounty: 10" }),
            new("ab_high", "HighBounty", 0, "Unmarked", 0, "", LeaderboardRole.None,
                0, 0, 100, 100, 0, PlayerAppearanceSelection.Default,
                System.Array.Empty<string>(), new Dictionary<EquipmentSlot, string>(),
                new[] { "Bounty: 75" }),
            new("ac_clean", "CleanPlayer", 0, "Unmarked", 0, "", LeaderboardRole.None,
                0, 0, 100, 100, 0, PlayerAppearanceSelection.Default,
                System.Array.Empty<string>(), new Dictionary<EquipmentSlot, string>(),
                System.Array.Empty<string>())
        };
        var blBoard = HudController.FormatBountyLeaderboard(blFakePlayers);
        ExpectTrue(blBoard.Contains("Top Bounties"),
            "FormatBountyLeaderboard shows header when bounties exist");
        ExpectTrue(blBoard.Contains("HighBounty") && blBoard.Contains("LowBounty"),
            "FormatBountyLeaderboard lists all players with active bounties");
        ExpectTrue(blBoard.IndexOf("HighBounty") < blBoard.IndexOf("LowBounty"),
            "FormatBountyLeaderboard sorts by bounty descending");
        ExpectFalse(blBoard.Contains("CleanPlayer"),
            "FormatBountyLeaderboard excludes players with no active bounty");
        ExpectTrue(HudController.FormatBountyLeaderboard(System.Array.Empty<PlayerSnapshot>())
                .Contains("none active"),
            "FormatBountyLeaderboard reports 'none active' when no bounties exist");
        var bountyBoard = HudController.FormatBountyBoard(blFakePlayers.Concat(new[]
        {
            new PlayerSnapshot("ad_wanted", "WantedPlayer", 0, "Unmarked", 0, "", LeaderboardRole.None,
                0, 0, 100, 100, 0, PlayerAppearanceSelection.Default,
                System.Array.Empty<string>(), new Dictionary<EquipmentSlot, string>(),
                new[] { "Wanted", "Bounty: 25" })
        }));
        ExpectTrue(bountyBoard.Contains("HighBounty") && bountyBoard.Contains("75 scrip"),
            "FormatBountyBoard lists bounty rows with scrip amount");
        ExpectTrue(bountyBoard.Contains("WantedPlayer") && bountyBoard.Contains("Wanted"),
            "FormatBountyBoard marks wanted players");

        var factionPanel = HudController.FormatFactionPanel(new[]
        {
            new FactionSnapshot(StarterFactions.FreeSettlersId, "aa_faction", 52),
            new FactionSnapshot(StarterFactions.CivicRepairGuildId, "aa_faction", -12),
            new FactionSnapshot(StarterFactions.BackroomMerchantsId, "aa_faction", -70)
        }, "aa_faction");
        ExpectTrue(factionPanel.Contains("Village Freeholders") && factionPanel.Contains("Loyal"),
            "FormatFactionPanel shows loyal standings for positive high reputation");
        ExpectTrue(factionPanel.Contains("Civic Repair Guild") && factionPanel.Contains("Wary"),
            "FormatFactionPanel shows wary standings for negative reputation");
        ExpectTrue(factionPanel.Contains("Backroom Merchants") && factionPanel.Contains("Hostile"),
            "FormatFactionPanel shows hostile standings for very negative reputation");
        ExpectEqual("Neutral", HudController.FormatFactionMood(0),
            "FormatFactionMood labels zero reputation neutral");

        ExpectTrue(HudController.FormatQuestLog(Array.Empty<QuestSnapshot>()).Contains("no active quests"),
            "FormatQuestLog reports empty active quest list");
        var questLog = HudController.FormatQuestLog(new[]
        {
            new QuestSnapshot(StarterQuests.MaraClinicFiltersId, QuestStatus.Active, 12, 0, 3, "Pick up a repair kit."),
            new QuestSnapshot(StarterQuests.GarrickBladeOrderId, QuestStatus.Active, 15, 2, 3, "Return to Garrick."),
            new QuestSnapshot(StarterQuests.MeriCellarStockId, QuestStatus.Completed, 10, 1, 1, string.Empty),
            new QuestSnapshot("unknown_side_job", QuestStatus.Active, 4, 0, 0, string.Empty)
        });
        ExpectTrue(questLog.Contains("Clinic Filters") && questLog.Contains("[1/3]"),
            "FormatQuestLog shows starter quest title and first step counter");
        ExpectTrue(questLog.Contains("Blade for the Watch") && questLog.Contains("[3/3]"),
            "FormatQuestLog shows final multi-step counter");
        ExpectTrue(questLog.Contains("unknown_side_job") && questLog.Contains("Ready to complete"),
            "FormatQuestLog falls back for unknown single-step quests");
        ExpectFalse(questLog.Contains("Cellar Stock"),
            "FormatQuestLog omits completed quests");

        // Hotbar formatter renders 9 slots; equipped slot is starred.
        var hbEmpty = HudController.FormatHotbar(System.Array.Empty<GameItem>(), -1);
        ExpectTrue(hbEmpty.Contains("[1") && hbEmpty.Contains("[9"),
            "FormatHotbar shows all 9 slots when inventory is empty");
        ExpectTrue(hbEmpty.Contains("—"),
            "empty hotbar slots show a dash placeholder");

        var hbInv = new List<GameItem>
        {
            StarterItems.PracticeStick,
            StarterItems.RepairKit,
            StarterItems.RationPack
        };
        var hbBar = HudController.FormatHotbar(hbInv, 1);
        ExpectTrue(hbBar.Contains("[2*"),
            "FormatHotbar marks the equipped slot with an asterisk");
        ExpectTrue(hbBar.Contains("Repair") || hbBar.Contains("Practi") || hbBar.Contains("Ration"),
            "hotbar shows item name fragments");

        var hbEquip = new Dictionary<EquipmentSlot, GameItem>
        {
            [EquipmentSlot.MainHand] = StarterItems.RepairKit
        };
        ExpectEqual(1, HudController.FindEquippedHotbarIndex(hbInv, hbEquip),
            "FindEquippedHotbarIndex locates the MainHand item by id");
        ExpectEqual(-1, HudController.FindEquippedHotbarIndex(hbInv, new Dictionary<EquipmentSlot, GameItem>()),
            "FindEquippedHotbarIndex returns -1 when nothing is equipped");
        var hbBindings = new Dictionary<int, string>();
        HudController.BindHotbarSlot(hbBindings, 2, StarterItems.RationPackId);
        ExpectEqual(StarterItems.RationPackId,
            HudController.ResolveHotbarSlotItemId(hbBindings, 2, hbInv),
            "hotbar binding map resolves slot to bound item id");
        var hbBoundBar = HudController.FormatHotbar(hbInv, -1, hbBindings);
        ExpectTrue(hbBoundBar.Contains("Ration"),
            "FormatHotbar renders bound item in its assigned slot");
        HudController.BindHotbarSlot(hbBindings, 2, string.Empty);
        ExpectEqual(string.Empty,
            HudController.ResolveHotbarSlotItemId(hbBindings, 2, hbInv),
            "empty hotbar binding clears the assigned slot");

        // Shop bubble rebuilds button rows on refresh — one button per visible
        // offer (browse) or per inventory item (sell), plus a Close button.
        var shopUiHud = new HudController();
        AddChild(shopUiHud);
        // Browse mode against an empty offer set still produces a Close button
        shopUiHud.OpenShopForVendor(StarterNpcs.Dallen.Id, sellMode: false);
        ExpectTrue(shopUiHud.IsShopOpen, "OpenShopForVendor opens panel");
        ExpectTrue(shopUiHud.ShopRowCount >= 1, "shop panel always has at least a Close row");
        shopUiHud.CloseShop();
        ExpectFalse(shopUiHud.IsShopOpen, "CloseShop closes panel");
        shopUiHud.QueueFree();

        // Dialogue choice picker UI: clicking browse/sell choices opens the shop
        // panel with the right vendor + mode and closes the dialogue.
        var dlgHud = new HudController();
        AddChild(dlgHud);
        var dlgChoices = new List<NpcDialogueChoice>
        {
            new("browse_wares", "Browse wares", "open_shop_browse"),
            new("sell_items", "Sell items", "open_shop_sell"),
            new("assist_need", "Help with the chore", "generated_assist_need")
        };
        var dlgSnapshot = new NpcDialogueSnapshot(
            StarterNpcs.Dallen.Id, StarterNpcs.Dallen.Name, "How can I help?", dlgChoices);

        dlgHud.OpenDialogue(dlgSnapshot, StarterNpcs.Dallen.Id);
        ExpectTrue(dlgHud.IsDialogueOpen, "OpenDialogue makes the dialogue panel visible");
        ExpectEqual(StarterNpcs.Dallen.Id, dlgHud.DialogueNpcId, "OpenDialogue records the active NPC id");
        ExpectEqual(3, dlgHud.LastDialogueSnapshot.Choices.Count, "OpenDialogue exposes the choice list");

        dlgHud.SelectDialogueChoice("browse_wares");
        ExpectFalse(dlgHud.IsDialogueOpen, "Selecting browse_wares closes the dialogue panel");
        ExpectTrue(dlgHud.IsShopOpen, "Selecting browse_wares opens the shop panel");
        ExpectEqual(StarterNpcs.Dallen.Id, dlgHud.ShopVendorNpcId, "Browse opens shop for the dialogued NPC");
        ExpectFalse(dlgHud.ShopSellMode, "Browse opens shop in browse mode");
        dlgHud.CloseShop();

        dlgHud.OpenDialogue(dlgSnapshot, StarterNpcs.Dallen.Id);
        dlgHud.SelectDialogueChoice("sell_items");
        ExpectFalse(dlgHud.IsDialogueOpen, "Selecting sell_items closes the dialogue panel");
        ExpectTrue(dlgHud.IsShopOpen, "Selecting sell_items opens the shop panel");
        ExpectTrue(dlgHud.ShopSellMode, "Sell opens shop in sell mode");
        dlgHud.CloseShop();

        // CloseDialogue clears state regardless of choice path
        dlgHud.OpenDialogue(dlgSnapshot, StarterNpcs.Dallen.Id);
        dlgHud.CloseDialogue();
        ExpectFalse(dlgHud.IsDialogueOpen, "CloseDialogue hides the panel");
        ExpectEqual(string.Empty, dlgHud.DialogueNpcId, "CloseDialogue clears the active NPC id");

        dlgHud.QueueFree();

        // ── Step 16: Clinic recovery hook ─────────────────────────────────────────
        // When a downed countdown expires near a clinic NPC and the player has
        // enough scrip, the server auto-revives them instead of triggering karma break.

        // Setup: patient near Mara (3,4) at Origin (distance²=25 ≤ 24²=576) with scrip
        var clinicState = new GameState();
        clinicState.RegisterPlayer("ae_attacker", "Striker");
        clinicState.RegisterPlayer("ae_patient", "Patient");
        clinicState.SetPlayerPosition("ae_attacker", TilePosition.Origin);
        clinicState.SetPlayerPosition("ae_patient", TilePosition.Origin);
        clinicState.AddScrip("ae_patient", 50);
        var clinicServer = new AuthoritativeWorldServer(clinicState, "clinic-test");

        clinicServer.ProcessIntent(new ServerIntent(
            "ae_attacker", 1, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ae_patient" }));
        clinicServer.AdvanceIdleTicks(5);
        clinicServer.ProcessIntent(new ServerIntent(
            "ae_attacker", 2, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ae_patient" }));
        clinicServer.AdvanceIdleTicks(5);
        clinicServer.ProcessIntent(new ServerIntent(
            "ae_attacker", 3, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ae_patient" }));
        ExpectTrue(clinicState.Players["ae_patient"].IsDown, "clinic test: patient is downed before countdown");

        clinicServer.AdvanceIdleTicks(AuthoritativeWorldServer.DownedCountdownTicks + 1);
        var clinicReviveEvent = clinicServer.EventLog[^1];
        ExpectTrue(clinicReviveEvent.EventId.Contains("clinic_revive"), "clinic revive emits clinic_revive event");
        ExpectFalse(clinicState.Players["ae_patient"].IsDown, "clinic-revived patient is no longer downed");
        ExpectEqual(AuthoritativeWorldServer.ClinicReviveHealAmount, clinicState.Players["ae_patient"].Health,
            "clinic revive heals patient to ClinicReviveHealAmount");
        ExpectEqual(50 - AuthoritativeWorldServer.ClinicReviveCost, clinicState.Players["ae_patient"].Scrip,
            "clinic revive deducts ClinicReviveCost scrip");
        ExpectTrue(clinicState.Players["ae_patient"].IsAlive, "clinic-revived patient remains alive");

        // Setup: patient near clinic but no scrip → karma break instead
        var noscripState = new GameState();
        noscripState.RegisterPlayer("af_attacker", "Striker");
        noscripState.RegisterPlayer("af_broke", "Broke");
        noscripState.SetPlayerPosition("af_attacker", TilePosition.Origin);
        noscripState.SetPlayerPosition("af_broke", TilePosition.Origin);
        var noscripServer = new AuthoritativeWorldServer(noscripState, "noscrip-test");

        noscripServer.ProcessIntent(new ServerIntent(
            "af_attacker", 1, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "af_broke" }));
        noscripServer.AdvanceIdleTicks(5);
        noscripServer.ProcessIntent(new ServerIntent(
            "af_attacker", 2, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "af_broke" }));
        noscripServer.AdvanceIdleTicks(5);
        noscripServer.ProcessIntent(new ServerIntent(
            "af_attacker", 3, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "af_broke" }));

        noscripServer.AdvanceIdleTicks(AuthoritativeWorldServer.DownedCountdownTicks + 1);
        ExpectTrue(noscripServer.EventLog[^1].EventId.Contains("player_respawned"),
            "near clinic but no scrip → karma break (player_respawned)");
        ExpectFalse(noscripState.Players["af_broke"].IsDown,
            "no-scrip player is no longer downed after karma break");

        // Setup: patient far from clinic (50,50) with scrip → karma break instead
        var farState = new GameState();
        farState.RegisterPlayer("ag_attacker", "Striker");
        farState.RegisterPlayer("ag_far", "Wanderer");
        farState.SetPlayerPosition("ag_attacker", new TilePosition(50, 50));
        farState.SetPlayerPosition("ag_far", new TilePosition(50, 50));
        farState.AddScrip("ag_far", 50);
        var farServer = new AuthoritativeWorldServer(farState, "far-test");

        farServer.ProcessIntent(new ServerIntent(
            "ag_attacker", 1, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ag_far" }));
        farServer.AdvanceIdleTicks(5);
        farServer.ProcessIntent(new ServerIntent(
            "ag_attacker", 2, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ag_far" }));
        farServer.AdvanceIdleTicks(5);
        farServer.ProcessIntent(new ServerIntent(
            "ag_attacker", 3, IntentType.Attack,
            new System.Collections.Generic.Dictionary<string, string> { ["targetId"] = "ag_far" }));

        farServer.AdvanceIdleTicks(AuthoritativeWorldServer.DownedCountdownTicks + 1);
        ExpectTrue(farServer.EventLog[^1].EventId.Contains("player_respawned"),
            "far from clinic with scrip → karma break (player_respawned)");

        // HUD formats clinic_revive event
        var clinicHudEvent = new ServerEvent(
            "clinic_revive",
            "clinic-test",
            1L,
            "Patient was revived by the clinic.",
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["playerId"] = "Patient",
                ["healAmount"] = AuthoritativeWorldServer.ClinicReviveHealAmount.ToString(),
                ["scripCost"] = AuthoritativeWorldServer.ClinicReviveCost.ToString()
            });
        var clinicHudText = HudController.FormatLatestServerEvent(new[] { clinicHudEvent });
        ExpectTrue(
            clinicHudText.Contains("clinic") &&
            clinicHudText.Contains(AuthoritativeWorldServer.ClinicReviveHealAmount.ToString()) &&
            clinicHudText.Contains(AuthoritativeWorldServer.ClinicReviveCost.ToString()),
            "HUD formats clinic_revive event with heal amount and scrip cost");

        // ── Step 10: Chat tabs — Local / Posse / System ──────────────────────────
        var chatTabState = new GameState();
        chatTabState.RegisterPlayer("ct_alpha", "Alpha");
        chatTabState.RegisterPlayer("ct_beta", "Beta");
        chatTabState.RegisterPlayer("ct_outsider", "Outsider");
        chatTabState.SetPlayerPosition("ct_alpha", TilePosition.Origin);
        chatTabState.SetPlayerPosition("ct_beta", TilePosition.Origin);
        chatTabState.SetPlayerPosition("ct_outsider", TilePosition.Origin);
        var chatTabServer = new AuthoritativeWorldServer(chatTabState, "chat-tab-world");

        var ctNoPosse = chatTabServer.ProcessIntent(new ServerIntent(
            "ct_alpha", 1, IntentType.SendPosseChat,
            new System.Collections.Generic.Dictionary<string, string> { ["text"] = "hello posse" }));
        ExpectFalse(ctNoPosse.WasAccepted, "SendPosseChat rejected when not in a posse");

        chatTabServer.ProcessIntent(new ServerIntent(
            "ct_alpha", 2, IntentType.InvitePosse,
            new System.Collections.Generic.Dictionary<string, string> { ["targetPlayerId"] = "ct_beta" }));
        chatTabServer.ProcessIntent(new ServerIntent(
            "ct_beta", 1, IntentType.AcceptPosse,
            new System.Collections.Generic.Dictionary<string, string>()));

        var ctPosseChat = chatTabServer.ProcessIntent(new ServerIntent(
            "ct_alpha", 3, IntentType.SendPosseChat,
            new System.Collections.Generic.Dictionary<string, string> { ["text"] = "posse only message" }));
        ExpectTrue(ctPosseChat.WasAccepted, "SendPosseChat accepted when in a posse");

        var ctBetaSnapshot = chatTabServer.CreateInterestSnapshot("ct_beta");
        ExpectTrue(ctBetaSnapshot.LocalChatMessages.Any(m => m.Text == "posse only message" && m.Channel == "posse"),
            "posse chat message visible to posse member with posse channel tag");

        var ctOutsiderSnapshot = chatTabServer.CreateInterestSnapshot("ct_outsider");
        ExpectFalse(ctOutsiderSnapshot.LocalChatMessages.Any(m => m.Text == "posse only message"),
            "posse chat message not visible to players outside the posse");

        var ctHudPosse = HudController.FormatLocalChatSummary(ctBetaSnapshot.LocalChatMessages);
        ExpectTrue(ctHudPosse.Contains("[Posse]"), "HUD formats posse channel messages with [Posse] prefix");
        ExpectTrue(ctHudPosse.Contains("Alpha"), "HUD includes speaker name in posse chat summary");

        // ── Step 11: Interior audibility filtering ───────────────────────────────
        var interiorState = new GameState();
        interiorState.RegisterPlayer("interior_speaker", "Speaker");
        interiorState.RegisterPlayer("interior_nearby", "Nearby");
        interiorState.RegisterPlayer("interior_far", "FarListener");
        interiorState.SetPlayerPosition("interior_speaker", TilePosition.Origin);
        interiorState.SetPlayerPosition("interior_nearby", new TilePosition(2, 0));
        interiorState.SetPlayerPosition("interior_far", new TilePosition(10, 0));
        var interiorServer = new AuthoritativeWorldServer(interiorState, "interior-world");
        interiorServer.SeedWorldStructure("test_structure", "Test Building", "greenhouse", TilePosition.Origin);

        interiorServer.ProcessIntent(new ServerIntent(
            "interior_speaker", 1, IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "test_structure",
                ["action"] = "enter"
            }));

        interiorServer.ProcessIntent(new ServerIntent(
            "interior_speaker", 2, IntentType.SendLocalChat,
            new System.Collections.Generic.Dictionary<string, string> { ["text"] = "inside voice" }));

        var nearbyOutsideSnapshot = interiorServer.CreateInterestSnapshot("interior_nearby");
        var insideChatNearby = nearbyOutsideSnapshot.LocalChatMessages.FirstOrDefault(m => m.Text == "inside voice");
        ExpectTrue(insideChatNearby is not null, "interior speaker chat still audible to nearby outside listener within halved range");
        ExpectTrue(insideChatNearby?.Volume < 1.0f, "interior chat volume is attenuated for outside listeners");

        var farOutsideSnapshot = interiorServer.CreateInterestSnapshot("interior_far");
        ExpectFalse(farOutsideSnapshot.LocalChatMessages.Any(m => m.Text == "inside voice"),
            "interior speaker chat not audible beyond halved max range from outside");

        interiorServer.ProcessIntent(new ServerIntent(
            "interior_speaker", 3, IntentType.Interact,
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["entityId"] = "test_structure",
                ["action"] = "exit"
            }));
        interiorServer.ProcessIntent(new ServerIntent(
            "interior_speaker", 4, IntentType.SendLocalChat,
            new System.Collections.Generic.Dictionary<string, string> { ["text"] = "exited voice" }));

        var exitedSnapshot = interiorServer.CreateInterestSnapshot("interior_far");
        ExpectTrue(exitedSnapshot.LocalChatMessages.Any(m => m.Text == "exited voice"),
            "post-exit local chat has full range from outside listener");

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

    private static int CountImagePixelDifferences(Image firstImage, Image secondImage)
    {
        if (firstImage.GetWidth() != secondImage.GetWidth() || firstImage.GetHeight() != secondImage.GetHeight())
        {
            return int.MaxValue;
        }

        var differences = 0;
        for (var y = 0; y < firstImage.GetHeight(); y++)
        {
            for (var x = 0; x < firstImage.GetWidth(); x++)
            {
                if (!firstImage.GetPixel(x, y).IsEqualApprox(secondImage.GetPixel(x, y)))
                {
                    differences++;
                }
            }
        }

        return differences;
    }

    private static int CountPixelDifferences(Image image, Vector2I firstFrame, Vector2I secondFrame, int frameSize = CharacterSheetLayout.StandardFrameSize)
    {
        return CountPixelDifferences(image, firstFrame, secondFrame, new Vector2I(frameSize, frameSize));
    }

    private static int CountPixelDifferences(Image image, Vector2I firstFrame, Vector2I secondFrame, Vector2I frameSize)
    {
        var differences = 0;
        for (var y = 0; y < frameSize.Y; y++)
        {
            for (var x = 0; x < frameSize.X; x++)
            {
                var first = image.GetPixel(
                    (firstFrame.X * frameSize.X) + x,
                    (firstFrame.Y * frameSize.Y) + y);
                var second = image.GetPixel(
                    (secondFrame.X * frameSize.X) + x,
                    (secondFrame.Y * frameSize.Y) + y);
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
