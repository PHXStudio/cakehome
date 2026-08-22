#pragma warning disable 0649
#pragma warning disable 0162

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Watermelon
{
    public class AdsManager
    {
        private const int DEFAULT_BANNER_HEIGHT = 110;
        private const int INIT_ATTEMPTS_AMOUNT = 30;

        private const double FORCED_AD_DISABLED_FOREVER = -1;
        private const string FIRST_LAUNCH_PREFS = "FIRST_LAUNCH";

        private static AdsManager instance;

        private static AdProviderHandler activeHandler;

        private static bool debugMode;
        public static bool DebugMode => debugMode;

        private static AdsSettings settings;
        public static AdsSettings Settings => settings;

        private static double lastInterstitialTime;

        private static AdProviderHandler.AdvertisementCallback rewardedVideoCallback;
        private static AdProviderHandler.AdvertisementCallback interstitalCallback;

        private static MainThreadDispatcher dispatcher;

        private static bool isFirstAdLoaded = false;
        private static bool waitingForRewardVideoCallback;

        private static bool isBannerActive = true;
        public static bool IsBannerActive => isBannerActive;

        private static float bannerHeight = DEFAULT_BANNER_HEIGHT;
        public static float BannerHeight => bannerHeight;

        private static Coroutine loadingCoroutine;
        private static TweenCase delayTweenCase;

        private static float intFirstStartDelay;
        private static float intStartDelay;
        private static float intShowingDelay;

        // Events
        public static event SimpleCallback ForcedAdDisabled;

        public static event AdsModuleCallback AdProviderInitialized;
        public static event AdsEventsCallback AdLoaded;
        public static event AdsEventsCallback AdDisplayed;
        public static event AdsEventsCallback AdClosed;

        public static AdsBoolCallback InterstitialConditions;

        private static AdSave save;

        #region Initialize
        public void Init(AdsSettings adsSettings, MonoBehaviour host)
        {
            if (instance != null)
            {
                LogManager.LogWarning("[AdsManager]: Module already exists!", LogCategory.Services);
                return;
            }

            instance = this;

            dispatcher = new MainThreadDispatcher(host);

            isFirstAdLoaded = false;

            debugMode = adsSettings.DebugMode;

            settings = adsSettings;

            intFirstStartDelay = settings.InterstitialFirstStartDelay;
            intStartDelay = settings.InterstitialStartDelay;
            intShowingDelay = settings.InterstitialShowingDelay;

#if MODULE_REMOTE_CONFIG
            AdsRemoteConfigData remoteConfigData = RemoteConfigController.TryGetConfig<AdsRemoteConfigData>("ads");
            if (remoteConfigData != null)
            {
                intFirstStartDelay = remoteConfigData.intFSDelay;
                intStartDelay = remoteConfigData.intSDelay;
                intShowingDelay = remoteConfigData.intDelay;

                if (!remoteConfigData.useBanner)
                    settings.DisableBanner();

                if (!remoteConfigData.useInterstitials)
                    settings.DisableInterstitial();

                if (!remoteConfigData.useRewardedVideo)
                    settings.DisableRewardedVideo();
            }
#endif

            save = SaveController.GetSaveObject<AdSave>("advertisement_forced_ad");

            if (settings == null)
            {
                Debug.LogError("[AdsManager]: Settings don't exist!");
                return;
            }

            if (!PlayerPrefs.HasKey(FIRST_LAUNCH_PREFS))
            {
                lastInterstitialTime = Time.time + intFirstStartDelay;
                PlayerPrefs.SetInt(FIRST_LAUNCH_PREFS, 1);
            }
            else
            {
                lastInterstitialTime = Time.time + intStartDelay;
            }

            // Check if ad-free period has expired and send analytics event only once
            if (save.ForcedAdDisabledUntil != 0 && save.ForcedAdDisabledUntil != FORCED_AD_DISABLED_FOREVER && IsForcedAdEnabled())
            {
#if MODULE_ANALYTICS
                Analytics.TrackEvent(AdsAnalytics.AdFreePeriodExpired);
#endif
                save.ForcedAdDisabledUntil = 0;
            }

            activeHandler = settings.GetContainer(settings.ActiveProvider)?.CreateHandler();

            if (activeHandler != null)
            {
                activeHandler.LinkSettings(settings);
            }
            else
            {
                Debug.LogError($"[AdsManager]: Provider '{settings.ActiveProvider}' not found or SDK not installed!");
            }

            InitializeModules(settings.LoadAdsOnStart);
        }

        private static async void InitializeModules(bool loadAds)
        {
            if (activeHandler == null)
                return;

            LogManager.Log($"[AdsManager]: {activeHandler.ProviderName} is trying to initialize!", LogCategory.Services);

            bool isInitialized = await activeHandler.InitAsync();

            if (isInitialized)
            {
                LogManager.Log($"[AdsManager]: {activeHandler.ProviderName} initialized successfully.", LogCategory.Services);
            }
            else
            {
                Debug.LogError($"[AdsManager]: {activeHandler.ProviderName} failed to initialize.");
            }

            if (loadAds)
            {
                TryToLoadFirstAds();
            }
        }
        #endregion

        public static void Unload()
        {
            debugMode = false;

            settings = null;
            lastInterstitialTime = 0;

            rewardedVideoCallback = null;
            interstitalCallback = null;

            dispatcher = null;

            isFirstAdLoaded = false;
            waitingForRewardVideoCallback = false;

            isBannerActive = true;
            bannerHeight = DEFAULT_BANNER_HEIGHT;

            loadingCoroutine = null;

            ForcedAdDisabled = null;

            AdProviderInitialized = null;
            AdLoaded = null;
            AdDisplayed = null;
            AdClosed = null;

            InterstitialConditions = null;

            save = null;

            instance = null;
        }

        public static void TryToLoadFirstAds()
        {
            if (loadingCoroutine == null)
            {
                LogManager.Log("[AdsManager]: Loading first ads..", LogCategory.Services);
                loadingCoroutine = Tween.InvokeCoroutine(TryToLoadAdsCoroutine());
            }
        }

        private static IEnumerator TryToLoadAdsCoroutine()
        {
            int initAttemps = 0;

            yield return new WaitForSeconds(1.0f);

            while (!isFirstAdLoaded && initAttemps < INIT_ATTEMPTS_AMOUNT)
            {
                if (LoadFirstAds())
                    break;

                yield return new WaitForSeconds(1.0f * (initAttemps + 1));

                initAttemps++;
            }

            LogManager.Log("[AdsManager]: First ads have loaded!", LogCategory.Services);
        }

        private static bool LoadFirstAds()
        {
            if (instance == null)
                return false;

            if (isFirstAdLoaded)
                return true;

            bool isProviderInitialized = activeHandler != null && activeHandler.IsInitialized;

            LogManager.Log($"[AdsManager]: LoadFirstAds — provider={settings?.ActiveProvider}, initialized={isProviderInitialized}", LogCategory.Services);

            if (!isProviderInitialized)
                return false;

            if (settings.RewardedVideoEnabled)
                RequestRewardBasedVideo();

            bool isForcedAdEnabled = IsForcedAdEnabled();
            LogManager.Log($"[AdsManager]: LoadFirstAds — IsForcedAdEnabled={isForcedAdEnabled}", LogCategory.Services);

            if (settings.InterstitialEnabled && isForcedAdEnabled)
                RequestInterstitial();

            if (settings.BannerEnabled && isForcedAdEnabled)
                ShowBanner();

            isFirstAdLoaded = true;

            return true;
        }

        public static void CallEventInMainThread(SimpleCallback callback)
        {
            dispatcher?.Dispatch(callback);
        }

        public static void ShowErrorMessage()
        {
            SystemMessage.ShowMessage("Network error. Please try again later");
        }

        public static bool IsProviderActive()
        {
            return instance != null && activeHandler != null;
        }

        public static bool IsProviderInitialized()
        {
            return instance != null && activeHandler != null && activeHandler.IsInitialized;
        }

        public static AdProviderHandler GetActiveProvider()
        {
            return activeHandler;
        }

        #region Interstitial
        public static bool IsInterstitialLoaded()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[AdsManager]: Mobile monetization is disabled!", LogCategory.Services);
                return false;
            }

            if (!IsForcedAdEnabled() || activeHandler == null)
                return false;

            return activeHandler.IsInterstitialLoaded();
        }

        public static void RequestInterstitial()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[AdsManager]: Mobile monetization is disabled!", LogCategory.Services);
                return;
            }

            if (!IsForcedAdEnabled() || activeHandler == null || !activeHandler.IsInitialized || activeHandler.IsInterstitialLoaded())
                return;

            activeHandler.RequestInterstitial();
        }

        public static void ShowInterstitial(AdProviderHandler.AdvertisementCallback callback, string analyticsEvent = "Default", bool ignoreConditions = false)
        {
            interstitalCallback = callback;
            interstitalCallback += (result) =>
            {
                if (string.IsNullOrEmpty(analyticsEvent))
                    analyticsEvent = "Default";

#if MODULE_ANALYTICS
                Analytics.TrackEvent(AdsAnalytics.InterstitialDisplayed, new AdsAnalytics.AnalyticsIntData(analyticsEvent));
#endif
            };

            if (instance == null)
            {
                LogManager.LogWarning("[AdsManager]: Mobile monetization is disabled!", LogCategory.Services);
                ExecuteInterstitialCallback(false);
                return;
            }

            if (!IsForcedAdEnabled() || activeHandler == null || (!ignoreConditions && (!CheckInterstitialTime() || !CheckExtraInterstitialCondition())) || !activeHandler.IsInitialized || !activeHandler.IsInterstitialLoaded())
            {
                ExecuteInterstitialCallback(false);
                return;
            }

            delayTweenCase.KillActive();

            if (settings.LoadingAdDuration > 0)
            {
                SystemMessage.ShowLoadingPanel();
                SystemMessage.ChangeLoadingMessage(settings.LoadingMessage);

                delayTweenCase.KillActive();
                delayTweenCase = Tween.DelayedCall(settings.LoadingAdDuration, () =>
                {
                    activeHandler.ShowInterstitial(callback);
                    SystemMessage.HideLoadingPanel();
                }, unscaledTime: true);
            }
            else
            {
                activeHandler.ShowInterstitial(callback);
            }
        }

        public static void ExecuteInterstitialCallback(bool result)
        {
            if (interstitalCallback != null)
            {
                CallEventInMainThread(() => interstitalCallback.Invoke(result));
            }
        }

        public static void SetInterstitialDelayTime(float time)
        {
            lastInterstitialTime = Time.time + time;
        }

        public static void ResetInterstitialDelayTime()
        {
            lastInterstitialTime = Time.time + intShowingDelay;
        }

        private static bool CheckInterstitialTime()
        {
            LogManager.Log("[AdsManager]: Interstitial Time: " + lastInterstitialTime + "; Time: " + Time.time, LogCategory.Services);

            return lastInterstitialTime < Time.time;
        }

        public static bool CheckExtraInterstitialCondition()
        {
            if (InterstitialConditions != null)
            {
                bool state = true;

                System.Delegate[] listDelegates = InterstitialConditions.GetInvocationList();
                for (int i = 0; i < listDelegates.Length; i++)
                {
                    if (!(bool)listDelegates[i].DynamicInvoke())
                    {
                        state = false;
                        break;
                    }
                }

                LogManager.Log("[AdsManager]: Extra condition interstitial state: " + state, LogCategory.Services);

                return state;
            }

            return true;
        }
        #endregion

        #region Rewarded Video
        public static bool IsRewardBasedVideoLoaded()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[AdsManager]: Mobile monetization is disabled!", LogCategory.Services);
                return false;
            }

            if (activeHandler == null || !activeHandler.IsInitialized)
                return false;

            return activeHandler.IsRewardedVideoLoaded();
        }

        public static void RequestRewardBasedVideo()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[AdsManager]: Mobile monetization is disabled!", LogCategory.Services);
                return;
            }

            if (!settings.RewardedVideoEnabled || activeHandler == null || !activeHandler.IsInitialized || activeHandler.IsRewardedVideoLoaded())
                return;

            activeHandler.RequestRewardedVideo();
        }

        public static void ShowRewardBasedVideo(AdProviderHandler.AdvertisementCallback callback, string analyticsEvent = "Default", bool showErrorMessage = true)
        {
            rewardedVideoCallback = callback;
            waitingForRewardVideoCallback = true;

            if (instance == null)
            {
                LogManager.LogWarning("[AdsManager]: Mobile monetization is disabled!", LogCategory.Services);
                ExecuteRewardVideoCallback(false);
                return;
            }

            if (!settings.RewardedVideoEnabled || activeHandler == null || !activeHandler.IsInitialized || !activeHandler.IsRewardedVideoLoaded())
            {
                ExecuteRewardVideoCallback(false);

                if (showErrorMessage)
                    ShowErrorMessage();

                return;
            }

            if (string.IsNullOrEmpty(analyticsEvent))
                analyticsEvent = "Default";

#if MODULE_ANALYTICS
            Analytics.TrackEvent(AdsAnalytics.RVClicked, new AdsAnalytics.AnalyticsRVData(analyticsEvent));
#endif

            delayTweenCase.KillActive();

            if (settings.LoadingAdDuration > 0)
            {
                SystemMessage.ShowLoadingPanel();
                SystemMessage.ChangeLoadingMessage(settings.LoadingMessage);

                delayTweenCase.KillActive();
                delayTweenCase = Tween.DelayedCall(settings.LoadingAdDuration, () =>
                {
                    activeHandler.ShowRewardedVideo(callback);
                    SystemMessage.HideLoadingPanel();
                }, unscaledTime: true);
            }
            else
            {
                activeHandler.ShowRewardedVideo(callback);
            }
        }

        public static void ExecuteRewardVideoCallback(bool result)
        {
            if (rewardedVideoCallback != null && waitingForRewardVideoCallback)
            {
                CallEventInMainThread(() => rewardedVideoCallback.Invoke(result));

                waitingForRewardVideoCallback = false;

                LogManager.Log("[AdsManager]: Reward received: " + result, LogCategory.Services);
            }
        }
        #endregion

        #region Banner
        public static void ShowBanner()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[AdsManager]: Mobile monetization is disabled!", LogCategory.Services);
                return;
            }

            if (!isBannerActive)
            {
                LogManager.Log("[AdsManager]: ShowBanner — skipped: isBannerActive=false", LogCategory.Services);
                return;
            }

            bool forcedEnabled = IsForcedAdEnabled();
            bool initialized = activeHandler != null && activeHandler.IsInitialized;
            LogManager.Log($"[AdsManager]: ShowBanner — provider={settings?.ActiveProvider}, forcedEnabled={forcedEnabled}, initialized={initialized}", LogCategory.Services);

            if (!forcedEnabled || !initialized)
                return;

            activeHandler.ShowBanner();
        }

        public static void DestroyBanner()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[AdsManager]: Mobile monetization is disabled!", LogCategory.Services);
                return;
            }

            if (activeHandler == null || !activeHandler.IsInitialized)
                return;

            activeHandler.DestroyBanner();
        }

        public static void HideBanner()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[AdsManager]: Mobile monetization is disabled!", LogCategory.Services);
                return;
            }

            if (activeHandler == null || !activeHandler.IsInitialized)
                return;

            activeHandler.HideBanner();
        }

        public static void EnableBanner()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[AdsManager]: Mobile monetization is disabled!", LogCategory.Services);
                return;
            }

            if (settings != null && !settings.BannerEnabled)
            {
                isBannerActive = false;
                return;
            }

            isBannerActive = true;

            SafeAreaAdapter.Refresh(true);

            ShowBanner();
        }

        public static void DisableBanner()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[AdsManager]: Mobile monetization is disabled!", LogCategory.Services);
                return;
            }

            isBannerActive = false;

            SafeAreaAdapter.Refresh(true);

            HideBanner();
        }

        public static void ResetBannerHeight()
        {
            bannerHeight = DEFAULT_BANNER_HEIGHT;

            SafeAreaAdapter.Refresh(true);
        }

        public static void SetBannerHeight(float height)
        {
            bannerHeight = height;

            SafeAreaAdapter.Refresh(true);
        }

        public static float GetBannerHeight()
        {
            if (Settings != null && Settings.BannerEnabled && IsForcedAdEnabled() && IsBannerActive)
            {
                return bannerHeight;
            }

            return 0.0f;
        }
        #endregion

        #region Forced Ad
        public static bool IsForcedAdEnabled()
        {
            if (save == null) return true;

            if (save.ForcedAdDisabledUntil == FORCED_AD_DISABLED_FOREVER)
                return false;

            return TimeUtils.GetCurrentUnixTimestamp() >= save.ForcedAdDisabledUntil;
        }

        public static void DisableForcedAdForever()
        {
            DisableForcedAdInternal(FORCED_AD_DISABLED_FOREVER);
        }

        public static void DisableForcedAd(int durationSeconds)
        {
            if (durationSeconds < 0)
            {
                LogManager.LogWarning("[AdsManager]: Invalid duration for ad disable: " + durationSeconds, LogCategory.Services);
                return;
            }

            double targetTime = TimeUtils.GetCurrentUnixTimestamp() + durationSeconds;
            if (targetTime <= save.ForcedAdDisabledUntil)
                return;

            DisableForcedAdInternal(targetTime);
        }

        private static void DisableForcedAdInternal(double targetTime)
        {
            LogManager.Log("[AdsManager]: Banners and interstitials are disabled!", LogCategory.Services);

            save.ForcedAdDisabledUntil = targetTime;

            SaveController.MarkAsSaveIsRequired();

            SafeAreaAdapter.Refresh(true);

            ForcedAdDisabled?.Invoke();

            DestroyBanner();
        }
        #endregion

        public static void OnProviderInitialized(string providerName)
        {
            AdProviderInitialized?.Invoke(providerName);
        }

        public static void OnProviderAdLoaded(string providerName, AdType advertisingType)
        {
            AdLoaded?.Invoke(providerName, advertisingType);
        }

        public static void OnProviderAdDisplayed(string providerName, AdType advertisingType)
        {
            AdDisplayed?.Invoke(providerName, advertisingType);

            if (advertisingType == AdType.Interstitial || advertisingType == AdType.RewardedVideo)
            {
                ResetInterstitialDelayTime();
            }
        }

        public static void OnProviderAdClosed(string providerName, AdType advertisingType)
        {
            AdClosed?.Invoke(providerName, advertisingType);

            if (advertisingType == AdType.Interstitial || advertisingType == AdType.RewardedVideo)
            {
                ResetInterstitialDelayTime();
            }
        }

        public delegate void AdsModuleCallback(string providerName);
        public delegate void AdsEventsCallback(string providerName, AdType advertisingType);
        public delegate bool AdsBoolCallback();
    }
}

// -----------------
// Advertisement v1.5.0
// -----------------
