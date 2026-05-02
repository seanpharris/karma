using System;
using System.Collections.Generic;
using System.Linq;
using Karma.Data;
using Karma.Net;
using Karma.Quests;

namespace Karma.World;

public static class WorldGenerator
{
    // Aspirational/wishlist theme ids that don't yet have full data
    // (theme.json + art) but are kept here so we can document them and
    // eventually back them with ThemeRegistry entries. The random fallback
    // below pulls from ThemeRegistry.AllIds (only fully-supported themes).
    public static readonly string[] WishlistThemes =
    {
        "farming",
        "haunted-coastal",
        "junkyard-fantasy",
        "corporate-moon-colony",
        "dieselpunk-warfront",
        "candy-neon-noir",
        "floating-monastery-market"
    };

    private static readonly SocialStationArchetype[] SocialStations =
    {
        new(
            "clinic",
            "Mara's Village Forge",
            "care",
            "mend the injured, steal remedies, expose false alms, or protect the vulnerable",
            "Village Freeholders",
            WorldTileIds.ClinicFloor,
            "Blacksmith",
            "needs iron, herbs, and honest hands"),
        new(
            "market",
            "The Bent Ledger Tavern",
            "trade",
            "gift coin, haggle honestly, pick pockets, fence stolen goods, or bruise someone's reputation",
            "Backroom Merchants",
            WorldTileIds.MarketFloor,
            "Tavern Factor",
            "tracks debts, favors, and suspicious generosity"),
        new(
            "workshop",
            "Low Noon Smithy",
            "repair",
            "repair public structures, sabotage machinery, post bounties, or launder blame through contractors",
            "Civic Repair Guild",
            WorldTileIds.WorkshopFloor,
            "Repair Guild Factor",
            "rewards maintenance and quietly blacklists vandals"),
        new(
            "notice-board",
            "The Village Notice Wall",
            "rumor",
            "pin rumors, expose secrets, accept public bounties, or weaponize Rumorcraft",
            "Village Freeholders",
            WorldTileIds.PathDust,
            "Rumor Clerk",
            "knows which stories are mercy and which are ammunition"),
        new(
            "social-hub",
            "Last Chair Alehouse",
            "relationship",
            "form temporary posses, buy apologies, start feuds, or learn who owes whom",
            "Village Freeholders",
            WorldTileIds.MarketFloor,
            "Tavern Witness",
            "remembers favors, insults, and who left whom behind"),
        new(
            "restricted-storage",
            "The Locked Tithe Barn",
            "temptation",
            "guard shared supplies, raid contraband, plant evidence, or return stolen Karma Break loot",
            "Civic Repair Guild",
            WorldTileIds.WorkshopFloor,
            "Shed Warden",
            "turns inventory crimes into faction consequences"),
        new(
            "oddity-yard",
            "Balloon Grave",
            "chaos",
            "trade cursed junk, trigger strange bargains, or convert useless items into social leverage",
            "Turnip Committee",
            WorldTileIds.GroundDust,
            "Oddity Wrangler",
            "insists every ridiculous object has moral weight"),
        new(
            "duel-ring",
            "The Polite Violence Circle",
            "combat",
            "settle grudges cleanly, break duel etiquette, spectate, intervene, or earn Dread Reputation",
            "Village Freeholders",
            WorldTileIds.DuelRingFloor,
            "Duel Marshal",
            "respects consent, hates cheap shots, sells clean revenge"),
        new(
            "farm-plot",
            "Turnip Lot",
            "sustenance",
            "grow shared food, steal harvests, poison irrigation, or rescue hungry NPCs",
            "Turnip Committee",
            WorldTileIds.GroundScrub,
            "Crop Mystic",
            "believes farming is just slow prophecy"),
        new(
            "black-market",
            "Under-Counter Goods",
            "crime",
            "buy heat reduction, hide stolen goods, betray contacts, or cash in Abyssal Mark protection",
            "Backroom Merchants",
            WorldTileIds.MarketFloor,
            "Contraband Saint",
            "sells bad choices with excellent customer service"),
        new(
            "memory-shrine",
            "The Penance Shrine",
            "redemption",
            "confess harm, restore trust, pay reparations, or fake remorse for short-term gain",
            "Civic Repair Guild",
            WorldTileIds.ClinicFloor,
            "Reparation Archivist",
            "judges whether an apology cost enough to matter"),
        new(
            "broadcast-tower",
            "Saint/Scourge Bell Tower",
            "broadcast",
            "amplify heroics, spread scandals, drown rumors, or make a private entanglement public",
            "Village Freeholders",
            WorldTileIds.WorkshopFloor,
            "Bell Deacon",
            "turns local drama into world events"),
        new(
            "war-memorial",
            "The Unfinished Surrender",
            "loyalty",
            "protect deserters, expose war profiteers, steal medals, or broker ceasefires",
            "Civic Repair Guild",
            WorldTileIds.PathDust,
            "Truce Veteran",
            "knows every side has a receipt"),
        new(
            "court-of-crows",
            "The Rook Court",
            "judgment",
            "let NPC witnesses vote on punishments, bribe testimony, or earn public absolution",
            "Village Freeholders",
            WorldTileIds.DuelRingFloor,
            "Rook Bailiff",
            "never forgets a witness and never blinks first")
    };

    public static GeneratedWorld Generate(WorldConfig config)
    {
        var random = new Random(config.Seed.Seed);
        var theme = string.IsNullOrWhiteSpace(config.Seed.Theme)
            ? ThemeRegistry.PickRandomId(random)
            : config.Seed.Theme;
        var locationCount = config.Server.Scale switch
        {
            WorldScale.Small => 5,
            WorldScale.Medium => 12,
            WorldScale.Large => 24,
            _ => 5
        };
        var rawTileMap = GenerateTileMap(random, config);
        var locations = GenerateLocations(random, config, theme, locationCount);
        var npcs = GenerateNpcs(random, config, locations);
        var placements = GenerateNpcPlacements(locations, npcs);
        var quests = GenerateStationQuests(locations, npcs, placements);
        var structurePlacements = GenerateStructurePlacements(config, locations);
        var oddities = GenerateOddities(config);
        var oddityPlacements = GenerateOddityPlacements(random, config, locations, oddities);
        var pathEdges = GeneratePathEdges(locations);
        var tileMap = ApplyPathEdges(rawTileMap, pathEdges);

        return new GeneratedWorld(
            config,
            theme,
            tileMap,
            locations,
            npcs,
            placements,
            quests,
            structurePlacements,
            oddities,
            oddityPlacements,
            StarterFactions.All,
            pathEdges);
    }

    private static GeneratedTileMap GenerateTileMap(Random random, WorldConfig config)
    {
        if (ThemeRegistry.Get(config.Seed.Theme).TileMapStyle == ThemeTileMapStyle.BoardingSchool)
        {
            return GenerateBoardingSchoolTileMap(config);
        }

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

    private static GeneratedTileMap GenerateBoardingSchoolTileMap(WorldConfig config)
    {
        var grassTileIds = new[]
        {
            WorldTileIds.GroundScrub,
            WorldTileIds.GroundDust,
            WorldTileIds.PathDust,
            WorldTileIds.ClinicFloor,
            WorldTileIds.MarketFloor,
            WorldTileIds.WorkshopFloor,
            WorldTileIds.DuelRingFloor
        };
        var tiles = new List<GeneratedTile>(config.WidthTiles * config.HeightTiles);
        for (var y = 0; y < config.HeightTiles; y++)
        {
            for (var x = 0; x < config.WidthTiles; x++)
            {
                var grassTileId = grassTileIds[Math.Abs((x * 31) + (y * 17) + ((x / 4) * 7) + (y / 3)) % grassTileIds.Length];
                tiles.Add(new GeneratedTile(x, y, grassTileId, ZoneId: "boarding_school_grass"));
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
        string theme,
        int count)
    {
        var locations = new List<GeneratedLocation>();
        var reserved = new[]
        {
            new TilePosition(5, 5),
            new TilePosition(config.WidthTiles / 2, config.HeightTiles / 2)
        };
        var stationPoints = ProceduralPlacementSampler.GenerateSeparatedPoints(
            random,
            config.WidthTiles,
            config.HeightTiles,
            count,
            edgePadding: 4,
            candidateAttemptsPerPoint: 24,
            reserved);

        for (var i = 0; i < count; i++)
        {
            var station = SocialStations[i % SocialStations.Length];
            var name = i < SocialStations.Length
                ? station.Name
                : $"{GetThemePrefix(theme, random)} {station.Name}";
            var point = stationPoints[i];
            locations.Add(new GeneratedLocation(
                $"location_{i}_{station.Role.Replace('-', '_')}",
                name,
                station.Role,
                station.ThemeTag,
                station.KarmaHook,
                station.Faction,
                $"interior_{station.Role.Replace('-', '_')}_{i}",
                GetStationInteriorKind(station.Role, station.ThemeTag),
                point.X,
                point.Y));
        }

        return locations;
    }

    private static string GetStationInteriorKind(string role, string themeTag)
    {
        return role switch
        {
            "clinic" => "clinic",
            "market" or "black-market" => "market",
            "workshop" or "restricted-storage" => "workshop",
            "notice-board" => "notice-board",
            "social-hub" => "saloon",
            "oddity-yard" => "oddity-yard",
            "duel-ring" => "duel-ring",
            "farm-plot" => "farmstead",
            "memory-shrine" or "witness-court" => "shrine",
            "broadcast-tower" => "broadcast-room",
            _ when themeTag.Contains("care", StringComparison.OrdinalIgnoreCase) => "clinic",
            _ when themeTag.Contains("trade", StringComparison.OrdinalIgnoreCase) => "market",
            _ when themeTag.Contains("repair", StringComparison.OrdinalIgnoreCase) => "workshop",
            _ => "common-room"
        };
    }

    private static IReadOnlyList<NpcProfile> GenerateNpcs(
        Random random,
        WorldConfig config,
        IReadOnlyList<GeneratedLocation> locations)
    {
        var targetCount = config.Server.Scale == WorldScale.Small
            ? Math.Max(1, (config.Server.TargetPlayers * 2) + 2)
            : Math.Max(1, config.Server.TargetPlayers * 3);
        var npcs = new List<NpcProfile> { StarterNpcs.Mara };

        for (var i = 1; i < targetCount; i++)
        {
            var location = locations[(i - 1) % locations.Count];
            var station = SocialStations.FirstOrDefault(candidate => candidate.Role == location.Role) ?? SocialStations[0];
            npcs.Add(new NpcProfile(
                $"generated_npc_{i}_{location.Role.Replace('-', '_')}",
                GeneratedNames[random.Next(GeneratedNames.Length)],
                station.NpcRole,
                $"{GeneratedPersonalities[random.Next(GeneratedPersonalities.Length)]}; {station.NpcTemperament}",
                location.SuggestedFaction,
                BuildNpcNeed(location),
                BuildNpcSecret(location, random),
                BuildLikes(location),
                BuildDislikes(location)));
        }

        return npcs;
    }

    private static IReadOnlyList<GeneratedNpcPlacement> GenerateNpcPlacements(
        IReadOnlyList<GeneratedLocation> locations,
        IReadOnlyList<NpcProfile> npcs)
    {
        var placements = new List<GeneratedNpcPlacement>();
        for (var i = 0; i < npcs.Count; i++)
        {
            var location = locations[Math.Min(i, locations.Count - 1)];
            var npc = npcs[i];
            placements.Add(new GeneratedNpcPlacement(
                npc.Id,
                location.Id,
                npc.Role,
                npc.Faction,
                location.KarmaHook,
                location.X,
                location.Y));
        }

        return placements;
    }

    private static IReadOnlyList<GeneratedStructurePlacement> GenerateStructurePlacements(
        WorldConfig config,
        IReadOnlyList<GeneratedLocation> locations)
    {
        var placements = new List<GeneratedStructurePlacement>();
        foreach (var location in locations)
        {
            var x = Math.Clamp(location.X + 1, 2, config.WidthTiles - 3);
            var y = Math.Clamp(location.Y + 1, 2, config.HeightTiles - 3);
            placements.Add(new GeneratedStructurePlacement(
                $"generated_structure_{SanitizeQuestId(location.Id)}",
                location.Id,
                BuildStructureName(location),
                location.KarmaHook,
                location.SuggestedFaction,
                x,
                y,
                GetInitialStructureIntegrity(location)));
        }

        return placements;
    }

    private static string BuildStructureName(GeneratedLocation location)
    {
        return location.ThemeTag switch
        {
            "care" => $"{location.Name} herb rack",
            "trade" => $"{location.Name} account desk",
            "repair" => $"{location.Name} public workbench",
            "rumor" => $"{location.Name} notice post",
            "relationship" => $"{location.Name} mediation table",
            "temptation" => $"{location.Name} supply lockup",
            "chaos" => $"{location.Name} oddity rig",
            "combat" => $"{location.Name} duel post",
            "sustenance" => $"{location.Name} irrigation wheel",
            "crime" => $"{location.Name} shadow dropbox",
            "redemption" => $"{location.Name} penance ledger",
            "broadcast" => $"{location.Name} bell frame",
            "loyalty" => $"{location.Name} memorial plinth",
            "judgment" => $"{location.Name} witness stand",
            _ => $"{location.Name} station fixture"
        };
    }

    private static int GetInitialStructureIntegrity(GeneratedLocation location)
    {
        return location.ThemeTag switch
        {
            "repair" or "care" or "sustenance" => 55,
            "crime" or "temptation" or "chaos" => 45,
            "broadcast" or "judgment" => 65,
            _ => 70
        };
    }

    private static IReadOnlyList<QuestDefinition> GenerateStationQuests(
        IReadOnlyList<GeneratedLocation> locations,
        IReadOnlyList<NpcProfile> npcs,
        IReadOnlyList<GeneratedNpcPlacement> placements)
    {
        var quests = new List<QuestDefinition>();
        foreach (var placement in placements)
        {
            var npc = npcs.FirstOrDefault(candidate => candidate.Id == placement.NpcId);
            var location = locations.FirstOrDefault(candidate => candidate.Id == placement.LocationId);
            if (npc is null || location is null || npc.Id == StarterNpcs.Mara.Id || npc.Id == StarterNpcs.Dallen.Id)
            {
                continue;
            }

            var questId = $"generated_station_help_{SanitizeQuestId(location.Id)}_{SanitizeQuestId(npc.Id)}";
            var scripReward = GetStationQuestReward(location);
            var module = QuestModuleRegistry.GetForStation(location.Role);
            QuestDefinition quest;
            if (module != null)
            {
                var otherPlacements = placements
                    .Where(p => p.NpcId != placement.NpcId &&
                                p.NpcId != StarterNpcs.Mara.Id &&
                                p.NpcId != StarterNpcs.Dallen.Id)
                    .Select(p => new QuestPlacementInfo(p.NpcId, p.LocationId))
                    .ToList();
                var ctx = new QuestCreationContext(
                    questId,
                    location.Id,
                    location.Name,
                    location.Role,
                    npc.Id,
                    scripReward,
                    otherPlacements);
                quest = module.CreateQuest(ctx);
            }
            else
            {
                quest = new QuestDefinition(
                    questId,
                    $"Stabilize {location.Name}",
                    npc.Id,
                    $"{npc.Name} needs help at {location.Name}: {npc.Need}. The local karma hook is to {location.KarmaHook}.",
                    Array.Empty<string>(),
                    $"generated_station_help:{location.Id}",
                    ScripReward: scripReward);
            }
            quests.Add(quest);
        }

        return quests;
    }

    private static int GetStationQuestReward(GeneratedLocation location)
    {
        return location.ThemeTag switch
        {
            "care" or "repair" or "redemption" => 14,
            "crime" or "temptation" or "judgment" => 16,
            "combat" or "loyalty" => 12,
            _ => 10
        };
    }

    private static string SanitizeQuestId(string value)
    {
        return string.Concat(value.Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '_')).Trim('_');
    }

    private static string BuildNpcNeed(GeneratedLocation location)
    {
        return location.ThemeTag switch
        {
            "care" => "medicine, clean filters, and someone willing to help without turning it into leverage",
            "trade" => "a debt settled before it becomes a faction incident",
            "repair" => "a public repair completed before sabotage becomes normal",
            "rumor" => "proof that separates warning from slander",
            "relationship" => "a mediator before a temporary posse becomes a permanent feud",
            "temptation" => "shared supplies protected from opportunists",
            "chaos" => "someone brave enough to test whether the weird junk is useful or cursed",
            "combat" => "duel etiquette enforced before violence spills into the street",
            "sustenance" => "food secured without turning hunger into theft",
            "crime" => "heat redirected away from a client who may or may not deserve it",
            "redemption" => "reparations that cost more than words",
            "broadcast" => "a proclamation raised, buried, or corrected before everyone believes the wrong story",
            "loyalty" => "old allegiances untangled before they become new violence",
            "judgment" => "witnesses protected from bribes, threats, and convenient forgetfulness",
            _ => location.KarmaHook
        };
    }

    private static string BuildNpcSecret(GeneratedLocation location, Random random)
    {
        var secret = GeneratedSecrets[random.Next(GeneratedSecrets.Length)];
        return $"{secret}; tied to {location.Name} where players can {location.KarmaHook}";
    }

    private static string[] BuildLikes(GeneratedLocation location)
    {
        return new[]
        {
            "follow-through",
            location.ThemeTag,
            location.SuggestedFaction,
            "karma choices with consequences"
        };
    }

    private static string[] BuildDislikes(GeneratedLocation location)
    {
        return new[]
        {
            "empty promises",
            "collateral damage",
            $"abusing {location.Role}"
        };
    }

    private static string GetThemePrefix(string theme, Random random)
    {
        var prefixes = theme switch
        {
            "farming" => new[] { "Sun-Baked", "Irrigated", "Compost-Sacred" },
            "western-sci-fi" => new[] { "Dust-Lit", "Railgun", "Frontier" },
            "boarding_school" => new[] { "Courtyard", "Ivy-Halled", "Headmaster's" },
            "haunted-coastal" => new[] { "Fogbound", "Salt-Ghost", "Tideworn" },
            "corporate-moon-colony" => new[] { "Compliance", "Low-G", "Shareholder" },
            "dieselpunk-warfront" => new[] { "Armistice", "Trenchside", "Signal-Flare" },
            _ => new[] { "Neon", "Patchwork", "Impossible" }
        };

        return prefixes[random.Next(prefixes.Length)];
    }

    private static IReadOnlyList<GeneratedOddityPlacement> GenerateOddityPlacements(
        Random random,
        WorldConfig config,
        IReadOnlyList<GeneratedLocation> locations,
        IReadOnlyList<GameItem> oddities)
    {
        var reserved = locations
            .Select(location => new TilePosition(location.X, location.Y))
            .Append(new TilePosition(config.WidthTiles / 2, config.HeightTiles / 2))
            .ToArray();
        var points = ProceduralPlacementSampler.GenerateSeparatedPoints(
            random,
            config.WidthTiles,
            config.HeightTiles,
            oddities.Count,
            edgePadding: 3,
            candidateAttemptsPerPoint: 18,
            reserved);
        var placements = new List<GeneratedOddityPlacement>(oddities.Count);

        for (var i = 0; i < oddities.Count; i++)
        {
            var nearestLocation = locations
                .OrderBy(location => new TilePosition(location.X, location.Y).DistanceSquaredTo(points[i]))
                .First();
            placements.Add(new GeneratedOddityPlacement(
                oddities[i].Id,
                nearestLocation.Id,
                BuildOddityPlacementReason(oddities[i], nearestLocation),
                points[i].X,
                points[i].Y));
        }

        return placements;
    }

    private static string BuildOddityPlacementReason(GameItem oddity, GeneratedLocation location)
    {
        return $"{oddity.Name} spawned near {location.Name} to support {location.ThemeTag} choices: {location.KarmaHook}";
    }

    private static IReadOnlyList<GameItem> GenerateOddities(WorldConfig config)
    {
        var oddities = new List<GameItem>
        {
            StarterItems.WhoopieCushion,
            StarterItems.DeflatedBalloon,
            StarterItems.DataChip,
            StarterItems.ContrabandPackage,
            StarterItems.ApologyFlower,
            StarterItems.PortableTerminal
        };

        if (config.Server.Scale != WorldScale.Small)
        {
            oddities.Add(StarterItems.RepairKit);
            oddities.Add(StarterItems.RationPack);
            oddities.Add(StarterItems.FilterCore);
        }

        return oddities;
    }

    private static IReadOnlyList<GeneratedPathEdge> GeneratePathEdges(IReadOnlyList<GeneratedLocation> locations)
    {
        if (locations.Count < 2)
        {
            return Array.Empty<GeneratedPathEdge>();
        }

        // Prim's MST: greedily add the closest unconnected location each step.
        var edges = new List<GeneratedPathEdge>(locations.Count - 1);
        var inTree = new HashSet<string> { locations[0].Id };
        var locationById = locations.ToDictionary(l => l.Id);

        while (inTree.Count < locations.Count)
        {
            GeneratedLocation bestFrom = null;
            GeneratedLocation bestTo = null;
            var bestDistSquared = int.MaxValue;

            foreach (var loc in locations.Where(l => inTree.Contains(l.Id)))
            {
                foreach (var candidate in locations.Where(l => !inTree.Contains(l.Id)))
                {
                    var dx = loc.X - candidate.X;
                    var dy = loc.Y - candidate.Y;
                    var distSquared = dx * dx + dy * dy;
                    if (distSquared < bestDistSquared)
                    {
                        bestDistSquared = distSquared;
                        bestFrom = loc;
                        bestTo = candidate;
                    }
                }
            }

            if (bestFrom is null)
            {
                break;
            }

            edges.Add(new GeneratedPathEdge(
                bestFrom.Id,
                bestTo.Id,
                bestFrom.X,
                bestFrom.Y,
                bestTo.X,
                bestTo.Y));
            inTree.Add(bestTo.Id);
        }

        return edges;
    }

    private static GeneratedTileMap ApplyPathEdges(GeneratedTileMap tileMap, IReadOnlyList<GeneratedPathEdge> pathEdges)
    {
        if (pathEdges.Count == 0)
        {
            return tileMap;
        }

        var tiles = tileMap.Tiles.ToArray();

        foreach (var edge in pathEdges)
        {
            var dx = edge.ToX - edge.FromX;
            var dy = edge.ToY - edge.FromY;
            var steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (steps == 0)
            {
                continue;
            }

            for (var i = 0; i <= steps; i++)
            {
                var x = (int)Math.Round(edge.FromX + dx * (double)i / steps);
                var y = (int)Math.Round(edge.FromY + dy * (double)i / steps);
                if (x < 0 || y < 0 || x >= tileMap.Width || y >= tileMap.Height)
                {
                    continue;
                }

                var idx = y * tileMap.Width + x;
                var tile = tiles[idx];
                if (tile.FloorId is WorldTileIds.GroundScrub or WorldTileIds.GroundDust)
                {
                    tiles[idx] = tile with { FloorId = WorldTileIds.PathDust, ZoneId = "road_path" };
                }
            }
        }

        return tileMap with { Tiles = tiles };
    }

    private static readonly string[] GeneratedNames =
    {
        "Tessa Null",
        "Bo Dimple",
        "Arlo Switch",
        "Nina Brack",
        "Cal Ponder",
        "Vera Mercy",
        "Hob Lark",
        "Sister Static",
        "June Hex",
        "Marshal Kettle"
    };

    private static readonly string[] GeneratedPersonalities =
    {
        "cheerful, evasive, allergic to plans",
        "stern, sentimental, easily bribed with soup",
        "curious, dramatic, terrible at secrets",
        "patient, gloomy, collects broken bells",
        "reckless, generous, keeps score in pencil",
        "polite, ominous, laughs exactly once"
    };

    private static readonly string[] GeneratedSecrets =
    {
        "owes a debt to the black market",
        "has been hiding town supplies",
        "is protecting a rival player",
        "knows why the mayor banned balloons",
        "forged a public bounty to catch a worse liar",
        "quietly funded both sides of a feud to keep them talking"
    };

    private sealed record SocialStationArchetype(
        string Role,
        string Name,
        string ThemeTag,
        string KarmaHook,
        string Faction,
        string FloorId,
        string NpcRole,
        string NpcTemperament);
}
