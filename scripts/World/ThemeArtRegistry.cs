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
                AtlasTile(WorldTileIds.GroundScrub, 14, 83, 47, 48, new Color(0.25f, 0.4f, 0.28f)),
                AtlasTile(WorldTileIds.GroundDust, 119, 136, 46, 48, new Color(0.45f, 0.34f, 0.24f)),
                AtlasTile(WorldTileIds.PathDust, 171, 189, 46, 47, new Color(0.56f, 0.45f, 0.31f)),
                AtlasTile(WorldTileIds.ClinicFloor, 14, 241, 47, 48, new Color(0.32f, 0.34f, 0.38f)),
                AtlasTile(WorldTileIds.MarketFloor, 65, 241, 47, 48, new Color(0.36f, 0.28f, 0.22f)),
                AtlasTile(WorldTileIds.WorkshopFloor, 119, 241, 46, 48, new Color(0.25f, 0.3f, 0.32f)),
                AtlasTile(WorldTileIds.DuelRingFloor, 829, 791, 65, 54, new Color(0.42f, 0.24f, 0.28f)),
                AtlasTile(WorldTileIds.WallMetal, 357, 36, 92, 80, new Color(0.34f, 0.37f, 0.42f)),
                AtlasTile(WorldTileIds.DoorAirlock, 942, 36, 87, 89, new Color(0.76f, 0.54f, 0.16f)),
                AtlasTile(WorldTileIds.OddityPile, 998, 850, 48, 42, new Color(0.82f, 0.3f, 0.76f))
            });
    }

    private static TileArtDefinition AtlasTile(
        string tileId,
        int sourceX,
        int sourceY,
        int sourceWidth,
        int sourceHeight,
        Color placeholderColor)
    {
        return new TileArtDefinition(
            tileId,
            PlaceholderAtlasPath,
            sourceX,
            sourceY,
            sourceWidth,
            sourceHeight,
            AtlasTileSizePixels: 1,
            HasAtlasRegion: true,
            placeholderColor);
    }
}
