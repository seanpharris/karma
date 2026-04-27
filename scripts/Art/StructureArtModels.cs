using System.Collections.Generic;
using Godot;

namespace Karma.Art;

public enum StructureSpriteKind
{
    GreenhouseStandard,
    GreenhouseOvergrown,
    GreenhouseDamaged,
    GreenhousePoweredOff,
    GreenhouseTopDown,
    GreenhouseBaseRing,
    GreenhouseDoorModule,
    GreenhouseTopCap,
    GreenhousePlanter,
    GreenhouseGrowRack,
    GreenhouseSupportColumn,
    GreenhouseGlassPanel
}

public sealed record StructureSpriteDefinition(
    StructureSpriteKind Kind,
    string Id,
    string DisplayName,
    string Category,
    Vector2 Size,
    string AtlasPath,
    Rect2 AtlasRegion,
    bool HasAtlasRegion);

public static class StructureArtCatalog
{
    public const string GreenhouseAtlasPath = "res://assets/art/structures/scifi_greenhouse_atlas.png";

    private static readonly IReadOnlyDictionary<StructureSpriteKind, StructureSpriteDefinition> Definitions =
        new Dictionary<StructureSpriteKind, StructureSpriteDefinition>
        {
            [StructureSpriteKind.GreenhouseStandard] = Greenhouse(
                StructureSpriteKind.GreenhouseStandard,
                "greenhouse_standard",
                "Greenhouse",
                new Vector2(96f, 72f),
                new Rect2(392f, 528f, 260f, 190f)),
            [StructureSpriteKind.GreenhouseOvergrown] = Greenhouse(
                StructureSpriteKind.GreenhouseOvergrown,
                "greenhouse_overgrown",
                "Overgrown Greenhouse",
                new Vector2(96f, 76f),
                new Rect2(660f, 520f, 250f, 210f)),
            [StructureSpriteKind.GreenhouseDamaged] = Greenhouse(
                StructureSpriteKind.GreenhouseDamaged,
                "greenhouse_damaged",
                "Damaged Greenhouse",
                new Vector2(96f, 72f),
                new Rect2(930f, 525f, 240f, 195f)),
            [StructureSpriteKind.GreenhousePoweredOff] = Greenhouse(
                StructureSpriteKind.GreenhousePoweredOff,
                "greenhouse_powered_off",
                "Powered Off Greenhouse",
                new Vector2(96f, 72f),
                new Rect2(1198f, 525f, 250f, 190f)),
            [StructureSpriteKind.GreenhouseTopDown] = Greenhouse(
                StructureSpriteKind.GreenhouseTopDown,
                "greenhouse_top_down",
                "Greenhouse Top Down",
                new Vector2(80f, 80f),
                new Rect2(24f, 488f, 240f, 220f)),
            [StructureSpriteKind.GreenhouseBaseRing] = Part(
                StructureSpriteKind.GreenhouseBaseRing,
                "greenhouse_base_ring",
                "Greenhouse Base Ring",
                new Vector2(64f, 36f),
                new Rect2(1000f, 82f, 230f, 110f)),
            [StructureSpriteKind.GreenhouseDoorModule] = Part(
                StructureSpriteKind.GreenhouseDoorModule,
                "greenhouse_door_module",
                "Greenhouse Door Module",
                new Vector2(38f, 44f),
                new Rect2(1240f, 74f, 120f, 128f)),
            [StructureSpriteKind.GreenhouseTopCap] = Part(
                StructureSpriteKind.GreenhouseTopCap,
                "greenhouse_top_cap",
                "Greenhouse Top Cap",
                new Vector2(36f, 24f),
                new Rect2(520f, 82f, 110f, 80f)),
            [StructureSpriteKind.GreenhousePlanter] = Part(
                StructureSpriteKind.GreenhousePlanter,
                "greenhouse_planter",
                "Greenhouse Planter",
                new Vector2(32f, 28f),
                new Rect2(34f, 792f, 120f, 98f)),
            [StructureSpriteKind.GreenhouseGrowRack] = Part(
                StructureSpriteKind.GreenhouseGrowRack,
                "greenhouse_grow_rack",
                "Greenhouse Grow Rack",
                new Vector2(36f, 34f),
                new Rect2(170f, 796f, 115f, 112f)),
            [StructureSpriteKind.GreenhouseSupportColumn] = Part(
                StructureSpriteKind.GreenhouseSupportColumn,
                "greenhouse_support_column",
                "Greenhouse Support Column",
                new Vector2(20f, 38f),
                new Rect2(667f, 312f, 55f, 105f)),
            [StructureSpriteKind.GreenhouseGlassPanel] = Part(
                StructureSpriteKind.GreenhouseGlassPanel,
                "greenhouse_glass_panel",
                "Greenhouse Glass Panel",
                new Vector2(28f, 34f),
                new Rect2(34f, 986f, 82f, 100f))
        };

    public static IReadOnlyDictionary<StructureSpriteKind, StructureSpriteDefinition> All => Definitions;

    public static StructureSpriteDefinition Get(StructureSpriteKind kind)
    {
        return Definitions[kind];
    }

    public static StructureSpriteDefinition GetById(string id)
    {
        return TryGetById(id, out var definition)
            ? definition
            : Definitions[StructureSpriteKind.GreenhouseStandard];
    }

    public static bool TryGetById(string id, out StructureSpriteDefinition definition)
    {
        foreach (var candidate in Definitions.Values)
        {
            if (candidate.Id == id)
            {
                definition = candidate;
                return true;
            }
        }

        definition = null;
        return false;
    }

    private static StructureSpriteDefinition Greenhouse(
        StructureSpriteKind kind,
        string id,
        string displayName,
        Vector2 size,
        Rect2 atlasRegion)
    {
        return new StructureSpriteDefinition(
            kind,
            id,
            displayName,
            "greenhouse",
            size,
            GreenhouseAtlasPath,
            atlasRegion,
            HasAtlasRegion: true);
    }

    private static StructureSpriteDefinition Part(
        StructureSpriteKind kind,
        string id,
        string displayName,
        Vector2 size,
        Rect2 atlasRegion)
    {
        return new StructureSpriteDefinition(
            kind,
            id,
            displayName,
            "greenhouse_part",
            size,
            GreenhouseAtlasPath,
            atlasRegion,
            HasAtlasRegion: true);
    }
}
