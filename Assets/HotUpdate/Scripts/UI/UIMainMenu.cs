using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.IAPStore;
using Watermelon.Map;

namespace Watermelon
{
    public class UIMainMenu : UIPage
    {
        public readonly float STORE_AD_RIGHT_OFFSET_X = 300F;

        [SerializeField] RectTransform safeAreaRectTransform;

        [Space]
        [SerializeField] RectTransform tapToPlayRect;
        [SerializeField] Button playButton;
        [SerializeField] TMP_Text playButtonText;

        [Space]
        [SerializeField] UIScaleAnimation coinsLabelScalable;
        [SerializeField] CurrencyUIPanelSimple coinsPanel;
        [SerializeField] UIScaleAnimation livesIndicatorScalable;

        [Space]
        [SerializeField] UIMainMenuButton iapStoreButton;
        [SerializeField] UIMainMenuButton noAdsButton;

        [Space]
        [SerializeField] UINoAdsPopUp noAdsPopUp;

        private TweenCase tapToPlayPingPong;
        private TweenCase showHideStoreAdButtonDelayTweenCase;

        private void OnEnable()
        {
            IAPManager.PurchaseCompleted += OnAdPurchased;
        }

        private void OnDisable()
        {
            IAPManager.PurchaseCompleted -= OnAdPurchased;
        }

        public override void Init()
        {
            coinsPanel.Init();

            noAdsPopUp.Init();

            iapStoreButton.Init(STORE_AD_RIGHT_OFFSET_X);
            noAdsButton.Init(STORE_AD_RIGHT_OFFSET_X);

            iapStoreButton.Button.onClick.AddListener(IAPStoreButton);
            noAdsButton.Button.onClick.AddListener(NoAdButton);
            coinsPanel.AddButton.onClick.AddListener(AddCoinsButton);
            playButton.onClick.AddListener(PlayButton);

            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
            ApplyBottomNavPadding();
        }

        private void ApplyBottomNavPadding()
        {
            if (safeAreaRectTransform == null)
                return;

            Vector2 offsetMin = safeAreaRectTransform.offsetMin;
            offsetMin.y = Mathf.Max(offsetMin.y, UIBottomNavBar.NavHeight);
            safeAreaRectTransform.offsetMin = offsetMin;
        }

        #region Show/Hide

        public override void PlayShowAnimation()
        {
            ApplyBottomNavPadding();

            showHideStoreAdButtonDelayTweenCase?.Kill();

            HideAdButton(true);
            iapStoreButton.Hide(true);
            ShowTapToPlay();

            coinsLabelScalable.Show();
            livesIndicatorScalable.Show();

            UILevelNumberText.Show();
            playButtonText.text = "LEVEL " + (LevelController.MaxReachedLevelIndex + 1);

            showHideStoreAdButtonDelayTweenCase = Tween.DelayedCall(0.12f, delegate
            {
                ShowAdButton();
                iapStoreButton.Show();
            });

            MapLevelAbstractBehavior.OnLevelClicked += OnLevelOnMapSelected;

            UIController.OnPageOpened(this);
        }

        public override void PlayHideAnimation()
        {
            showHideStoreAdButtonDelayTweenCase?.Kill();

            HideTapToPlayButton(immediately: true);

            coinsLabelScalable.Hide(immediately: true);
            livesIndicatorScalable.Hide(immediately: true);
            iapStoreButton.Hide(immediately: true);

            HideAdButton(immediately: true);

            MapLevelAbstractBehavior.OnLevelClicked -= OnLevelOnMapSelected;

            UIController.OnPageClosed(this);
        }

        #endregion

        #region Tap To Play Label

        public void ShowTapToPlay(bool immediately = false)
        {
            if (tapToPlayPingPong != null && tapToPlayPingPong.IsActive)
                tapToPlayPingPong.Kill();

            if (immediately)
            {
                tapToPlayRect.localScale = Vector3.one;

                tapToPlayPingPong = tapToPlayRect.transform.DOPingPongScale(1.0f, 1.05f, 0.9f, Ease.Type.QuadIn, Ease.Type.QuadOut, unscaledTime: true);

                return;
            }

            // RESET
            tapToPlayRect.localScale = Vector3.zero;

            tapToPlayRect.DOPushScale(Vector3.one * 1.2f, Vector3.one, 0.35f, 0.2f, Ease.Type.CubicOut, Ease.Type.CubicIn).OnComplete(delegate
            {

                tapToPlayPingPong = tapToPlayRect.transform.DOPingPongScale(1.0f, 1.05f, 0.9f, Ease.Type.QuadIn, Ease.Type.QuadOut, unscaledTime: true);

            });

        }

        public void HideTapToPlayButton(bool immediately = false)
        {
            if (tapToPlayPingPong != null && tapToPlayPingPong.IsActive)
                tapToPlayPingPong.Kill();

            if (immediately)
            {
                tapToPlayRect.localScale = Vector3.zero;

                return;
            }

            tapToPlayRect.DOScale(Vector3.zero, 0.3f).SetEasing(Ease.Type.CubicIn);
        }

        #endregion

        #region Ad Button Label

        private void ShowAdButton(bool immediately = false)
        {
            if (AdsManager.IsForcedAdEnabled())
            {
                noAdsButton.Show(immediately);
            }
            else
            {
                noAdsButton.Hide(immediately: true);
            }
        }

        private void HideAdButton(bool immediately = false)
        {
            noAdsButton.Hide(immediately);
        }

        private void OnAdPurchased(ProductKeyType productKeyType)
        {
            if (productKeyType == ProductKeyType.NoAds)
            {
                HideAdButton(immediately: true);
            }
        }

        #endregion

        #region Buttons

        private void PlayButton()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            OnPlayTriggered(LevelController.MaxReachedLevelIndex);
        }

        private void OnLevelOnMapSelected(int levelId)
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            OnPlayTriggered(levelId);
        }

        private void OnPlayTriggered(int levelId)
        {
            if (LivesSystem.Lives > 0 || LivesSystem.InfiniteMode)
            {
                // start level
                GameController.LoadLevel(levelId);
            }
            else
            {
                UIAddLivesPanel.Show((bool lifeRecieved) =>
                {
                    if (lifeRecieved)
                    {
                        // start level
                        GameController.LoadLevel(levelId);
                    }
                });
            }
        }

        private void IAPStoreButton()
        {
            if (UIController.GetPage<UIStore>().IsPageDisplayed)
                return;

            UILevelNumberText.Hide(true);

            UIController.HidePage<UIMainMenu>();
            UIController.ShowPage<UIStore>();

            // reopening main menu only after store page was opened throug main menu
            UIController.PageClosed += OnIapStoreClosed;
            MapBehavior.DisableScroll();

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }

        private void OnIapStoreClosed(UIPage page, System.Type pageType)
        {
            if (pageType.Equals(typeof(UIStore)))
            {
                UIController.PageClosed -= OnIapStoreClosed;

                MapBehavior.EnableScroll();
                UIController.ShowPage<UIMainMenu>();
            }
        }

        private void NoAdButton()
        {
            noAdsPopUp.Show();

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }

        private void AddCoinsButton()
        {
            IAPStoreButton();

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
        }

        #endregion
    }


}
