#pragma warning disable 0414

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Watermelon
{
    [HelpURL("https://www.notion.so/wmelongames/Advertisement-221053e32d4047bb880275027daba9f0?pvs=4")]
    public class AdsSettings : ScriptableObject
    {
        [BoxGroup("Advertisement", "Advertisement")]
        [SerializeField, Hide] string activeProvider = "Dummy";
        public string ActiveProvider => activeProvider;

        [BoxGroup("Settings", "Settings")]
        [SerializeField] bool debugMode = false;
        public bool DebugMode => debugMode;

        [BoxGroup("Settings")]
        [ShowIf("debugMode")]
        [SerializeField] List<string> testDevices;
        public List<string> TestDevices => testDevices;

        [BoxGroup("Settings")]
        [SerializeField] bool loadAdsOnStart = true;
        public bool LoadAdsOnStart => loadAdsOnStart;

        [BoxGroup("Settings/Ad Types")]
        [SerializeField] bool bannerEnabled = true;
        public bool BannerEnabled => bannerEnabled;

        [BoxGroup("Settings/Ad Types")]
        [SerializeField] bool interstitialEnabled = true;
        public bool InterstitialEnabled => interstitialEnabled;

        [BoxGroup("Settings/Ad Types")]
        [SerializeField] bool rewardedVideoEnabled = true;
        public bool RewardedVideoEnabled => rewardedVideoEnabled;

        [BoxGroup("Reward", "Reward")]
        [SerializeField] Sprite noAdsRewardSprite;
        public Sprite NoAdsRewardSprite => noAdsRewardSprite;

        [Space]
        [BoxGroup("Settings/Interstitial")]
        [Tooltip("Delay in seconds before interstitial appearings on first game launch.")]
        [SerializeField] float interstitialFirstStartDelay = 40f;
        public float InterstitialFirstStartDelay => interstitialFirstStartDelay;

        [BoxGroup("Settings/Interstitial")]
        [Tooltip("Delay in seconds before interstitial appearings.")]
        [SerializeField] float interstitialStartDelay = 40f;
        public float InterstitialStartDelay => interstitialStartDelay;

        [BoxGroup("Settings/Interstitial")]
        [Tooltip("Delay in seconds between interstitial appearings.")]
        [SerializeField] float interstitialShowingDelay = 30f;
        public float InterstitialShowingDelay => interstitialShowingDelay;

        [BoxGroup("Settings/Delay")]
        [SerializeField] float loadingAdDuration = 0f;
        public float LoadingAdDuration => loadingAdDuration;

        [BoxGroup("Settings/Delay")]
        [SerializeField] string loadingMessage = "Ad is loading..";
        public string LoadingMessage => loadingMessage;

        [SerializeReference, Hide]
        private List<AdsProviderContainer> providerContainers = new List<AdsProviderContainer>();

        public T GetContainer<T>() where T : AdsProviderContainer
            => providerContainers.OfType<T>().FirstOrDefault();

        public AdsProviderContainer GetContainer(string providerName)
            => providerContainers.FirstOrDefault(c => c != null && c.ProviderName == providerName);

        public bool HasContainer(Type containerType)
            => providerContainers.Any(c => c != null && c.GetType() == containerType);

        public void AddContainer(AdsProviderContainer container)
        {
            providerContainers.Add(container);
        }

        public void DisableBanner() => bannerEnabled = false;
        public void DisableInterstitial() => interstitialEnabled = false;
        public void DisableRewardedVideo() => rewardedVideoEnabled = false;

        [BoxGroup("Legal", "Legal")]
        [SerializeField] string privacyLink = "";
        public string PrivacyLink => privacyLink;

        [BoxGroup("Legal")]
        [SerializeField] string termsOfUseLink = "";
        public string TermsOfUseLink => termsOfUseLink;
    }
}
