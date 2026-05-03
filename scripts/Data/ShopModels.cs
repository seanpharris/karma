using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public sealed record ShopOffer(
    string Id,
    string VendorNpcId,
    string ItemId,
    int Price,
    string Currency = "scrip",
    string RequiredFactionId = "",
    int MinReputation = 0);

public static class StarterShopCatalog
{
    public const string DallenWhoopieCushionOfferId = "dallen_whoopie_cushion";
    public const string DallenRepairKitOfferId = "dallen_repair_kit";
    public const string DallenWorkVestOfferId = "dallen_work_vest";
    public const string DallenRationPackOfferId = "dallen_ration_pack";
    public const string DallenDataChipOfferId = "dallen_data_chip";
    public const string DallenApologyFlowerOfferId = "dallen_apology_flower";
    public const string DallenStunBatonOfferId = "dallen_stun_baton";
    public const string DallenElectroPistolOfferId = "dallen_electro_pistol";
    public const string DallenRifle27OfferId = "dallen_rifle_27";
    public const string DallenMultiToolOfferId = "dallen_multi_tool";
    public const string DallenMediPatchOfferId = "dallen_medi_patch";
    public const string DallenFlashlightOfferId = "dallen_flashlight";
    public const string DallenPortableShieldOfferId = "dallen_portable_shield";

    public static readonly IReadOnlyList<ShopOffer> Offers = new[]
    {
        new ShopOffer(DallenWhoopieCushionOfferId, StarterNpcs.Dallen.Id, StarterItems.WhoopieCushionId, 7),
        new ShopOffer(DallenRepairKitOfferId, StarterNpcs.Dallen.Id, StarterItems.RepairKitId, 18),
        new ShopOffer(DallenWorkVestOfferId, StarterNpcs.Dallen.Id, StarterItems.WorkVestId, 35),
        new ShopOffer(DallenRationPackOfferId, StarterNpcs.Dallen.Id, StarterItems.RationPackId, 9),
        new ShopOffer(DallenDataChipOfferId, StarterNpcs.Dallen.Id, StarterItems.DataChipId, 14),
        new ShopOffer(DallenApologyFlowerOfferId, StarterNpcs.Dallen.Id, StarterItems.ApologyFlowerId, 11),
        new ShopOffer(DallenStunBatonOfferId, StarterNpcs.Dallen.Id, StarterItems.StunBatonId, 42),
        new ShopOffer(DallenElectroPistolOfferId, StarterNpcs.Dallen.Id, StarterItems.ElectroPistolId, 58),
        new ShopOffer(DallenRifle27OfferId, StarterNpcs.Dallen.Id, StarterItems.Rifle27Id, 86),
        new ShopOffer(DallenMultiToolOfferId, StarterNpcs.Dallen.Id, StarterItems.MultiToolId, 24),
        new ShopOffer(DallenMediPatchOfferId, StarterNpcs.Dallen.Id, StarterItems.MediPatchId, 12),
        new ShopOffer(DallenFlashlightOfferId, StarterNpcs.Dallen.Id, StarterItems.FlashlightId, 16),
        new ShopOffer(DallenPortableShieldOfferId, StarterNpcs.Dallen.Id, StarterItems.PortableShieldId, 64)
    };

    public static bool TryGet(string offerId, out ShopOffer offer)
    {
        offer = Offers.FirstOrDefault(candidate => candidate.Id == offerId);
        return offer is not null;
    }
}

public static class ShopPricing
{
    public const int TrustedDiscountPercent = 10;
    public const int SaintCommunityDiscountPercent = 5;
    public const int ExaltedFavorDiscountPercent = 25;
    public const int ShiftyPricesDiscountPercent = 15;
    public const int RenegadeMarkDiscountPercent = 50;

    public static int CalculatePrice(ShopOffer offer, PlayerState player, LeaderboardStanding standing)
    {
        var discountPercent = CalculateDiscountPercent(player, standing);
        if (discountPercent <= 0)
        {
            return offer.Price;
        }

        var discount = System.Math.Max(1, (offer.Price * discountPercent) / 100);
        return System.Math.Max(1, offer.Price - discount);
    }

    public static int CalculateDiscountPercent(PlayerState player, LeaderboardStanding standing)
    {
        if (player is null)
        {
            return 0;
        }

        var perks = PerkCatalog.GetForPlayer(player, standing);
        var discountPercent = 0;
        if (standing.SaintPlayerId == player.Id && player.Karma.Score > 0)
        {
            discountPercent = System.Math.Max(discountPercent, SaintCommunityDiscountPercent);
        }

        if (perks.Any(perk => perk.Id == PerkCatalog.TrustedDiscountId))
        {
            discountPercent = System.Math.Max(discountPercent, TrustedDiscountPercent);
        }

        if (perks.Any(perk => perk.Id == PerkCatalog.ExaltedFavorId))
        {
            discountPercent = System.Math.Max(discountPercent, ExaltedFavorDiscountPercent);
        }

        if (perks.Any(perk => perk.Id == PerkCatalog.ShiftyPricesId))
        {
            discountPercent = System.Math.Max(discountPercent, ShiftyPricesDiscountPercent);
        }

        if (perks.Any(perk => perk.Id == PerkCatalog.RenegadeMarkId))
        {
            discountPercent = System.Math.Max(discountPercent, RenegadeMarkDiscountPercent);
        }

        return discountPercent;
    }

    public static int CalculateRelationshipModifierPercent(int opinion)
    {
        return opinion switch
        {
            >= 50 => -15,
            >= 20 => -10,
            <= -50 => 25,
            <= -20 => 10,
            _ => 0
        };
    }

    public static int ApplySignedModifier(int price, int modifierPercent)
    {
        if (modifierPercent == 0)
        {
            return price;
        }

        if (modifierPercent < 0)
        {
            var discount = System.Math.Max(1, (price * System.Math.Abs(modifierPercent)) / 100);
            return System.Math.Max(1, price - discount);
        }

        return System.Math.Max(1, (price * (100 + modifierPercent) + 99) / 100);
    }
}
