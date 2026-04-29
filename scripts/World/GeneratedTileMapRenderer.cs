using Godot;
using System.Collections.Generic;
using System.Linq;
using Karma.Art;
using Karma.Net;

namespace Karma.World;

public partial class GeneratedTileMapRenderer : Node2D
{
    private const float TileSize = 32f;
    private readonly Dictionary<GeneratedChunkCoordinate, MapChunkSnapshot> _loadedChunks = new();
    private GeneratedTileMap _tileMap;
    private ThemeArtSet _artSet = ThemeArtRegistry.GetForTheme("boarding_school");
    private readonly Dictionary<string, Texture2D> _atlasTextures = new();

    public int LoadedChunkCount => _loadedChunks.Count;
    public int LastUpdatedChunkCount { get; private set; }
    public bool PreferAtlasArt { get; set; } = true;

    public void SetTileMap(GeneratedTileMap tileMap, ThemeArtSet artSet)
    {
        _tileMap = tileMap;
        _loadedChunks.Clear();
        LastUpdatedChunkCount = 0;
        SetArtSet(artSet);
        QueueRedraw();
    }

    public void SetChunks(IReadOnlyList<MapChunkSnapshot> chunks, ThemeArtSet artSet)
    {
        _tileMap = null;
        LastUpdatedChunkCount = 0;
        SetArtSet(artSet);
        var visibleChunkKeys = chunks
            .Select(chunk => new GeneratedChunkCoordinate(chunk.ChunkX, chunk.ChunkY))
            .ToHashSet();

        foreach (var removedKey in _loadedChunks.Keys.Where(key => !visibleChunkKeys.Contains(key)).ToArray())
        {
            _loadedChunks.Remove(removedKey);
            LastUpdatedChunkCount++;
        }

        foreach (var chunk in chunks)
        {
            var key = new GeneratedChunkCoordinate(chunk.ChunkX, chunk.ChunkY);
            if (_loadedChunks.TryGetValue(key, out var existingChunk) &&
                existingChunk.ChunkKey == chunk.ChunkKey &&
                existingChunk.Revision == chunk.Revision)
            {
                continue;
            }

            _loadedChunks[key] = chunk;
            LastUpdatedChunkCount++;
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

        if (!string.IsNullOrWhiteSpace(structureId))
        {
            DrawStructure(structureId, rect);
        }
    }

    private void DrawTileArt(Rect2 target, TileArtDefinition art)
    {
        if (PreferAtlasArt && art.HasAtlasRegion && TryGetAtlasTexture(art.AtlasPath, out var texture))
        {
            DrawTextureRectRegion(texture, target, art.SourceRegion);
            return;
        }

        DrawRect(target, art.PlaceholderColor);
    }

    private void SetArtSet(ThemeArtSet artSet)
    {
        _artSet = artSet;
    }

    private bool TryGetAtlasTexture(string atlasPath, out Texture2D texture)
    {
        if (_atlasTextures.TryGetValue(atlasPath, out texture))
        {
            return texture is not null;
        }

        texture = AtlasTextureLoader.Load(atlasPath);
        _atlasTextures[atlasPath] = texture;
        return texture is not null;
    }

    private void DrawStructure(string structureId, Rect2 rect)
    {
        if (structureId == WorldTileIds.WallMetal)
        {
            DrawTileArt(rect, _artSet.GetTile(structureId));
            return;
        }

        if (structureId == WorldTileIds.DoorAirlock)
        {
            DrawTileArt(rect, _artSet.GetTile(structureId));
            return;
        }

        if (structureId == WorldTileIds.OddityPile)
        {
            DrawTileArt(rect.Grow(-2f), _artSet.GetTile(structureId));
        }
    }
}
