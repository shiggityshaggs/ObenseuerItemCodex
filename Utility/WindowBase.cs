using UnityEngine;

namespace ItemCodex.Utility
{
    internal abstract class WindowBase : MonoBehaviour
    {
        internal readonly int windowId = nameof(ItemCodex).GetHashCode();
        internal Rect windowRect;
    }
}
