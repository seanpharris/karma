using Karma.Data;
using Karma.Net;

namespace Karma.UI;

public static class ShopText
{
    public static string FormatOfferLine(ShopOfferSnapshot offer)
    {
        var item = StarterItems.GetById(offer.ItemId);
        return $"9 - Buy {offer.ItemName} ({offer.Price} {offer.Currency}) [{ItemText.FormatSummary(item)}]";
    }
}
