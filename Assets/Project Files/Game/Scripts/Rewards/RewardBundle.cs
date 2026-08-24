using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class RewardBundle
    {
        [SerializeField] SimpleReward[] rewards;

        public bool IsEmpty => rewards == null || rewards.Length == 0;

        public void ApplyReward()
        {
            if (rewards == null) return;
            foreach (SimpleReward reward in rewards)
                reward?.ApplyReward();
        }

        public IEnumerable<Reward> GetRewards()
        {
            if (rewards == null) yield break;
            foreach (SimpleReward simpleReward in rewards)
                if (simpleReward?.Reward != null)
                    yield return simpleReward.Reward;
        }
    }
}
