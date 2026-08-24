using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// A lightweight reward-only wrapper without a view.
    /// Use when you need to apply a reward but do not need any UI representation.
    /// For a reward paired with a view, use <see cref="SingleReward"/> instead.
    /// </summary>
    [System.Serializable]
    public sealed class SimpleReward
    {
        [SerializeReference] Reward reward;
        public Reward Reward => reward;

        public void ApplyReward()
        {
            reward?.ApplyReward();
        }

        public IRewardPreview GetPreview()
        {
            if (reward == null) return null;

            List<IRewardPreview> previews = reward.GetRewardPreviews();
            if(previews.IsNullOrEmpty()) return null;

            return previews[0];
        }
    }
}
