// Auto-generated file. Do not edit.
// Regenerate via: Tools/Rewards/Regenerate Map
using UnityEngine;

namespace Watermelon
{
    static class GeneratedRewardsMap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            RewardsMap.Register(typeof(Watermelon.CurrencyReward), typeof(Watermelon.CurrencyRewardView));
            RewardsMap.Register(typeof(Watermelon.EnergyReward), typeof(Watermelon.EnergyRewardView));
            RewardsMap.Register(typeof(Watermelon.NoAdsReward), typeof(Watermelon.NoAdsRewardView));
            RewardsMap.Register(typeof(Watermelon.SpawnerQueueReward), typeof(Watermelon.SpawnerQueueRewardView));
        }
    }
}