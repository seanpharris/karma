using Godot;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Data;
using Karma.Net;

namespace Karma.World;

public partial class WorldRoot : Node2D
{
    public const long LocalChatBubbleVisibleTicks = 10;
    private const string StaticPrototypePeerId = "peer_stand_in";
    private readonly Dictionary<string, Node2D> _renderedServerNpcs = new();
    private readonly Dictionary<string, Node2D> _renderedRemotePlayers = new();
    private readonly Dictionary<string, Label> _renderedChatBubbles = new();
    private readonly Dictionary<string, Node2D> _renderedServerItems = new();
    private readonly Dictionary<string, Node2D> _renderedServerStructures = new();
    private readonly List<PickupObject> _prototypeCatalogPickups = new();
    private readonly List<Node2D> _prototypeStructureShowcase = new();
    private readonly List<Node2D> _prototypeBuildingShowcase = new();
    private readonly List<Node2D> _prototypeBoardingSchoolProps = new();
    private readonly List<Node2D> _prototypeBoardingSchoolTrees = new();
    private readonly List<Node2D> _prototypeBoardingSchoolFlowers = new();
    private static readonly bool RenderGeneratedPrototypeActors = false;
    private PrototypeWanderingNpc _pixellabTrialWalker;
    private GeneratedTileMapRenderer _tileMapRenderer;
    private PrototypeServerSession _serverSession;

    public WorldConfig Config { get; private set; } = WorldConfig.CreatePrototype();
    public GeneratedWorld GeneratedWorld { get; private set; } =
        WorldGenerator.Generate(WorldConfig.CreatePrototype());

    public override void _Ready()
    {
        GeneratedWorld = WorldGenerator.Generate(Config);
        GD.Print(
            $"Generated starter world: {GeneratedWorld.Summary}, " +
            $"{Config.WidthTiles}x{Config.HeightTiles}, max players {Config.Server.MaxPlayers}");
        CreateTileMapRenderer();
        RemoveNonGeminiScenePrototypeActors();

        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        if (_serverSession is not null)
        {
            _serverSession.SetTileMap(GeneratedWorld.TileMap);
            _serverSession.SeedGeneratedWorldContent(GeneratedWorld);
            _serverSession.LocalSnapshotChanged += OnLocalSnapshotChanged;
            RenderSnapshot(_serverSession.LastLocalSnapshot);
        }
        else
        {
            _tileMapRenderer.SetTileMap(GeneratedWorld.TileMap, ThemeArtRegistry.GetForTheme(GeneratedWorld.Theme));
        }

        CreatePrototypeCatalogPickups();
        CreatePrototypeStructureShowcase();
        CreatePrototypeBuildingShowcase();
        CreatePrototypeBoardingSchoolProps();
        CreatePrototypeBoardingSchoolTrees();
        CreatePrototypeBoardingSchoolFlowers();
        CreatePixellabTrialWalker();
    }

    private void OnLocalSnapshotChanged(string snapshotSummary)
    {
        if (_serverSession is not null)
        {
            RenderSnapshot(_serverSession.LastLocalSnapshot);
        }
    }

    private void CreateTileMapRenderer()
    {
        _tileMapRenderer = new GeneratedTileMapRenderer
        {
            Name = "GeneratedTileMap",
            ZIndex = TopDownDepth.TileLayerZ
        };
        AddChild(_tileMapRenderer);
    }

    private void RenderSnapshot(ClientInterestSnapshot snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        _tileMapRenderer.SetChunks(snapshot.MapChunks, ThemeArtRegistry.GetForTheme(GeneratedWorld.Theme));
        if (RenderGeneratedPrototypeActors)
        {
            RenderServerNpcs(snapshot);
            RenderRemotePlayers(snapshot);
            RenderServerStructures(snapshot);
            RenderServerItems(snapshot);
            RenderLocalChatBubbles(snapshot);
        }
    }

    private void RenderServerNpcs(ClientInterestSnapshot snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        var visibleNpcIds = snapshot.Npcs
            .Where(ShouldRenderServerNpc)
            .Select(npc => npc.Id)
            .ToHashSet();
        foreach (var removedId in _renderedServerNpcs.Keys.Where(id => !visibleNpcIds.Contains(id)).ToArray())
        {
            _renderedServerNpcs[removedId].QueueFree();
            _renderedServerNpcs.Remove(removedId);
        }

        foreach (var npc in snapshot.Npcs.Where(ShouldRenderServerNpc))
        {
            var position = new Vector2(npc.TileX * 32f, npc.TileY * 32f);
            if (_renderedServerNpcs.TryGetValue(npc.Id, out var existing))
            {
                existing.Position = position;
                TopDownDepth.Apply(existing);
                continue;
            }

            var spriteKind = PrototypeSpriteCatalog.GetKindForNpc(npc.Id);
            var node = new ServerNpcObject
            {
                Name = npc.Id,
                NpcId = npc.Id,
                DisplayName = npc.Name,
                Role = npc.Role,
                Faction = npc.Faction,
                SpriteKind = spriteKind,
                Position = position
            };
            node.ZIndex = TopDownDepth.CalculateZIndex(position.Y);
            node.AddChild(new PrototypeCharacterSprite
            {
                Kind = spriteKind
            });
            node.AddChild(new CollisionShape2D
            {
                Shape = new CircleShape2D
                {
                    Radius = 24f
                }
            });
            AddChild(node);
            _renderedServerNpcs[npc.Id] = node;
        }
    }

    private void RenderRemotePlayers(ClientInterestSnapshot snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        var visiblePlayerIds = snapshot.Players
            .Where(player => ShouldRenderRemotePlayer(snapshot, player))
            .Select(player => player.Id)
            .ToHashSet();
        foreach (var removedId in _renderedRemotePlayers.Keys.Where(id => !visiblePlayerIds.Contains(id)).ToArray())
        {
            _renderedRemotePlayers[removedId].QueueFree();
            _renderedRemotePlayers.Remove(removedId);
        }

        foreach (var player in snapshot.Players.Where(candidate => ShouldRenderRemotePlayer(snapshot, candidate)))
        {
            var position = new Vector2(player.TileX * 32f, player.TileY * 32f);
            if (_renderedRemotePlayers.TryGetValue(player.Id, out var existing))
            {
                existing.Position = position;
                TopDownDepth.Apply(existing);
                existing.GetNodeOrNull<PrototypeCharacterSprite>("RemotePlayerSprite")?.ApplyPlayerAppearanceSelection(player.Appearance);
                var label = existing.GetNodeOrNull<Label>("RemotePlayerName");
                if (label is not null)
                {
                    label.Text = player.DisplayName;
                }

                continue;
            }

            var node = new Node2D
            {
                Name = $"RemotePlayer_{player.Id}",
                Position = position
            };
            node.ZIndex = TopDownDepth.CalculateZIndex(position.Y);
            var sprite = new PrototypeCharacterSprite
            {
                Name = "RemotePlayerSprite",
                Kind = PrototypeSpriteKind.Player
            };
            sprite.ApplyPlayerAppearanceSelection(player.Appearance);
            node.AddChild(sprite);
            node.AddChild(new Label
            {
                Name = "RemotePlayerName",
                Text = player.DisplayName,
                Position = new Vector2(-48f, -70f),
                Size = new Vector2(96f, 18f),
                HorizontalAlignment = HorizontalAlignment.Center,
                ZIndex = TopDownDepth.HudOffsetZ
            });
            AddChild(node);
            _renderedRemotePlayers[player.Id] = node;
        }
    }

    private void CreatePixellabTrialWalker()
    {
        if (!FileAccess.FileExists(PrototypeSpriteCatalog.PixellabTrialNpcRuntimeAtlasPath) || _pixellabTrialWalker is not null)
        {
            return;
        }

        _pixellabTrialWalker = new PrototypeWanderingNpc
        {
            Name = "PixellabTrialWalker",
            Position = new Vector2(420f, 300f),
            WalkRadius = 42f,
            WalkSpeed = 14f,
            VerticalPatrolSlowdown = 6f,
            HorizontalOnly = false,
            SpriteKind = PrototypeSpriteKind.PixellabTrialNpc
        };
        AddChild(_pixellabTrialWalker);
    }

    public static bool ShouldRenderRemotePlayer(ClientInterestSnapshot snapshot, PlayerSnapshot player)
    {
        return snapshot is not null &&
            player is not null &&
            player.Id != snapshot.PlayerId &&
            player.Id != StaticPrototypePeerId;
    }

    private void RenderLocalChatBubbles(ClientInterestSnapshot snapshot)
    {
        var recentMessages = snapshot.LocalChatMessages
            .Where(message => IsChatBubbleFresh(snapshot, message))
            .GroupBy(message => message.SpeakerId)
            .Select(group => group.OrderBy(message => message.Tick).Last())
            .ToDictionary(message => message.SpeakerId);

        foreach (var removedId in _renderedChatBubbles.Keys.Where(id => !recentMessages.ContainsKey(id)).ToArray())
        {
            _renderedChatBubbles[removedId].QueueFree();
            _renderedChatBubbles.Remove(removedId);
        }

        foreach (var message in recentMessages.Values)
        {
            var position = new Vector2(message.SpeakerTileX * 32f - 80f, message.SpeakerTileY * 32f - 58f);
            if (_renderedChatBubbles.TryGetValue(message.SpeakerId, out var existing))
            {
                existing.Text = FormatChatBubbleText(message);
                existing.Position = position;
                continue;
            }

            var bubble = new Label
            {
                Name = $"ChatBubble_{message.SpeakerId}",
                Text = FormatChatBubbleText(message),
                Position = position,
                Size = new Vector2(160f, 42f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                ZIndex = TopDownDepth.HudOffsetZ
            };
            AddChild(bubble);
            _renderedChatBubbles[message.SpeakerId] = bubble;
        }
    }

    public static bool IsChatBubbleFresh(ClientInterestSnapshot snapshot, LocalChatMessageSnapshot message)
    {
        return snapshot is not null &&
            message is not null &&
            snapshot.Tick - message.Tick <= LocalChatBubbleVisibleTicks;
    }

    public static string FormatChatBubbleText(LocalChatMessageSnapshot message)
    {
        var text = string.IsNullOrWhiteSpace(message.Text) ? "..." : message.Text.Trim();
        if (text.Length > 56)
        {
            text = text[..53] + "...";
        }

        return $"{message.SpeakerName}: {text}";
    }

    private void RenderServerStructures(ClientInterestSnapshot snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        var visibleStructureIds = snapshot.Structures
            .Select(structure => structure.EntityId)
            .ToHashSet();
        foreach (var removedId in _renderedServerStructures.Keys.Where(id => !visibleStructureIds.Contains(id)).ToArray())
        {
            _renderedServerStructures[removedId].QueueFree();
            _renderedServerStructures.Remove(removedId);
        }

        foreach (var structure in snapshot.Structures)
        {
            if (_renderedServerStructures.TryGetValue(structure.EntityId, out var existing))
            {
                if (existing is ServerStructureObject existingStructure)
                {
                    existingStructure.StructureName = structure.Name;
                    existingStructure.InteractionPrompt = structure.InteractionPrompt;
                    existingStructure.IsInteractable = structure.IsInteractable;
                }

                continue;
            }

            var node = new ServerStructureObject
            {
                Name = structure.EntityId,
                EntityId = structure.EntityId,
                StructureName = structure.Name,
                InteractionPrompt = structure.InteractionPrompt,
                IsInteractable = structure.IsInteractable,
                Position = new Vector2(structure.TileX * 32f, structure.TileY * 32f)
            };
            node.ZIndex = TopDownDepth.CalculateZIndex(node.Position.Y, TopDownDepth.StructureOffsetZ);
            var sprite = new StructureSprite
            {
                StructureId = structure.StructureId
            };
            node.AddChild(sprite);
            node.AddChild(new CollisionShape2D
            {
                Shape = new RectangleShape2D
                {
                    Size = new Vector2(
                        Mathf.Max(32f, structure.WidthPx),
                        Mathf.Max(32f, structure.HeightPx))
                },
                Position = new Vector2(0f, -Mathf.Max(32f, structure.HeightPx) * 0.5f)
            });
            AddChild(node);
            _renderedServerStructures[structure.EntityId] = node;
        }
    }

    private void RenderServerItems(ClientInterestSnapshot snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        var visibleServerItemIds = snapshot.WorldItems
            .Where(IsDynamicWorldItem)
            .Select(item => item.EntityId)
            .ToHashSet();
        foreach (var removedId in _renderedServerItems.Keys.Where(id => !visibleServerItemIds.Contains(id)).ToArray())
        {
            _renderedServerItems[removedId].QueueFree();
            _renderedServerItems.Remove(removedId);
        }

        foreach (var item in snapshot.WorldItems)
        {
            if (!IsDynamicWorldItem(item) || _renderedServerItems.ContainsKey(item.EntityId))
            {
                continue;
            }

            var node = new ServerWorldItemObject
            {
                Name = item.EntityId,
                EntityId = item.EntityId,
                ItemId = item.ItemId,
                DropOwnerName = item.DropOwnerName,
                Position = new Vector2(item.TileX * 32f, item.TileY * 32f)
            };
            node.ZIndex = TopDownDepth.CalculateZIndex(node.Position.Y, TopDownDepth.ItemOffsetZ);
            var marker = new PrototypeAtlasSprite
            {
                Name = $"{item.ItemId}_sprite",
                Kind = PrototypeSpriteCatalog.GetKindForItem(item.ItemId),
                DrawShadow = false
            };
            var collision = new CollisionShape2D
            {
                Shape = new CircleShape2D { Radius = 20f }
            };
            node.AddChild(marker);
            node.AddChild(collision);
            AddChild(node);
            _renderedServerItems[item.EntityId] = node;
        }
    }

    private void RemoveNonGeminiScenePrototypeActors()
    {
        GetNodeOrNull<Node>("Npc")?.QueueFree();
        GetNodeOrNull<Node>("PeerStandIn")?.QueueFree();

        foreach (var pickup in GetChildren().OfType<PickupObject>().ToArray())
        {
            if (!StarterItems.TryGetById(pickup.ItemId, out var item) || !IsPolishedPrototypeItem(item))
            {
                pickup.QueueFree();
            }
        }
    }

    private void CreatePrototypeCatalogPickups()
    {
        if (_prototypeCatalogPickups.Count > 0)
        {
            return;
        }

        var sceneItemIds = GetChildren()
            .OfType<PickupObject>()
            .Select(pickup => pickup.ItemId)
            .ToHashSet();
        var itemsToShowcase = StarterItems.All
            .Where(item => !sceneItemIds.Contains(item.Id))
            .Where(IsPolishedPrototypeItem)
            .ToArray();

        for (var index = 0; index < itemsToShowcase.Length; index++)
        {
            var item = itemsToShowcase[index];
            var pickup = CreatePrototypeCatalogPickup(item, index);
            AddChild(pickup);
            _prototypeCatalogPickups.Add(pickup);
        }
    }

    private static PickupObject CreatePrototypeCatalogPickup(GameItem item, int index)
    {
        var pickup = new PickupObject
        {
            Name = $"Catalog_{item.Id}",
            EntityId = $"catalog_{item.Id}",
            ItemId = item.Id,
            Position = CalculateCatalogShowcasePosition(index)
        };
        pickup.AddChild(new PrototypeAtlasSprite
        {
            Name = $"{item.Id}_sprite",
            Kind = PrototypeSpriteCatalog.GetKindForItem(item.Id),
            DrawShadow = false
        });
        pickup.AddChild(new CollisionShape2D
        {
            Shape = new CircleShape2D
            {
                Radius = 18f
            }
        });
        return pickup;
    }

    public static Vector2 CalculateCatalogShowcasePosition(int index)
    {
        const int columns = 7;
        var safeIndex = Mathf.Max(0, index);
        var column = safeIndex % columns;
        var row = safeIndex / columns;
        return new Vector2(500f + (column * 48f), 180f + (row * 48f));
    }

    private void CreatePrototypeBuildingShowcase()
    {
        if (_prototypeBuildingShowcase.Count > 0 || GeneratedWorld.Theme is not ("boarding_school" or "boarding-school"))
        {
            return;
        }

        var buildingKinds = new[]
        {
            StructureSpriteKind.BoardingSchoolMainHall,
            StructureSpriteKind.BoardingSchoolNoticeBoard,
            StructureSpriteKind.BoardingSchoolFountain,
            StructureSpriteKind.BoardingSchoolStudentRooms,
            StructureSpriteKind.BoardingSchoolCommonRoom,
            StructureSpriteKind.BoardingSchoolClassroom,
            StructureSpriteKind.BoardingSchoolFacultyOffice,
            StructureSpriteKind.BoardingSchoolLibrary
        };

        for (var index = 0; index < buildingKinds.Length; index++)
        {
            var kind = buildingKinds[index];
            var definition = StructureArtCatalog.Get(kind);
            var node = new StaticBody2D
            {
                Name = $"Building_{definition.Id}",
                Position = CalculateBoardingSchoolBuildingPosition(index)
            };
            node.ZIndex = TopDownDepth.CalculateZIndex(node.Position.Y, TopDownDepth.StructureOffsetZ);
            node.AddChild(new StructureSprite
            {
                Name = $"{definition.Id}_sprite",
                Kind = kind,
                DrawShadow = false
            });
            node.AddChild(new CollisionShape2D
            {
                Name = $"{definition.Id}_collision",
                Shape = new RectangleShape2D
                {
                    Size = CalculateBoardingSchoolBuildingCollisionSize(definition)
                },
                Position = CalculateBoardingSchoolBuildingCollisionOffset(definition)
            });
            AddChild(node);
            _prototypeBuildingShowcase.Add(node);
        }
    }

    public static Vector2 CalculateBoardingSchoolBuildingPosition(int index)
    {
        return index switch
        {
            0 => new Vector2(14f * 32f, 22f * 32f),
            1 => new Vector2(35f * 32f, 10f * 32f),
            2 => new Vector2(45f * 32f, 10f * 32f),
            3 => new Vector2(58f * 32f, 22f * 32f),
            4 => new Vector2(12f * 32f, 43f * 32f),
            5 => new Vector2(32f * 32f, 43f * 32f),
            6 => new Vector2(52f * 32f, 43f * 32f),
            7 => new Vector2(70f * 32f, 62f * 32f),
            _ => new Vector2(16f * 32f, 16f * 32f)
        };
    }

    public static Vector2 CalculateBoardingSchoolBuildingCollisionSize(StructureSpriteDefinition definition)
    {
        if (definition.Kind == StructureSpriteKind.BoardingSchoolNoticeBoard ||
            definition.Kind == StructureSpriteKind.BoardingSchoolFountain)
        {
            return new Vector2(
                Mathf.Max(24f, definition.Size.X * 0.45f),
                Mathf.Clamp(definition.Size.Y * 0.18f, 18f, 48f));
        }

        return new Vector2(
            Mathf.Max(48f, definition.Size.X * 0.46f),
            Mathf.Clamp(definition.Size.Y * 0.10f, 24f, 56f));
    }

    public static Vector2 CalculateBoardingSchoolBuildingCollisionOffset(StructureSpriteDefinition definition)
    {
        var size = CalculateBoardingSchoolBuildingCollisionSize(definition);
        return new Vector2(0f, -size.Y * 0.35f);
    }

    private void CreatePrototypeBoardingSchoolProps()
    {
        if (_prototypeBoardingSchoolProps.Count > 0 || GeneratedWorld.Theme is not ("boarding_school" or "boarding-school"))
        {
            return;
        }

        var propKinds = new[]
        {
            StructureSpriteKind.BoardingSchoolCourtyardTree,
            StructureSpriteKind.BoardingSchoolStoneBench,
            StructureSpriteKind.BoardingSchoolHedgerowSegment,
            StructureSpriteKind.BoardingSchoolStatuePlinth,
            StructureSpriteKind.BoardingSchoolBoulderCluster,
            StructureSpriteKind.BoardingSchoolOldLanternPost,
            StructureSpriteKind.BoardingSchoolIronFenceStraight,
            StructureSpriteKind.BoardingSchoolIronFenceGate,
            StructureSpriteKind.BoardingSchoolBookStack,
            StructureSpriteKind.BoardingSchoolParchmentPile,
            StructureSpriteKind.BoardingSchoolSchoolBag,
            StructureSpriteKind.BoardingSchoolBroomBucket,
            StructureSpriteKind.BoardingSchoolWoodenChair,
            StructureSpriteKind.BoardingSchoolStudyTable,
            StructureSpriteKind.BoardingSchoolNoticeStand,
            StructureSpriteKind.BoardingSchoolPrankBox
        };

        for (var index = 0; index < propKinds.Length; index++)
        {
            var kind = propKinds[index];
            var definition = StructureArtCatalog.Get(kind);
            var node = new StaticBody2D
            {
                Name = $"Prop_{definition.Id}",
                Position = CalculateBoardingSchoolPropPosition(index)
            };
            node.ZIndex = TopDownDepth.CalculateZIndex(node.Position.Y, TopDownDepth.ItemOffsetZ);
            node.AddChild(new StructureSprite
            {
                Name = $"{definition.Id}_sprite",
                Kind = kind,
                DrawShadow = false
            });
            node.AddChild(new CollisionShape2D
            {
                Name = $"{definition.Id}_collision",
                Shape = new RectangleShape2D
                {
                    Size = CalculateBoardingSchoolPropCollisionSize(definition)
                },
                Position = CalculateBoardingSchoolPropCollisionOffset(definition)
            });
            AddChild(node);
            _prototypeBoardingSchoolProps.Add(node);
        }
    }

    public static Vector2 CalculateBoardingSchoolPropPosition(int index)
    {
        return index switch
        {
            0 => new Vector2(5f * 32f, 14f * 32f),
            1 => new Vector2(29f * 32f, 15f * 32f),
            2 => new Vector2(20f * 32f, 12f * 32f),
            3 => new Vector2(44f * 32f, 17f * 32f),
            4 => new Vector2(72f * 32f, 14f * 32f),
            5 => new Vector2(50f * 32f, 19f * 32f),
            6 => new Vector2(66f * 32f, 33f * 32f),
            7 => new Vector2(72f * 32f, 33f * 32f),
            8 => new Vector2(24f * 32f, 36f * 32f),
            9 => new Vector2(28f * 32f, 37f * 32f),
            10 => new Vector2(36f * 32f, 36f * 32f),
            11 => new Vector2(48f * 32f, 36f * 32f),
            12 => new Vector2(25f * 32f, 52f * 32f),
            13 => new Vector2(31f * 32f, 52f * 32f),
            14 => new Vector2(45f * 32f, 52f * 32f),
            15 => new Vector2(57f * 32f, 52f * 32f),
            _ => new Vector2(8f * 32f, 8f * 32f)
        };
    }

    public static Vector2 CalculateBoardingSchoolPropCollisionSize(StructureSpriteDefinition definition)
    {
        return new Vector2(
            Mathf.Clamp(definition.Size.X * 0.55f, 16f, 96f),
            Mathf.Clamp(definition.Size.Y * 0.25f, 12f, 48f));
    }

    public static Vector2 CalculateBoardingSchoolPropCollisionOffset(StructureSpriteDefinition definition)
    {
        var size = CalculateBoardingSchoolPropCollisionSize(definition);
        return new Vector2(0f, -size.Y * 0.35f);
    }

    private void CreatePrototypeBoardingSchoolTrees()
    {
        if (_prototypeBoardingSchoolTrees.Count > 0 || GeneratedWorld.Theme is not ("boarding_school" or "boarding-school"))
        {
            return;
        }

        var treeKinds = new[]
        {
            StructureSpriteKind.BoardingSchoolCourtyardOak,
            StructureSpriteKind.BoardingSchoolIvyTree,
            StructureSpriteKind.BoardingSchoolNarrowCypress,
            StructureSpriteKind.BoardingSchoolSmallMaple,
            StructureSpriteKind.BoardingSchoolHedgeStraight,
            StructureSpriteKind.BoardingSchoolHedgeCorner,
            StructureSpriteKind.BoardingSchoolHedgeGateGap,
            StructureSpriteKind.BoardingSchoolIvyWallStrip,
            StructureSpriteKind.BoardingSchoolStonePlanter,
            StructureSpriteKind.BoardingSchoolFlowerBed,
            StructureSpriteKind.BoardingSchoolPottedPlant,
            StructureSpriteKind.BoardingSchoolBrassUrnPlant,
            StructureSpriteKind.BoardingSchoolMossyStump,
            StructureSpriteKind.BoardingSchoolIvyClump,
            StructureSpriteKind.BoardingSchoolTallGrassClump,
            StructureSpriteKind.BoardingSchoolFallenLeavesPile
        };

        for (var index = 0; index < treeKinds.Length; index++)
        {
            var kind = treeKinds[index];
            var definition = StructureArtCatalog.Get(kind);
            var node = new StaticBody2D
            {
                Name = $"Tree_{definition.Id}",
                Position = CalculateBoardingSchoolTreePosition(index)
            };
            node.ZIndex = TopDownDepth.CalculateZIndex(node.Position.Y, TopDownDepth.ItemOffsetZ);
            node.AddChild(new StructureSprite
            {
                Name = $"{definition.Id}_sprite",
                Kind = kind,
                DrawShadow = false
            });
            node.AddChild(new CollisionShape2D
            {
                Name = $"{definition.Id}_collision",
                Shape = new RectangleShape2D
                {
                    Size = CalculateBoardingSchoolTreeCollisionSize(definition)
                },
                Position = CalculateBoardingSchoolTreeCollisionOffset(definition)
            });
            AddChild(node);
            _prototypeBoardingSchoolTrees.Add(node);
        }
    }

    public static Vector2 CalculateBoardingSchoolTreePosition(int index)
    {
        return index switch
        {
            0 => new Vector2(6f * 32f, 25f * 32f),
            1 => new Vector2(74f * 32f, 25f * 32f),
            2 => new Vector2(6f * 32f, 54f * 32f),
            3 => new Vector2(74f * 32f, 54f * 32f),
            4 => new Vector2(20f * 32f, 28f * 32f),
            5 => new Vector2(28f * 32f, 28f * 32f),
            6 => new Vector2(36f * 32f, 28f * 32f),
            7 => new Vector2(64f * 32f, 44f * 32f),
            8 => new Vector2(10f * 32f, 34f * 32f),
            9 => new Vector2(18f * 32f, 34f * 32f),
            10 => new Vector2(40f * 32f, 34f * 32f),
            11 => new Vector2(54f * 32f, 34f * 32f),
            12 => new Vector2(18f * 32f, 61f * 32f),
            13 => new Vector2(24f * 32f, 61f * 32f),
            14 => new Vector2(36f * 32f, 61f * 32f),
            15 => new Vector2(42f * 32f, 61f * 32f),
            _ => new Vector2(8f * 32f, 8f * 32f)
        };
    }

    public static Vector2 CalculateBoardingSchoolTreeCollisionSize(StructureSpriteDefinition definition)
    {
        return new Vector2(
            Mathf.Clamp(definition.Size.X * 0.42f, 14f, 80f),
            Mathf.Clamp(definition.Size.Y * 0.20f, 12f, 48f));
    }

    public static Vector2 CalculateBoardingSchoolTreeCollisionOffset(StructureSpriteDefinition definition)
    {
        var size = CalculateBoardingSchoolTreeCollisionSize(definition);
        return new Vector2(0f, -size.Y * 0.35f);
    }

    private void CreatePrototypeBoardingSchoolFlowers()
    {
        if (_prototypeBoardingSchoolFlowers.Count > 0 || GeneratedWorld.Theme is not ("boarding_school" or "boarding-school"))
        {
            return;
        }

        var flowerKinds = new[]
        {
            StructureSpriteKind.BoardingSchoolGrassFlowersA,
            StructureSpriteKind.BoardingSchoolGrassFlowersB,
            StructureSpriteKind.BoardingSchoolGrassFlowersC
        };

        for (var index = 0; index < 24; index++)
        {
            var kind = flowerKinds[index % flowerKinds.Length];
            var definition = StructureArtCatalog.Get(kind);
            var node = new Node2D
            {
                Name = $"Flower_{index}_{definition.Id}",
                Position = CalculateBoardingSchoolFlowerPosition(index)
            };
            node.ZIndex = TopDownDepth.TileLayerZ + 1;
            node.AddChild(new StructureSprite
            {
                Name = $"{definition.Id}_sprite",
                Kind = kind,
                DrawShadow = false
            });
            AddChild(node);
            _prototypeBoardingSchoolFlowers.Add(node);
        }
    }

    public static Vector2 CalculateBoardingSchoolFlowerPosition(int index)
    {
        var safeIndex = Mathf.Max(0, index);
        var x = 7 + ((safeIndex * 11) % 66);
        var y = 8 + ((safeIndex * 17) % 56);

        if (x is >= 10 and <= 21 && y is >= 14 and <= 24)
        {
            x += 14;
        }
        if (x is >= 48 and <= 68 && y is >= 14 and <= 24)
        {
            y += 12;
        }
        if (x is >= 3 and <= 22 && y is >= 36 and <= 46)
        {
            x += 18;
        }
        if (x is >= 60 and <= 79 && y is >= 50 and <= 70)
        {
            x -= 18;
        }

        return new Vector2(x * 32f, y * 32f);
    }

    private void CreatePrototypeStructureShowcase()
    {
        if (_prototypeStructureShowcase.Count > 0)
        {
            return;
        }

        var showcaseKinds = new[]
        {
            StructureSpriteKind.CargoCrate,
            StructureSpriteKind.UtilityJunctionBox,
            StructureSpriteKind.StationWallSegment,
            StructureSpriteKind.CompactKioskTerminal,
            StructureSpriteKind.RedMineralRock,
            StructureSpriteKind.BlueCrystalShard,
            StructureSpriteKind.CactusSucculent,
            StructureSpriteKind.BerryBush,
            StructureSpriteKind.MossPatch,
            StructureSpriteKind.MushroomCluster
        };

        for (var index = 0; index < showcaseKinds.Length; index++)
        {
            var kind = showcaseKinds[index];
            var definition = StructureArtCatalog.Get(kind);
            var node = new Node2D
            {
                Name = $"Preview_{definition.Id}",
                Position = CalculateStructureShowcasePosition(index)
            };
            node.ZIndex = TopDownDepth.CalculateZIndex(node.Position.Y, TopDownDepth.ItemOffsetZ);
            node.AddChild(new StructureSprite
            {
                Name = $"{definition.Id}_sprite",
                Kind = kind,
                DrawShadow = false
            });
            AddChild(node);
            _prototypeStructureShowcase.Add(node);
        }
    }

    public static Vector2 CalculateStructureShowcasePosition(int index)
    {
        const int columns = 5;
        var safeIndex = Mathf.Max(0, index);
        var column = safeIndex % columns;
        var row = safeIndex / columns;
        return new Vector2(500f + (column * 58f), 420f + (row * 58f));
    }

    private static bool IsPolishedPrototypeItem(GameItem item)
    {
        return PrototypeSpriteCatalog
            .Get(PrototypeSpriteCatalog.GetKindForItem(item.Id))
            .AtlasPath
            .Contains(PrototypeSpriteCatalog.GeminiStaticPropsRoot);
    }

    private static bool IsDynamicWorldItem(WorldItemSnapshot item)
    {
        return item.EntityId.StartsWith("placed_") || item.EntityId.StartsWith("drop_");
    }

    private static bool ShouldRenderServerNpc(NpcSnapshot npc)
    {
        return npc.Id != StarterNpcs.Mara.Id;
    }
}

public sealed record WorldSeed(int Seed, string Name, string Theme);
