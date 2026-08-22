using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Concrete <see cref="UIPage"/> that implements <see cref="IRewardsPopup"/> and displays a
    /// grid of reward previews with fade-in/scale animations.
    /// Registers itself with the <see cref="RewardsPopup"/> static facade on <see cref="Init"/> so that
    /// <see cref="RewardsHolder"/> instances remain decoupled from this specific UIPage class.
    /// Grid cell size adapts automatically: 300×300 for ≤6 items, 200×200 for larger sets.
    /// </summary>
    public class UIRewardsPopup : UIPage, IRewardsPopup
    {
        public override bool IsPopup => true;
        [SerializeField] Image backgroundImage;
        [SerializeField] RectTransform rewardsContainerTransform;
        [SerializeField] GridLayoutGroup rewardsGridLayoutGroup;
        [SerializeField] GameObject rewardUIPrefab;
        [SerializeField] TMP_Text tapToClaimText;

        [Space]
        [SerializeField] Sprite defaultRewardSprite;

        private SimpleCallback closeCallback;
        private RewardUIData[] rewards;

        private TweenCase fadeTweenCase;

        private float rewardItemScale = 1f;

        public override void Init()
        {
            RewardsPopup.Register(this);

            backgroundImage.AddEvent(UnityEngine.EventSystems.EventTriggerType.PointerUp, (data) => OnCloseButtonClicked());
        }

        public bool Display(List<IRewardPreview> previews, SimpleCallback closeCallback)
        {
            this.closeCallback = closeCallback;

            RewardUIData[] oldRewards = rewards;
            if (!oldRewards.IsNullOrEmpty())
            {
                foreach (RewardUIData reward in oldRewards)
                    reward?.Destroy();
            }

            previews = previews.OrderBy(x => x.SortingOrder).ToList();

            RewardUIData[] newRewards = new RewardUIData[previews.Count];
            for (int i = 0; i < newRewards.Length; i++)
            {
                IRewardPreview preview = previews[i];

                GameObject uiPrefab = preview.GetCustomUIPrefab();
                if (uiPrefab == null)
                    uiPrefab = rewardUIPrefab;

                GameObject uiObject = Instantiate(uiPrefab, rewardsContainerTransform);
                uiObject.transform.ResetLocal();

                UIRewardPreviewItem previewItem = uiObject.GetComponent<UIRewardPreviewItem>();
                previewItem.Init(preview, defaultRewardSprite);

                newRewards[i] = new RewardUIData(previewItem, preview);
            }

            rewards = newRewards;

            UIController.ShowPage<UIRewardsPopup>();

            return true;
        }

        protected override void OnShow()
        {
            float appearanceDelay = 0.1f;

            if (!rewards.IsNullOrEmpty())
            {
                UpdateDynamicSize();

                for (int i = 0; i < rewards.Length; i++)
                {
                    RewardUIData reward = rewards[i];

                    UIRewardPreviewItem item = reward.Item;
                    item.transform.localScale = Vector3.one * rewardItemScale;

                    item.CanvasGroup.alpha = 0.0f;

                    reward.FadeTweenCase = item.CanvasGroup.DOFade(1.0f, 0.45f, appearanceDelay * (i + 1), unscaledTime: true);

                    reward.ScaleTweenCase = item.Image.DOScale(1.1f, 0.15f, appearanceDelay * (i + 1), unscaledTime: true).SetEasing(Ease.Type.SineOut).OnComplete(() =>
                    {
                        reward.ScaleTweenCase = item.Image.DOScale(0.95f, 0.2f, unscaledTime: true).OnComplete(() =>
                        {
                            reward.ScaleTweenCase = item.Image.DOScale(1f, 0.1f, unscaledTime: true).SetEasing(Ease.Type.SineOut);
                        });
                    });
                }
            }

            float tapTextDelay = rewards.IsNullOrEmpty() ? 0f : appearanceDelay * rewards.Length + 0.5f;

            tapToClaimText.color = tapToClaimText.color.SetAlpha(0f);
            fadeTweenCase = tapToClaimText.DOFade(1f, 0.2f, tapTextDelay, unscaledTime: true).SetEasing(Ease.Type.CubicInOut);

            NotifyOpened();
        }

        public void UpdateDynamicSize()
        {
            if (rewards.Length <= 6)
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

        protected override void OnHide()
        {
            float animationDuration = 0f;

            if (!rewards.IsNullOrEmpty())
            {
                animationDuration = 0.3f;

                for (int i = 0; i < rewards.Length; i++)
                {
                    RewardUIData reward = rewards[i];

                    UIRewardPreviewItem item = reward.Item;
                    item.CanvasGroup.alpha = 1f;

                    reward.FadeTweenCase = item.CanvasGroup.DOFade(0f, 0.28f, unscaledTime: true);

                    reward.ScaleTweenCase = item.Image.DOScaleY(1.1f, 0.05f, 0.1f, unscaledTime: true).SetEasing(Ease.Type.SineIn).OnComplete(() =>
                    {
                        reward.ScaleTweenCase = item.Image.DOScaleY(0f, 0.15f, unscaledTime: true).SetEasing(Ease.Type.SineOut);
                    });
                }
            }

            Tween.DelayedCall(animationDuration, () =>
            {
                closeCallback?.Invoke();
                NotifyClosed();
            }, unscaledTime: true);
        }

        private void OnDestroy()
        {
            fadeTweenCase.KillActive();

            if (!rewards.IsNullOrEmpty())
            {
                foreach (RewardUIData reward in rewards)
                    reward?.Destroy();
            }
        }

        private void OnCloseButtonClicked()
        {
            UIController.HidePage(this);
        }

        public class RewardUIData
        {
            public readonly UIRewardPreviewItem Item;
            public readonly IRewardPreview Preview;

            public TweenCase FadeTweenCase;
            public TweenCase ScaleTweenCase;

            public RewardUIData(UIRewardPreviewItem item, IRewardPreview preview)
            {
                Item = item;
                Preview = preview;
            }

            public void Destroy()
            {
                if (Item != null && Item.gameObject != null)
                    GameObject.Destroy(Item.gameObject);

                FadeTweenCase.KillActive();
                ScaleTweenCase.KillActive();
            }
        }
    }
}
