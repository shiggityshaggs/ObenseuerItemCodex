using System;
using System.Collections.Generic;
using System.Linq;

namespace ItemCodex.Utility
{
    internal static class ItemHandler
    {
        public static IEnumerable<string> Categories
        {
            get { return field ??= GetCategories(); }
        }

        public static IEnumerable<Item> Items
        {
            get { return field ??= GetItems(); }
        }

        static IEnumerable<string> GetCategories()
        {
            IEnumerable<string> categories = ItemDatabase.database
                .SelectMany(x => x.Categories)
                .Distinct()
                .OrderBy(x => x);

            return categories.ToList();
        }

        static IEnumerable<Item> GetItems()
        {
            var items = ItemDatabase.database
                .Where(x => x.ID != -1)
                .OrderBy(x => x.Title);

            return items.ToList();
        }
    }
}
