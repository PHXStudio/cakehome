using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 埋点封装。当前输出本地日志（验证事件触发）；
    /// 接入 Firebase Analytics / Adjust 时，在 TrackEvent 内补充 SDK 调用（见 #if FIREBASE_ANALYTICS）。
    /// 关键：session_start + daily_login 用于区分"每日回访"，是次日留存的数据来源。
    /// </summary>
    public static class CustomAnalytics
    {
        private const string PREFS_DEVICE_ID = "analytics_device_id";
        private const string PREFS_INSTALL_DAY = "analytics_install_day";
        private const string PREFS_LAST_LOGIN_DAY = "analytics_last_login_day";
        private const string PREFS_DAILY_LOGIN_COUNT = "analytics_daily_login_count";

        private static bool isInitialized;
        private static string sessionId;
        private static string deviceId;
        private static string installDay;

        public static void Init()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            sessionId = System.Guid.NewGuid().ToString("N").Substring(0, 16);
            deviceId = GetOrCreateDeviceId();
            installDay = PlayerPrefs.GetString(PREFS_INSTALL_DAY, "");

            TrackEvent("session_start", "session_id=" + sessionId, "device_id=" + deviceId, "install_day=" + installDay);

            // 每日首次启动 → daily_login（次日留存核心指标）
            string today = System.DateTime.UtcNow.ToString("yyyyMMdd");
            string lastDay = PlayerPrefs.GetString(PREFS_LAST_LOGIN_DAY, "");
            if (lastDay != today)
            {
                int loginCount = PlayerPrefs.GetInt(PREFS_DAILY_LOGIN_COUNT, 0) + 1;
                PlayerPrefs.SetInt(PREFS_DAILY_LOGIN_COUNT, loginCount);
                PlayerPrefs.SetString(PREFS_LAST_LOGIN_DAY, today);

                int daysSinceInstall = 0;
                if (!string.IsNullOrEmpty(installDay))
                {
                    int.TryParse(installDay, out int install);
                    int.TryParse(today, out int now);
                    daysSinceInstall = now - install;
                }

                TrackEvent("daily_login", "day=" + today, "login_count=" + loginCount, "days_since_install=" + daysSinceInstall);
            }
        }

        private static string GetOrCreateDeviceId()
        {
            string id = PlayerPrefs.GetString(PREFS_DEVICE_ID, "");
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(PREFS_DEVICE_ID, id);
                PlayerPrefs.SetString(PREFS_INSTALL_DAY, System.DateTime.UtcNow.ToString("yyyyMMdd"));
                PlayerPrefs.SetInt(PREFS_DAILY_LOGIN_COUNT, 1);
            }

            return id;
        }

        // ------------------------------------------------------------------ 生命周期

        public static void TrackInstall()
        {
            TrackEvent("install", "device_id=" + deviceId);
        }

        public static void TrackLaunch()
        {
            TrackEvent("launch", "device_id=" + deviceId);
        }

        public static void TrackTutorialStart()
        {
            TrackEvent("tutorial_start");
        }

        public static void TrackTutorialComplete(int stepIndex)
        {
            TrackEvent("tutorial_complete", "step=" + stepIndex);
        }

        public static void TrackRegistration(string method)
        {
            TrackEvent("registration", "method=" + method);
        }

        // ------------------------------------------------------------------ 经济

        public static void TrackCurrencyGain(string currencyType, int amount, string source)
        {
            TrackEvent("currency_gain", "currency=" + currencyType, "amount=" + amount, "source=" + source);
        }

        public static void TrackCurrencySpend(string currencyType, int amount, string purpose)
        {
            TrackEvent("currency_spend", "currency=" + currencyType, "amount=" + amount, "purpose=" + purpose);
        }

        public static void TrackIAPPurchase(string productId, string price)
        {
            TrackEvent("iap_purchase", "product=" + productId, "price=" + price);
        }

        // ------------------------------------------------------------------ 关卡（漏斗）

        public static void TrackLevelStart(int levelIndex)
        {
            TrackEvent("level_start", "level=" + levelIndex);
        }

        public static void TrackLevelRestart(int levelIndex)
        {
            TrackEvent("level_restart", "level=" + levelIndex);
        }

        public static void TrackLevelComplete(int levelIndex, float playTime, int tilesLeft)
        {
            TrackEvent("level_complete", "level=" + levelIndex, "play_time=" + playTime.ToString("F1"), "tiles_left=" + tilesLeft);
        }

        public static void TrackLevelFail(int levelIndex, float playTime, int tilesLeft)
        {
            TrackEvent("level_fail", "level=" + levelIndex, "play_time=" + playTime.ToString("F1"), "tiles_left=" + tilesLeft);
        }

        // ------------------------------------------------------------------ 广告

        public static void TrackRewardVideo(string placement)
        {
            TrackEvent("reward_video", "placement=" + placement, "session_id=" + sessionId);
        }

        public static void TrackRewardVideoCompleted(string placement)
        {
            TrackEvent("reward_video_completed", "placement=" + placement);
        }

        public static void TrackInterstitial(string placement)
        {
            TrackEvent("interstitial", "placement=" + placement);
        }

        // ------------------------------------------------------------------ 商店挂机

        public static void TrackShopHarvest(int amount, int totalCoins)
        {
            TrackEvent("shop_harvest", "amount=" + amount, "total_coins=" + totalCoins);
        }

        public static void TrackShopExpand(int unlockedShelfCount)
        {
            TrackEvent("shop_expand", "unlocked_shelves=" + unlockedShelfCount);
        }

        public static void TrackShopPlaceCake(string definitionId)
        {
            TrackEvent("shop_place_cake", "cake=" + definitionId);
        }

        // ------------------------------------------------------------------ 上报

        private static void TrackEvent(string eventName, params string[] parameters)
        {
            if (!isInitialized)
            {
                Debug.LogWarning($"[CustomAnalytics] Event '{eventName}' tracked before Init()");
            }

#if FIREBASE_ANALYTICS
            // 接入 Firebase Analytics 时在此补充：
            // Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, new Parameter[] { ... });
#endif

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
