using System.Collections.Generic;

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
            _ => null
        };

        return item is not null;
    }
}
