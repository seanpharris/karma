using System.Collections.Generic;
using System.Linq;

namespace Karma.Data;

public sealed record ShopOffer(
    string Id,
    string VendorNpcId,
    string ItemId,
    int Price,
    string Currency = "scrip");

public static class StarterShopCatalog
{
    public const string DallenWhoopieCushionOfferId = "dallen_whoopie_cushion";
    public const string DallenRepairKitOfferId = "dallen_repair_kit";
    public const string DallenWorkVestOfferId = "dallen_work_vest";

    public static readonly IReadOnlyList<ShopOffer> Offers = new[]
    {
        new ShopOffer(DallenWhoopieCushionOfferId, StarterNpcs.Dallen.Id, StarterItems.WhoopieCushionId, 7),
        new ShopOffer(DallenRepairKitOfferId, StarterNpcs.Dallen.Id, StarterItems.RepairKitId, 18),
        new ShopOffer(DallenWorkVestOfferId, StarterNpcs.Dallen.Id, StarterItems.WorkVestId, 35)
    };

    public static bool TryGet(string offerId, out ShopOffer offer)
    {
        offer = Offers.FirstOrDefault(candidate => candidate.Id == offerId);
        return offer is not null;
    }
}
