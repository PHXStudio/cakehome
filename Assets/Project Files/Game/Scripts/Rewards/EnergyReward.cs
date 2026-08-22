using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [Serializable]
    [RegisterReward(typeof(EnergyRewardView))]
    public sealed class EnergyReward : Reward
    {
        [SerializeField] int amount;
        public int Amount => amount;

        public EnergyReward() { }
        public EnergyReward(int amount) { this.amount = amount; }

        public override void ApplyReward()
        {
            EnergyController.Add(amount, ignoreCap: true);
        }

        public override List<IRewardPreview> GetRewardPreviews()
        {
            return new List<IRewardPreview>
            {
                new RewardPreview(EnergyController.Icon, $"+{amount}", 0)
            };
        }
    }
}
