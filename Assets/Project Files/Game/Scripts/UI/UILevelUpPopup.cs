using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Full-screen congratulation popup shown when the player levels up via <see cref="ExperienceController"/>.
    /// Displays the new level and the level's reward grid (mirrors <see cref="UIRewardsPopup"/>'s rendering).
    /// If a big XP grant crosses several levels at once, level-ups are queued and shown one at a time.
    /// On close, Coins/Energy rewards fly into the HUD via <see cref="CurrencyCloud"/> and are
    /// credited to the player the moment the cloud lands (see <see cref="SpawnRewardClouds"/>).
    /// </summary>
    public class UILevelUpPopup : UIPage
    {
        public override bool IsPopup => true;

        [SerializeField] Image backgroundImage;
        [SerializeField] Image headerImage;
        [SerializeField] Image levelBackImage;
        [SerializeField] Image levelBadgeImage;
        [SerializeField] TMP_Text levelText;
        [SerializeField] ParticleSystem levelUpParticle;
        [SerializeField] RectTransform rewardsContainerTransform;
        [SerializeField] GridLayoutGroup rewardsGridLayoutGroup;
        [SerializeField] GameObject rewardUIPrefab;
        [SerializeField] TMP_Text tapToCloseText;

        [Space]
        [SerializeField] Sprite defaultRewardSprite;

        private readonly Queue<PendingLevelUp> pendingLevelUps = new Queue<PendingLevelUp>();

        // Non-null while this popup holds its turn on UIQueueController — call it once all
        // pending level-ups have been shown to let the next queued window (dialog, etc.) run.
        private Action releaseQueueTurn;

        private int currentLevel;
        private readonly List<RewardEntry> currentEntries = new List<RewardEntry>();

        private float rewardItemScale = 1f;

        private float backgroundOriginalAlpha;
        private Vector3 headerImageOriginalScale;
        private Vector3 levelBackImageOriginalScale;
        private Vector2 levelBadgeOriginalAnchoredPosition;

        public override void Init()
        {
            backgroundImage.AddEvent(UnityEngine.EventSystems.EventTriggerType.PointerUp, (data) => OnCloseButtonClicked());

            backgroundOriginalAlpha = backgroundImage.color.a;
            headerImageOriginalScale = headerImage.transform.localScale;
            levelBackImageOriginalScale = levelBackImage.transform.localScale;
            levelBadgeOriginalAnchoredPosition = levelBadgeImage.rectTransform.anchoredPosition;

            levelUpParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ExperienceController.OnLevelUp += OnLevelUp;
        }

        private void OnDestroy()
        {
            ExperienceController.OnLevelUp -= OnLevelUp;
        }

        private void OnLevelUp(int level, RewardBundle bundle)
        {
            pendingLevelUps.Enqueue(new PendingLevelUp(level, bundle));

            // Ask for a turn only once per batch — while we already hold one (releaseQueueTurn
            // set), newly queued level-ups are picked up as ShowNextInQueue loops through them.
            if (releaseQueueTurn == null)
                UIQueueController.Enqueue(UIQueuePriority.LevelUp, onDone =>
                {
                    releaseQueueTurn = onDone;
                    ShowNextInQueue();
                });
        }

        private void ShowNextInQueue()
        {
            if (pendingLevelUps.Count == 0)
            {
                releaseQueueTurn?.Invoke();
                releaseQueueTurn = null;
                return;
            }

            PendingLevelUp next = pendingLevelUps.Dequeue();
            currentLevel = next.Level;

            BuildRewardsGrid(next.Bundle);

            UIController.ShowPage<UILevelUpPopup>();
        }

        private void BuildRewardsGrid(RewardBundle bundle)
        {
            ClearRewardsGrid();

            if (bundle != null)
            {
                foreach (Reward reward in bundle.GetRewards())
                {
                    List<IRewardPreview> previews = reward.GetRewardPreviews();
                    if (previews.IsNullOrEmpty()) continue;

                    foreach (IRewardPreview preview in previews)
                    {
                        GameObject uiPrefab = preview.GetCustomUIPrefab();
                        if (uiPrefab == null)
                            uiPrefab = rewardUIPrefab;

                        GameObject uiObject = Instantiate(uiPrefab, rewardsContainerTransform);
                        uiObject.transform.ResetLocal();

                        UIRewardPreviewItem previewItem = uiObject.GetComponent<UIRewardPreviewItem>();
                        previewItem.Init(preview, defaultRewardSprite);

                        currentEntries.Add(new RewardEntry(reward, previewItem));
                    }
                }
            }

            UpdateDynamicSize();
        }

        private void UpdateDynamicSize()
        {
            if (currentEntries.Count <= 6)
            {
                rewardsGridLayoutGroup.cellSize = new Vector2(300f, 300f);
                rewardItemScale = 1f;
            }
            else
            {
                rewardsGridLayoutGroup.cellSize = new Vector2(200f, 200f);
                rewardItemScale = 0.8f;
            }
        }

        protected override void OnShow()
        {
            AudioController.PlaySound(AudioController.GetClip("level_up"));

            levelText.text = currentLevel.ToString();

            const float backgroundFadeDuration = 0.25f;
            const float headerAppearDelay = 0.12f;
            const float headerAppearDuration = 0.25f;
            const float badgeAppearDelay = headerAppearDelay + headerAppearDuration;
            const float badgeFlyDuration = 0.35f;
            const float badgeFadeDuration = 0.2f;
            const float badgeFlyOffset = 80f;

            float sequenceStartDelay = badgeAppearDelay + badgeFlyDuration;

            // 1. Background fades in from full transparency
            backgroundImage.color = backgroundImage.color.SetAlpha(0f);
            backgroundImage.DOFade(backgroundOriginalAlpha, backgroundFadeDuration, unscaledTime: true);

            // 2. Header & level back quickly bounce-fade in
            headerImage.transform.localScale = Vector3.zero;
            headerImage.color = headerImage.color.SetAlpha(0f);
            headerImage.DOScale(headerImageOriginalScale, headerAppearDuration, headerAppearDelay, unscaledTime: true).SetEasing(Ease.Type.BackOut);
            headerImage.DOFade(1f, headerAppearDuration, headerAppearDelay, unscaledTime: true);

            levelBackImage.transform.localScale = Vector3.zero;
            levelBackImage.color = levelBackImage.color.SetAlpha(0f);
            levelBackImage.DOScale(levelBackImageOriginalScale, headerAppearDuration, headerAppearDelay, unscaledTime: true).SetEasing(Ease.Type.BackOut);
            levelBackImage.DOFade(1f, headerAppearDuration, headerAppearDelay, unscaledTime: true);

            // 3. Level badge flies in from slightly below with a fade, then bounces to a stop
            levelBadgeImage.rectTransform.anchoredPosition = levelBadgeOriginalAnchoredPosition - new Vector2(0f, badgeFlyOffset);
            levelBadgeImage.color = levelBadgeImage.color.SetAlpha(0f);
            levelBadgeImage.DOFade(1f, badgeFadeDuration, badgeAppearDelay, unscaledTime: true);
            levelBadgeImage.DOAnchoredPosition(levelBadgeOriginalAnchoredPosition, badgeFlyDuration, badgeAppearDelay, unscaledTime: true).SetEasing(Ease.Type.BackOut);

            // 4. Particle fires the moment the badge lands
            Tween.DelayedCall(sequenceStartDelay, () =>
            {
                levelUpParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                levelUpParticle.Play();

                AudioController.PlaySound(AudioController.GetClip("level_sparks"));
            }, unscaledTime: true);

            // 5. Rewards spawn right after
            float appearanceDelay = 0.1f;

            for (int i = 0; i < currentEntries.Count; i++)
            {
                UIRewardPreviewItem item = currentEntries[i].Item;

                item.transform.localScale = Vector3.one * rewardItemScale;
                item.CanvasGroup.alpha = 0f;

                float itemDelay = sequenceStartDelay + appearanceDelay * (i + 1);

                item.CanvasGroup.DOFade(1f, 0.45f, itemDelay, unscaledTime: true);

                item.Image.DOScale(1.1f, 0.15f, itemDelay, unscaledTime: true).SetEasing(Ease.Type.SineOut).OnComplete(() =>
                {
                    item.Image.DOScale(0.95f, 0.2f, unscaledTime: true).OnComplete(() =>
                    {
                        item.Image.DOScale(1f, 0.1f, unscaledTime: true).SetEasing(Ease.Type.SineOut);
                    });
                });
            }

            // 6. Tap-to-close text fades in last
            float tapTextDelay = sequenceStartDelay + (currentEntries.Count == 0 ? 0.2f : appearanceDelay * currentEntries.Count + 0.5f);

            tapToCloseText.color = tapToCloseText.color.SetAlpha(0f);
            tapToCloseText.DOFade(1f, 0.2f, tapTextDelay, unscaledTime: true).SetEasing(Ease.Type.CubicInOut);

            NotifyOpened();
        }

        protected override void OnHide()
        {
            float animationDuration = currentEntries.Count == 0 ? 0f : 0.3f;

            for (int i = 0; i < currentEntries.Count; i++)
            {
                UIRewardPreviewItem item = currentEntries[i].Item;

                item.CanvasGroup.alpha = 1f;
                item.CanvasGroup.DOFade(0f, 0.28f, unscaledTime: true);

                item.Image.DOScaleY(1.1f, 0.05f, 0.1f, unscaledTime: true).SetEasing(Ease.Type.SineIn).OnComplete(() =>
                {
                    item.Image.DOScaleY(0f, 0.15f, unscaledTime: true).SetEasing(Ease.Type.SineOut);
                });
            }

            Tween.DelayedCall(animationDuration, () =>
            {
                SpawnRewardClouds();
                ClearRewardsGrid();

                NotifyClosed();

                ShowNextInQueue();
            }, unscaledTime: true);
        }

        // Currency/Energy rewards aren't applied yet at this point (ExperienceController skips
        // them) — credit happens here, inside each fly-cloud's onDone, so the HUD number changes
        // the moment the cloud lands instead of jumping before the animation starts.
        private void SpawnRewardClouds()
        {
            UIHeader headerPage = UIController.GetPage<UIHeader>();

            foreach (RewardEntry entry in currentEntries)
            {
                RectTransform source = (RectTransform)entry.Item.transform;

                if (entry.Reward is CurrencyReward currencyReward)
                {
                    int coinsAmount = currencyReward.GetAmount(CurrencyType.Coins);
                    if (coinsAmount > 0 && headerPage?.CoinsPanel != null)
                        RewardFlyController.FlyCurrency(CurrencyType.Coins, source, onDone: () => currencyReward.ApplyReward());
                    else
                        currencyReward.ApplyReward();
                }
                else if (entry.Reward is EnergyReward energyReward)
                {
                    if (headerPage?.EnergyPanel != null)
                        RewardFlyController.FlyEnergy(source, onDone: () => energyReward.ApplyReward());
                    else
                        energyReward.ApplyReward();
                }
            }
        }

        private void ClearRewardsGrid()
        {
            foreach (RewardEntry entry in currentEntries)
            {
                if (entry.Item != null && entry.Item.gameObject != null)
                    Destroy(entry.Item.gameObject);
            }

            currentEntries.Clear();
        }

        private void OnCloseButtonClicked()
        {
            UIController.HidePage(this);
        }

        private readonly struct PendingLevelUp
        {
            public readonly int Level;
            public readonly RewardBundle Bundle;

            public PendingLevelUp(int level, RewardBundle bundle)
            {
                Level = level;
                Bundle = bundle;
            }
        }

        private class RewardEntry
        {
            public readonly Reward Reward;
            public readonly UIRewardPreviewItem Item;

            public RewardEntry(Reward reward, UIRewardPreviewItem item)
            {
                Reward = reward;
                Item = item;
            }
        }
    }
}
