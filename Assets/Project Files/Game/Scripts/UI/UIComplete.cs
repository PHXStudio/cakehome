using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Collections.Generic;
using TMPro;

namespace Watermelon
{
    public class UIComplete : UIPage
    {
        [SerializeField] RectTransform safeAreaTransform;

        [Space]
        [SerializeField] UIFadeAnimation backgroundFade;
        [SerializeField] UIScaleAnimation levelCompleteLabel;

        [Space]
        [SerializeField] UIScaleAnimation rewardLabel;
        [SerializeField] TextMeshProUGUI rewardAmountText;

        [Header("Coins Label")]
        [SerializeField] UIScaleAnimation coinsPanelScalable;
        [SerializeField] CurrencyUIPanelSimple coinsPanelUI;

        [Header("Buttons")]
        [SerializeField] UIFadeAnimation multiplyRewardButtonFade;
        [SerializeField] UIScaleAnimation homeButtonScaleAnimation;
        [SerializeField] UIScaleAnimation nextLevelButtonScaleAnimation;
        [SerializeField] Button multiplyRewardButton;
        [SerializeField] Button homeButton;
        [SerializeField] Button nextLevelButton;


        private TweenCase noThanksAppearTween;

        private int coinsHash = "Coins".GetHashCode();
        private int currentReward;

        public override void Init()
        {
            multiplyRewardButton.onClick.AddListener(MultiplyRewardButton);
            homeButton.onClick.AddListener(HomeButton);
            nextLevelButton.onClick.AddListener(NextLevelButton);

            coinsPanelUI.Init();

            NotchSaveArea.RegisterRectTransform(safeAreaTransform);
        }

        #region Show/Hide
        public override void PlayShowAnimation()
        {
            rewardLabel.Hide(immediately: true);
            multiplyRewardButtonFade.Hide(immediately: true);
            multiplyRewardButton.interactable = false;
            nextLevelButtonScaleAnimation.Hide(immediately: true);
            nextLevelButton.interactable = false;
            homeButtonScaleAnimation.Hide(immediately: true);
            homeButton.interactable = false;
            coinsPanelScalable.Hide(immediately: true);


            backgroundFade.Show(duration: 0.3f);
            levelCompleteLabel.Show();

            coinsPanelScalable.Show();

            currentReward = LevelController.CurrentReward;

            ShowRewardLabel(currentReward, false, 0.3f, delegate
            {
                rewardLabel.Transform.DOPushScale(Vector3.one * 1.1f, Vector3.one, 0.2f, 0.2f).OnComplete(delegate
                {
                    FloatingCloud.SpawnCurrency(coinsHash, (RectTransform)rewardLabel.Transform, (RectTransform)coinsPanelScalable.Transform, 10, "", () =>
                    {
                        CurrencyController.Add(CurrencyType.Coins, currentReward);

                        multiplyRewardButtonFade.Show();
                        multiplyRewardButton.interactable = true;

                        homeButtonScaleAnimation.Show(1.05f, 0.25f, 1f);
                        nextLevelButtonScaleAnimation.Show(1.05f, 0.25f, 1f);

                        homeButton.interactable = true;
                        nextLevelButton.interactable = true;
                    });
                });
            });
        }

        public override void PlayHideAnimation()
        {
            if (!isPageDisplayed)
                return;

            backgroundFade.Hide(0.25f);
            coinsPanelScalable.Hide();

            Tween.DelayedCall(0.25f, delegate
            {
                canvas.enabled = false;
                isPageDisplayed = false;

                UIController.OnPageClosed(this);
            });
        }


        #endregion

        #region RewardLabel

        public void ShowRewardLabel(float rewardAmounts, bool immediately = false, float duration = 0.3f, Action onComplted = null)
        {
            rewardLabel.Show(immediately: immediately);

            if (immediately)
            {
                rewardAmountText.text = "+" + rewardAmounts;
                onComplted?.Invoke();

                return;
            }

            rewardAmountText.text = "+" + 0;

            Tween.DoFloat(0, rewardAmounts, duration, (float value) =>
            {

                rewardAmountText.text = "+" + (int)value;
            }).OnComplete(delegate
            {

                onComplted?.Invoke();
            });
        }

        #endregion

        #region Buttons

        public void MultiplyRewardButton()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            if (noThanksAppearTween != null && noThanksAppearTween.IsActive)
            {
                noThanksAppearTween.Kill();
            }

            homeButton.interactable = false;
            nextLevelButton.interactable = false;

            AdsManager.ShowRewardBasedVideo((bool success) =>
            {
                if (success)
                {
                    int rewardMult = 3;

                    multiplyRewardButtonFade.Hide(immediately: true);
                    multiplyRewardButton.interactable = false;

                    ShowRewardLabel(currentReward * rewardMult, false, 0.3f, delegate
                    {
                        FloatingCloud.SpawnCurrency(coinsHash, (RectTransform)rewardLabel.Transform, (RectTransform)coinsPanelScalable.Transform, 10, "", () =>
                        {
                            CurrencyController.Add(CurrencyType.Coins, currentReward * rewardMult);

                            homeButton.interactable = true;
                            nextLevelButton.interactable = true;
                        });
                    });
                }
                else
                {
                    NextLevelButton();
                }
            });
        }

        public void NextLevelButton()
        {
            if(!GameController.Data.InfiniteLevels && LevelController.MaxReachedLevelIndex >= LevelController.Database.AmountOfLevels)
            {
                LevelController.ClampMaxReachedLevel();
                HomeButton();
            }
            else
            {
                AudioController.PlaySound(AudioController.AudioClips.buttonSound);

                UIController.HidePage<UIComplete>(() =>
                {
                    GameController.LoadNextLevel();
                });
            }
        }

        public void HomeButton()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            UIController.HidePage<UIComplete>(() =>
            {
                GameController.ReturnToMenu();
            });

            LivesSystem.UnlockLife(false);
        }

        #endregion
    }
}
