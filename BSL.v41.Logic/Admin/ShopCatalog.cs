using Newtonsoft.Json;
using BSL.v41.Logic.Environment.LaserMessage.Sepo.Home.Laser.Laser;
using BSL.v41.Supercell.Titan.CommonUtils.Utils;
using BSL.v41.Titan.Mathematical.Data;
using BSL.v41.Titan.Utilities;

namespace BSL.v41.Logic.Admin;

public class ShopItemEntry
{
    public int Type { get; set; } // ShopItemHelperTable value.
    public int Count { get; set; } = 1;
    public int BrawlerId { get; set; } // characters.csv index (Type 3 = guaranteed hero).
    public int SkinId { get; set; } // skins.csv index (Type 4 = skin).
}

public class ShopOfferEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public int PriceType { get; set; } // ShopPriceTypeHelperTable value.
    public int Price { get; set; } = 1;
    public int OldPrice { get; set; } = 1;
    public int DurationHours { get; set; } // 0 = no time limit.
    public int StartTime { get; set; } // unix time when the offer was (re)published.
    public bool Daily { get; set; }
    public bool Confirm { get; set; }
    public List<ShopItemEntry> Items { get; set; } = new();
}

// Admin managed shop. Offers stored here are shown to every player in addition to personal offers.
public static class ShopCatalog
{
    private static readonly object Gate = new();
    public static string PathToJson { get; set; } = "SaveBase/shop_catalog.json";

    public static List<ShopOfferEntry> Load()
    {
        lock (Gate)
        {
            try
            {
                return File.Exists(PathToJson)
                    ? JsonConvert.DeserializeObject<List<ShopOfferEntry>>(File.ReadAllText(PathToJson)) ?? new()
                    : new List<ShopOfferEntry>();
            }
            catch
            {
                return new List<ShopOfferEntry>();
            }
        }
    }

    public static void Save(List<ShopOfferEntry> offers)
    {
        lock (Gate)
        {
            File.WriteAllText(PathToJson, JsonConvert.SerializeObject(offers, Formatting.Indented));
        }
    }

    public static bool IsExpired(ShopOfferEntry offer)
    {
        return offer.DurationHours > 0 &&
               offer.StartTime + (long)offer.DurationHours * 3600 <= LogicTimeUtil.GetTimestamp();
    }

    public static List<LogicOfferBundle> GetActiveBundles()
    {
        var result = new List<LogicOfferBundle>();

        foreach (var offer in Load())
        {
            if (!offer.Enabled || offer.Items.Count == 0 || IsExpired(offer)) continue;

            try
            {
                result.Add(ToBundle(offer));
            }
            catch
            {
                // ignored: a broken entry must never break the home data of every player.
            }
        }

        return result;
    }

    private static LogicOfferBundle ToBundle(ShopOfferEntry offer)
    {
        var gemOffers = new List<LogicGemOffer>();

        foreach (var item in offer.Items)
        {
            var gemOffer = new LogicGemOffer((ShopItemHelperTable)item.Type, Math.Max(item.Count, 1));

            if (item.Type == (int)ShopItemHelperTable.GuaranteedHero)
                gemOffer.SetItem(GlobalId.CreateGlobalId(16, item.BrawlerId));
            else if (item.Type == (int)ShopItemHelperTable.Skin)
                gemOffer.SetSkin(item.SkinId);

            gemOffers.Add(gemOffer);
        }

        var price = Math.Max(offer.Price, 1);
        var oldPrice = Math.Max(offer.OldPrice, price);
        var endTime = offer.DurationHours > 0
            ? offer.StartTime + offer.DurationHours * 3600
            : LogicTimeUtil.GetTimestamp() + 30 * 24 * 3600;

        return new LogicOfferBundle(gemOffers, offer.Title ?? "", "", (ShopPriceTypeHelperTable)offer.PriceType,
            price, oldPrice, endTime, offer.Daily, offer.Confirm);
    }
}
