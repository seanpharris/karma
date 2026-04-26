using Godot;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Data;
using Karma.Net;

namespace Karma.World;

public partial class WorldRoot : Node2D
{
    private readonly Dictionary<string, Node2D> _renderedServerNpcs = new();
    private readonly Dictionary<string, Node2D> _renderedServerItems = new();
    private readonly Dictionary<string, Node2D> _renderedServerStructures = new();
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
            _serverSession.LocalSnapshotChanged += OnLocalSnapshotChanged;
            RenderSnapshot(_serverSession.LastLocalSnapshot);
        }
        else
        {
            _tileMapRenderer.SetTileMap(GeneratedWorld.TileMap, ThemeArtRegistry.GetForTheme(GeneratedWorld.Theme));
        }
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
            ZIndex = -100
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
        RenderServerStructures(snapshot);
        RenderServerItems(snapshot);
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
                Position = position,
                ZIndex = 3
            };
            node.AddChild(new PrototypeSprite
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
            if (_renderedServerStructures.ContainsKey(structure.EntityId))
            {
                continue;
            }

            var node = new ServerStructureObject
            {
                Name = structure.EntityId,
                EntityId = structure.EntityId,
                StructureName = structure.Name,
                InteractionPrompt = structure.InteractionPrompt,
                IsInteractable = structure.IsInteractable,
                Position = new Vector2(structure.TileX * 32f, structure.TileY * 32f),
                ZIndex = -10
            };
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
                Position = new Vector2(item.TileX * 32f, item.TileY * 32f)
            };
            var marker = new PrototypeSprite
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
