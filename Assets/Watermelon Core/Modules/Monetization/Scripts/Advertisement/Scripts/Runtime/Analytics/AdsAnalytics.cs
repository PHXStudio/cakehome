namespace Watermelon
{
    public static class AdsAnalytics
    {
        public const string AdFreePeriodExpired = "AdFreePeriodExpired";
        public const string InterstitialDisplayed = "InterstitialDisplayed";
        public const string RewardedVideoDisplayed = "RewardedVideoDisplayed";
        public const string RVClicked = "RVClicked";

        [System.Serializable]
        public class AnalyticsIntData : IAnalyticsData
        {
            public string InterstitialSource { get; }

            public AnalyticsIntData(string interstitialSource)
            {
                InterstitialSource = interstitialSource;
            }
        }

        [System.Serializable]
        public class AnalyticsRVData : IAnalyticsData
        {
            public string RVSource { get; }

            public AnalyticsRVData(string rvSource)
            {
                RVSource = rvSource;
            }
        }
    }
}