#pragma warning disable 0618

using System.Threading.Tasks;
using UnityEngine;

#if MODULE_LEVELPLAY
using Unity.Services.LevelPlay;
#endif

namespace Watermelon
{
#if MODULE_LEVELPLAY
    [UnityEngine.Scripting.Preserve]
    public class LevelPlayHandler : AdProviderHandler
    {
        public override string ProviderName => "LevelPlay";

        private LevelPlayContainer Container => adsSettings.GetContainer<LevelPlayContainer>();

        private LevelPlayBannerAd bannerAd;
        private LevelPlayInterstitialAd interstitialAd;
        private LevelPlayRewardedAd rewardedVideoAd;

        protected TaskCompletionSource<bool> loadingTask;

        protected override async Task<bool> InitProviderAsync()
        {
            LogManager.Log("[AdsManager]: LevelPlay is trying to initialize!", LogCategory.Services);

            LevelPlay.ValidateIntegration();

            loadingTask = new TaskCompletionSource<bool>();

            LevelPlay.OnInitSuccess += OnInitSucces;
            LevelPlay.OnInitFailed += OnInitFailed;

            UMPTaskBehavior umpProvider = ConsentData.GetProvider<UMPTaskBehavior>();
            if (umpProvider != null)
            {
                LevelPlay.SetConsent(umpProvider.IsConsentGiven);
            }
            else
            {
                LogManager.LogWarning("[LevelPlay]: UMPTaskBehavior provider not found — skipping consent setup.", LogCategory.Services);
            }

            if (AdsManager.DebugMode)
                LevelPlay.SetMetaData("is_test_suite", "enable");

            LevelPlay.Init(GetAppKey());

            return await loadingTask.Task;
        }

        private void OnInitFailed(LevelPlayInitError error)
        {
            Debug.LogError($"[AdsManager]: LevelPlay failed to initialized! Error: #{error.ErrorCode} {error.ErrorMessage}");

            loadingTask?.SetResult(false);
        }

        private void OnInitSucces(LevelPlayConfiguration configuration)
        {
            LogManager.Log("[AdsManager]: LevelPlay is initialized!", LogCategory.Services);

            OnInitCompleted();

            loadingTask?.SetResult(true);

            LevelPlay.OnInitSuccess -= OnInitSucces;
            LevelPlay.OnInitFailed -= OnInitFailed;
        }


        private void OnInitCompleted()
        {
            // Banner
            LevelPlayAdSize bannerSize = LevelPlayAdSize.BANNER;
            switch (Container.BannerType)
            {
                case LevelPlayContainer.BannerPlacementType.Large:
                    bannerSize = LevelPlayAdSize.LARGE;
                    break;
                case LevelPlayContainer.BannerPlacementType.Rectangle:
                    bannerSize = LevelPlayAdSize.MEDIUM_RECTANGLE;
                    break;
                case LevelPlayContainer.BannerPlacementType.Leaderboard:
                    bannerSize = LevelPlayAdSize.LEADERBOARD;
                    break;
            }

            LevelPlayBannerPosition bannerPosition = LevelPlayBannerPosition.BottomCenter;
            if (Container.BannerPosition == BannerPosition.Top)
                bannerPosition = LevelPlayBannerPosition.TopCenter;

            LevelPlayBannerAd.Config bannerConfig = new LevelPlayBannerAd.Config.Builder().SetPosition(bannerPosition).SetSize(bannerSize).Build();

            bannerAd = new LevelPlayBannerAd(GetBannerID(), bannerConfig);
            bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
            bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;
            bannerAd.LoadAd();

            // Interstitial
            interstitialAd = new LevelPlayInterstitialAd(GetInterstitialID());

            // Register to events
            interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
            interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
            interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
            interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
            interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;

            // Create Rewarded Video object
            rewardedVideoAd = new LevelPlayRewardedAd(GetRewardedVideoID());

            // Register to Rewarded Video events
            rewardedVideoAd.OnAdDisplayed += RewardedVideoOnAdOpenedEvent;
            rewardedVideoAd.OnAdDisplayFailed += RewardedVideoOnAdShowFailedEvent;
            rewardedVideoAd.OnAdRewarded += RewardedVideoOnAdRewardedEvent;
            rewardedVideoAd.OnAdClosed += RewardedVideoOnAdClosedEvent;

            if (AdsManager.DebugMode)
                OpenTestSuite();
        }

        #region Rewarded Ad
        public override void RequestRewardedVideo()
        {
            if (rewardedVideoAd != null)
                rewardedVideoAd.LoadAd();
        }

        public override void ShowRewardedVideo(AdvertisementCallback callback)
        {
            if (rewardedVideoAd != null)
                rewardedVideoAd.ShowAd();
        }

        public override bool IsRewardedVideoLoaded()
        {
            return rewardedVideoAd != null && rewardedVideoAd.IsAdReady();
        }

        // The Rewarded Video ad view has opened. Your activity will loose focus.
        private void RewardedVideoOnAdOpenedEvent(LevelPlayAdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AudioListener.pause = true;

                LogManager.Log("[AdsManager]: RewardedVideoOnAdOpenedEvent event received", LogCategory.Services);

                AdsManager.OnProviderAdDisplayed(ProviderName, AdType.RewardedVideo);
            });
        }

        // The Rewarded Video ad view is about to be closed. Your activity will regain its focus.
        private void RewardedVideoOnAdClosedEvent(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AudioListener.pause = false;

                LogManager.Log("[AdsManager]: RewardedVideoOnAdClosedEvent event received", LogCategory.Services);

                AdsManager.OnProviderAdClosed(ProviderName, AdType.RewardedVideo);
            });
        }

        // The user completed to watch the video, and should be rewarded.
        // The placement parameter will include the reward data.
        // When using server-to-server callbacks, you may ignore this event and wait for the ironSource server callback.
        private void RewardedVideoOnAdRewardedEvent(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AdsManager.ExecuteRewardVideoCallback(true);

                LogManager.Log("[AdsManager]: RewardedVideoOnAdRewardedEvent event received", LogCategory.Services);

                AdsManager.ResetInterstitialDelayTime();
                AdsManager.RequestRewardBasedVideo();
            });
        }

        // The rewarded video ad was failed to show.
        private void RewardedVideoOnAdShowFailedEvent(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AdsManager.ExecuteRewardVideoCallback(false);

                LogManager.Log("[AdsManager]: RewardedVideoOnAdShowFailedEvent event received with message: " + error, LogCategory.Services);

                HandleAdLoadFailure(AdType.RewardedVideo, error.ErrorMessage, ref rewardedRetryAttempt, () => RequestRewardedVideo());
            });
        }

        /// <summary>
        /// Retrieves the Rewarded Video ID based on the platform
        /// </summary>
        public string GetRewardedVideoID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return Container.AndroidRVID;
#elif UNITY_IOS
            return Container.IOSRVID;
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        #region Interstitial
        public override void RequestInterstitial()
        {
            if (interstitialAd != null)
                interstitialAd.LoadAd();
        }

        public override void ShowInterstitial(AdvertisementCallback callback)
        {
            if(interstitialAd != null)
                interstitialAd.ShowAd();
        }

        public override bool IsInterstitialLoaded()
        {
            return interstitialAd != null && interstitialAd.IsAdReady();
        }

        private void InterstitialOnAdClosedEvent(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AudioListener.pause = false;

                LogManager.Log("[AdsManager]: InterstitialOnAdClosedEvent event received", LogCategory.Services);

                AdsManager.OnProviderAdClosed(ProviderName, AdType.Interstitial);

                AdsManager.ExecuteInterstitialCallback(true);

                AdsManager.ResetInterstitialDelayTime();
                AdsManager.RequestInterstitial();
            });
        }

        private void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: Interstitial ad failed to load an ad with error: " + error.ErrorMessage, LogCategory.Services);

                HandleAdLoadFailure(AdType.Interstitial, error.ErrorMessage, ref interstitialRetryAttempt, () => RequestInterstitial());
            });
        }

        private void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AudioListener.pause = true;

                LogManager.Log("[AdsManager]: InterstitialOnAdDisplayedEvent event received", LogCategory.Services);

                AdsManager.OnProviderAdDisplayed(ProviderName, AdType.Interstitial);
            });
        }

        private void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: Interstitial ad failed to load an ad with error: " + error.ErrorMessage, LogCategory.Services);

                HandleAdLoadFailure(AdType.Interstitial, error.ErrorMessage, ref interstitialRetryAttempt, () => RequestInterstitial());
            });
        }

        private void InterstitialOnAdLoadedEvent(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: Interstitial ad loaded", LogCategory.Services);

                interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                AdsManager.OnProviderAdLoaded(ProviderName, AdType.Interstitial);
            });
        }

        /// <summary>
        /// Retrieves the interstitial ID based on the platform
        /// </summary>
        public string GetInterstitialID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return Container.AndroidInterstitialID;
#elif UNITY_IOS
            return Container.IOSInterstitialID;
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        #region Banner
        public override void DestroyBanner()
        {
            if(bannerAd != null)
            {
                bannerAd.DestroyAd();
                bannerAd = null;

                AdsManager.OnProviderAdClosed(ProviderName, AdType.Banner);
            }
        }

        public override void HideBanner()
        {
            if (bannerAd != null)
            {
                bannerAd.HideAd();

                AdsManager.OnProviderAdClosed(ProviderName, AdType.Banner);
            }
        }

        public override void ShowBanner()
        {
            if(bannerAd != null)
            {
                bannerAd.ShowAd();
            }
        }

        private void BannerOnAdLoadedEvent(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: BannerOnAdLoadedEvent event received", LogCategory.Services);

                AdsManager.OnProviderAdLoaded(ProviderName, AdType.Banner);
            });
        }

        private void BannerOnAdDisplayedEvent(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                LogManager.Log("[AdsManager]: BannerOnAdDisplayedEvent event received", LogCategory.Services);

                AdsManager.OnProviderAdDisplayed(ProviderName, AdType.Banner);
            });
        }

        /// <summary>
        /// Retrieves the banner ID based on the platform
        /// </summary>
        public string GetBannerID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return Container.AndroidBannerID;
#elif UNITY_IOS
            return Container.IOSBannerID;
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        public void Unload()
        {
            if (interstitialAd != null)
            {
                interstitialAd.OnAdLoaded -= InterstitialOnAdLoadedEvent;
                interstitialAd.OnAdLoadFailed -= InterstitialOnAdLoadFailedEvent;
                interstitialAd.OnAdDisplayed -= InterstitialOnAdDisplayedEvent;
                interstitialAd.OnAdDisplayFailed -= InterstitialOnAdDisplayFailedEvent;
                interstitialAd.OnAdClosed -= InterstitialOnAdClosedEvent;
                interstitialAd = null;
            }

            if (rewardedVideoAd != null)
            {
                rewardedVideoAd.OnAdDisplayed -= RewardedVideoOnAdOpenedEvent;
                rewardedVideoAd.OnAdDisplayFailed -= RewardedVideoOnAdShowFailedEvent;
                rewardedVideoAd.OnAdRewarded -= RewardedVideoOnAdRewardedEvent;
                rewardedVideoAd.OnAdClosed -= RewardedVideoOnAdClosedEvent;
                rewardedVideoAd = null;
            }

            if (bannerAd != null)
            {
                bannerAd.OnAdLoaded -= BannerOnAdLoadedEvent;
                bannerAd.OnAdDisplayed -= BannerOnAdDisplayedEvent;
                bannerAd.DestroyAd();
                bannerAd = null;
            }
        }

        public void OpenTestSuite()
        {
            LevelPlay.LaunchTestSuite();
        }

        public string GetAppKey()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return Container.AndroidAppKey;
#elif UNITY_IOS
            return Container.IOSAppKey;
#else
            return "unexpected_platform";
#endif
        }
    }
#endif
}