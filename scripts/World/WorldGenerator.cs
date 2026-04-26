using System;
using System.Collections.Generic;
using Karma.Data;
using Karma.Net;

namespace Karma.World;

public static class WorldGenerator
{
    private static readonly string[] Themes =
    {
        "western-sci-fi",
        "farming",
        "haunted-coastal",
        "junkyard-fantasy",
        "corporate-moon-colony"
    };

    private static readonly string[] LocationRoles =
    {
        "clinic",
        "market",
        "workshop",
        "notice-board",
        "social-hub",
        "restricted-storage",
        "oddity-yard",
        "duel-ring",
        "farm-plot",
        "black-market"
    };

    private static readonly string[] LocationNames =
    {
        "Mara's Patch Clinic",
        "The Bent Ledger",
        "Low Noon Tools",
        "The Public Problem Wall",
        "Last Chair Saloon",
        "Authorized Shed 7",
        "Balloon Grave",
        "The Polite Violence Circle",
        "Turnip Lot",
        "Under-Counter Goods"
    };

    public static GeneratedWorld Generate(WorldConfig config)
    {
        var random = new Random(config.Seed.Seed);
        var theme = string.IsNullOrWhiteSpace(config.Seed.Theme)
            ? Themes[random.Next(Themes.Length)]
            : config.Seed.Theme;
        var locationCount = config.Server.Scale switch
        {
            WorldScale.Small => 5,
            WorldScale.Medium => 12,
            WorldScale.Large => 24,
            _ => 5
        };

        return new GeneratedWorld(
            config,
            theme,
            GenerateTileMap(random, config),
            GenerateLocations(random, config, locationCount),
            GenerateNpcs(random, config),
            GenerateOddities(config),
            StarterFactions.All);
    }

    private static GeneratedTileMap GenerateTileMap(Random random, WorldConfig config)
    {
        var tiles = new List<GeneratedTile>(config.WidthTiles * config.HeightTiles);
        var centerX = config.WidthTiles / 2;
        var centerY = config.HeightTiles / 2;

        for (var y = 0; y < config.HeightTiles; y++)
        {
            for (var x = 0; x < config.WidthTiles; x++)
            {
                var floor = random.NextDouble() < 0.16
                    ? WorldTileIds.GroundDust
                    : WorldTileIds.GroundScrub;
                var structure = string.Empty;
                var zone = string.Empty;

                if (Math.Abs(x - centerX) <= 1 || Math.Abs(y - centerY) <= 1)
                {
                    floor = WorldTileIds.PathDust;
                    zone = "starter_path";
                }

                if (IsInRect(x, y, 2, 2, 8, 6))
                {
                    floor = WorldTileIds.ClinicFloor;
                    zone = "clinic";
                    structure = IsBorder(x, y, 2, 2, 8, 6)
                        ? WorldTileIds.WallMetal
                        : string.Empty;
                }

                if (x == 5 && y == 7)
                {
                    structure = WorldTileIds.DoorAirlock;
                }

                if (IsInRect(x, y, 10, 4, 7, 5))
                {
                    floor = WorldTileIds.MarketFloor;
                    zone = "market";
                }

                if (IsInRect(x, y, 18, 5, 7, 5))
                {
                    floor = WorldTileIds.WorkshopFloor;
                    zone = "workshop";
                }

                if (IsInRect(x, y, centerX + 6, centerY + 3, 7, 7))
                {
                    floor = WorldTileIds.DuelRingFloor;
                    zone = "duel_ring";
                }

                if (x == centerX + 10 && y == centerY + 6)
                {
                    structure = WorldTileIds.OddityPile;
                }

                tiles.Add(new GeneratedTile(x, y, floor, structure, zone));
            }
        }

        return new GeneratedTileMap(config.WidthTiles, config.HeightTiles, config.Server.ChunkSizeTiles, tiles);
    }

    private static bool IsInRect(int x, int y, int left, int top, int width, int height)
    {
        return x >= left && x < left + width && y >= top && y < top + height;
    }

    private static bool IsBorder(int x, int y, int left, int top, int width, int height)
    {
        return x == left || x == left + width - 1 || y == top || y == top + height - 1;
    }

    private static IReadOnlyList<GeneratedLocation> GenerateLocations(
        Random random,
        WorldConfig config,
        int count)
    {
        var locations = new List<GeneratedLocation>();
        for (var i = 0; i < count; i++)
        {
            var role = LocationRoles[i % LocationRoles.Length];
            var name = LocationNames[i % LocationNames.Length];
            locations.Add(new GeneratedLocation(
                $"location_{i}",
                name,
                role,
                random.Next(4, config.WidthTiles - 4),
                random.Next(4, config.HeightTiles - 4)));
        }

        return locations;
    }

    private static IReadOnlyList<NpcProfile> GenerateNpcs(Random random, WorldConfig config)
    {
        var targetCount = Math.Max(1, config.Server.TargetPlayers * 3);
        var npcs = new List<NpcProfile> { StarterNpcs.Mara };

        for (var i = 1; i < targetCount; i++)
        {
            npcs.Add(new NpcProfile(
                $"generated_npc_{i}",
                GeneratedNames[random.Next(GeneratedNames.Length)],
                GeneratedRoles[random.Next(GeneratedRoles.Length)],
                GeneratedPersonalities[random.Next(GeneratedPersonalities.Length)],
                GeneratedFactions[random.Next(GeneratedFactions.Length)],
                GeneratedNeeds[random.Next(GeneratedNeeds.Length)],
                GeneratedSecrets[random.Next(GeneratedSecrets.Length)],
                new[] { "honesty", "useful junk", "good timing" },
                new[] { "threats", "waste", "being called decorative" }));
        }

        return npcs;
    }

    private static IReadOnlyList<GameItem> GenerateOddities(WorldConfig config)
    {
        var oddities = new List<GameItem>
        {
            StarterItems.WhoopieCushion,
            StarterItems.DeflatedBalloon
        };

        if (config.Server.Scale != WorldScale.Small)
        {
            oddities.Add(StarterItems.RepairKit);
        }

        return oddities;
    }

    private static readonly string[] GeneratedNames =
    {
        "Tessa Null",
        "Bo Dimple",
        "Arlo Switch",
        "Nina Brack",
        "Cal Ponder"
    };

    private static readonly string[] GeneratedRoles =
    {
        "Deputy Botanist",
        "Fence Apologist",
        "Part-Time Oracle",
        "Battery Rancher",
        "Festival Auditor"
    };

    private static readonly string[] GeneratedPersonalities =
    {
        "cheerful, evasive, allergic to plans",
        "stern, sentimental, easily bribed with soup",
        "curious, dramatic, terrible at secrets",
        "patient, gloomy, collects broken bells"
    };

    private static readonly string[] GeneratedFactions =
    {
        "Free Settlers",
        "Civic Repair Guild",
        "Backroom Merchants",
        "Turnip Committee"
    };

    private static readonly string[] GeneratedNeeds =
    {
        "someone to repair a public machine",
        "proof that a rumor is false",
        "a replacement tool with no questions attached",
        "help finding a suspiciously specific object"
    };

    private static readonly string[] GeneratedSecrets =
    {
        "owes a debt to the black market",
        "has been hiding town supplies",
        "is protecting a rival player",
        "knows why the mayor banned balloons"
    };
}
