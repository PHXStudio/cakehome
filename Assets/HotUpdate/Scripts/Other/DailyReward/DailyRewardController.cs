using System;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 每日登录奖励（7 天签到循环）。奖励 = 烘焙积分 + 特定天给无限生命。
    /// 存档经 SaveController 持久化。Init 在游戏启动时调用。
    /// </summary>
    public static class DailyRewardController
    {
        private const string SAVE_NAME = "daily_reward";
        private const int CYCLE_DAYS = 7;
        private const int INFINITE_LIFE_DAY = 5;
        private const int INFINITE_LIFE_MINUTES = 30;

        private static DailyRewardSave save;
        private static bool isInitialized;

        public static event Action<int, int> OnRewardClaimed; // dayIndex(1-7), coinAmount

        public static DailyRewardSave Save => save;

        public static bool IsInitialized => isInitialized;

        public static int Today => Convert.ToInt32(DateTime.UtcNow.ToString("yyyyMMdd"));

        public static void Init()
        {
            if (isInitialized)
                return;

            isInitialized = true;
            save = SaveController.GetSaveObject<DailyRewardSave>(SAVE_NAME);
        }

        /// <summary>今天是否还能领取（当天未领过）。</summary>
        public static bool CanClaimToday()
        {
            EnsureInit();
            return save.LastClaimDay != Today;
        }

        /// <summary>当前应领的档位（1-7，按连续天数循环）。</summary>
        public static int GetCurrentDayIndex()
        {
            EnsureInit();
            return (save.StreakCount % CYCLE_DAYS) + 1;
        }

        /// <summary>该档位的烘焙积分奖励。</summary>
        public static int GetCoinReward(int dayIndex)
        {
            switch (dayIndex)
            {
                case 1: return 50;
                case 2: return 100;
                case 3: return 150;
                case 4: return 200;
                case 5: return 250;
                case 6: return 300;
                case 7: return 500;
                default: return 50;
            }
        }

        /// <summary>该档位是否附带无限生命。</summary>
        public static bool HasInfiniteLifeReward(int dayIndex)
        {
            return dayIndex == INFINITE_LIFE_DAY;
        }

        /// <summary>领取今日奖励。成功返回 true，当天已领或未初始化返回 false。</summary>
        public static bool Claim()
        {
            EnsureInit();

            if (!CanClaimToday())
                return false;

            int dayIndex = GetCurrentDayIndex();
            int coins = GetCoinReward(dayIndex);

            CurrencyController.Add(CurrencyType.Coins, coins);
            CustomAnalytics.TrackCurrencyGain("coins", coins, "daily_login_reward");

            if (HasInfiniteLifeReward(dayIndex))
            {
                LivesSystem.EnableInfiniteMode(INFINITE_LIFE_MINUTES * 60);
                CustomAnalytics.TrackCurrencyGain("infinite_life", 1, "daily_login_reward");
            }

            save.LastClaimDay = Today;
            save.StreakCount++;
            save.ClaimedMask |= (1 << (dayIndex - 1));
            SaveController.MarkAsSaveIsRequired();

            OnRewardClaimed?.Invoke(dayIndex, coins);
            return true;
        }

        /// <summary>重置连续登录（跨天但未领取时调用）。</summary>
        public static void ResetStreakIfMissed()
        {
            EnsureInit();

            int yesterday = Today - 1;
            if (save.LastClaimDay != 0 && save.LastClaimDay < yesterday)
            {
                save.StreakCount = 0;
                save.ClaimedMask = 0;
                SaveController.MarkAsSaveIsRequired();
            }
        }

        private static void EnsureInit()
        {
            if (!isInitialized)
                Init();
        }
    }
}
