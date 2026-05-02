using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public sealed record CraftingRecipe(
    string Id,
    string OutputItemId,
    IReadOnlyList<string> IngredientItemIds,
    string RequiredStructureCategory = "workshop");

public static class StarterRecipes
{
    public const string RepairKitFromPartsId = "craft_repair_kit";
    public const string MediPatchFromHerbsId = "craft_medi_patch";
    public const string BallisticRoundFromScrapId = "craft_ballistic_round";
    public const string EnergyCellFromPowerCellId = "craft_energy_cell";
    public const string FlashlightFromTerminalId = "craft_flashlight";
    public const string StunBatonFromStickId = "craft_stun_baton";
    public const string GrapplingHookFromToolsId = "craft_grappling_hook";
    public const string ContrabandPackageFromFlowerId = "craft_contraband_package";
    public const string LongSwordFromIronId = "craft_long_sword";
    public const string ShortBowFromYewId = "craft_short_bow";
    public const string HealingTinctureFromHerbsId = "craft_healing_tincture";
    public const string LockpickSetFromWireId = "craft_lockpick_set";

    public static readonly IReadOnlyList<CraftingRecipe> All = new[]
    {
        new CraftingRecipe(
            RepairKitFromPartsId,
            StarterItems.RepairKitId,
            new[] { StarterItems.MultiToolId, StarterItems.DataChipId }),
        // Healing tincture: herbs + clean vessel.
        new CraftingRecipe(
            MediPatchFromHerbsId,
            StarterItems.MediPatchId,
            new[] { StarterItems.RationPackId, StarterItems.FilterCoreId }),
        // Arrow: yew shaft + feather fletching + iron point.
        new CraftingRecipe(
            BallisticRoundFromScrapId,
            StarterItems.BallisticRoundId,
            new[] { StarterItems.PracticeStickId, StarterItems.ApologyFlowerId, StarterItems.BoltCuttersId }),
        // Blessing oil: herb + wax + holy water.
        new CraftingRecipe(
            EnergyCellFromPowerCellId,
            StarterItems.EnergyCellId,
            new[] { StarterItems.RationPackId, StarterItems.PowerCellId, StarterItems.ChemInjectorId }),
        // Torch: wood + cloth + oil.
        new CraftingRecipe(
            FlashlightFromTerminalId,
            StarterItems.FlashlightId,
            new[] { StarterItems.PracticeStickId, StarterItems.WorkVestId, StarterItems.PowerCellId }),
        // Cudgel: wood + iron strip.
        new CraftingRecipe(
            StunBatonFromStickId,
            StarterItems.StunBatonId,
            new[] { StarterItems.PracticeStickId, StarterItems.BoltCuttersId }),
        new CraftingRecipe(
            GrapplingHookFromToolsId,
            StarterItems.GrapplingHookId,
            new[] { StarterItems.BoltCuttersId, StarterItems.MultiToolId }),
        // Comedic recipe: a contraband package disguised as a bouquet.
        // Tone-fit with the WhoopieCushion / DeflatedBalloon item lineage.
        new CraftingRecipe(
            ContrabandPackageFromFlowerId,
            StarterItems.ContrabandPackageId,
            new[] { StarterItems.FilterCoreId, StarterItems.ApologyFlowerId }),
        // Long sword: practice blade + iron stock + smith's tool.
        new CraftingRecipe(
            LongSwordFromIronId,
            StarterItems.Rifle27Id,
            new[] { StarterItems.PracticeStickId, StarterItems.BoltCuttersId, StarterItems.MultiToolId }),
        // Short bow: yew stave + cord + iron knife work.
        new CraftingRecipe(
            ShortBowFromYewId,
            StarterItems.ElectroPistolId,
            new[] { StarterItems.PracticeStickId, StarterItems.WorkVestId, StarterItems.BoltCuttersId }),
        // Strong healing tincture: herbs + injector vial.
        new CraftingRecipe(
            HealingTinctureFromHerbsId,
            StarterItems.MediPatchId,
            new[] { StarterItems.RationPackId, StarterItems.ChemInjectorId }),
        // Lock pick set: thin wire + cutters.
        new CraftingRecipe(
            LockpickSetFromWireId,
            StarterItems.LockpickSetId,
            new[] { StarterItems.BoltCuttersId, StarterItems.DataChipId })
    };

    public static bool TryGet(string recipeId, out CraftingRecipe recipe)
    {
        recipe = All.FirstOrDefault(r => r.Id == recipeId);
        return recipe is not null;
    }
}
