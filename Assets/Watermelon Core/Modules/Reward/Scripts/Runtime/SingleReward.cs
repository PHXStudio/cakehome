using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// A self-contained reward+view pair, serialized inline in a MonoBehaviour or ScriptableObject.
    /// Use when you need exactly one reward with its view directly on a component (no RewardsSet SO required).
    /// For a reward without a view, use <see cref="SimpleReward"/> instead.
    /// </summary>
    [System.Serializable]
    public sealed class SingleReward : IRewardHolder
    {
        [SerializeReference] Reward reward;
        public Reward Reward => reward;

        [SerializeReference] RewardView rewardView;
        public RewardView RewardView => rewardView;

        // SingleReward has no Update loop, so dirty state would never be processed.
        public bool IsDirty => false;

        public void Init()
        {
            if (reward == null || rewardView == null) return;

            rewardView.Init(reward, this);
        }

        public void ApplyReward()
        {
            if (reward == null) return;

            reward.ApplyReward();

            if (rewardView == null)
            {
                LogManager.LogWarning($"[SingleReward] rewardView is null for reward '{reward.GetType().Name}'. Assign a view in the Inspector.", LogCategory.Systems);
                return;
            }

            rewardView.OnPurchased();
        }

        // No-op: SingleReward has no Update loop to process dirty state.
        public void MarkAsDirty() { }
    }
}
