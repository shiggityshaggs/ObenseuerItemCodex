using OS.Items;
using System.Collections.Generic;
using System.Linq;

namespace ItemCodex.Utility
{
    internal class Wrapper
    {
        public Wrapper(Item item)
        {
            Item = item;
            IsLiquid = item.Categories.Contains("Liquid");
            ItemLiquidData = ItemMetaUtilities.GetMetaOfType<ItemLiquidData>(item.Meta);
            IsLiquidContainer = (ItemLiquidData != null) && item.Categories.Contains("Liquid Container");
            ItemQualityData = IsLiquidContainer ? ItemMetaUtilities.GetMetaOfType<ItemQualityData>(ItemLiquidData.liquidItemMeta) : null;
            HasQuality = ItemQualityData != null;
        }

        public Item Item { get; }
        public bool IsLiquid { get; }
        public bool IsLiquidContainer { get; }
        public bool HasQuality { get; }
        public ItemLiquidData ItemLiquidData { get; }
        public ItemQualityData ItemQualityData { get; }

        public bool AddLiquidAndGiveContainer(Item liquid, float quality = 2, float quantity = 0)
        {
            if (!IsLiquidContainer) return false;
            if (liquid == null || !liquid.Categories.Contains("Liquid")) return false;

            var meta = this.Item.CloneMetaAndSetLiquid(liquid, this.Item.Meta, quantity, out float _, 1);
            if (HasQuality) ItemQualityData.quality = quality;

            ItemOperations.AddItemsAndDropRemaining(item: this.Item,
                            owner: -1,
                            slot: null,
                            meta: meta,
                            amount: 1,
                            stackAmount: 0);

            return true;
        }
    }

    internal static class ItemHandler
    {
        public static List<Item> Items { get => field ??= GetItems().ToList(); }
        public static List<string> Categories { get => field ??= GetCategories().ToList(); }
        public static List<Wrapper> ItemWrappers { get => field ??= GetItemWrappers().ToList(); }

        static IEnumerable<Item> GetItems()
        {
            return ItemDatabase.database
                .Where(x => x.ID != -1)
                .OrderBy(x => x.Title);
        }

        static IEnumerable<string> GetCategories()
        {
            return ItemDatabase.database
                .SelectMany(x => x.Categories)
                .Distinct()
                .OrderBy(x => x);
        }

        static IEnumerable<Wrapper> GetItemWrappers()
        {
            return GetItems()
                .Select(item => new Wrapper(item));
        }
    }
}
