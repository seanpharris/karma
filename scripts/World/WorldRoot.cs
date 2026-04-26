using Godot;
using System.Collections.Generic;
using System.Linq;
using Karma.Data;
using Karma.Net;

namespace Karma.World;

public partial class WorldRoot : Node2D
{
    private readonly Dictionary<string, Node2D> _renderedServerItems = new();
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
        RenderGeneratedTileMap();

        _serverSession = GetNodeOrNull<PrototypeServerSession>("/root/PrototypeServerSession");
        if (_serverSession is not null)
        {
            _serverSession.LocalSnapshotChanged += OnLocalSnapshotChanged;
            RenderServerItems(_serverSession.LastLocalSnapshot);
        }
    }

    private void OnLocalSnapshotChanged(string snapshotSummary)
    {
        if (_serverSession is not null)
        {
            RenderServerItems(_serverSession.LastLocalSnapshot);
        }
    }

    private void RenderGeneratedTileMap()
    {
        _tileMapRenderer = new GeneratedTileMapRenderer
        {
            Name = "GeneratedTileMap",
            ZIndex = -100
        };
        AddChild(_tileMapRenderer);
        _tileMapRenderer.SetTileMap(GeneratedWorld.TileMap);
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
            var marker = new ColorRect
            {
                OffsetLeft = -8,
                OffsetTop = -8,
                OffsetRight = 8,
                OffsetBottom = 8,
                Color = GetItemColor(item.Category)
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

    private static Color GetItemColor(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Armor => new Color(0.929f, 0.529f, 0.176f),
            ItemCategory.Weapon => new Color(0.541f, 0.349f, 0.184f),
            ItemCategory.Tool => new Color(0.141f, 0.761f, 0.608f),
            ItemCategory.Oddity => new Color(0.925f, 0.678f, 0.922f),
            ItemCategory.Cosmetic => new Color(0.392f, 0.749f, 0.941f),
            _ => new Color(0.9f, 0.9f, 0.9f)
        };
    }
}

public sealed record WorldSeed(int Seed, string Name, string Theme);
