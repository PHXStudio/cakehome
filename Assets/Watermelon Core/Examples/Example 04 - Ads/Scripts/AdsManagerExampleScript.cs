#pragma warning disable 0649

using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class AdsManagerExampleScript : MonoBehaviour
    {
        [BoxGroup("Log")]
        [SerializeField] GameObject logPanelObject;
        [BoxGroup("Log")]
        [SerializeField] Button logOpenButton;
        [BoxGroup("Log")]
        [SerializeField] Button logCloseButton;
        [BoxGroup("Log")]
        [SerializeField] Text logText;

        [BoxGroup("LevelPlay")]
        [SerializeField] GameObject levelPlayObject;
        [BoxGroup("LevelPlay")]
        [SerializeField] Button levelPlayTestSuiteButton;

        [BoxGroup("Banner")]
        [SerializeField] Text bannerTitleText;
        [BoxGroup("Banner")]
        [SerializeField] Button[] bannerButtons;

        [BoxGroup("Interstitial")]
        [SerializeField] Text interstitialTitleText;
        [BoxGroup("Interstitial")]
        [SerializeField] Button[] interstitialButtons;

        [BoxGroup("RV")]
        [SerializeField] Text rewardVideoTitleText;
        [BoxGroup("RV")]
        [SerializeField] Button[] rewardVideoButtons;

        private AdsSettings settings;

        private void Awake()
        {
            logOpenButton.onClick.AddListener(() => OnLogOpenButtonClicked());
            logCloseButton.onClick.AddListener(() => OnLogCloseButtonClicked());

#if MODULE_LEVELPLAY
            var activeProvider = AdsManager.GetActiveProvider();
            if (activeProvider is LevelPlayHandler levelPlayHandler)
            {
                levelPlayObject.SetActive(true);
                levelPlayTestSuiteButton.onClick.AddListener(() => levelPlayHandler.OpenTestSuite());
            }
            else
            {
                levelPlayObject.SetActive(false);
            }
#else
            levelPlayObject.SetActive(false);
#endif

            Application.logMessageReceived += Log;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= Log;
        }

        private void Start()
        {
            if (AdsManager.Settings == null) return;

            settings = AdsManager.Settings;

            logText.text = string.Empty;

            bannerTitleText.text = string.Format("Banner ({0})", settings.ActiveProvider);
            if (!settings.BannerEnabled)
            {
                for (int i = 0; i < bannerButtons.Length; i++)
                    bannerButtons[i].interactable = false;
            }

            interstitialTitleText.text = string.Format("Interstitial ({0})", settings.ActiveProvider);
            if (!settings.InterstitialEnabled)
            {
                for (int i = 0; i < interstitialButtons.Length; i++)
                    interstitialButtons[i].interactable = false;
            }

            rewardVideoTitleText.text = string.Format("Rewarded Video ({0})", settings.ActiveProvider);
            if (!settings.RewardedVideoEnabled)
            {
                for (int i = 0; i < rewardVideoButtons.Length; i++)
                    rewardVideoButtons[i].interactable = false;
            }

            GameLoading.MarkAsReadyToHide();
        }

        #region Log
        private void Log(string condition, string stackTrace, LogType type)
        {
            if (logText != null)
                logText.text = logText.text.Insert(0, condition + "\n");
        }

        private void Log(string condition)
        {
            if (logText != null)
                logText.text = logText.text.Insert(0, condition + "\n");
        }

        public void OnLogOpenButtonClicked()
        {
            logPanelObject.SetActive(true);
        }

        public void OnLogCloseButtonClicked()
        {
            logPanelObject.SetActive(false);
        }
        #endregion

        #region Buttons
        public void ShowBannerButton()
        {
            AdsManager.ShowBanner();
        }

        public void HideBannerButton()
        {
            AdsManager.HideBanner();
        }

        public void DestroyBannerButton()
        {
            AdsManager.DestroyBanner();
        }

        public void InterstitialStatusButton()
        {
            string message = "Interstitial " + (AdsManager.IsInterstitialLoaded() ? "is loaded" : "isn't loaded");
            SystemMessage.ShowMessage(message, 5.0f);
            Log("[AdsManager]: " + message);
        }

        public void RequestInterstitialButton()
        {
            AdsManager.RequestInterstitial();
        }

        public void ShowInterstitialButton()
        {
            AdsManager.ShowInterstitial((isDisplayed) =>
            {
                Debug.Log("[AdsManager]: Interstitial " + (isDisplayed ? "is" : "isn't") + " displayed!");
            }, "Default", true);
        }

        public void RewardedVideoStatusButton()
        {
            string message = "RV " + (AdsManager.IsRewardBasedVideoLoaded() ? "is loaded" : "isn't loaded");
            SystemMessage.ShowMessage(message, 5.0f);
            Log("[AdsManager]: " + message);
        }

        public void RequestRewardedVideoButton()
        {
            AdsManager.RequestRewardBasedVideo();
        }

        public void ShowRewardedVideoButton()
        {
            AdsManager.ShowRewardBasedVideo((hasReward) =>
            {
                if (hasReward)
                    Log("[AdsManager]: Reward is received");
                else
                    Log("[AdsManager]: Reward isn't received");
            });
        }
        #endregion
    }
}
