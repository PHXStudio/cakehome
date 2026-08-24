using System;
using System.Collections.Generic;

namespace Watermelon
{
    /// <summary>
    /// Runtime registry that maps Reward types to their View types.
    ///
    /// Populated before scene load by MD_GeneratedRewardsMap (auto-generated).
    /// Regenerate via: Tools/Rewards/Regenerate Map
    /// </summary>
    public static class RewardsMap
    {
        private static readonly Dictionary<Type, Type> map = new();

        /// <summary>Registers a reward-to-view type mapping; called by the auto-generated registration file at startup.</summary>
        public static void Register(Type rewardType, Type viewType) => map[rewardType] = viewType;

        /// <summary>Looks up the View type for the given Reward type; returns null if not registered.</summary>
        public static Type GetView(Type rewardType)
        {
            map.TryGetValue(rewardType, out Type viewType);
            return viewType;
        }
    }
}
