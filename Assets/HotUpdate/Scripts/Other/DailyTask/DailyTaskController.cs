using System;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 每日任务系统：通关/收获/看广告等进度追踪 + 领取烘焙积分奖励。
    /// 跨天自动重置进度。AddProgress 在关键流程调用，UI 经 DailyTaskPanel 展示。
    /// </summary>
    public enum DailyTaskType
    {
        LevelsPlayed = 0,   // 玩 N 关
        LevelsWon = 1,      // 赢 N 关
        MergeOrders = 2,    // 完成 N 个合成订单
        RewardedVideos = 3, // 看 N 次激励视频
    }

    public static class DailyTaskController
    {
        private const string SAVE_NAME = "daily_task";
        private const int TASK_COUNT = 4;

        private static DailyTaskSave save;
        private static bool isInitialized;

        public static event Action OnTaskProgressChanged;

        public static DailyTaskSave Save => save;
        public static bool IsInitialized => isInitialized;
        public static int Today => Convert.ToInt32(DateTime.UtcNow.ToString("yyyyMMdd"));

        public static void Init()
        {
            if (isInitialized)
                return;

            isInitialized = true;
            save = SaveController.GetSaveObject<DailyTaskSave>(SAVE_NAME);

            ResetIfNewDay();
        }

        private static void ResetIfNewDay()
        {
            if (save.TaskDay == Today)
                return;

            save.TaskDay = Today;
            save.LevelsPlayed = 0;
            save.LevelsWon = 0;
            save.MergeOrders = 0;
            save.RewardedVideos = 0;
            save.ClaimedMask = 0;
            SaveController.MarkAsSaveIsRequired();
        }

        // ------------------------------------------------------------------ 目标与奖励

        public static int GetTaskTarget(DailyTaskType type)
        {
            switch (type)
            {
                case DailyTaskType.LevelsPlayed: return 3;
                case DailyTaskType.LevelsWon: return 1;
                case DailyTaskType.MergeOrders: return 2;
                case DailyTaskType.RewardedVideos: return 1;
                default: return 1;
            }
        }

        public static int GetTaskReward(DailyTaskType type)
        {
            switch (type)
            {
                case DailyTaskType.LevelsPlayed: return 30;
                case DailyTaskType.LevelsWon: return 50;
                case DailyTaskType.MergeOrders: return 40;
                case DailyTaskType.RewardedVideos: return 25;
                default: return 30;
            }
        }

        public static string GetTaskName(DailyTaskType type)
        {
            switch (type)
            {
                case DailyTaskType.LevelsPlayed: return "完成 3 关";
                case DailyTaskType.LevelsWon: return "通关 1 关";
                case DailyTaskType.MergeOrders: return "完成 2 个订单";
                case DailyTaskType.RewardedVideos: return "看 1 次广告";
                default: return "";
            }
        }

        // ------------------------------------------------------------------ 进度

        public static int GetProgress(DailyTaskType type)
        {
            EnsureInit();
            switch (type)
            {
                case DailyTaskType.LevelsPlayed: return save.LevelsPlayed;
                case DailyTaskType.LevelsWon: return save.LevelsWon;
                case DailyTaskType.MergeOrders: return save.MergeOrders;
                case DailyTaskType.RewardedVideos: return save.RewardedVideos;
                default: return 0;
            }
        }

        public static void AddProgress(DailyTaskType type, int amount = 1)
        {
            EnsureInit();
            ResetIfNewDay();

            switch (type)
            {
                case DailyTaskType.LevelsPlayed: save.LevelsPlayed += amount; break;
                case DailyTaskType.LevelsWon: save.LevelsWon += amount; break;
                case DailyTaskType.MergeOrders: save.MergeOrders += amount; break;
                case DailyTaskType.RewardedVideos: save.RewardedVideos += amount; break;
            }

            SaveController.MarkAsSaveIsRequired();
            OnTaskProgressChanged?.Invoke();
        }

        public static bool IsClaimed(DailyTaskType type)
        {
            EnsureInit();
            return (save.ClaimedMask & (1 << (int)type)) != 0;
        }

        public static bool CanClaim(DailyTaskType type)
        {
            EnsureInit();
            return !IsClaimed(type) && GetProgress(type) >= GetTaskTarget(type);
        }

        /// <summary>领取任务奖励，成功返回 true。</summary>
        public static bool Claim(DailyTaskType type)
        {
            EnsureInit();
            ResetIfNewDay();

            if (!CanClaim(type))
                return false;

            int reward = GetTaskReward(type);
            CurrencyController.Add(CurrencyType.Coins, reward);
            CustomAnalytics.TrackCurrencyGain("coins", reward, "daily_task_reward");

            save.ClaimedMask |= (1 << (int)type);
            SaveController.MarkAsSaveIsRequired();

            OnTaskProgressChanged?.Invoke();
            return true;
        }

        /// <summary>是否有可领取的奖励（主菜单红点提示用）。</summary>
        public static bool HasClaimable()
        {
            EnsureInit();
            for (int i = 0; i < TASK_COUNT; i++)
            {
                if (CanClaim((DailyTaskType)i))
                    return true;
            }

            return false;
        }

        private static void EnsureInit()
        {
            if (!isInitialized)
                Init();
        }
    }
}
