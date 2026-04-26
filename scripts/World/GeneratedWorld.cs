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
    IReadOnlyList<GameItem> Oddities,
    IReadOnlyList<FactionProfile> Factions)
{
    public string Summary =>
        $"{Config.Seed.Name} ({Theme}) - {Locations.Count} locations, {Npcs.Count} NPCs, {Oddities.Count} oddities, {Factions.Count} factions";

    public GeneratedWorldAdapter ToAdapter()
    {
        return new GeneratedWorldAdapter(Theme, Npcs, Array.Empty<QuestDefinition>(), Oddities, Factions);
    }
}

public sealed record GeneratedLocation(
    string Id,
    string Name,
    string Role,
    int X,
    int Y);

public sealed record GeneratedTileMap(
    int Width,
    int Height,
    IReadOnlyList<GeneratedTile> Tiles)
{
    public GeneratedTile Get(int x, int y)
    {
        return Tiles[y * Width + x];
    }
}

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
