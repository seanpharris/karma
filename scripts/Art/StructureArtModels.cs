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
    GreenhouseGlassPanel,
    CargoCrate,
    UtilityJunctionBox,
    StationWallSegment,
    CompactKioskTerminal,
    RedMineralRock,
    BlueCrystalShard,
    CactusSucculent,
    BerryBush,
    MossPatch,
    MushroomCluster,
    BoardingSchoolMainHall,
    BoardingSchoolNoticeBoard,
    BoardingSchoolFountain,
    BoardingSchoolStudentRooms,
    BoardingSchoolCommonRoom,
    BoardingSchoolClassroom,
    BoardingSchoolFacultyOffice,
    BoardingSchoolLibrary,
    BoardingSchoolCourtyardTree,
    BoardingSchoolStoneBench,
    BoardingSchoolHedgerowSegment,
    BoardingSchoolStatuePlinth,
    BoardingSchoolBoulderCluster,
    BoardingSchoolOldLanternPost,
    BoardingSchoolIronFenceStraight,
    BoardingSchoolIronFenceGate,
    BoardingSchoolBookStack,
    BoardingSchoolParchmentPile,
    BoardingSchoolSchoolBag,
    BoardingSchoolBroomBucket,
    BoardingSchoolWoodenChair,
    BoardingSchoolStudyTable,
    BoardingSchoolNoticeStand,
    BoardingSchoolPrankBox,
    BoardingSchoolCourtyardOak,
    BoardingSchoolIvyTree,
    BoardingSchoolNarrowCypress,
    BoardingSchoolSmallMaple,
    BoardingSchoolHedgeStraight,
    BoardingSchoolHedgeCorner,
    BoardingSchoolHedgeGateGap,
    BoardingSchoolIvyWallStrip,
    BoardingSchoolStonePlanter,
    BoardingSchoolFlowerBed,
    BoardingSchoolPottedPlant,
    BoardingSchoolBrassUrnPlant,
    BoardingSchoolMossyStump,
    BoardingSchoolIvyClump,
    BoardingSchoolTallGrassClump,
    BoardingSchoolFallenLeavesPile,
    BoardingSchoolGrassFlowersA,
    BoardingSchoolGrassFlowersB,
    BoardingSchoolGrassFlowersC,
    // Sliced props from the priority Gemini atlases (2026-04-30):
    ClinicBed,
    MedicalCrate,
    AmbulanceStretcher,
    MedicineCabinet,
    SupplyDropParachute,
    ShopKiosk,
    ShopShelves,
    LockedMetalChest,
    WoodChestOpen,
    AmmoCrateMetal,
    WeaponCaseLong,
    ToolBoxOpen,
    BackpackBrown,
    WrappedParcel,
    WantedMugShotFrame,
    WantedBulletinBoard,
    JailBarredWindow,
    BarricadeGate,
    EvidenceTable,
    EvidenceLockers,
    GuardBoothKiosk,
    SirenBeaconBlue,
    HandcuffsSilver,
    // Structure damage states (Step 12 sabotage display):
    GeneratorPristine,
    GeneratorDamaged,
    GeneratorWrecked,
    GeneratorSabotaged,
    GreenhousePristineAlt,
    GreenhouseShattered,
    ElectricalBoxClosed,
    ElectricalBoxSparking,
    NoticeBoardCluttered,
    NoticeBoardBlank,
    FirePit,
    // Interior furniture:
    BedClean,
    CotBed,
    ScribeDeskInterior,
    SofaGrey,
    SofaTan,
    BenchWood,
    ChairWood,
    StoolRound,
    VendingMachine,
    CorkboardNotes,
    StockedShelves,
    RegisterCounter,
    // Crafting stations (Step 36):
    MechanicalWorkbench,
    AlchemyWorkbench,
    WeaponsDisplayPegboard,
    ElectronicsWorkshop,
    ComputerWorkbench,
    HydroponicsPlanterGrown,
    // Containers / loot variety:
    WoodChestClosed,
    SciFiChest,
    OrnateChest,
    MedkitWhiteBox,
    DuffelBag,
    MilitaryTrunk,
    ShippingContainer,
    BarrelWood,
    SafePadlock,
    // Modular walls / doors (Step 17-18):
    ClinicDoor,
    ShopDoor,
    JailDoor,
    AirlockDoorOpen,
    AirlockDoorClosed,
    GateClosed,
    GateOpen,
    WindowLit,
    WindowBroken,
    FenceStraight,
    Archway
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
    public const string BoardingSchoolBuildingsAtlasPath = "res://assets/themes/boarding_school/buildings_atlas.png";
    public const string BoardingSchoolPropsAtlasPath = "res://assets/themes/boarding_school/props_atlas.png";
    public const string BoardingSchoolTreesAtlasPath = "res://assets/themes/boarding_school/trees_atlas.png";
    public const string BoardingSchoolGrassAtlasPath = "res://assets/themes/boarding_school/grass_tiles_1_32.png";
    public const string GeminiStaticPropsRoot = "res://assets/art/sprites/generated/gemini_static_props_2026_04_27/polished/";
    public const string GeminiNaturalPropsRoot = "res://assets/art/sprites/generated/gemini_natural_props_2026_04_27/polished/";
    public const string SlicedClinicRoot = "res://assets/art/generated/sliced/clinic_rescue_revive/";
    public const string SlicedSupplyShopRoot = "res://assets/art/generated/sliced/supply_shop_loot/";
    public const string SlicedWantedBountyRoot = "res://assets/art/generated/sliced/wanted_bounty_law/";
    public const string SlicedStructureStateRoot = "res://assets/art/generated/sliced/structure_world_state/";
    public const string SlicedInteriorFurnitureRoot = "res://assets/art/generated/sliced/interior_furniture/";
    public const string SlicedCraftingStationsRoot = "res://assets/art/generated/sliced/crafting_stations/";
    public const string SlicedContainersLootRoot = "res://assets/art/generated/sliced/containers_loot/";
    public const string SlicedWallsDoorsRoot = "res://assets/art/generated/sliced/modular_walls_doors/";

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
                new Rect2(34f, 986f, 82f, 100f)),
            [StructureSpriteKind.CargoCrate] = GeminiStaticProp(
                StructureSpriteKind.CargoCrate,
                "cargo_crate",
                "Cargo Crate",
                "station_prop",
                new Vector2(36f, 32f),
                "cargo_crate.png"),
            [StructureSpriteKind.UtilityJunctionBox] = GeminiStaticProp(
                StructureSpriteKind.UtilityJunctionBox,
                "utility_junction_box",
                "Utility Junction Box",
                "station_prop",
                new Vector2(30f, 34f),
                "utility_junction_box.png"),
            [StructureSpriteKind.StationWallSegment] = GeminiStaticProp(
                StructureSpriteKind.StationWallSegment,
                "station_wall_segment",
                "Station Wall Segment",
                "station_prop",
                new Vector2(48f, 42f),
                "station_wall_segment.png"),
            [StructureSpriteKind.CompactKioskTerminal] = GeminiStaticProp(
                StructureSpriteKind.CompactKioskTerminal,
                "compact_kiosk_terminal",
                "Compact Kiosk Terminal",
                "station_prop",
                new Vector2(30f, 34f),
                "compact_kiosk_terminal.png"),
            [StructureSpriteKind.RedMineralRock] = GeminiNaturalProp(
                StructureSpriteKind.RedMineralRock,
                "red_mineral_rock",
                "Red Mineral Rock",
                "natural_prop",
                new Vector2(34f, 28f),
                "red_mineral_rock.png"),
            [StructureSpriteKind.BlueCrystalShard] = GeminiNaturalProp(
                StructureSpriteKind.BlueCrystalShard,
                "blue_crystal_shard",
                "Blue Crystal Shard",
                "natural_prop",
                new Vector2(34f, 38f),
                "blue_crystal_shard.png"),
            [StructureSpriteKind.CactusSucculent] = GeminiNaturalProp(
                StructureSpriteKind.CactusSucculent,
                "cactus_succulent",
                "Cactus Succulent",
                "natural_prop",
                new Vector2(30f, 38f),
                "cactus_succulent.png"),
            [StructureSpriteKind.BerryBush] = GeminiNaturalProp(
                StructureSpriteKind.BerryBush,
                "berry_bush",
                "Berry Bush",
                "natural_prop",
                new Vector2(34f, 32f),
                "berry_bush.png"),
            [StructureSpriteKind.MossPatch] = GeminiNaturalProp(
                StructureSpriteKind.MossPatch,
                "moss_patch",
                "Moss Patch",
                "natural_prop",
                new Vector2(36f, 22f),
                "moss_patch.png"),
            [StructureSpriteKind.MushroomCluster] = GeminiNaturalProp(
                StructureSpriteKind.MushroomCluster,
                "mushroom_cluster",
                "Mushroom Cluster",
                "natural_prop",
                new Vector2(30f, 30f),
                "mushroom_cluster.png"),
            [StructureSpriteKind.BoardingSchoolMainHall] = BoardingSchoolBuilding(
                StructureSpriteKind.BoardingSchoolMainHall,
                "boarding_school_main_hall",
                "Main Hall",
                new Rect2(0f, 0f, 768f, 576f)),
            [StructureSpriteKind.BoardingSchoolNoticeBoard] = BoardingSchoolBuilding(
                StructureSpriteKind.BoardingSchoolNoticeBoard,
                "boarding_school_notice_board",
                "Notice Board",
                new Rect2(768f, 0f, 128f, 128f)),
            [StructureSpriteKind.BoardingSchoolFountain] = BoardingSchoolBuilding(
                StructureSpriteKind.BoardingSchoolFountain,
                "boarding_school_fountain",
                "Fountain",
                new Rect2(1408f, 0f, 256f, 256f)),
            [StructureSpriteKind.BoardingSchoolStudentRooms] = BoardingSchoolBuilding(
                StructureSpriteKind.BoardingSchoolStudentRooms,
                "boarding_school_student_rooms",
                "Student Rooms",
                new Rect2(0f, 576f, 768f, 432f)),
            [StructureSpriteKind.BoardingSchoolCommonRoom] = BoardingSchoolBuilding(
                StructureSpriteKind.BoardingSchoolCommonRoom,
                "boarding_school_common_room",
                "Common Room",
                new Rect2(768f, 576f, 512f, 384f)),
            [StructureSpriteKind.BoardingSchoolClassroom] = BoardingSchoolBuilding(
                StructureSpriteKind.BoardingSchoolClassroom,
                "boarding_school_classroom",
                "Classroom",
                new Rect2(1408f, 576f, 512f, 384f)),
            [StructureSpriteKind.BoardingSchoolFacultyOffice] = BoardingSchoolBuilding(
                StructureSpriteKind.BoardingSchoolFacultyOffice,
                "boarding_school_faculty_office",
                "Faculty Office",
                new Rect2(0f, 1008f, 512f, 384f)),
            [StructureSpriteKind.BoardingSchoolLibrary] = BoardingSchoolBuilding(
                StructureSpriteKind.BoardingSchoolLibrary,
                "boarding_school_library",
                "Library",
                new Rect2(768f, 1008f, 640f, 640f)),
            [StructureSpriteKind.BoardingSchoolCourtyardTree] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolCourtyardTree, "boarding_school_courtyard_tree", "Courtyard Tree", new Vector2(128f, 160f), new Rect2(0f, 0f, 256f, 320f)),
            [StructureSpriteKind.BoardingSchoolStoneBench] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolStoneBench, "boarding_school_stone_bench", "Stone Bench", new Vector2(112f, 64f), new Rect2(256f, 0f, 112f, 64f)),
            [StructureSpriteKind.BoardingSchoolHedgerowSegment] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolHedgerowSegment, "boarding_school_hedgerow_segment", "Hedgerow Segment", new Vector2(160f, 96f), new Rect2(384f, 0f, 160f, 96f)),
            [StructureSpriteKind.BoardingSchoolStatuePlinth] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolStatuePlinth, "boarding_school_statue_plinth", "Statue Plinth", new Vector2(128f, 160f), new Rect2(544f, 0f, 128f, 160f)),
            [StructureSpriteKind.BoardingSchoolBoulderCluster] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolBoulderCluster, "boarding_school_boulder_cluster", "Boulder Cluster", new Vector2(128f, 128f), new Rect2(0f, 320f, 128f, 128f)),
            [StructureSpriteKind.BoardingSchoolOldLanternPost] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolOldLanternPost, "boarding_school_old_lantern_post", "Old Lantern Post", new Vector2(96f, 144f), new Rect2(256f, 320f, 128f, 192f)),
            [StructureSpriteKind.BoardingSchoolIronFenceStraight] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolIronFenceStraight, "boarding_school_iron_fence_straight", "Iron Fence", new Vector2(160f, 96f), new Rect2(384f, 320f, 160f, 96f)),
            [StructureSpriteKind.BoardingSchoolIronFenceGate] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolIronFenceGate, "boarding_school_iron_fence_gate", "Iron Fence Gate", new Vector2(160f, 128f), new Rect2(544f, 320f, 160f, 128f)),
            [StructureSpriteKind.BoardingSchoolBookStack] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolBookStack, "boarding_school_book_stack", "Book Stack", new Vector2(40f, 40f), new Rect2(0f, 512f, 40f, 40f)),
            [StructureSpriteKind.BoardingSchoolParchmentPile] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolParchmentPile, "boarding_school_parchment_pile", "Parchment Pile", new Vector2(40f, 40f), new Rect2(256f, 512f, 40f, 40f)),
            [StructureSpriteKind.BoardingSchoolSchoolBag] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolSchoolBag, "boarding_school_school_bag", "School Bag", new Vector2(40f, 40f), new Rect2(384f, 512f, 40f, 40f)),
            [StructureSpriteKind.BoardingSchoolBroomBucket] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolBroomBucket, "boarding_school_broom_bucket", "Broom Bucket", new Vector2(56f, 72f), new Rect2(544f, 512f, 56f, 72f)),
            [StructureSpriteKind.BoardingSchoolWoodenChair] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolWoodenChair, "boarding_school_wooden_chair", "Wooden Chair", new Vector2(56f, 64f), new Rect2(0f, 584f, 56f, 64f)),
            [StructureSpriteKind.BoardingSchoolStudyTable] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolStudyTable, "boarding_school_study_table", "Study Table", new Vector2(80f, 56f), new Rect2(256f, 584f, 80f, 56f)),
            [StructureSpriteKind.BoardingSchoolNoticeStand] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolNoticeStand, "boarding_school_notice_stand", "Notice Stand", new Vector2(128f, 160f), new Rect2(384f, 584f, 128f, 160f)),
            [StructureSpriteKind.BoardingSchoolPrankBox] = BoardingSchoolProp(
                StructureSpriteKind.BoardingSchoolPrankBox, "boarding_school_prank_box", "Prank Box", new Vector2(40f, 40f), new Rect2(544f, 584f, 40f, 40f)),
            [StructureSpriteKind.BoardingSchoolCourtyardOak] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolCourtyardOak, "boarding_school_courtyard_oak", "Courtyard Oak", new Vector2(128f, 160f), new Rect2(0f, 0f, 256f, 320f)),
            [StructureSpriteKind.BoardingSchoolIvyTree] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolIvyTree, "boarding_school_ivy_tree", "Ivy Tree", new Vector2(144f, 168f), new Rect2(256f, 0f, 192f, 224f)),
            [StructureSpriteKind.BoardingSchoolNarrowCypress] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolNarrowCypress, "boarding_school_narrow_cypress", "Narrow Cypress", new Vector2(96f, 168f), new Rect2(448f, 0f, 128f, 224f)),
            [StructureSpriteKind.BoardingSchoolSmallMaple] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolSmallMaple, "boarding_school_small_maple", "Small Maple", new Vector2(120f, 144f), new Rect2(608f, 0f, 160f, 192f)),
            [StructureSpriteKind.BoardingSchoolHedgeStraight] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolHedgeStraight, "boarding_school_hedge_straight", "Hedge", new Vector2(160f, 96f), new Rect2(0f, 320f, 160f, 96f)),
            [StructureSpriteKind.BoardingSchoolHedgeCorner] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolHedgeCorner, "boarding_school_hedge_corner", "Hedge Corner", new Vector2(128f, 128f), new Rect2(256f, 320f, 128f, 128f)),
            [StructureSpriteKind.BoardingSchoolHedgeGateGap] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolHedgeGateGap, "boarding_school_hedge_gate_gap", "Hedge Gate Gap", new Vector2(160f, 96f), new Rect2(448f, 320f, 160f, 96f)),
            [StructureSpriteKind.BoardingSchoolIvyWallStrip] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolIvyWallStrip, "boarding_school_ivy_wall_strip", "Ivy Wall Strip", new Vector2(96f, 160f), new Rect2(608f, 320f, 96f, 160f)),
            [StructureSpriteKind.BoardingSchoolStonePlanter] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolStonePlanter, "boarding_school_stone_planter", "Stone Planter", new Vector2(128f, 64f), new Rect2(0f, 480f, 128f, 64f)),
            [StructureSpriteKind.BoardingSchoolFlowerBed] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolFlowerBed, "boarding_school_flower_bed", "Flower Bed", new Vector2(128f, 64f), new Rect2(256f, 480f, 128f, 64f)),
            [StructureSpriteKind.BoardingSchoolPottedPlant] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolPottedPlant, "boarding_school_potted_plant", "Potted Plant", new Vector2(48f, 64f), new Rect2(448f, 480f, 48f, 64f)),
            [StructureSpriteKind.BoardingSchoolBrassUrnPlant] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolBrassUrnPlant, "boarding_school_brass_urn_plant", "Brass Urn Plant", new Vector2(64f, 96f), new Rect2(608f, 480f, 64f, 96f)),
            [StructureSpriteKind.BoardingSchoolMossyStump] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolMossyStump, "boarding_school_mossy_stump", "Mossy Stump", new Vector2(64f, 64f), new Rect2(0f, 576f, 64f, 64f)),
            [StructureSpriteKind.BoardingSchoolIvyClump] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolIvyClump, "boarding_school_ivy_clump", "Ivy Clump", new Vector2(64f, 48f), new Rect2(256f, 576f, 64f, 48f)),
            [StructureSpriteKind.BoardingSchoolTallGrassClump] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolTallGrassClump, "boarding_school_tall_grass_clump", "Tall Grass", new Vector2(48f, 48f), new Rect2(448f, 576f, 48f, 48f)),
            [StructureSpriteKind.BoardingSchoolFallenLeavesPile] = BoardingSchoolTree(
                StructureSpriteKind.BoardingSchoolFallenLeavesPile, "boarding_school_fallen_leaves_pile", "Fallen Leaves", new Vector2(48f, 48f), new Rect2(608f, 576f, 48f, 48f)),
            [StructureSpriteKind.BoardingSchoolGrassFlowersA] = BoardingSchoolGrassDetail(
                StructureSpriteKind.BoardingSchoolGrassFlowersA, "boarding_school_grass_flowers_a", "Grass Flowers A", new Rect2(0f, 0f, 32f, 32f)),
            [StructureSpriteKind.BoardingSchoolGrassFlowersB] = BoardingSchoolGrassDetail(
                StructureSpriteKind.BoardingSchoolGrassFlowersB, "boarding_school_grass_flowers_b", "Grass Flowers B", new Rect2(32f, 0f, 32f, 32f)),
            [StructureSpriteKind.BoardingSchoolGrassFlowersC] = BoardingSchoolGrassDetail(
                StructureSpriteKind.BoardingSchoolGrassFlowersC, "boarding_school_grass_flowers_c", "Grass Flowers C", new Rect2(64f, 0f, 32f, 32f)),

            // Sliced clinic / rescue / revive props
            [StructureSpriteKind.ClinicBed] = SlicedProp(
                StructureSpriteKind.ClinicBed, "clinic_bed", "Clinic Bed", "clinic_prop",
                new Vector2(40f, 28f), SlicedClinicRoot, "clinic_bed.png"),
            [StructureSpriteKind.MedicalCrate] = SlicedProp(
                StructureSpriteKind.MedicalCrate, "medical_crate", "Medical Crate", "clinic_prop",
                new Vector2(28f, 28f), SlicedClinicRoot, "medical_crate.png"),
            [StructureSpriteKind.AmbulanceStretcher] = SlicedProp(
                StructureSpriteKind.AmbulanceStretcher, "ambulance_stretcher", "Ambulance Stretcher", "clinic_prop",
                new Vector2(48f, 28f), SlicedClinicRoot, "ambulance_stretcher.png"),
            [StructureSpriteKind.MedicineCabinet] = SlicedProp(
                StructureSpriteKind.MedicineCabinet, "medicine_cabinet", "Medicine Cabinet", "clinic_prop",
                new Vector2(28f, 36f), SlicedClinicRoot, "medicine_cabinet.png"),

            // Sliced supply / shop / loot props
            [StructureSpriteKind.SupplyDropParachute] = SlicedProp(
                StructureSpriteKind.SupplyDropParachute, "supply_drop_parachute", "Supply Drop", "supply_prop",
                new Vector2(40f, 40f), SlicedSupplyShopRoot, "supply_drop_parachute.png"),
            [StructureSpriteKind.ShopKiosk] = SlicedProp(
                StructureSpriteKind.ShopKiosk, "shop_kiosk", "Shop Kiosk", "shop_prop",
                new Vector2(48f, 48f), SlicedSupplyShopRoot, "shop_kiosk.png"),
            [StructureSpriteKind.ShopShelves] = SlicedProp(
                StructureSpriteKind.ShopShelves, "shop_shelves", "Shop Shelves", "shop_prop",
                new Vector2(40f, 44f), SlicedSupplyShopRoot, "shop_shelves.png"),
            [StructureSpriteKind.LockedMetalChest] = SlicedProp(
                StructureSpriteKind.LockedMetalChest, "locked_metal_chest", "Locked Metal Chest", "loot_prop",
                new Vector2(32f, 28f), SlicedSupplyShopRoot, "locked_metal_chest.png"),
            [StructureSpriteKind.WoodChestOpen] = SlicedProp(
                StructureSpriteKind.WoodChestOpen, "wood_chest_open", "Wood Chest (Open)", "loot_prop",
                new Vector2(32f, 28f), SlicedSupplyShopRoot, "wood_chest_open.png"),
            [StructureSpriteKind.AmmoCrateMetal] = SlicedProp(
                StructureSpriteKind.AmmoCrateMetal, "ammo_crate_metal", "Ammo Crate", "loot_prop",
                new Vector2(32f, 28f), SlicedSupplyShopRoot, "ammo_crate_metal.png"),
            [StructureSpriteKind.WeaponCaseLong] = SlicedProp(
                StructureSpriteKind.WeaponCaseLong, "weapon_case_long", "Weapon Case", "loot_prop",
                new Vector2(40f, 20f), SlicedSupplyShopRoot, "weapon_case_long.png"),
            [StructureSpriteKind.ToolBoxOpen] = SlicedProp(
                StructureSpriteKind.ToolBoxOpen, "tool_box_open", "Tool Box (Open)", "loot_prop",
                new Vector2(32f, 24f), SlicedSupplyShopRoot, "tool_box_open.png"),
            [StructureSpriteKind.BackpackBrown] = SlicedProp(
                StructureSpriteKind.BackpackBrown, "backpack_brown", "Backpack", "loot_prop",
                new Vector2(28f, 32f), SlicedSupplyShopRoot, "backpack_brown.png"),
            [StructureSpriteKind.WrappedParcel] = SlicedProp(
                StructureSpriteKind.WrappedParcel, "wrapped_parcel", "Wrapped Parcel", "loot_prop",
                new Vector2(28f, 28f), SlicedSupplyShopRoot, "wrapped_parcel.png"),

            // Sliced wanted / bounty / law props
            [StructureSpriteKind.WantedMugShotFrame] = SlicedProp(
                StructureSpriteKind.WantedMugShotFrame, "wanted_mug_shot_frame", "Wanted Mug Shot", "law_prop",
                new Vector2(28f, 36f), SlicedWantedBountyRoot, "wanted_mug_shot_frame.png"),
            [StructureSpriteKind.WantedBulletinBoard] = SlicedProp(
                StructureSpriteKind.WantedBulletinBoard, "wanted_bulletin_board", "Wanted Bulletin Board", "law_prop",
                new Vector2(40f, 36f), SlicedWantedBountyRoot, "wanted_bulletin_board.png"),
            [StructureSpriteKind.JailBarredWindow] = SlicedProp(
                StructureSpriteKind.JailBarredWindow, "jail_barred_window", "Jail Barred Window", "law_prop",
                new Vector2(28f, 36f), SlicedWantedBountyRoot, "jail_barred_window.png"),
            [StructureSpriteKind.BarricadeGate] = SlicedProp(
                StructureSpriteKind.BarricadeGate, "barricade_gate", "Barricade Gate", "law_prop",
                new Vector2(40f, 28f), SlicedWantedBountyRoot, "barricade_gate.png"),
            [StructureSpriteKind.EvidenceTable] = SlicedProp(
                StructureSpriteKind.EvidenceTable, "evidence_table", "Evidence Table", "law_prop",
                new Vector2(40f, 28f), SlicedWantedBountyRoot, "evidence_table.png"),
            [StructureSpriteKind.EvidenceLockers] = SlicedProp(
                StructureSpriteKind.EvidenceLockers, "evidence_lockers", "Evidence Lockers", "law_prop",
                new Vector2(36f, 40f), SlicedWantedBountyRoot, "evidence_lockers.png"),
            [StructureSpriteKind.GuardBoothKiosk] = SlicedProp(
                StructureSpriteKind.GuardBoothKiosk, "guard_booth_kiosk", "Guard Booth", "law_prop",
                new Vector2(40f, 44f), SlicedWantedBountyRoot, "guard_booth_kiosk.png"),
            [StructureSpriteKind.SirenBeaconBlue] = SlicedProp(
                StructureSpriteKind.SirenBeaconBlue, "siren_beacon_blue", "Siren Beacon", "law_prop",
                new Vector2(20f, 40f), SlicedWantedBountyRoot, "siren_beacon_blue.png"),
            [StructureSpriteKind.HandcuffsSilver] = SlicedProp(
                StructureSpriteKind.HandcuffsSilver, "handcuffs_silver", "Handcuffs", "law_prop",
                new Vector2(28f, 24f), SlicedWantedBountyRoot, "handcuffs_silver.png"),

            // Structure damage states (Step 12)
            [StructureSpriteKind.GeneratorPristine] = SlicedProp(
                StructureSpriteKind.GeneratorPristine, "generator_pristine", "Generator (Pristine)", "structure_state",
                new Vector2(48f, 36f), SlicedStructureStateRoot, "generator_pristine.png"),
            [StructureSpriteKind.GeneratorDamaged] = SlicedProp(
                StructureSpriteKind.GeneratorDamaged, "generator_damaged", "Generator (Damaged)", "structure_state",
                new Vector2(48f, 36f), SlicedStructureStateRoot, "generator_damaged.png"),
            [StructureSpriteKind.GeneratorWrecked] = SlicedProp(
                StructureSpriteKind.GeneratorWrecked, "generator_wrecked", "Generator (Wrecked)", "structure_state",
                new Vector2(48f, 36f), SlicedStructureStateRoot, "generator_wrecked.png"),
            [StructureSpriteKind.GeneratorSabotaged] = SlicedProp(
                StructureSpriteKind.GeneratorSabotaged, "generator_sabotaged", "Generator (Sabotaged)", "structure_state",
                new Vector2(48f, 36f), SlicedStructureStateRoot, "generator_sabotaged_wires.png"),
            [StructureSpriteKind.GreenhousePristineAlt] = SlicedProp(
                StructureSpriteKind.GreenhousePristineAlt, "greenhouse_pristine_alt", "Greenhouse (Alt)", "structure_state",
                new Vector2(64f, 48f), SlicedStructureStateRoot, "greenhouse_pristine.png"),
            [StructureSpriteKind.GreenhouseShattered] = SlicedProp(
                StructureSpriteKind.GreenhouseShattered, "greenhouse_shattered", "Greenhouse (Shattered)", "structure_state",
                new Vector2(64f, 48f), SlicedStructureStateRoot, "greenhouse_shattered.png"),
            [StructureSpriteKind.ElectricalBoxClosed] = SlicedProp(
                StructureSpriteKind.ElectricalBoxClosed, "electrical_box_closed", "Electrical Box", "structure_state",
                new Vector2(24f, 36f), SlicedStructureStateRoot, "electrical_box_closed.png"),
            [StructureSpriteKind.ElectricalBoxSparking] = SlicedProp(
                StructureSpriteKind.ElectricalBoxSparking, "electrical_box_sparking", "Electrical Box (Sparking)", "structure_state",
                new Vector2(24f, 36f), SlicedStructureStateRoot, "electrical_box_sparking.png"),
            [StructureSpriteKind.NoticeBoardCluttered] = SlicedProp(
                StructureSpriteKind.NoticeBoardCluttered, "notice_board_cluttered", "Notice Board (Cluttered)", "structure_state",
                new Vector2(36f, 32f), SlicedStructureStateRoot, "notice_board_cluttered.png"),
            [StructureSpriteKind.NoticeBoardBlank] = SlicedProp(
                StructureSpriteKind.NoticeBoardBlank, "notice_board_blank", "Notice Board (Blank)", "structure_state",
                new Vector2(36f, 32f), SlicedStructureStateRoot, "notice_board_blank.png"),
            [StructureSpriteKind.FirePit] = SlicedProp(
                StructureSpriteKind.FirePit, "fire_pit", "Fire Pit", "structure_state",
                new Vector2(28f, 24f), SlicedStructureStateRoot, "fire_pit.png"),

            // Interior furniture
            [StructureSpriteKind.BedClean] = SlicedProp(
                StructureSpriteKind.BedClean, "bed_clean", "Bed", "interior_prop",
                new Vector2(40f, 28f), SlicedInteriorFurnitureRoot, "bed_clean.png"),
            [StructureSpriteKind.CotBed] = SlicedProp(
                StructureSpriteKind.CotBed, "cot_bed", "Cot Bed", "interior_prop",
                new Vector2(40f, 24f), SlicedInteriorFurnitureRoot, "cot_bed.png"),
            [StructureSpriteKind.ScribeDeskInterior] = SlicedProp(
                StructureSpriteKind.ScribeDeskInterior, "scribe_desk_interior", "Scribe Desk", "interior_prop",
                new Vector2(40f, 32f), SlicedInteriorFurnitureRoot, "scribe_desk.png"),
            [StructureSpriteKind.SofaGrey] = SlicedProp(
                StructureSpriteKind.SofaGrey, "sofa_grey", "Sofa (Grey)", "interior_prop",
                new Vector2(40f, 24f), SlicedInteriorFurnitureRoot, "sofa_grey.png"),
            [StructureSpriteKind.SofaTan] = SlicedProp(
                StructureSpriteKind.SofaTan, "sofa_tan", "Sofa (Tan)", "interior_prop",
                new Vector2(40f, 24f), SlicedInteriorFurnitureRoot, "sofa_tan.png"),
            [StructureSpriteKind.BenchWood] = SlicedProp(
                StructureSpriteKind.BenchWood, "bench_wood", "Bench (Wood)", "interior_prop",
                new Vector2(36f, 20f), SlicedInteriorFurnitureRoot, "bench_wood.png"),
            [StructureSpriteKind.ChairWood] = SlicedProp(
                StructureSpriteKind.ChairWood, "chair_wood", "Chair (Wood)", "interior_prop",
                new Vector2(20f, 28f), SlicedInteriorFurnitureRoot, "chair_wood.png"),
            [StructureSpriteKind.StoolRound] = SlicedProp(
                StructureSpriteKind.StoolRound, "stool_round", "Stool", "interior_prop",
                new Vector2(20f, 24f), SlicedInteriorFurnitureRoot, "stool_round.png"),
            [StructureSpriteKind.VendingMachine] = SlicedProp(
                StructureSpriteKind.VendingMachine, "vending_machine", "Vending Machine", "interior_prop",
                new Vector2(28f, 40f), SlicedInteriorFurnitureRoot, "vending_machine.png"),
            [StructureSpriteKind.CorkboardNotes] = SlicedProp(
                StructureSpriteKind.CorkboardNotes, "corkboard_notes", "Corkboard", "interior_prop",
                new Vector2(40f, 28f), SlicedInteriorFurnitureRoot, "corkboard_notes.png"),
            [StructureSpriteKind.StockedShelves] = SlicedProp(
                StructureSpriteKind.StockedShelves, "stocked_shelves", "Stocked Shelves", "interior_prop",
                new Vector2(40f, 44f), SlicedInteriorFurnitureRoot, "stocked_shelves.png"),
            [StructureSpriteKind.RegisterCounter] = SlicedProp(
                StructureSpriteKind.RegisterCounter, "register_counter", "Register Counter", "interior_prop",
                new Vector2(40f, 28f), SlicedInteriorFurnitureRoot, "register_counter.png"),

            // Crafting stations (Step 36)
            [StructureSpriteKind.MechanicalWorkbench] = SlicedProp(
                StructureSpriteKind.MechanicalWorkbench, "mechanical_workbench", "Mechanical Workbench", "workshop",
                new Vector2(48f, 36f), SlicedCraftingStationsRoot, "mechanical_workbench.png"),
            [StructureSpriteKind.AlchemyWorkbench] = SlicedProp(
                StructureSpriteKind.AlchemyWorkbench, "alchemy_workbench", "Alchemy Workbench", "workshop",
                new Vector2(48f, 36f), SlicedCraftingStationsRoot, "alchemy_workbench.png"),
            [StructureSpriteKind.WeaponsDisplayPegboard] = SlicedProp(
                StructureSpriteKind.WeaponsDisplayPegboard, "weapons_display_pegboard", "Weapons Display", "workshop",
                new Vector2(48f, 36f), SlicedCraftingStationsRoot, "weapons_display_pegboard.png"),
            [StructureSpriteKind.ElectronicsWorkshop] = SlicedProp(
                StructureSpriteKind.ElectronicsWorkshop, "electronics_workshop", "Electronics Workshop", "workshop",
                new Vector2(48f, 36f), SlicedCraftingStationsRoot, "electronics_workshop.png"),
            [StructureSpriteKind.ComputerWorkbench] = SlicedProp(
                StructureSpriteKind.ComputerWorkbench, "computer_workbench", "Computer Workbench", "workshop",
                new Vector2(48f, 36f), SlicedCraftingStationsRoot, "computer_workbench.png"),
            [StructureSpriteKind.HydroponicsPlanterGrown] = SlicedProp(
                StructureSpriteKind.HydroponicsPlanterGrown, "hydroponics_planter_grown", "Hydroponics Planter", "workshop",
                new Vector2(48f, 36f), SlicedCraftingStationsRoot, "hydroponics_planter_grown.png"),

            // Containers / loot variety
            [StructureSpriteKind.WoodChestClosed] = SlicedProp(
                StructureSpriteKind.WoodChestClosed, "wood_chest_closed", "Wood Chest (Closed)", "loot_prop",
                new Vector2(28f, 24f), SlicedContainersLootRoot, "wood_chest.png"),
            [StructureSpriteKind.SciFiChest] = SlicedProp(
                StructureSpriteKind.SciFiChest, "scifi_chest", "Sci-Fi Chest", "loot_prop",
                new Vector2(28f, 24f), SlicedContainersLootRoot, "scifi_chest.png"),
            [StructureSpriteKind.OrnateChest] = SlicedProp(
                StructureSpriteKind.OrnateChest, "ornate_chest", "Ornate Chest", "loot_prop",
                new Vector2(28f, 28f), SlicedContainersLootRoot, "ornate_chest_jeweled.png"),
            [StructureSpriteKind.MedkitWhiteBox] = SlicedProp(
                StructureSpriteKind.MedkitWhiteBox, "medkit_white_box", "Medkit Box", "loot_prop",
                new Vector2(24f, 24f), SlicedContainersLootRoot, "medkit_white_box.png"),
            [StructureSpriteKind.DuffelBag] = SlicedProp(
                StructureSpriteKind.DuffelBag, "duffel_bag", "Duffel Bag", "loot_prop",
                new Vector2(36f, 24f), SlicedContainersLootRoot, "duffel_bag_olive.png"),
            [StructureSpriteKind.MilitaryTrunk] = SlicedProp(
                StructureSpriteKind.MilitaryTrunk, "military_trunk", "Military Trunk", "loot_prop",
                new Vector2(32f, 24f), SlicedContainersLootRoot, "military_trunk_locked.png"),
            [StructureSpriteKind.ShippingContainer] = SlicedProp(
                StructureSpriteKind.ShippingContainer, "shipping_container", "Shipping Container", "loot_prop",
                new Vector2(48f, 32f), SlicedContainersLootRoot, "shipping_container_blue.png"),
            [StructureSpriteKind.BarrelWood] = SlicedProp(
                StructureSpriteKind.BarrelWood, "barrel_wood", "Wood Barrel", "loot_prop",
                new Vector2(20f, 28f), SlicedContainersLootRoot, "barrel_wood.png"),
            [StructureSpriteKind.SafePadlock] = SlicedProp(
                StructureSpriteKind.SafePadlock, "safe_padlock", "Safe (Padlock)", "loot_prop",
                new Vector2(28f, 28f), SlicedContainersLootRoot, "safe_padlock.png"),

            // Modular walls / doors (Step 17-18)
            [StructureSpriteKind.ClinicDoor] = SlicedProp(
                StructureSpriteKind.ClinicDoor, "clinic_door", "Clinic Door", "door",
                new Vector2(24f, 36f), SlicedWallsDoorsRoot, "clinic_door.png"),
            [StructureSpriteKind.ShopDoor] = SlicedProp(
                StructureSpriteKind.ShopDoor, "shop_door", "Shop Door", "door",
                new Vector2(24f, 36f), SlicedWallsDoorsRoot, "shop_door.png"),
            [StructureSpriteKind.JailDoor] = SlicedProp(
                StructureSpriteKind.JailDoor, "jail_door", "Jail Door", "door",
                new Vector2(24f, 36f), SlicedWallsDoorsRoot, "jail_door.png"),
            [StructureSpriteKind.AirlockDoorOpen] = SlicedProp(
                StructureSpriteKind.AirlockDoorOpen, "airlock_door_open", "Airlock Door (Open)", "door",
                new Vector2(28f, 36f), SlicedWallsDoorsRoot, "airlock_door_open.png"),
            [StructureSpriteKind.AirlockDoorClosed] = SlicedProp(
                StructureSpriteKind.AirlockDoorClosed, "airlock_door_closed", "Airlock Door (Closed)", "door",
                new Vector2(28f, 36f), SlicedWallsDoorsRoot, "airlock_door_closed.png"),
            [StructureSpriteKind.GateClosed] = SlicedProp(
                StructureSpriteKind.GateClosed, "gate_closed", "Gate (Closed)", "door",
                new Vector2(36f, 36f), SlicedWallsDoorsRoot, "gate_closed.png"),
            [StructureSpriteKind.GateOpen] = SlicedProp(
                StructureSpriteKind.GateOpen, "gate_open", "Gate (Open)", "door",
                new Vector2(36f, 36f), SlicedWallsDoorsRoot, "gate_open.png"),
            [StructureSpriteKind.WindowLit] = SlicedProp(
                StructureSpriteKind.WindowLit, "window_lit", "Window (Lit)", "wall_prop",
                new Vector2(24f, 32f), SlicedWallsDoorsRoot, "window_lit.png"),
            [StructureSpriteKind.WindowBroken] = SlicedProp(
                StructureSpriteKind.WindowBroken, "window_broken", "Window (Broken)", "wall_prop",
                new Vector2(24f, 32f), SlicedWallsDoorsRoot, "window_broken.png"),
            [StructureSpriteKind.FenceStraight] = SlicedProp(
                StructureSpriteKind.FenceStraight, "fence_straight", "Fence", "wall_prop",
                new Vector2(36f, 24f), SlicedWallsDoorsRoot, "fence_straight.png"),
            [StructureSpriteKind.Archway] = SlicedProp(
                StructureSpriteKind.Archway, "archway", "Archway", "wall_prop",
                new Vector2(40f, 40f), SlicedWallsDoorsRoot, "archway.png")
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


    private static StructureSpriteDefinition BoardingSchoolBuilding(
        StructureSpriteKind kind,
        string id,
        string displayName,
        Rect2 atlasRegion)
    {
        return new StructureSpriteDefinition(
            kind,
            id,
            displayName,
            "boarding_school_building",
            atlasRegion.Size,
            BoardingSchoolBuildingsAtlasPath,
            atlasRegion,
            HasAtlasRegion: true);
    }


    private static StructureSpriteDefinition BoardingSchoolProp(
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
            "boarding_school_prop",
            size,
            BoardingSchoolPropsAtlasPath,
            atlasRegion,
            HasAtlasRegion: true);
    }


    private static StructureSpriteDefinition BoardingSchoolTree(
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
            "boarding_school_tree",
            size,
            BoardingSchoolTreesAtlasPath,
            atlasRegion,
            HasAtlasRegion: true);
    }


    private static StructureSpriteDefinition BoardingSchoolGrassDetail(
        StructureSpriteKind kind,
        string id,
        string displayName,
        Rect2 atlasRegion)
    {
        return new StructureSpriteDefinition(
            kind,
            id,
            displayName,
            "boarding_school_grass_detail",
            atlasRegion.Size,
            BoardingSchoolGrassAtlasPath,
            atlasRegion,
            HasAtlasRegion: true);
    }

    private static StructureSpriteDefinition GeminiStaticProp(
        StructureSpriteKind kind,
        string id,
        string displayName,
        string category,
        Vector2 size,
        string fileName)
    {
        return GeminiProp(kind, id, displayName, category, size, GeminiStaticPropsRoot + fileName);
    }

    private static StructureSpriteDefinition SlicedProp(
        StructureSpriteKind kind,
        string id,
        string displayName,
        string category,
        Vector2 size,
        string root,
        string fileName)
    {
        // Sliced PNGs are whole images at variable sizes (each atlas cell
        // produces its own file), so we use HasAtlasRegion=false rather than
        // the GeminiProp helper's hardcoded 128x128 region.
        return new StructureSpriteDefinition(
            kind,
            id,
            displayName,
            category,
            size,
            root + fileName,
            new Rect2(0f, 0f, 0f, 0f),
            HasAtlasRegion: false);
    }

    private static StructureSpriteDefinition GeminiNaturalProp(
        StructureSpriteKind kind,
        string id,
        string displayName,
        string category,
        Vector2 size,
        string fileName)
    {
        return GeminiProp(kind, id, displayName, category, size, GeminiNaturalPropsRoot + fileName);
    }

    private static StructureSpriteDefinition GeminiProp(
        StructureSpriteKind kind,
        string id,
        string displayName,
        string category,
        Vector2 size,
        string atlasPath)
    {
        return new StructureSpriteDefinition(
            kind,
            id,
            displayName,
            category,
            size,
            atlasPath,
            new Rect2(0f, 0f, 128f, 128f),
            HasAtlasRegion: true);
    }
}
