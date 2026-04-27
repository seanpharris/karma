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
        RenderServerNpcs(snapshot);
        RenderRemotePlayers(snapshot);
        RenderServerStructures(snapshot);
        RenderServerItems(snapshot);
        RenderLocalChatBubbles(snapshot);
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
        return new Vector2(620f + (column * 48f), 220f + (row * 48f));
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
