using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace Watermelon
{
    [Preserve]
    public abstract class AdProviderHandler
    {
        protected const int RETRY_ATTEMPT_DEFAULT_VALUE = 1;
        protected const int MAX_RETRY_ATTEMPTS = 5;

        protected int interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        protected int rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

        public abstract string ProviderName { get; }

        protected AdsSettings adsSettings;

        public bool IsInitialized { get; private set; }

        public void LinkSettings(AdsSettings adsSettings)
        {
            this.adsSettings = adsSettings;
        }

        public async Task<bool> InitAsync()
        {
            if (IsInitialized)
                return true;

            bool initResult = await InitProviderAsync();

            if (initResult)
            {
                IsInitialized = true;

                AdsManager.OnProviderInitialized(ProviderName);

                LogManager.Log(string.Format("[AdsManager]: {0} is initialized!", ProviderName), LogCategory.Services);

                return true;
            }

            return false;
        }

        protected abstract Task<bool> InitProviderAsync();

        public abstract void ShowBanner();
        public abstract void HideBanner();
        public abstract void DestroyBanner();

        public abstract void RequestInterstitial();
        public abstract void ShowInterstitial(AdvertisementCallback callback);
        public abstract bool IsInterstitialLoaded();

        public abstract void RequestRewardedVideo();
        public abstract void ShowRewardedVideo(AdvertisementCallback callback);
        public abstract bool IsRewardedVideoLoaded();

        public delegate void AdvertisementCallback(bool result);

        protected void HandleAdLoadFailure(AdType adType, string errorMessage, ref int retryAttempt, SimpleCallback retryAction)
        {
            LogManager.LogError($"[AdsManager]: {adType} failed to load with error: {errorMessage}", LogCategory.Services);

            retryAttempt++;
            if (retryAttempt <= MAX_RETRY_ATTEMPTS)
            {
                float retryDelay = Mathf.Pow(2, retryAttempt);
                Tween.DelayedCall(retryDelay, retryAction, true, UpdateMethod.Update);
            }
            else
            {
                Debug.LogError($"[AdsManager]: {adType} failed after {MAX_RETRY_ATTEMPTS} retries.");
            }
        }
    }
}
