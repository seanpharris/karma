using System;
using System.Collections.Generic;
using Karma.Data;

namespace Karma.World;

public sealed record GeneratedWorld(
    WorldConfig Config,
    string Theme,
    GeneratedTileMap TileMap,
    IReadOnlyList<GeneratedLocation> Locations,
    IReadOnlyList<NpcProfile> Npcs,
    IReadOnlyList<GeneratedNpcPlacement> NpcPlacements,
    IReadOnlyList<QuestDefinition> Quests,
    IReadOnlyList<GeneratedStructurePlacement> StructurePlacements,
    IReadOnlyList<GameItem> Oddities,
    IReadOnlyList<GeneratedOddityPlacement> OddityPlacements,
    IReadOnlyList<FactionProfile> Factions,
    IReadOnlyList<GeneratedPathEdge> PathEdges)
{
    public string Summary =>
        $"{Config.Seed.Name} ({Theme}) - {Locations.Count} locations, {Npcs.Count} NPCs, {Oddities.Count} oddities, {Factions.Count} factions, {PathEdges.Count} path edges";

    public GeneratedWorldAdapter ToAdapter()
    {
        return new GeneratedWorldAdapter(Theme, Npcs, Quests, Oddities, Factions);
    }
}

public sealed record GeneratedLocation(
    string Id,
    string Name,
    string Role,
    string ThemeTag,
    string KarmaHook,
    string SuggestedFaction,
    string InteriorId,
    string InteriorKind,
    int X,
    int Y);

public sealed record GeneratedNpcPlacement(
    string NpcId,
    string LocationId,
    string Role,
    string Faction,
    string GameplayHook,
    int X,
    int Y);

public sealed record GeneratedStructurePlacement(
    string StructureId,
    string LocationId,
    string Name,
    string GameplayHook,
    string SuggestedFaction,
    int X,
    int Y,
    int Integrity);

public sealed record GeneratedOddityPlacement(
    string ItemId,
    string LocationId,
    string PlacementReason,
    int X,
    int Y);

public sealed record GeneratedPathEdge(
    string FromLocationId,
    string ToLocationId,
    int FromX,
    int FromY,
    int ToX,
    int ToY);

public sealed record GeneratedTileMap(
    int Width,
    int Height,
    int ChunkSize,
    IReadOnlyList<GeneratedTile> Tiles)
{
    public int ChunkColumns => (int)Math.Ceiling(Width / (double)ChunkSize);
    public int ChunkRows => (int)Math.Ceiling(Height / (double)ChunkSize);

    public GeneratedTile Get(int x, int y)
    {
        return Tiles[y * Width + x];
    }

    public GeneratedChunkCoordinate GetChunkCoordinateForTile(int x, int y)
    {
        return new GeneratedChunkCoordinate(x / ChunkSize, y / ChunkSize);
    }

    public GeneratedTileChunk GetChunk(GeneratedChunkCoordinate coordinate)
    {
        var left = coordinate.X * ChunkSize;
        var top = coordinate.Y * ChunkSize;
        var right = Math.Min(Width, left + ChunkSize);
        var bottom = Math.Min(Height, top + ChunkSize);
        var tiles = new List<GeneratedTile>((right - left) * (bottom - top));

        for (var y = top; y < bottom; y++)
        {
            for (var x = left; x < right; x++)
            {
                tiles.Add(Get(x, y));
            }
        }

        return new GeneratedTileChunk(coordinate, left, top, right - left, bottom - top, tiles);
    }

    public IReadOnlyList<GeneratedTileChunk> GetChunksAround(int tileX, int tileY, int radiusChunks)
    {
        var center = GetChunkCoordinateForTile(tileX, tileY);
        var chunks = new List<GeneratedTileChunk>();
        var minX = Math.Max(0, center.X - radiusChunks);
        var maxX = Math.Min(ChunkColumns - 1, center.X + radiusChunks);
        var minY = Math.Max(0, center.Y - radiusChunks);
        var maxY = Math.Min(ChunkRows - 1, center.Y + radiusChunks);

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                chunks.Add(GetChunk(new GeneratedChunkCoordinate(x, y)));
            }
        }

        return chunks;
    }
}

public sealed record GeneratedChunkCoordinate(int X, int Y);

public sealed record GeneratedTileChunk(
    GeneratedChunkCoordinate Coordinate,
    int Left,
    int Top,
    int Width,
    int Height,
    IReadOnlyList<GeneratedTile> Tiles);

public sealed record GeneratedTile(
    int X,
    int Y,
    string FloorId,
    string StructureId = "",
    string ZoneId = "");

public static class WorldTileIds
{
    public const string GroundScrub = "ground_scrub";
    public const string GroundDust = "ground_dust";
    public const string PathDust = "path_dust";
    public const string ClinicFloor = "clinic_floor";
    public const string MarketFloor = "market_floor";
    public const string WorkshopFloor = "workshop_floor";
    public const string DuelRingFloor = "duel_ring_floor";
    public const string WallMetal = "wall_metal";
    public const string DoorAirlock = "door_airlock";
    public const string OddityPile = "oddity_pile";
}
