using ItemCodex.Utility;
using System.Reactive.Subjects;

namespace ItemCodex
{
    internal partial class GUIComponent
    {
        readonly ReactiveHashSet<string> SelectedCategories = [];
        readonly BehaviorSubject<Item> HoverItem = new(null);
        readonly Subject<Item> ItemButton = new();
        readonly BehaviorSubject<string> TextFilter = new(string.Empty);
    }
}
