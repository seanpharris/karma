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

    public static readonly IReadOnlyList<CraftingRecipe> All = new[]
    {
        new CraftingRecipe(
            RepairKitFromPartsId,
            StarterItems.RepairKitId,
            new[] { StarterItems.MultiToolId, StarterItems.DataChipId }),
        new CraftingRecipe(
            MediPatchFromHerbsId,
            StarterItems.MediPatchId,
            new[] { StarterItems.RationPackId, StarterItems.FilterCoreId })
    };

    public static bool TryGet(string recipeId, out CraftingRecipe recipe)
    {
        recipe = All.FirstOrDefault(r => r.Id == recipeId);
        return recipe is not null;
    }
}
