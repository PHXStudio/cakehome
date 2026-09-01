
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

        // 表现加成拆分显示（运行时创建，挂在安全区下）
        private TextMeshProUGUI perfBreakdownText;

        public override void Init()
        {
            multiplyRewardButton.onClick.AddListener(MultiplyRewardButton);
            homeButton.onClick.AddListener(HomeButton);
            nextLevelButton.onClick.AddListener(NextLevelButton);

            coinsPanelUI.Init();

            NotchSaveArea.RegisterRectTransform(safeAreaTransform);
        }

        #region Show/Hide
        protected override void OnShow()
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
            levelCompleteLabel.Transform.localScale = Vector3.one * 1.4f;
            levelCompleteLabel.Transform.DOScale(Vector3.one, 0.6f).SetEasing(Ease.Type.ElasticOut);
            levelCompleteLabel.Transform.localScale = Vector3.one * 1.4f;
            levelCompleteLabel.Transform.DOScale(Vector3.one, 0.6f).SetEasing(Ease.Type.ElasticOut);
            levelCompleteLabel.Transform.localScale = Vector3.one * 1.4f;
            levelCompleteLabel.Transform.DOScale(Vector3.one, 0.6f).SetEasing(Ease.Type.ElasticOut);
            levelCompleteLabel.Show();

            coinsPanelScalable.Show();

            currentReward = LevelController.CurrentReward;

            // 表现加成拆分：基础 + 表现（消除棋子数/最高连击）
            CreatePerfBreakdown(LevelController.BaseReward, LevelController.PerformanceReward);

            ShowRewardLabel(currentReward, false, 0.3f, delegate
            {
                rewardLabel.Transform.DOPushScale(Vector3.one * 1.1f, Vector3.one, 0.2f, 0.2f).OnComplete(delegate
                {
                    CurrencyCloud.SpawnCurrency(coinsHash, (RectTransform)rewardLabel.Transform, (RectTransform)coinsPanelScalable.Transform, 10, "", () =>
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

        protected override void OnHide()
        {
            if (!isPageDisplayed)
                return;

            if (perfBreakdownText != null) perfBreakdownText.gameObject.SetActive(false);

            backgroundFade.Hide(0.25f);
            coinsPanelScalable.Hide();

            Tween.DelayedCall(0.25f, delegate
            {
                canvas.enabled = false;
                isPageDisplayed = false;

                NotifyClosed();
            });
        }


        #endregion

        #region PerformanceBreakdown

        private void CreatePerfBreakdown(int baseReward, int perfReward)
        {
            if (perfBreakdownText == null)
            {
                var go = new GameObject("PerfBreakdown", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                perfBreakdownText = go.GetComponent<TextMeshProUGUI>();
                perfBreakdownText.alignment = TextAlignmentOptions.Center;
                perfBreakdownText.fontSize = 30;
                perfBreakdownText.fontStyle = FontStyles.Bold;
                perfBreakdownText.color = new Color(1f, 0.9f, 0.4f);
                perfBreakdownText.outlineWidth = 0.25f;
                perfBreakdownText.outlineColor = new Color(0f, 0f, 0f, 0.6f);

                if (rewardAmountText != null) perfBreakdownText.font = rewardAmountText.font;

                var rt = perfBreakdownText.rectTransform;
                rt.SetParent(safeAreaTransform, false);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, -46f);
                rt.sizeDelta = new Vector2(600f, 42f);
            }

            perfBreakdownText.text = perfReward > 0
                ? $"基础 +{baseReward}    表现 +{perfReward}"
                : $"通关奖励 +{baseReward}";
            perfBreakdownText.gameObject.SetActive(true);
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

            CustomAnalytics.TrackRewardVideo("multiply_reward");

            AdsManager.ShowRewardBasedVideo((bool success) =>
            {
                if (success)
                {
                    int rewardMult = 3;

                    multiplyRewardButtonFade.Hide(immediately: true);
                    multiplyRewardButton.interactable = false;

                    ShowRewardLabel(currentReward * rewardMult, false, 0.3f, delegate
                    {
                        CurrencyCloud.SpawnCurrency(coinsHash, (RectTransform)rewardLabel.Transform, (RectTransform)coinsPanelScalable.Transform, 10, "", () =>
                        {
                            // 基础奖励已在页面展示时发放（OnShow 的 ShowRewardLabel 回调），
                            // 这里只补发倍数差额，否则宣传 x3 实际到账 x4。
                            CurrencyController.Add(CurrencyType.Coins, currentReward * (rewardMult - 1));
                            DailyTaskController.AddProgress(DailyTaskType.RewardedVideos);

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
