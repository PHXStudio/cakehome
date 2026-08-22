namespace Watermelon
{
    [System.Serializable]
    public class DailyTaskSave : ISaveObject
    {
        /// <summary>任务记录对应的天（UTC yyyyMMdd），跨天自动重置</summary>
        public int TaskDay;

        public int LevelsPlayed;
        public int LevelsWon;
        public int ShopHarvests;
        public int RewardedVideos;

        /// <summary>已领取奖励的任务 bitmask</summary>
        public int ClaimedMask;

        public void OnBeforeSave()
        {
        }
    }
}
