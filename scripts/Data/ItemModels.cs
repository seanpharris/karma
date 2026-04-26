using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public enum ItemCategory
{
    Cosmetic,
    Armor,
    Weapon,
    Tool,
    InteractibleObject,
    Oddity
}

public enum EquipmentSlot
{
    None,
    MainHand,
    Body,
    Trinket
}

public sealed record GameItem(
    string Id,
    string Name,
    ItemCategory Category,
    IReadOnlyCollection<string> Tags,
    string Description,
    EquipmentSlot Slot = EquipmentSlot.None,
    int Power = 0,
    int Defense = 0,
    int? KarmaRequirement = null,
    KarmaDirection RequiredPath = KarmaDirection.Neutral);

public static class StarterItems
{
    public const string WhoopieCushionId = "whoopie_cushion";
    public const string DeflatedBalloonId = "deflated_balloon";
    public const string RepairKitId = "repair_kit";
    public const string PracticeStickId = "practice_stick";
    public const string WorkVestId = "work_vest";
    public const string RationPackId = "ration_pack";
    public const string DataChipId = "data_chip";
    public const string FilterCoreId = "filter_core";
    public const string ContrabandPackageId = "contraband_package";
    public const string ApologyFlowerId = "apology_flower";
    public const string PortableTerminalId = "portable_terminal";
    public const string StunBatonId = "stun_baton";
    public const string ElectroPistolId = "electro_pistol";
    public const string Smg11Id = "smg_11";
    public const string ShotgunMk1Id = "shotgun_mk1";
    public const string Rifle27Id = "rifle_27";
    public const string SniperX9Id = "sniper_x9";
    public const string PlasmaCutterId = "plasma_cutter";
    public const string FlameThrowerId = "flame_thrower";
    public const string GrenadeLauncherId = "grenade_launcher";
    public const string RailgunId = "railgun";
    public const string ImpactMineId = "impact_mine";
    public const string EmpGrenadeId = "emp_grenade";
    public const string MultiToolId = "multi_tool";
    public const string WeldingTorchId = "welding_torch";
    public const string MediPatchId = "medi_patch";
    public const string LockpickSetId = "lockpick_set";
    public const string FlashlightId = "flashlight";
    public const string PortableShieldId = "portable_shield";
    public const string HackingDeviceId = "hacking_device";
    public const string ScannerId = "scanner";
    public const string GrapplingHookId = "grappling_hook";
    public const string ChemInjectorId = "chem_injector";
    public const string PowerCellId = "power_cell";
    public const string BoltCuttersId = "bolt_cutters";
    public const string MagneticGrabberId = "magnetic_grabber";

    public static readonly GameItem WhoopieCushion = new(
        WhoopieCushionId,
        "Whoopie Cushion",
        ItemCategory.Oddity,
        new[] { "funny", "humiliating", "deceptive" },
        "A small comedy weapon. Context decides whether this is charming or unforgivable.");

    public static readonly GameItem DeflatedBalloon = new(
        DeflatedBalloonId,
        "Deflated Balloon",
        ItemCategory.Oddity,
        new[] { "funny", "sad", "gift" },
        "A tired little balloon with surprising emotional range.");

    public static readonly GameItem RepairKit = new(
        RepairKitId,
        "Repair Kit",
        ItemCategory.Tool,
        new[] { "helpful", "protective" },
        "Fixes simple machines, broken fences, and some social mistakes.");

    public static readonly GameItem PracticeStick = new(
        PracticeStickId,
        "Practice Stick",
        ItemCategory.Weapon,
        new[] { "violent", "training" },
        "Technically a weapon. Emotionally, a stick with ambition.",
        EquipmentSlot.MainHand,
        Power: 10);

    public static readonly GameItem WorkVest = new(
        WorkVestId,
        "Work Vest",
        ItemCategory.Armor,
        new[] { "protective", "workwear" },
        "A padded vest that turns a bad hit into a less bad hit.",
        EquipmentSlot.Body,
        Defense: 10);

    public static readonly GameItem RationPack = new(
        RationPackId,
        "Ration Pack",
        ItemCategory.Tool,
        new[] { "helpful", "consumable" },
        "A compact meal brick that tastes like somebody described soup to a printer.");

    public static readonly GameItem DataChip = new(
        DataChipId,
        "Data Chip",
        ItemCategory.InteractibleObject,
        new[] { "quest", "tech" },
        "A tiny chunk of memory with oversized consequences.");

    public static readonly GameItem FilterCore = new(
        FilterCoreId,
        "Filter Core",
        ItemCategory.Tool,
        new[] { "quest", "repair", "protective" },
        "The part Mara actually wanted before everyone started improvising.");

    public static readonly GameItem ContrabandPackage = new(
        ContrabandPackageId,
        "Contraband Package",
        ItemCategory.InteractibleObject,
        new[] { "deceptive", "forbidden", "shady" },
        "Wrapped in enough tape to make innocence unlikely.");

    public static readonly GameItem ApologyFlower = new(
        ApologyFlowerId,
        "Apology Flower",
        ItemCategory.Oddity,
        new[] { "gift", "helpful", "funny" },
        "A resilient little flower in a cracked oxygen valve cap.");

    public static readonly GameItem PortableTerminal = new(
        PortableTerminalId,
        "Portable Terminal",
        ItemCategory.InteractibleObject,
        new[] { "tech", "placeable" },
        "A chunky console for messages, rumors, and poor decisions with timestamps.");

    public static readonly GameItem StunBaton = Weapon(
        StunBatonId,
        "Stun Baton",
        new[] { "melee", "non_lethal", "electric" },
        "Delivers a shocking charge that stuns.",
        Power: 12);

    public static readonly GameItem ElectroPistol = Weapon(
        ElectroPistolId,
        "Electro Pistol",
        new[] { "pistol", "energy" },
        "A short range energy pistol with low recoil.",
        Power: 16);

    public static readonly GameItem Smg11 = Weapon(
        Smg11Id,
        "SMG-11",
        new[] { "smg", "ballistic" },
        "Fully automatic, loud, and rude about it.",
        Power: 18);

    public static readonly GameItem ShotgunMk1 = Weapon(
        ShotgunMk1Id,
        "Shotgun Mk1",
        new[] { "shotgun", "ballistic" },
        "Devastating at close range.",
        Power: 22);

    public static readonly GameItem Rifle27 = Weapon(
        Rifle27Id,
        "Rifle-27",
        new[] { "rifle", "ballistic" },
        "Balanced assault rifle. Reliable all-rounder.",
        Power: 24);

    public static readonly GameItem SniperX9 = Weapon(
        SniperX9Id,
        "Sniper X9",
        new[] { "sniper", "ballistic" },
        "High precision. High damage. Low chill.",
        Power: 30);

    public static readonly GameItem PlasmaCutter = Weapon(
        PlasmaCutterId,
        "Plasma Cutter",
        new[] { "tool", "energy", "machinery" },
        "Industrial cutter. Strong against machinery.",
        Power: 20);

    public static readonly GameItem FlameThrower = Weapon(
        FlameThrowerId,
        "Flame Thrower",
        new[] { "special", "fire" },
        "Projects a stream of ignited fuel.",
        Power: 26);

    public static readonly GameItem GrenadeLauncher = Weapon(
        GrenadeLauncherId,
        "Grenade Launcher",
        new[] { "heavy", "explosive" },
        "Fires explosive rounds in an arc.",
        Power: 32);

    public static readonly GameItem Railgun = Weapon(
        RailgunId,
        "Railgun",
        new[] { "heavy", "energy" },
        "High penetration. Charges before firing.",
        Power: 34);

    public static readonly GameItem ImpactMine = Weapon(
        ImpactMineId,
        "Impact Mine",
        new[] { "thrown", "explosive", "trap" },
        "A proximity triggered explosive mine.",
        Power: 28);

    public static readonly GameItem EmpGrenade = Weapon(
        EmpGrenadeId,
        "EMP Grenade",
        new[] { "thrown", "energy", "electric" },
        "Disables electronics in an area.",
        Power: 18);

    public static readonly GameItem MultiTool = Tool(
        MultiToolId,
        "Multi Tool",
        new[] { "utility", "interaction", "tech" },
        "A versatile device for various interactions.");

    public static readonly GameItem WeldingTorch = Tool(
        WeldingTorchId,
        "Welding Torch",
        new[] { "repair", "machinery", "fire" },
        "Repairs machinery and structures.");

    public static readonly GameItem MediPatch = Tool(
        MediPatchId,
        "Medi Patch",
        new[] { "medical", "helpful", "consumable" },
        "Heals and stabilizes wounds.");

    public static readonly GameItem LockpickSet = Tool(
        LockpickSetId,
        "Lockpick Set",
        new[] { "tech", "deceptive", "locks" },
        "Bypasses electronic and mechanical locks.");

    public static readonly GameItem Flashlight = Tool(
        FlashlightId,
        "Flashlight",
        new[] { "utility", "light", "scouting" },
        "Illuminates dark areas and reveals details.");

    public static readonly GameItem PortableShield = new(
        PortableShieldId,
        "Portable Shield",
        ItemCategory.Armor,
        new[] { "defense", "energy", "protective" },
        "Deploys a personal energy barrier for protection.",
        EquipmentSlot.Body,
        Defense: 16);

    public static readonly GameItem HackingDevice = Tool(
        HackingDeviceId,
        "Hacking Device",
        new[] { "tech", "hacking", "data" },
        "Grants access to secure systems and data.");

    public static readonly GameItem Scanner = Tool(
        ScannerId,
        "Scanner",
        new[] { "utility", "scouting", "analysis" },
        "Scans objects, enemies, and environments.");

    public static readonly GameItem GrapplingHook = Tool(
        GrapplingHookId,
        "Grappling Hook",
        new[] { "mobility", "utility" },
        "Reaches distant areas and pulls objects.");

    public static readonly GameItem ChemInjector = Tool(
        ChemInjectorId,
        "Chem Injector",
        new[] { "utility", "medical", "consumable" },
        "Injects chemicals or buffs into targets.");

    public static readonly GameItem PowerCell = Tool(
        PowerCellId,
        "Power Cell",
        new[] { "resource", "energy" },
        "Stores and transfers electrical power.");

    public static readonly GameItem BoltCutters = Tool(
        BoltCuttersId,
        "Bolt Cutters",
        new[] { "utility", "locks", "force" },
        "Cuts chains, fences, and reinforced locks.");

    public static readonly GameItem MagneticGrabber = Tool(
        MagneticGrabberId,
        "Magnetic Grabber",
        new[] { "utility", "retrieval", "metal" },
        "Retrieves metallic items from a distance.");

    public static IReadOnlyList<GameItem> All { get; } = new[]
    {
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
        MagneticGrabber
    };

    public static GameItem GetById(string id)
    {
        return TryGetById(id, out var item) ? item : WhoopieCushion;
    }

    public static bool TryGetById(string id, out GameItem item)
    {
        item = id switch
        {
            WhoopieCushionId => WhoopieCushion,
            DeflatedBalloonId => DeflatedBalloon,
            RepairKitId => RepairKit,
            PracticeStickId => PracticeStick,
            WorkVestId => WorkVest,
            RationPackId => RationPack,
            DataChipId => DataChip,
            FilterCoreId => FilterCore,
            ContrabandPackageId => ContrabandPackage,
            ApologyFlowerId => ApologyFlower,
            PortableTerminalId => PortableTerminal,
            StunBatonId => StunBaton,
            ElectroPistolId => ElectroPistol,
            Smg11Id => Smg11,
            ShotgunMk1Id => ShotgunMk1,
            Rifle27Id => Rifle27,
            SniperX9Id => SniperX9,
            PlasmaCutterId => PlasmaCutter,
            FlameThrowerId => FlameThrower,
            GrenadeLauncherId => GrenadeLauncher,
            RailgunId => Railgun,
            ImpactMineId => ImpactMine,
            EmpGrenadeId => EmpGrenade,
            MultiToolId => MultiTool,
            WeldingTorchId => WeldingTorch,
            MediPatchId => MediPatch,
            LockpickSetId => LockpickSet,
            FlashlightId => Flashlight,
            PortableShieldId => PortableShield,
            HackingDeviceId => HackingDevice,
            ScannerId => Scanner,
            GrapplingHookId => GrapplingHook,
            ChemInjectorId => ChemInjector,
            PowerCellId => PowerCell,
            BoltCuttersId => BoltCutters,
            MagneticGrabberId => MagneticGrabber,
            _ => null
        };

        return item is not null;
    }

    private static GameItem Weapon(
        string id,
        string name,
        IReadOnlyCollection<string> tags,
        string description,
        int Power)
    {
        return new GameItem(
            id,
            name,
            ItemCategory.Weapon,
            tags,
            description,
            EquipmentSlot.MainHand,
            Power);
    }

    private static GameItem Tool(
        string id,
        string name,
        IReadOnlyCollection<string> tags,
        string description)
    {
        return new GameItem(
            id,
            name,
            ItemCategory.Tool,
            tags,
            description);
    }
}

public static class ItemText
{
    public static string FormatSummary(GameItem item)
    {
        var parts = new List<string>
        {
            item.Category.ToString()
        };

        if (item.Slot != EquipmentSlot.None)
        {
            parts.Add(item.Slot.ToString());
        }

        if (item.Power > 0)
        {
            parts.Add($"Power {item.Power}");
        }

        if (item.Defense > 0)
        {
            parts.Add($"Defense {item.Defense}");
        }

        if (item.KarmaRequirement is not null)
        {
            parts.Add($"{item.RequiredPath} {item.KarmaRequirement:+#;-#;0}");
        }

        return string.Join(" | ", parts);
    }

    public static string FormatTags(GameItem item)
    {
        return item.Tags is null || item.Tags.Count == 0
            ? "Tags: none"
            : $"Tags: {string.Join(", ", item.Tags.OrderBy(tag => tag))}";
    }

    public static string FormatPickupPrompt(GameItem item)
    {
        return
            $"{item.Name}\n" +
            $"{FormatSummary(item)}\n" +
            $"{FormatTags(item)}\n\n" +
            $"{item.Description}\n\n" +
            "Press E to pick it up.";
    }
}
