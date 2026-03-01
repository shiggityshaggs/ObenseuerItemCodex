using ItemCodex.Utility;
using OS.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using UnityEngine;

namespace ItemCodex
{
    internal partial class GUIComponent : WindowBase
    {
        GUIStyle buttonStyle;
        Vector2 categoryScrollPos;
        Vector2 itemScrollPos;
        bool windowHover;

        readonly float minHeight = 500f;
        readonly float minWidth = 705f;

        const int delayForFrames = 3; // 1 to avoid null per cycle. 3 to avoid null per click.
        int framesSinceHover;

        IEnumerable<Item> Filtereditems;
        readonly HashSet<string> excludeCategory = ["ItemGroup"];
        List<string> validCategories = [];

        IDisposable selectedCategoriesSub;
        IDisposable hoverItemSub;
        IDisposable itemButtonSub;
        IDisposable textFilterSub;

        void CategorySelectionChanged<T>(T _)
        {
            var filterText = TextFilter.Value.Trim();
            var hasText = filterText.Length > 0;

            // Parse ID if possible
            var validId = int.TryParse(filterText, out int parsedId)
                          && parsedId > 0 && parsedId < 1_000_000;

            // Text predicate
            bool TextMatch(Item item)
            {
                if (validId && parsedId == item.ID)
                    return true;

                if (!hasText)
                    return true;

                return item.Title.ToLower().Contains(filterText.ToLower());
            }

            // Compute valid categories (based on text filter)
            validCategories = ItemHandler.Items
                .Where(item => !item.Categories.Any(excludeCategory.Contains))
                .Where(item => !SelectedCategories.Any() || SelectedCategories.All(item.Categories.Contains))
                .Where(TextMatch)
                .SelectMany(item => item.Categories)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Selected categories that actually exist in validCategories
            var effectiveSelected = SelectedCategories
                .Where(validCategories.Contains)
                .ToList();

            // Selected-categories predicate
            bool SelectedMatch(Item item) =>
                effectiveSelected.All(item.Categories.Contains);

            // Choose predicate based on whether text is present
            Func<Item, bool> predicateToUse =
                hasText ? TextMatch : SelectedMatch;

            // Final filtered items
            Filtereditems = ItemHandler.Items
                .Where(item => item.Categories.All(validCategories.Contains))
                .Where(item => !item.Categories.Contains("Liquid Container") || ItemMetaUtilities.GetMetaOfType<ItemLiquidData>(item.Meta) != null)
                .Where(predicateToUse)
                .Where(SelectedMatch)   // always enforce selected categories
                .ToList();
        }

        void ItemClickedDispatcher(Item item)
        {
            if (item?.Categories?.Contains("Liquid") ?? false)
            {
                SelectedLiquid.OnNext(item);
                return;
            }

            if (item?.Categories?.Contains("Liquid Container") ?? false)
            {
                liquidQuantity = 100;
                SelectedLiquidContainer.OnNext(item);
                return;
            }

            AddItem(item);
        }

        void OnLiquidSelected(Item liquidItem)
        {
            var container = SelectedLiquidContainer.Value;
            if (container != null)
            {
                var meta = container.CloneMetaAndSetLiquid(liquidItem, container.Meta, 1, out float _, 0);
            }
        }

        void OnLiquidContainerSelected(Item liquidContainer)
        {
            ItemLiquidData itemLiquidData = ItemMetaUtilities.GetMetaOfType<ItemLiquidData>(liquidContainer.Meta);
            if (itemLiquidData != null)
            {
                itemLiquidData.SetToMaxAmount();

                ItemQualityData itemQualityData = ItemMetaUtilities.GetMetaOfType<ItemQualityData>(itemLiquidData.liquidItemMeta);
                if (itemQualityData != null)
                {
                    itemQualityData.quality = 2;
                }
            }
        }

        void Init()
        {
            if (windowRect.Equals(default))
            {
                var x = Screen.width / 2 - minWidth / 2;
                var y = Screen.height / 2f - minHeight / 2;
                windowRect = new(x, y, 0, 0);
            }

            textFilterSub ??= TextFilter
                .Throttle(TimeSpan.FromMilliseconds(250))
                .DistinctUntilChanged()
                .Subscribe(CategorySelectionChanged);

            itemButtonSub ??= ItemButton
                .Subscribe(ItemClickedDispatcher);

            //hoverItemSub ??= HoverItem
            //    .DistinctUntilChanged()
            //    .Subscribe(HoverItemChanged);

            selectedCategoriesSub ??= SelectedCategories.Changes
                .Subscribe(CategorySelectionChanged);

            buttonStyle ??= new(GUI.skin.button)
            {
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft
            };
        }

        private void AddLiquidContainer(Item container, Item liquid, float quantity, float quality)
        {
            if (Inventory.instance == null)
            {
                Console.WriteLine("Inventory is null");
                return;
            }

            if (container == null)
            {
                Console.WriteLine("Container is null");
                return;
            }

            if (liquid == null)
            {
                Console.WriteLine("Liquid is null");
                return; 
            }

            if (!container.Categories.Contains("Liquid Container"))
            {
                Console.WriteLine("Container missing category 'Liquid Container'");
                return;
            }

            if (!liquid.Categories.Contains("Liquid"))
            {
                Console.WriteLine("Liquid missing category 'Liquid'");
                return;
            }

            var meta = container.CloneMetaAndSetLiquid(liquid, container.Meta, quantity, out float _);

            ItemLiquidData itemLiquidData = ItemMetaUtilities.GetMetaOfType<ItemLiquidData>(container.Meta);
            if (itemLiquidData == null)
            {
                Console.WriteLine("ItemLiquidData is null");
                return;
            }

            ItemQualityData itemQualityData = ItemMetaUtilities.GetMetaOfType<ItemQualityData>(itemLiquidData.liquidItemMeta);
            if (itemQualityData == null)
            {
                Console.WriteLine("ItemQualityData is null");
                //return;
            }
            itemQualityData?.quality = quality;

            ItemOperations.AddItemsAndDropRemaining(item: container,
                                        owner: -1,
                                        slot: null,
                                        meta: meta,
                                        amount: 1,
                                        stackAmount: 0);
        }

        private void AddItem(Item item, object[] meta = null)
        {
            if (item == null) return;
            if (Inventory.instance == null) return;
            if (item.Categories.Contains("Liquid")) return;
            //if (item.Categories.Contains("Liquid Container")) return;

            int amount = (item.Stackable > 1) && (Event.current.button == 1) ? item.Stackable : 1;

            ItemOperations.AddItemsAndDropRemaining(item: item,
                                                    owner: -1,
                                                    slot: null,
                                                    meta: meta ?? item.Meta,
                                                    amount: amount,
                                                    stackAmount: 0);
        }

        void Release()
        {
            if (GUI.GetNameOfFocusedControl() == "Filter")
            {
                GUI.FocusControl(null);
                GUI.FocusWindow(windowId);
            }
        }

        void OnGUI()
        {
            Init();
            windowHover = windowRect.Contains(Event.current.mousePosition);
            GUI.backgroundColor = Color.black;
            windowRect = GUILayout.Window(windowId, windowRect, WindowFunction, "Item Codex");
            GUI.backgroundColor = Color.white;

            if (HoverItem.Value != null && framesSinceHover++ > delayForFrames)
                HoverItem.OnNext(null);
        }

        private void WindowFunction(int id)
        {
            using (new GUILayout.HorizontalScope(GUILayout.MinHeight(minHeight)))
            {
                using (new GUILayout.VerticalScope())
                {
                    FilterBox();
                    Categories();
                }

                using (new GUILayout.VerticalScope())
                {
                    Items();
                }

                using (new GUILayout.VerticalScope(GUILayout.MinWidth(135)))
                {
                    LiquidInterface();
                }
            }

            Footer();

            if (!GUI.changed) GUI.DragWindow();
        }

        float liquidQuantity = 0;
        float liquidQuality = 2;
        float buttonSize = 128;

        void LiquidInterface()
        {
            var liquid = SelectedLiquid.Value;
            var container = SelectedLiquidContainer.Value;

            GUI.color = Color.white;

            GUILayout.Label(string.Empty);

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (liquid == null)
                {
                    if (GUILayout.Button("Select liquid"))
                    {
                        SelectedCategories.Clear();
                        SelectedCategories.Add("Liquid");
                    }
                }
                else
                {
                    GUILayout.Label($"Liquid: {liquid.Title}");

                    if (liquid.Appearance?.Sprite != null)
                    {
                        if (Components.SpriteButton(liquid.Appearance?.Sprite, buttonSize))
                        {
                            SelectedCategories.Clear();
                            SelectedCategories.Add("Liquid");
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(Texture2D.redTexture, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                        {
                            SelectedCategories.Clear();
                            SelectedCategories.Add("Liquid");
                        }
                    }

                    ItemQualityData itemQualityData = ItemMetaUtilities.GetMetaOfType<ItemQualityData>(liquid.Meta);
                    if (itemQualityData != null)
                    {
                        GUILayout.Label($"Quality {liquidQuality} {ItemQualityData.GetQuality(liquidQuality)}");
                        liquidQuality = (float)Math.Round(GUILayout.HorizontalSlider(liquidQuality, 0, 2), 1);
                    }
                    else
                    {
                        GUILayout.Label("No ItemQualityData");
                    }
                }
            }

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (container == null)
                {
                    if (GUILayout.Button("Select container"))
                    {
                        SelectedCategories.Clear();
                        SelectedCategories.Add("Liquid Container");
                    }
                }
                else
                {
                    GUILayout.Label($"Container: {container.Title}");

                    if (container.Appearance?.Sprite != null)
                    {
                        if (Components.SpriteButton(container.Appearance?.Sprite, buttonSize))
                        {
                            SelectedCategories.Clear();
                            SelectedCategories.Add("Liquid Container");
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(Texture2D.redTexture, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
                        {
                            SelectedCategories.Clear();
                            SelectedCategories.Add("Liquid Container");
                        }
                    }

                    ItemLiquidData itemLiquidData = ItemMetaUtilities.GetMetaOfType<ItemLiquidData>(container.Meta);
                    if (itemLiquidData != null)
                    {
                        if (liquidQuantity > itemLiquidData.liquidMaxAmount) liquidQuantity = itemLiquidData.liquidMaxAmount;
                        GUILayout.Label($"Quantity {liquidQuantity}/{itemLiquidData.liquidMaxAmount}L");
                        liquidQuantity = (float)Math.Round(GUILayout.HorizontalSlider(liquidQuantity, 0, itemLiquidData.liquidMaxAmount), 1);

                        if (liquid != null && liquidQuantity > 0)
                        {
                            if (GUILayout.Button("Give container"))
                                AddLiquidContainer(container, liquid, liquidQuantity, liquidQuality);
                        }
                        else
                        {
                            GUILayout.Label("No liquid");
                            if (GUILayout.Button("Give empty"))
                                AddItem(container);
                        }
                    }
                    else
                    {
                        GUILayout.Label("No ItemLiquidData");
                        if (GUILayout.Button("Give empty"))
                            AddItem(container);
                    }
                }
            }
        }

        void Footer()
        {
            using (new GUILayout.HorizontalScope())
            {
                string text = string.Empty;
                if (HoverItem.Value != null)
                {
                    if (HoverItem.Value.Categories.Contains("Liquid"))
                        text = "LMB: Set liquid";
                    else if (HoverItem.Value.Categories.Contains("Liquid Container"))
                        text = "LMB: Set liquid container";
                    else
                    {
                        text += "LMB: Add one";
                        if (HoverItem.Value.Stackable > 1) text += $", RMB: Add stack of {HoverItem.Value.Stackable}";
                    }
                }

                GUILayout.Space(170);
                GUILayout.Label(text);
            }
        }

        void FilterBox()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUI.SetNextControlName("Filter");
                string text = GUILayout.TextField(TextFilter.Value, GUILayout.Width(124));
                if (!string.IsNullOrEmpty(text))
                {
                    if (GUILayout.Button("X", GUILayout.Width(23)))
                    {
                        text = string.Empty;
                        Release();
                    }
                }
                if (TextFilter.Value != text)
                    TextFilter.OnNext(text);
            }

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.KeyDown)
                Release();
        }

        void Categories()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Categories");
                if (SelectedCategories.Any() && GUILayout.Button("X", GUILayout.Width(23)))
                    SelectedCategories.Clear();
                GUILayout.FlexibleSpace();
            }

            using (var scope = new GUILayout.ScrollViewScope(categoryScrollPos, GUILayout.Width(160)))
            {
                categoryScrollPos = scope.scrollPosition;

                foreach (var category in validCategories)
                {
                    bool selected = SelectedCategories.Contains(category);

                    GUI.color = selected ? Color.green : (SelectedCategories.Any() ? Color.gray : Color.white);
                    if (GUILayout.Button(category, buttonStyle))
                    {
                        if (selected) SelectedCategories.Remove(category);
                        else SelectedCategories.Add(category);
                    }
                    GUI.color = Color.white;
                }
            }
        }

        void Items()
        {
            using (new GUILayout.HorizontalScope())
            {
                if (HoverItem.Value != null)
                {
                    GUI.color = Color.white;

                    string formattedId = HoverItem?.Value.ID.ToString();
                    if (int.TryParse(TextFilter.Value, out int id) && id == HoverItem?.Value.ID)
                        formattedId = $"<color=cyan>{id}</color>";
                    GUILayout.Label($"{formattedId}", GUILayout.MinWidth(50));

                    GUIStyle labelMatchStyle = new(GUI.skin.label) { richText = true };
                    var formattedTitle = Helpers.ColorizedMatch(TextFilter.Value, HoverItem.Value.Title, RichtextColor.cyan);
                    GUILayout.Label($"{formattedTitle}", labelMatchStyle);

                    GUILayout.FlexibleSpace();
                    GUI.color = Color.gray;

                    var categories = HoverItem?.Value?.Categories
                        .Select(cat => SelectedCategories.Contains(cat) ? $"<color=cyan>{cat}</color>" : cat);

                    GUILayout.Label(string.Join(", ", categories));

                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label(string.Empty);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(string.Empty);
                }
            }

            using (var scope = new GUILayout.ScrollViewScope(itemScrollPos, GUILayout.Width(minWidth)))
            {
                itemScrollPos = scope.scrollPosition;

                for (int skip = 0; skip < Filtereditems.Count(); skip += 10)
                {
                    var narrowed = Filtereditems.Skip(skip).Take(10);

                    using (new GUILayout.HorizontalScope())
                    {
                        foreach (var item in narrowed)
                        {
                            if (item.Appearance?.Sprite != null)
                            {
                                if (Components.SpriteButton(item.Appearance?.Sprite, 64))
                                    ItemButton.OnNext(item);
                            }
                            else
                            {
                                if (GUILayout.Button(Texture2D.redTexture, GUILayout.Width(64), GUILayout.Height(64)))
                                    ItemButton.OnNext(item);
                            }

                            if (windowHover && Event.current.type == EventType.Repaint)
                            {
                                var rect = GUILayoutUtility.GetLastRect();
                                if (rect.Contains(Event.current.mousePosition))
                                {
                                    HoverItem.OnNext(item);
                                    framesSinceHover = 0;
                                }
                            }
                        }

                        GUILayout.FlexibleSpace();
                    }
                }
            }
        }
    }
}
