using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace Watermelon
{
    [Preserve]
    public class AdDummyHandler : AdProviderHandler
    {
        public override string ProviderName => "Dummy";

        private AdDummyController dummyController;

        private bool isInterstitialLoaded = false;
        private bool isRewardVideoLoaded = false;

        protected override async Task<bool> InitProviderAsync()
        {
            LogManager.Log("[AdsManager/Dummy]: InitProviderAsync called", LogCategory.Services);

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            dummyController = AdDummyController.CreateObject();
            dummyController.Init(adsSettings, ProviderName);

            tcs.SetResult(true);

            return await tcs.Task;
        }

        public override void ShowBanner()
        {
            LogManager.Log($"[AdsManager/Dummy]: ShowBanner called (controller={(dummyController != null ? "OK" : "NULL")})", LogCategory.Services);

            if (dummyController == null) return;

            dummyController.ShowBanner();

            AdsManager.OnProviderAdDisplayed(ProviderName, AdType.Banner);

            AdsManager.SetBannerHeight(50);
        }

        public override void HideBanner()
        {
            LogManager.Log($"[AdsManager/Dummy]: HideBanner called (controller={(dummyController != null ? "OK" : "NULL")})", LogCategory.Services);

            if (dummyController == null) return;

            dummyController.HideBanner();

            AdsManager.OnProviderAdClosed(ProviderName, AdType.Banner);
        }

        public override void DestroyBanner()
        {
            LogManager.Log($"[AdsManager/Dummy]: DestroyBanner called (controller={(dummyController != null ? "OK" : "NULL")})", LogCategory.Services);

            if (dummyController == null) return;

            dummyController.HideBanner();

            AdsManager.OnProviderAdClosed(ProviderName, AdType.Banner);
        }

        public override void RequestInterstitial()
        {
            LogManager.Log("[AdsManager/Dummy]: RequestInterstitial called", LogCategory.Services);

            isInterstitialLoaded = true;

            AdsManager.OnProviderAdLoaded(ProviderName, AdType.Interstitial);
        }

        public override bool IsInterstitialLoaded()
        {
            return isInterstitialLoaded;
        }

        public override void ShowInterstitial(AdvertisementCallback callback)
        {
            LogManager.Log($"[AdsManager/Dummy]: ShowInterstitial called (controller={(dummyController != null ? "OK" : "NULL")}, loaded={isInterstitialLoaded})", LogCategory.Services);

            if (dummyController == null) return;

            dummyController.ShowInterstitial();

            AdsManager.OnProviderAdDisplayed(ProviderName, AdType.Interstitial);
        }

        public override void RequestRewardedVideo()
        {
            LogManager.Log("[AdsManager/Dummy]: RequestRewardedVideo called", LogCategory.Services);

            isRewardVideoLoaded = true;

            AdsManager.OnProviderAdLoaded(ProviderName, AdType.RewardedVideo);
        }

        public override bool IsRewardedVideoLoaded()
        {
            return isRewardVideoLoaded;
        }

        public override void ShowRewardedVideo(AdvertisementCallback callback)
        {
            LogManager.Log($"[AdsManager/Dummy]: ShowRewardedVideo called (controller={(dummyController != null ? "OK" : "NULL")}, loaded={isRewardVideoLoaded})", LogCategory.Services);

            if (dummyController == null) return;

            dummyController.ShowRewardedVideo();

            AdsManager.OnProviderAdDisplayed(ProviderName, AdType.RewardedVideo);
        }
    }
}
