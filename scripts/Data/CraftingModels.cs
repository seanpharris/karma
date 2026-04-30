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

    public static readonly IReadOnlyList<CraftingRecipe> All = new[]
    {
        new CraftingRecipe(
            RepairKitFromPartsId,
            StarterItems.RepairKitId,
            new[] { StarterItems.MultiToolId, StarterItems.DataChipId }),
        new CraftingRecipe(
            MediPatchFromHerbsId,
            StarterItems.MediPatchId,
            new[] { StarterItems.RationPackId, StarterItems.FilterCoreId }),
        new CraftingRecipe(
            BallisticRoundFromScrapId,
            StarterItems.BallisticRoundId,
            new[] { StarterItems.BoltCuttersId, StarterItems.DataChipId }),
        new CraftingRecipe(
            EnergyCellFromPowerCellId,
            StarterItems.EnergyCellId,
            new[] { StarterItems.PowerCellId, StarterItems.ChemInjectorId }),
        new CraftingRecipe(
            FlashlightFromTerminalId,
            StarterItems.FlashlightId,
            new[] { StarterItems.PortableTerminalId, StarterItems.PowerCellId }),
        new CraftingRecipe(
            StunBatonFromStickId,
            StarterItems.StunBatonId,
            new[] { StarterItems.PracticeStickId, StarterItems.PowerCellId }),
        new CraftingRecipe(
            GrapplingHookFromToolsId,
            StarterItems.GrapplingHookId,
            new[] { StarterItems.BoltCuttersId, StarterItems.MultiToolId }),
        // Comedic recipe: a contraband package disguised as a bouquet.
        // Tone-fit with the WhoopieCushion / DeflatedBalloon item lineage.
        new CraftingRecipe(
            ContrabandPackageFromFlowerId,
            StarterItems.ContrabandPackageId,
            new[] { StarterItems.FilterCoreId, StarterItems.ApologyFlowerId })
    };

    public static bool TryGet(string recipeId, out CraftingRecipe recipe)
    {
        recipe = All.FirstOrDefault(r => r.Id == recipeId);
        return recipe is not null;
    }
}
