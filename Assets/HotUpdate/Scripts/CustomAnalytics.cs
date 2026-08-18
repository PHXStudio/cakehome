using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Lightweight analytics wrapper. Currently logs events to console;
    /// hook up a real analytics SDK (Firebase/GameAnalytics/Amplitude etc.)
    /// inside the Track* methods when needed.
    /// </summary>
    public static class CustomAnalytics
    {
        private static bool isInitialized;

        public static void Init()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            Debug.Log("[CustomAnalytics] Init");
        }

        public static void TrackInstall()
        {
            LogEvent("install");
        }

        public static void TrackLaunch()
        {
            LogEvent("launch");
        }

        public static void TrackTutorialStart()
        {
            LogEvent("tutorial_start");
        }

        public static void TrackRegistration(string method)
        {
            LogEvent("registration", $"method={method}");
        }

        public static void TrackCurrencyGain(string currencyType, int amount, string source)
        {
            LogEvent("currency_gain", $"currency={currencyType}", $"amount={amount}", $"source={source}");
        }

        public static void TrackLevelComplete(int levelIndex, float playTime, int tilesLeft)
        {
            LogEvent("level_complete", $"level={levelIndex}", $"play_time={playTime:F1}", $"tiles_left={tilesLeft}");
        }

        public static void TrackLevelFail(int levelIndex, float playTime, int tilesLeft)
        {
            LogEvent("level_fail", $"level={levelIndex}", $"play_time={playTime:F1}", $"tiles_left={tilesLeft}");
        }

        private static void LogEvent(string eventName, params string[] parameters)
        {
            if (!isInitialized)
            {
                Debug.LogWarning($"[CustomAnalytics] Event '{eventName}' tracked before Init()");
            }

            if (parameters != null && parameters.Length > 0)
            {
                Debug.Log($"[CustomAnalytics] {eventName} ({string.Join(", ", parameters)})");
            }
            else
            {
                Debug.Log($"[CustomAnalytics] {eventName}");
            }
        }
    }
}
