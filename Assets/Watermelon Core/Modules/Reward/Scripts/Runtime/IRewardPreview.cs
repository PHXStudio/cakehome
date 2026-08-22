using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Read-only data contract for a single reward preview entry shown in the rewards popup.
    /// Implement this in reward types (or dedicated preview classes) to supply the
    /// icon, display text, sort order, and an optional custom UI prefab per reward.
    /// </summary>
    public interface IRewardPreview
    {
        public Sprite Icon { get; }
        public string Text { get; }

        public int SortingOrder { get; }

        GameObject GetCustomUIPrefab();
    }
}
