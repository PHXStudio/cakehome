using System;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Data-only reward that grants power-ups. UI display is handled by <see cref="PURewardView"/>.
    /// </summary>
    [Serializable]
    [RegisterReward(typeof(PURewardView))]
    public sealed class PUReward : Reward
    {
        [SerializeField] PUItem[] powerUps;
        public PUItem[] PowerUps => powerUps;

        public override void ApplyReward()
        {
            if (powerUps.IsNullOrEmpty()) return;

            foreach (PUItem item in powerUps)
            {
                if (item == null) continue;

                PUController.AddPowerUp(item.PowerUpType, item.Amount);
            }
        }

        [Serializable]
        public class PUItem
        {
            [SerializeField] PUType powerUpType;
            public PUType PowerUpType => powerUpType;

            [SerializeField] int amount;
            public int Amount => amount;
        }
    }
}
