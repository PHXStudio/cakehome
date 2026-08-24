using System.Threading.Tasks;
using UnityEngine;

namespace Watermelon
{
#if MODULE_APPLOVIN
    [UnityEngine.Scripting.Preserve]
    public class ApplovinHandler : AdProviderHandler
    {
        public override string ProviderName => "Applovin";

        private ApplovinContainer Container => adsSettings.GetContainer<ApplovinContainer>();

        private bool isBannerLoaded = false;

        protected override async Task<bool> InitProviderAsync()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += RewardedVideoOnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += RewardedVideoOnAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += RewardedVideoOnAdOpenedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += RewardedVideoOnAdClosedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += RewardedVideoOnAdShowFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += RewardedVideoOnAdRewardedEvent;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += InterstitialOnAdReadyEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += InterstitialOnAdLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += InterstitialOnAdOpenedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += InterstitialOnAdClosedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialOnAdShowFailedEvent;

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += BannerOnAdLoadedEvent;

            MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
            {
                // Mark initialization as successful
                tcs.SetResult(true);
            };

            MaxSdk.InitializeSdk();

            return await tcs.Task;
        }

        #region RewardedAd callback handlers
        private void RewardedVideoOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: Rewarded ad loaded", LogCategory.Services);

                rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                AdsManager.OnProviderAdLoaded(ProviderName, AdType.RewardedVideo);
            });
        }

        private void RewardedVideoOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: Rewarded ad failed to load with error: " + errorInfo, LogCategory.Services);

                rewardedRetryAttempt++;
                float retryDelay = Mathf.Pow(2, rewardedRetryAttempt);

                Tween.DelayedCall(retryDelay, () => AdsManager.RequestRewardBasedVideo(), true, UpdateMethod.Update);
            });
        }

        private void RewardedVideoOnAdOpenedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: RewardedVideoOnAdOpenedEvent event received", LogCategory.Services);

                AdsManager.OnProviderAdDisplayed(ProviderName, AdType.RewardedVideo);
            });
        }

        private void RewardedVideoOnAdClosedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AdsManager.ExecuteRewardVideoCallback(false);

                LogManager.Log("[AdsManager]: RewardedVideoOnAdClosedEvent event received", LogCategory.Services);

                AdsManager.OnProviderAdClosed(ProviderName, AdType.RewardedVideo);

                AdsManager.RequestRewardBasedVideo();
            });
        }

        private void RewardedVideoOnAdRewardedEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AdsManager.ExecuteRewardVideoCallback(true);

                LogManager.Log("[AdsManager]: RewardedVideoOnAdRewardedEvent event received", LogCategory.Services);

                AdsManager.ResetInterstitialDelayTime();
            });
        }

        private void RewardedVideoOnAdShowFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AdsManager.ExecuteRewardVideoCallback(false);

                LogManager.Log("[AdsManager]: RewardedVideoOnAdShowFailedEvent event received with message: " + errorInfo, LogCategory.Services);

                rewardedRetryAttempt++;
                float retryDelay = Mathf.Pow(2, rewardedRetryAttempt);

                Tween.DelayedCall(retryDelay, () => AdsManager.RequestRewardBasedVideo(), true, UpdateMethod.Update);
            });
        }
        #endregion

        #region Interstitial callback handlers
        private void InterstitialOnAdReadyEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: Interstitial ad loaded", LogCategory.Services);

                interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                AdsManager.OnProviderAdLoaded(ProviderName, AdType.Interstitial);
            });
        }

        private void InterstitialOnAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: Interstitial ad failed to load an ad with error: " + errorInfo, LogCategory.Services);

                interstitialRetryAttempt++;
                float retryDelay = Mathf.Pow(2, interstitialRetryAttempt);

                Tween.DelayedCall(retryDelay, () => AdsManager.RequestInterstitial(), true, UpdateMethod.Update);
            });
        }

        private void InterstitialOnAdOpenedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: InterstitialOnAdOpenedEvent event received", LogCategory.Services);

                AdsManager.OnProviderAdDisplayed(ProviderName, AdType.Interstitial);
            });
        }

        private void InterstitialOnAdShowFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: Interstitial ad failed to show with error: " + errorInfo, LogCategory.Services);

                interstitialRetryAttempt++;
                float retryDelay = Mathf.Pow(2, interstitialRetryAttempt);

                Tween.DelayedCall(retryDelay, () => AdsManager.RequestInterstitial(), true, UpdateMethod.Update);
            });
        }

        private void InterstitialOnAdClosedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: InterstitialOnAdClosedEvent event received", LogCategory.Services);

                AdsManager.OnProviderAdClosed(ProviderName, AdType.Interstitial);

                AdsManager.ExecuteInterstitialCallback(true);

                AdsManager.ResetInterstitialDelayTime();
                AdsManager.RequestInterstitial();
            });
        }
        #endregion

        #region Banner callback handlers
        private void BannerOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: BannerOnAdLoadedEvent event received", LogCategory.Services);

                AdsManager.OnProviderAdLoaded(ProviderName, AdType.Banner);

                if(Container.UseAdaptiveBanner)
                {
                    float adapsiveHeight = MaxSdkUtils.GetAdaptiveBannerHeight();
                    float screenPixelHeight = adapsiveHeight * MaxSdkUtils.GetScreenDensity();

                    if (screenPixelHeight > 0)
                    {
                        AdsManager.SetBannerHeight(screenPixelHeight);
                    }
                    else
                    {
                        AdsManager.ResetBannerHeight();
                    }
                }
                else
                {
                    Rect layout = MaxSdk.GetBannerLayout(adUnitId);

                    float screenPixelHeight = layout.height * MaxSdkUtils.GetScreenDensity();
                    if (screenPixelHeight > 0)
                    {
                        AdsManager.SetBannerHeight(screenPixelHeight);
                    }
                    else
                    {
                        AdsManager.ResetBannerHeight();
                    }
                }
            });
        }
        #endregion

        public override void DestroyBanner()
        {
            MaxSdk.DestroyBanner(GetBannerID());

            isBannerLoaded = false;

            AdsManager.OnProviderAdClosed(ProviderName, AdType.Banner);
        }

        public override void HideBanner()
        {
            if (isBannerLoaded)
                MaxSdk.HideBanner(GetBannerID());

            AdsManager.OnProviderAdClosed(ProviderName, AdType.Banner);
        }

        public override void ShowBanner()
        {
            if (!isBannerLoaded)
            {
                MaxSdkBase.AdViewConfiguration bannerConfiguration = new MaxSdkBase.AdViewConfiguration((MaxSdkBase.AdViewPosition)Container.BannerPosition);
                bannerConfiguration.IsAdaptive = Container.UseAdaptiveBanner;

                MaxSdk.CreateBanner(GetBannerID(), bannerConfiguration);

                // Set background or background color for banners to be fully functional
                MaxSdk.SetBannerBackgroundColor(GetBannerID(), Container.BannerBackgroundColor);

                isBannerLoaded = true;
            }
            else
            {
                MaxSdk.ShowBanner(GetBannerID());

            }

            AdsManager.OnProviderAdDisplayed(ProviderName, AdType.Banner);
        }

        public override void RequestInterstitial()
        {
            MaxSdk.LoadInterstitial(GetInterstitialID());
        }

        public override void ShowInterstitial(AdvertisementCallback callback)
        {
            MaxSdk.ShowInterstitial(GetInterstitialID());
        }

        public override void RequestRewardedVideo()
        {
            MaxSdk.LoadRewardedAd(GetRewardedVideoID());
        }

        public override void ShowRewardedVideo(AdvertisementCallback callback)
        {
            MaxSdk.ShowRewardedAd(GetRewardedVideoID());
        }

        public override bool IsInterstitialLoaded()
        {
            return MaxSdk.IsInterstitialReady(GetInterstitialID());
        }

        public override bool IsRewardedVideoLoaded()
        {
            return MaxSdk.IsRewardedAdReady(GetRewardedVideoID());
        }

        public string GetBannerID()
        {
#if UNITY_ANDROID
            return Container.AndroidBannerID;
#elif UNITY_IOS
            return Container.IOSBannerID;
#else
            return string.Empty;
#endif
        }

        public string GetInterstitialID()
        {
#if UNITY_ANDROID
            return Container.AndroidInterstitialID;
#elif UNITY_IOS
            return Container.IOSInterstitialID;
#else
            return string.Empty;
#endif
        }

        public string GetRewardedVideoID()
        {
#if UNITY_ANDROID
            return Container.AndroidRewardedVideoID;
#elif UNITY_IOS
            return Container.IOSRewardedVideoID;
#else
            return string.Empty;
#endif
        }
    }
#endif
}