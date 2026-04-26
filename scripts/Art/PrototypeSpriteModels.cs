using System.Collections.Generic;
using Godot;
using Karma.Data;

namespace Karma.Art;

public enum PrototypeSpriteKind
{
    Player,
    Mara,
    Peer,
    WhoopieCushion,
    DeflatedBalloon,
    RepairKit,
    PracticeStick,
    WorkVest,
    RationPack,
    DataChip,
    FilterCore,
    ContrabandPackage,
    ApologyFlower,
    PortableTerminal,
    Scrip,
    StunBaton,
    ElectroPistol,
    Smg11,
    ShotgunMk1,
    Rifle27,
    SniperX9,
    PlasmaCutter,
    FlameThrower,
    GrenadeLauncher,
    Railgun,
    ImpactMine,
    EmpGrenade,
    MultiTool,
    WeldingTorch,
    MediPatch,
    LockpickSet,
    Flashlight,
    PortableShield,
    HackingDevice,
    Scanner,
    GrapplingHook,
    ChemInjector,
    PowerCell,
    BoltCutters,
    MagneticGrabber,
    Dallen
}

public enum PrototypeSpriteShape
{
    Rect,
    Circle,
    Line
}

public sealed record PrototypeSpriteLayer(
    PrototypeSpriteShape Shape,
    Color Color,
    Rect2 Rect,
    Vector2 From,
    Vector2 To,
    float Radius,
    float Width);

public sealed record PrototypeSpriteDefinition(
    PrototypeSpriteKind Kind,
    string DisplayName,
    Vector2 Size,
    IReadOnlyList<PrototypeSpriteLayer> Layers,
    string AtlasPath = "",
    Rect2 AtlasRegion = new(),
    bool HasAtlasRegion = false);

public static class PrototypeSpriteCatalog
{
    public const string CharacterAtlasPath = "res://assets/art/character.png";
    public const string EngineerPlayerAtlasPath = "res://assets/art/sprites/scifi_engineer_player_sheet.png";
    public const string ItemAtlasPath = "res://assets/art/sprites/scifi_item_atlas.png";
    public const string UtilityItemAtlasPath = "res://assets/art/sprites/scifi_utility_item_atlas.png";
    public const string WeaponAtlasPath = "res://assets/art/sprites/scifi_weapon_atlas.png";
    public const string ToolAtlasPath = "res://assets/art/sprites/scifi_tool_atlas.png";

    public static PrototypeSpriteKind GetKindForItem(string itemId)
    {
        return itemId switch
        {
            StarterItems.WhoopieCushionId => PrototypeSpriteKind.WhoopieCushion,
            StarterItems.DeflatedBalloonId => PrototypeSpriteKind.DeflatedBalloon,
            StarterItems.RepairKitId => PrototypeSpriteKind.RepairKit,
            StarterItems.PracticeStickId => PrototypeSpriteKind.PracticeStick,
            StarterItems.WorkVestId => PrototypeSpriteKind.WorkVest,
            StarterItems.RationPackId => PrototypeSpriteKind.RationPack,
            StarterItems.DataChipId => PrototypeSpriteKind.DataChip,
            StarterItems.FilterCoreId => PrototypeSpriteKind.FilterCore,
            StarterItems.ContrabandPackageId => PrototypeSpriteKind.ContrabandPackage,
            StarterItems.ApologyFlowerId => PrototypeSpriteKind.ApologyFlower,
            StarterItems.PortableTerminalId => PrototypeSpriteKind.PortableTerminal,
            StarterItems.StunBatonId => PrototypeSpriteKind.StunBaton,
            StarterItems.ElectroPistolId => PrototypeSpriteKind.ElectroPistol,
            StarterItems.Smg11Id => PrototypeSpriteKind.Smg11,
            StarterItems.ShotgunMk1Id => PrototypeSpriteKind.ShotgunMk1,
            StarterItems.Rifle27Id => PrototypeSpriteKind.Rifle27,
            StarterItems.SniperX9Id => PrototypeSpriteKind.SniperX9,
            StarterItems.PlasmaCutterId => PrototypeSpriteKind.PlasmaCutter,
            StarterItems.FlameThrowerId => PrototypeSpriteKind.FlameThrower,
            StarterItems.GrenadeLauncherId => PrototypeSpriteKind.GrenadeLauncher,
            StarterItems.RailgunId => PrototypeSpriteKind.Railgun,
            StarterItems.ImpactMineId => PrototypeSpriteKind.ImpactMine,
            StarterItems.EmpGrenadeId => PrototypeSpriteKind.EmpGrenade,
            StarterItems.MultiToolId => PrototypeSpriteKind.MultiTool,
            StarterItems.WeldingTorchId => PrototypeSpriteKind.WeldingTorch,
            StarterItems.MediPatchId => PrototypeSpriteKind.MediPatch,
            StarterItems.LockpickSetId => PrototypeSpriteKind.LockpickSet,
            StarterItems.FlashlightId => PrototypeSpriteKind.Flashlight,
            StarterItems.PortableShieldId => PrototypeSpriteKind.PortableShield,
            StarterItems.HackingDeviceId => PrototypeSpriteKind.HackingDevice,
            StarterItems.ScannerId => PrototypeSpriteKind.Scanner,
            StarterItems.GrapplingHookId => PrototypeSpriteKind.GrapplingHook,
            StarterItems.ChemInjectorId => PrototypeSpriteKind.ChemInjector,
            StarterItems.PowerCellId => PrototypeSpriteKind.PowerCell,
            StarterItems.BoltCuttersId => PrototypeSpriteKind.BoltCutters,
            StarterItems.MagneticGrabberId => PrototypeSpriteKind.MagneticGrabber,
            _ => PrototypeSpriteKind.WhoopieCushion
        };
    }

    public static PrototypeSpriteKind GetKindForNpc(string npcId)
    {
        if (npcId == StarterNpcs.Mara.Id)
        {
            return PrototypeSpriteKind.Mara;
        }

        return npcId == StarterNpcs.Dallen.Id
            ? PrototypeSpriteKind.Dallen
            : PrototypeSpriteKind.Peer;
    }

    public static PrototypeSpriteDefinition Get(PrototypeSpriteKind kind)
    {
        return kind switch
        {
            PrototypeSpriteKind.Player => Humanoid(
                kind,
                "Player",
                new Color(0.22f, 0.76f, 0.94f),
                new Color(0.08f, 0.19f, 0.24f),
                new Color(0.96f, 0.94f, 0.72f),
                new Rect2(285f, 20f, 140f, 190f),
                EngineerPlayerAtlasPath),
            PrototypeSpriteKind.Mara => Humanoid(
                kind,
                "Mara Venn",
                new Color(0.94f, 0.69f, 0.28f),
                new Color(0.21f, 0.14f, 0.11f),
                new Color(0.58f, 0.93f, 0.76f),
                new Rect2(282f, 82f, 42f, 82f)),
            PrototypeSpriteKind.Peer => Humanoid(
                kind,
                "Stranded Player",
                new Color(0.65f, 0.45f, 0.94f),
                new Color(0.18f, 0.14f, 0.28f),
                new Color(0.94f, 0.82f, 0.51f),
                new Rect2(454f, 82f, 42f, 82f)),
            PrototypeSpriteKind.Dallen => Humanoid(
                kind,
                "Dallen Venn",
                new Color(0.45f, 0.58f, 0.7f),
                new Color(0.11f, 0.16f, 0.2f),
                new Color(0.88f, 0.76f, 0.52f),
                new Rect2(220f, 248f, 42f, 82f)),
            PrototypeSpriteKind.WhoopieCushion => WhoopieCushion(),
            PrototypeSpriteKind.DeflatedBalloon => DeflatedBalloon(),
            PrototypeSpriteKind.RepairKit => RepairKit(),
            PrototypeSpriteKind.PracticeStick => PracticeStick(),
            PrototypeSpriteKind.WorkVest => WorkVest(),
            PrototypeSpriteKind.RationPack => RationPack(),
            PrototypeSpriteKind.DataChip => DataChip(),
            PrototypeSpriteKind.FilterCore => FilterCore(),
            PrototypeSpriteKind.ContrabandPackage => ContrabandPackage(),
            PrototypeSpriteKind.ApologyFlower => ApologyFlower(),
            PrototypeSpriteKind.PortableTerminal => PortableTerminal(),
            PrototypeSpriteKind.Scrip => Scrip(),
            PrototypeSpriteKind.StunBaton => Weapon(PrototypeSpriteKind.StunBaton, "Stun Baton", new Vector2(30f, 18f), new Rect2(34f, 165f, 152f, 136f)),
            PrototypeSpriteKind.ElectroPistol => Weapon(PrototypeSpriteKind.ElectroPistol, "Electro Pistol", new Vector2(30f, 18f), new Rect2(270f, 182f, 160f, 100f)),
            PrototypeSpriteKind.Smg11 => Weapon(PrototypeSpriteKind.Smg11, "SMG-11", new Vector2(30f, 18f), new Rect2(500f, 182f, 158f, 92f)),
            PrototypeSpriteKind.ShotgunMk1 => Weapon(PrototypeSpriteKind.ShotgunMk1, "Shotgun Mk1", new Vector2(32f, 18f), new Rect2(700f, 182f, 188f, 103f)),
            PrototypeSpriteKind.Rifle27 => Weapon(PrototypeSpriteKind.Rifle27, "Rifle-27", new Vector2(34f, 16f), new Rect2(925f, 182f, 205f, 82f)),
            PrototypeSpriteKind.SniperX9 => Weapon(PrototypeSpriteKind.SniperX9, "Sniper X9", new Vector2(36f, 16f), new Rect2(1198f, 178f, 244f, 89f)),
            PrototypeSpriteKind.PlasmaCutter => Weapon(PrototypeSpriteKind.PlasmaCutter, "Plasma Cutter", new Vector2(32f, 22f), new Rect2(26f, 666f, 190f, 105f)),
            PrototypeSpriteKind.FlameThrower => Weapon(PrototypeSpriteKind.FlameThrower, "Flame Thrower", new Vector2(34f, 18f), new Rect2(270f, 686f, 202f, 86f)),
            PrototypeSpriteKind.GrenadeLauncher => Weapon(PrototypeSpriteKind.GrenadeLauncher, "Grenade Launcher", new Vector2(34f, 18f), new Rect2(506f, 670f, 202f, 104f)),
            PrototypeSpriteKind.Railgun => Weapon(PrototypeSpriteKind.Railgun, "Railgun", new Vector2(36f, 18f), new Rect2(754f, 684f, 225f, 83f)),
            PrototypeSpriteKind.ImpactMine => Weapon(PrototypeSpriteKind.ImpactMine, "Impact Mine", new Vector2(24f, 18f), new Rect2(1038f, 680f, 128f, 86f)),
            PrototypeSpriteKind.EmpGrenade => Weapon(PrototypeSpriteKind.EmpGrenade, "EMP Grenade", new Vector2(20f, 24f), new Rect2(1308f, 670f, 105f, 130f)),
            PrototypeSpriteKind.MultiTool => Tool(PrototypeSpriteKind.MultiTool, "Multi Tool", new Vector2(28f, 20f), new Rect2(32f, 151f, 165f, 139f)),
            PrototypeSpriteKind.WeldingTorch => Tool(PrototypeSpriteKind.WeldingTorch, "Welding Torch", new Vector2(28f, 20f), new Rect2(288f, 171f, 165f, 110f)),
            PrototypeSpriteKind.MediPatch => Tool(PrototypeSpriteKind.MediPatch, "Medi Patch", new Vector2(24f, 20f), new Rect2(506f, 170f, 160f, 120f)),
            PrototypeSpriteKind.LockpickSet => Tool(PrototypeSpriteKind.LockpickSet, "Lockpick Set", new Vector2(26f, 22f), new Rect2(916f, 168f, 148f, 126f)),
            PrototypeSpriteKind.Flashlight => Tool(PrototypeSpriteKind.Flashlight, "Flashlight", new Vector2(28f, 18f), new Rect2(1160f, 173f, 152f, 95f)),
            PrototypeSpriteKind.PortableShield => Tool(PrototypeSpriteKind.PortableShield, "Portable Shield", new Vector2(24f, 24f), new Rect2(1358f, 166f, 160f, 136f)),
            PrototypeSpriteKind.HackingDevice => Tool(PrototypeSpriteKind.HackingDevice, "Hacking Device", new Vector2(26f, 20f), new Rect2(33f, 717f, 155f, 110f)),
            PrototypeSpriteKind.Scanner => Tool(PrototypeSpriteKind.Scanner, "Scanner", new Vector2(24f, 20f), new Rect2(270f, 712f, 145f, 102f)),
            PrototypeSpriteKind.GrapplingHook => Tool(PrototypeSpriteKind.GrapplingHook, "Grappling Hook", new Vector2(30f, 18f), new Rect2(492f, 717f, 180f, 100f)),
            PrototypeSpriteKind.ChemInjector => Tool(PrototypeSpriteKind.ChemInjector, "Chem Injector", new Vector2(28f, 18f), new Rect2(722f, 716f, 164f, 92f)),
            PrototypeSpriteKind.PowerCell => Tool(PrototypeSpriteKind.PowerCell, "Power Cell", new Vector2(22f, 20f), new Rect2(978f, 718f, 105f, 95f)),
            PrototypeSpriteKind.BoltCutters => Tool(PrototypeSpriteKind.BoltCutters, "Bolt Cutters", new Vector2(28f, 18f), new Rect2(1165f, 716f, 165f, 90f)),
            PrototypeSpriteKind.MagneticGrabber => Tool(PrototypeSpriteKind.MagneticGrabber, "Magnetic Grabber", new Vector2(30f, 16f), new Rect2(1370f, 720f, 145f, 82f)),
            _ => Humanoid(
                PrototypeSpriteKind.Player,
                "Unknown",
                Colors.White,
                Colors.Black,
                Colors.White,
                new Rect2())
        };
    }

    private static PrototypeSpriteDefinition Humanoid(
        PrototypeSpriteKind kind,
        string displayName,
        Color body,
        Color outline,
        Color accent,
        Rect2 atlasRegion,
        string atlasPath = CharacterAtlasPath)
    {
        return new PrototypeSpriteDefinition(
            kind,
            displayName,
            new Vector2(24f, 44f),
            new[]
            {
                Rect(outline, -8f, -13f, 16f, 24f),
                Rect(body, -7f, -12f, 14f, 22f),
                Circle(outline, 0f, -18f, 7f),
                Circle(accent, 0f, -18f, 5f),
                Rect(outline, -5f, 11f, 4f, 7f),
                Rect(outline, 1f, 11f, 4f, 7f),
                Rect(accent, -5f, 0f, 10f, 3f),
                Rect(outline, -3f, -20f, 2f, 2f),
                Rect(outline, 2f, -20f, 2f, 2f)
            },
            atlasPath,
            atlasRegion,
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition WhoopieCushion()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.WhoopieCushion,
            "Whoopie Cushion",
            new Vector2(24f, 24f),
            new[]
            {
                Circle(new Color(0.33f, 0.04f, 0.08f), 0f, 1f, 9f),
                Circle(new Color(0.94f, 0.13f, 0.22f), 0f, 0f, 7f),
                Rect(new Color(0.78f, 0.08f, 0.16f), 5f, -2f, 8f, 4f),
                Rect(new Color(1f, 0.48f, 0.56f), -3f, -5f, 4f, 2f)
            },
            UtilityItemAtlasPath,
            new Rect2(35f, 250f, 170f, 190f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition DeflatedBalloon()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.DeflatedBalloon,
            "Deflated Balloon",
            new Vector2(26f, 18f),
            new[]
            {
                Rect(new Color(0.28f, 0.06f, 0.22f), -11f, -3f, 19f, 8f),
                Rect(new Color(0.93f, 0.62f, 0.9f), -10f, -4f, 18f, 7f),
                Line(new Color(0.36f, 0.2f, 0.32f), new Vector2(6f, 1f), new Vector2(12f, 4f), 2f),
                Rect(new Color(1f, 0.85f, 0.98f), -8f, -3f, 5f, 2f)
            },
            UtilityItemAtlasPath,
            new Rect2(344f, 258f, 115f, 165f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition RepairKit()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.RepairKit,
            "Repair Kit",
            new Vector2(24f, 20f),
            new[]
            {
                Rect(new Color(0.04f, 0.2f, 0.18f), -10f, -7f, 20f, 14f),
                Rect(new Color(0.1f, 0.72f, 0.6f), -8f, -5f, 16f, 10f),
                Rect(new Color(0.86f, 0.96f, 0.9f), -2f, -6f, 4f, 12f),
                Rect(new Color(0.86f, 0.96f, 0.9f), -6f, -2f, 12f, 4f)
            },
            UtilityItemAtlasPath,
            new Rect2(608f, 250f, 106f, 178f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition PracticeStick()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.PracticeStick,
            "Practice Stick",
            new Vector2(30f, 16f),
            new[]
            {
                Line(new Color(0.22f, 0.12f, 0.05f), new Vector2(-13f, 3f), new Vector2(13f, -3f), 5f),
                Line(new Color(0.58f, 0.35f, 0.15f), new Vector2(-12f, 2f), new Vector2(12f, -4f), 3f),
                Rect(new Color(0.82f, 0.66f, 0.34f), -3f, -1f, 5f, 3f)
            },
            UtilityItemAtlasPath,
            new Rect2(832f, 255f, 160f, 135f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition WorkVest()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.WorkVest,
            "Work Vest",
            new Vector2(26f, 24f),
            new[]
            {
                Rect(new Color(0.27f, 0.12f, 0.03f), -9f, -11f, 18f, 22f),
                Rect(new Color(0.94f, 0.52f, 0.16f), -7f, -10f, 14f, 20f),
                Rect(new Color(0.18f, 0.12f, 0.08f), -1f, -10f, 2f, 20f),
                Rect(new Color(0.98f, 0.9f, 0.34f), -6f, -4f, 4f, 2f),
                Rect(new Color(0.98f, 0.9f, 0.34f), 2f, -4f, 4f, 2f)
            },
            UtilityItemAtlasPath,
            new Rect2(1122f, 252f, 126f, 175f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition Scrip()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.Scrip,
            "Scrip",
            new Vector2(22f, 22f),
            new[]
            {
                Circle(new Color(0.12f, 0.12f, 0.12f), -2f, 1f, 10f),
                Circle(new Color(0.74f, 0.65f, 0.46f), -2f, 0f, 8f),
                Rect(new Color(0.96f, 0.76f, 0.2f), -4f, -5f, 4f, 10f)
            },
            UtilityItemAtlasPath,
            new Rect2(1360f, 250f, 130f, 180f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition RationPack()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.RationPack,
            "Ration Pack",
            new Vector2(20f, 16f),
            new[]
            {
                Rect(new Color(0.18f, 0.16f, 0.1f), -9f, -6f, 18f, 12f),
                Rect(new Color(0.62f, 0.54f, 0.32f), -7f, -4f, 14f, 8f),
                Rect(new Color(0.86f, 0.78f, 0.45f), -4f, -2f, 8f, 2f)
            },
            ItemAtlasPath,
            new Rect2(40f, 246f, 150f, 210f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition DataChip()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.DataChip,
            "Data Chip",
            new Vector2(18f, 14f),
            new[]
            {
                Rect(new Color(0.02f, 0.16f, 0.2f), -8f, -5f, 16f, 10f),
                Rect(new Color(0.08f, 0.62f, 0.82f), -6f, -3f, 12f, 6f),
                Rect(new Color(0.9f, 0.98f, 1f), -2f, -1f, 4f, 2f)
            },
            ItemAtlasPath,
            new Rect2(310f, 262f, 160f, 126f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition FilterCore()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.FilterCore,
            "Filter Core",
            new Vector2(18f, 22f),
            new[]
            {
                Rect(new Color(0.09f, 0.12f, 0.14f), -7f, -10f, 14f, 20f),
                Rect(new Color(0.56f, 0.66f, 0.68f), -5f, -8f, 10f, 16f),
                Rect(new Color(0.18f, 0.78f, 0.72f), -3f, -5f, 6f, 10f)
            },
            ItemAtlasPath,
            new Rect2(560f, 245f, 150f, 130f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition ContrabandPackage()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.ContrabandPackage,
            "Contraband Package",
            new Vector2(22f, 18f),
            new[]
            {
                Rect(new Color(0.08f, 0.06f, 0.05f), -10f, -7f, 20f, 14f),
                Rect(new Color(0.28f, 0.19f, 0.12f), -8f, -5f, 16f, 10f),
                Line(new Color(0.9f, 0.2f, 0.16f), new Vector2(-8f, -5f), new Vector2(8f, 5f), 2f),
                Line(new Color(0.9f, 0.2f, 0.16f), new Vector2(8f, -5f), new Vector2(-8f, 5f), 2f)
            },
            ItemAtlasPath,
            new Rect2(812f, 244f, 144f, 180f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition ApologyFlower()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.ApologyFlower,
            "Apology Flower",
            new Vector2(20f, 24f),
            new[]
            {
                Line(new Color(0.16f, 0.58f, 0.24f), new Vector2(0f, 8f), new Vector2(0f, -5f), 2f),
                Circle(new Color(0.98f, 0.72f, 0.2f), 0f, -8f, 4f),
                Circle(new Color(0.96f, 0.3f, 0.54f), -4f, -8f, 3f),
                Circle(new Color(0.96f, 0.3f, 0.54f), 4f, -8f, 3f),
                Rect(new Color(0.36f, 0.38f, 0.36f), -6f, 7f, 12f, 5f)
            },
            ItemAtlasPath,
            new Rect2(1072f, 234f, 146f, 160f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition PortableTerminal()
    {
        return new PrototypeSpriteDefinition(
            PrototypeSpriteKind.PortableTerminal,
            "Portable Terminal",
            new Vector2(24f, 22f),
            new[]
            {
                Rect(new Color(0.04f, 0.05f, 0.06f), -11f, -9f, 22f, 18f),
                Rect(new Color(0.22f, 0.25f, 0.3f), -9f, -7f, 18f, 14f),
                Rect(new Color(0.08f, 0.72f, 0.94f), -6f, -5f, 12f, 6f),
                Rect(new Color(0.95f, 0.68f, 0.18f), -6f, 3f, 4f, 2f),
                Rect(new Color(0.95f, 0.68f, 0.18f), 2f, 3f, 4f, 2f)
            },
            ItemAtlasPath,
            new Rect2(1336f, 246f, 155f, 115f),
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition Weapon(
        PrototypeSpriteKind kind,
        string displayName,
        Vector2 size,
        Rect2 atlasRegion)
    {
        return new PrototypeSpriteDefinition(
            kind,
            displayName,
            size,
            new[]
            {
                Line(new Color(0.08f, 0.09f, 0.1f), new Vector2(-12f, 2f), new Vector2(12f, -2f), 5f),
                Line(new Color(0.48f, 0.52f, 0.55f), new Vector2(-10f, 1f), new Vector2(10f, -3f), 3f),
                Rect(new Color(0.08f, 0.48f, 0.82f), -2f, -3f, 5f, 2f)
            },
            WeaponAtlasPath,
            atlasRegion,
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteDefinition Tool(
        PrototypeSpriteKind kind,
        string displayName,
        Vector2 size,
        Rect2 atlasRegion)
    {
        return new PrototypeSpriteDefinition(
            kind,
            displayName,
            size,
            new[]
            {
                Rect(new Color(0.08f, 0.1f, 0.12f), -10f, -7f, 20f, 14f),
                Rect(new Color(0.48f, 0.52f, 0.54f), -8f, -5f, 16f, 10f),
                Rect(new Color(0.1f, 0.62f, 0.86f), -4f, -3f, 8f, 3f)
            },
            ToolAtlasPath,
            atlasRegion,
            HasAtlasRegion: true);
    }

    private static PrototypeSpriteLayer Rect(Color color, float x, float y, float width, float height)
    {
        return new PrototypeSpriteLayer(
            PrototypeSpriteShape.Rect,
            color,
            new Rect2(x, y, width, height),
            Vector2.Zero,
            Vector2.Zero,
            0f,
            1f);
    }

    private static PrototypeSpriteLayer Circle(Color color, float x, float y, float radius)
    {
        return new PrototypeSpriteLayer(
            PrototypeSpriteShape.Circle,
            color,
            new Rect2(),
            new Vector2(x, y),
            Vector2.Zero,
            radius,
            1f);
    }

    private static PrototypeSpriteLayer Line(Color color, Vector2 from, Vector2 to, float width)
    {
        return new PrototypeSpriteLayer(
            PrototypeSpriteShape.Line,
            color,
            new Rect2(),
            from,
            to,
            0f,
            width);
    }
}
