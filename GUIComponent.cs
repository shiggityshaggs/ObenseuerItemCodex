using ItemCodex.Utility;
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

        float minHeight = 500f;
        float minWidth = 700f;

        const int delayForFrames = 3; // 1 to avoid null per cycle. 3 to avoid null per click.
        int framesSinceHover;

        IEnumerable<Item> Filtereditems;
        readonly HashSet<string> excludeCategory = ["Liquid", "ItemGroup"];
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
                .Where(predicateToUse)
                .Where(SelectedMatch)   // always enforce selected categories
                .ToList();
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
                .Subscribe(AddItem);

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

        private void AddItem(Item item)
        {
            if (item == null) return;
            if (BackpackStorage.instance == null) return;

            int amount = (item.Stackable > 1) && (Event.current.button == 1) ? item.Stackable : 1;

            ItemOperations.AddItemsAndDropRemaining(item: item,
                                                    owner: -1,
                                                    slot: null,
                                                    meta: item.Meta,
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
            }

            using (new GUILayout.HorizontalScope())
            {
                Footer();
            }

            if (!GUI.changed) GUI.DragWindow();
        }

        void Footer()
        {
            string text = string.Empty;
            if (HoverItem.Value != null)
            {
                text += "LMB: Add one";
                if (HoverItem.Value.Stackable > 1) text += $", RMB: Add stack of {HoverItem.Value.Stackable}";
            }

            //GUILayout.FlexibleSpace();
            GUILayout.Space(170);
            GUILayout.Label(text);
            //GUILayout.FlexibleSpace();
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
                }
            }

            using (var scope = new GUILayout.ScrollViewScope(itemScrollPos, GUILayout.MinWidth(minWidth)))
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
