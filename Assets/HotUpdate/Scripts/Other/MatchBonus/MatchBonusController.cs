using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 积分棋子（规划 TAB2）：三消匹配成功时实时加分。
    /// 每次消除按匹配棋子数 × 每棋子积分入账，作为消消乐局内积分印钞增强。
    /// </summary>
    public static class MatchBonusController
    {
        private const int COINS_PER_MATCHED_TILE = 5;
        private const int MIN_MATCH = 3;

        private static bool isInitialized;
        private static bool isInLevel;

        public static int CoinsPerMatchedTile => COINS_PER_MATCHED_TILE;

        public static void Init()
        {
            if (isInitialized)
                return;

            isInitialized = true;
        }

        /// <summary>进入关卡时启用。</summary>
        public static void OnLevelStarted()
        {
            isInLevel = true;
        }

        /// <summary>离开关卡时禁用（防止主菜单误加分）。</summary>
        public static void OnLevelEnded()
        {
            isInLevel = false;
        }

        /// <summary>消除回调：匹配棋子实时加分。</summary>
        public static void OnMatchCombined(List<ISlotable> match)
        {
            if (!isInLevel || match == null)
                return;

            int count = match.Count;
            if (count < MIN_MATCH)
                return;

            int reward = count * COINS_PER_MATCHED_TILE;
            CurrencyController.Add(CurrencyType.Coins, reward);
            CustomAnalytics.TrackCurrencyGain("coins", reward, "match_bonus");
        }
    }
}
