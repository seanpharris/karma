using System.Collections.Generic;
using Godot;

namespace Karma.World;

public sealed record TileArtDefinition(
    string TileId,
    string AtlasPath,
    int AtlasX,
    int AtlasY,
    int WidthTiles,
    int HeightTiles,
    int AtlasTileSizePixels,
    bool HasAtlasRegion,
    Color PlaceholderColor)
{
    public Rect2 SourceRegion => new(
        AtlasX * AtlasTileSizePixels,
        AtlasY * AtlasTileSizePixels,
        WidthTiles * AtlasTileSizePixels,
        HeightTiles * AtlasTileSizePixels);
}

public sealed class ThemeArtSet
{
    private readonly Dictionary<string, TileArtDefinition> _tiles = new();

    public ThemeArtSet(string theme, IEnumerable<TileArtDefinition> tiles)
    {
        Theme = theme;
        foreach (var tile in tiles)
        {
            _tiles[tile.TileId] = tile;
        }
    }

    public string Theme { get; }
    public IReadOnlyDictionary<string, TileArtDefinition> Tiles => _tiles;

    public TileArtDefinition GetTile(string tileId)
    {
        return _tiles.TryGetValue(tileId, out var tile)
            ? tile
            : _tiles[WorldTileIds.GroundScrub];
    }
}

public static class ThemeArtRegistry
{
    public const string PlaceholderAtlasPath = "res://assets/art/tilesets/scifi_station_atlas.png";
    public const int DefaultAtlasTileSizePixels = 32;

    public static ThemeArtSet GetForTheme(string theme)
    {
        return new ThemeArtSet(
            theme,
            new[]
            {
                PlaceholderTile(WorldTileIds.GroundScrub, 0, 0, new Color(0.25f, 0.4f, 0.28f)),
                PlaceholderTile(WorldTileIds.GroundDust, 1, 0, new Color(0.45f, 0.34f, 0.24f)),
                PlaceholderTile(WorldTileIds.PathDust, 2, 0, new Color(0.56f, 0.45f, 0.31f)),
                PlaceholderTile(WorldTileIds.ClinicFloor, 0, 1, new Color(0.32f, 0.34f, 0.38f)),
                PlaceholderTile(WorldTileIds.MarketFloor, 1, 1, new Color(0.36f, 0.28f, 0.22f)),
                PlaceholderTile(WorldTileIds.WorkshopFloor, 2, 1, new Color(0.25f, 0.3f, 0.32f)),
                PlaceholderTile(WorldTileIds.DuelRingFloor, 3, 1, new Color(0.42f, 0.24f, 0.28f)),
                PlaceholderTile(WorldTileIds.WallMetal, 0, 2, new Color(0.34f, 0.37f, 0.42f)),
                PlaceholderTile(WorldTileIds.DoorAirlock, 1, 2, new Color(0.76f, 0.54f, 0.16f)),
                PlaceholderTile(WorldTileIds.OddityPile, 2, 2, new Color(0.82f, 0.3f, 0.76f))
            });
    }

    private static TileArtDefinition PlaceholderTile(string tileId, int atlasX, int atlasY, Color placeholderColor)
    {
        return new TileArtDefinition(
            tileId,
            PlaceholderAtlasPath,
            atlasX,
            atlasY,
            WidthTiles: 1,
            HeightTiles: 1,
            AtlasTileSizePixels: DefaultAtlasTileSizePixels,
            HasAtlasRegion: false,
            placeholderColor);
    }
}
