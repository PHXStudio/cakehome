using System;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Data-only reward that enables the infinite-lives mode for a fixed duration.
    /// UI display is handled by <see cref="LivesInfiniteModeRewardView"/>.
    /// </summary>
    [Serializable]
    [RegisterReward(typeof(LivesInfiniteModeRewardView))]
    public sealed class LivesInfiniteModeReward : Reward
    {
        [SerializeField] float durationInMinutes = 60;
        public float DurationInMinutes => durationInMinutes;

        public override void ApplyReward()
        {
            LivesSystem.EnableInfiniteMode(durationInMinutes * 60);
        }
    }
}
