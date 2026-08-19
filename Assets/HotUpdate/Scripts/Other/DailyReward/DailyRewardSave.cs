namespace Watermelon
{
    [System.Serializable]
    public class DailyRewardSave : ISaveObject
    {
        /// <summary>上次领取日期（UTC yyyyMMdd）</summary>
        public int LastClaimDay;

        /// <summary>连续领取天数（决定当前奖励档位）</summary>
        public int StreakCount;

        /// <summary>本 7 天周期内已领取的天数 bitmask（bit0=Day1 ... bit6=Day7）</summary>
        public int ClaimedMask;

        public void Flush()
        {
        }
    }
}
