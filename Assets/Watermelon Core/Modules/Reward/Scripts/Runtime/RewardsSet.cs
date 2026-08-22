using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// ScriptableObject that holds a list of <see cref="Reward"/> entries forming a single reward bundle.
    /// Assign to a <see cref="RewardsHolder"/> to define what rewards are granted when the holder is activated.
    /// Fires <see cref="RewardReceived"/> after all rewards in the set have been applied.
    /// </summary>
    [CreateAssetMenu(fileName = "Rewards Set", menuName = "Data/Rewards/Rewards Set")]
    public class RewardsSet : ScriptableObject
    {
        [Group("System")]
        [SerializeReference] List<Reward> rewards = new List<Reward>();
        public List<Reward> Rewards => rewards;

        [Group("System")]
        [SerializeField] string notes;

        public event SimpleCallback RewardReceived;

        public void ApplyReward()
        {
            if(!rewards.IsNullOrEmpty())
            {
                for (int i = 0; i < rewards.Count; i++)
                    rewards[i]?.ApplyReward();
            }

            RewardReceived?.Invoke();
        }

        public void RestoreReward()
        {
            if (!rewards.IsNullOrEmpty())
            {
                for (int i = 0; i < rewards.Count; i++)
                    rewards[i]?.RestoreReward();
            }
        }

        public List<IRewardPreview> GetPreviews()
        {
            List<IRewardPreview> preview = new List<IRewardPreview>();
            foreach (Reward reward in rewards)
            {
                if (reward == null) continue;

                List<IRewardPreview> rewardPreview = reward.GetRewardPreviews();
                if (!rewardPreview.IsNullOrEmpty())
                {
                    preview.AddRange(rewardPreview);
                }
            }

            return preview;
        }
    }
}
