using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// View counterpart of <see cref="PUReward"/>. Lives on a <see cref="RewardsHolder"/> and
    /// mirrors the reward's power-up entries onto prefab UI (icons, amount labels, floating text).
    /// Entries are matched to <see cref="PUReward.PUItem"/> by index.
    /// </summary>
    [Serializable]
    public sealed class PURewardView : RewardView
    {
        [SerializeField] PUViewItem[] powerUps;

        protected override void OnInitialized()
        {
            if (reward is not PUReward puReward) return;
            if (powerUps.IsNullOrEmpty()) return;

            PUReward.PUItem[] items = puReward.PowerUps;
            int count = Mathf.Min(items.Length, powerUps.Length);

            for (int i = 0; i < count; i++)
            {
                PUViewItem viewItem = powerUps[i];
                if (viewItem == null) continue;

                viewItem.Init();

                if (viewItem.IconImage != null)
                {
                    PUBehavior powerUpBehavior = PUController.GetPowerUpBehavior(items[i].PowerUpType);
                    if (powerUpBehavior != null)
                    {
                        viewItem.IconImage.sprite = powerUpBehavior.Settings.Icon;
                    }
                }

                if (viewItem.AmountText != null)
                {
                    viewItem.AmountText.text = string.IsNullOrEmpty(viewItem.TextFormating)
                        ? items[i].Amount.ToString()
                        : string.Format(viewItem.TextFormating, items[i].Amount);
                }
            }
        }

        public override void OnPurchased()
        {
            if (reward is not PUReward puReward) return;
            if (powerUps.IsNullOrEmpty()) return;

            PUReward.PUItem[] items = puReward.PowerUps;
            int count = Mathf.Min(items.Length, powerUps.Length);

            for (int i = 0; i < count; i++)
            {
                TextMeshProUGUI floatingText = powerUps[i]?.PurchaseFloatingText;
                if (floatingText == null) continue;

                floatingText.gameObject.SetActive(true);
                floatingText.text = string.Format("+{0}", items[i].Amount);

                RectTransform textRectTransform = floatingText.rectTransform;
                textRectTransform.anchoredPosition = powerUps[i].FloatingTextPosition;
                floatingText.color = floatingText.color.SetAlpha(1.0f);

                Vector2 startPosition = powerUps[i].FloatingTextPosition;
                textRectTransform.DOAnchoredPosition(startPosition + new Vector2(0, 100), 1.0f).SetEasing(Ease.Type.SineIn);
                floatingText.DOFade(0.0f, 1.0f).SetEasing(Ease.Type.QuintIn).OnComplete(() =>
                {
                    textRectTransform.anchoredPosition = startPosition;
                    floatingText.gameObject.SetActive(false);
                });
            }
        }

        [Serializable]
        public class PUViewItem
        {
            [SerializeField] Image iconImage;
            public Image IconImage => iconImage;

            [SerializeField] TextMeshProUGUI amountText;
            public TextMeshProUGUI AmountText => amountText;

            [SerializeField] TextMeshProUGUI purchaseFloatingText;
            public TextMeshProUGUI PurchaseFloatingText => purchaseFloatingText;

            [SerializeField] string textFormating = "x{0}";
            public string TextFormating => textFormating;

            private Vector2 floatingTextPosition;
            public Vector2 FloatingTextPosition => floatingTextPosition;

            public void Init()
            {
                if (purchaseFloatingText != null)
                {
                    floatingTextPosition = purchaseFloatingText.rectTransform.anchoredPosition;
                }
            }
        }
    }
}
