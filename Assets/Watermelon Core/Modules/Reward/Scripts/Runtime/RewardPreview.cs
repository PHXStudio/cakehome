using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Default concrete implementation of <see cref="IRewardPreview"/>.
    /// Stores icon, display text, sorting order, and an optional custom UI prefab
    /// used by <see cref="UIRewardsPopup"/> when rendering the rewards popup.
    /// </summary>
    public class RewardPreview : IRewardPreview
    {
        private Sprite icon;
        public Sprite Icon => icon;

        private string text;
        public string Text => text;

        private int sortingOrder;
        public int SortingOrder => sortingOrder;

        private GameObject customPrefab;
        public GameObject CustomPrefab => customPrefab;

        public RewardPreview(Sprite icon, string text, int sortingOrder = 0, GameObject customPrefab = null)
        {
            this.icon = icon;
            this.text = text;
            this.sortingOrder = sortingOrder;
            this.customPrefab = customPrefab;
        }

        public GameObject GetCustomUIPrefab() => customPrefab;
    }
}
