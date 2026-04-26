using Godot;
using System.Collections.Generic;
using System.Linq;
using Karma.Net;

namespace Karma.World;

public partial class GeneratedTileMapRenderer : Node2D
{
    private const float TileSize = 32f;
    private readonly Dictionary<GeneratedChunkCoordinate, MapChunkSnapshot> _loadedChunks = new();
    private GeneratedTileMap _tileMap;
    private ThemeArtSet _artSet = ThemeArtRegistry.GetForTheme("western-sci-fi");
    private Texture2D _atlasTexture;
    private string _atlasPath = string.Empty;

    public int LoadedChunkCount => _loadedChunks.Count;
    public bool PreferAtlasArt { get; set; }

    public void SetTileMap(GeneratedTileMap tileMap, ThemeArtSet artSet)
    {
        _tileMap = tileMap;
        _loadedChunks.Clear();
        SetArtSet(artSet);
        QueueRedraw();
    }

    public void SetChunks(IReadOnlyList<MapChunkSnapshot> chunks, ThemeArtSet artSet)
    {
        _tileMap = null;
        SetArtSet(artSet);
        var visibleChunkKeys = chunks
            .Select(chunk => new GeneratedChunkCoordinate(chunk.ChunkX, chunk.ChunkY))
            .ToHashSet();

        foreach (var removedKey in _loadedChunks.Keys.Where(key => !visibleChunkKeys.Contains(key)).ToArray())
        {
            _loadedChunks.Remove(removedKey);
        }

        foreach (var chunk in chunks)
        {
            _loadedChunks[new GeneratedChunkCoordinate(chunk.ChunkX, chunk.ChunkY)] = chunk;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_loadedChunks.Count > 0)
        {
            foreach (var chunk in _loadedChunks.Values)
            {
                foreach (var tile in chunk.Tiles)
                {
                    DrawTile(tile.TileX, tile.TileY, tile.FloorId, tile.StructureId);
                }
            }

            return;
        }

        if (_tileMap is null)
        {
            return;
        }

        foreach (var tile in _tileMap.Tiles)
        {
            DrawTile(tile.X, tile.Y, tile.FloorId, tile.StructureId);
        }
    }

    private void DrawTile(int x, int y, string floorId, string structureId)
    {
        var rect = new Rect2(x * TileSize, y * TileSize, TileSize, TileSize);
        DrawTileArt(rect, _artSet.GetTile(floorId));
        DrawRect(rect, new Color(0f, 0f, 0f, 0.08f), filled: false, width: 1f);

        if (!string.IsNullOrWhiteSpace(structureId))
        {
            DrawStructure(structureId, rect);
        }
    }

    private void DrawTileArt(Rect2 target, TileArtDefinition art)
    {
        if (PreferAtlasArt && art.HasAtlasRegion && _atlasTexture is not null)
        {
            DrawTextureRectRegion(_atlasTexture, target, art.SourceRegion);
            return;
        }

        DrawRect(target, art.PlaceholderColor);
    }

    private void SetArtSet(ThemeArtSet artSet)
    {
        _artSet = artSet;
        var atlasPath = _artSet.GetTile(WorldTileIds.GroundScrub).AtlasPath;
        if (_atlasPath == atlasPath)
        {
            return;
        }

        _atlasPath = atlasPath;
        _atlasTexture = ResourceLoader.Exists(atlasPath)
            ? ResourceLoader.Load<Texture2D>(atlasPath)
            : null;
    }

    private void DrawStructure(string structureId, Rect2 rect)
    {
        if (structureId == WorldTileIds.WallMetal)
        {
            DrawRect(rect.Grow(-4f), new Color(0.18f, 0.2f, 0.24f));
            DrawRect(rect.Grow(-8f), _artSet.GetTile(structureId).PlaceholderColor);
            return;
        }

        if (structureId == WorldTileIds.DoorAirlock)
        {
            DrawRect(rect.Grow(-4f), new Color(0.12f, 0.15f, 0.18f));
            DrawRect(new Rect2(rect.Position + new Vector2(9f, 4f), new Vector2(14f, 24f)), _artSet.GetTile(structureId).PlaceholderColor);
            return;
        }

        if (structureId == WorldTileIds.OddityPile)
        {
            DrawCircle(rect.GetCenter(), 9f, _artSet.GetTile(structureId).PlaceholderColor);
            DrawCircle(rect.GetCenter() + new Vector2(7f, -5f), 5f, new Color(0.25f, 0.84f, 0.78f));
        }
    }
}
