using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// 每日登录奖励签到面板（7 天循环）。由 DailyRewardUIBuilder 构建。
    /// 主菜单显示时若今日可领则自动弹出。
    /// </summary>
    public class DailyRewardPanel : MonoBehaviour, IPopupWindow
    {
        [SerializeField] UIScaleAnimation panelScalable;
        [SerializeField] Button claimButton;
        [SerializeField] TMP_Text statusText;
        [SerializeField] RectTransform slotsContainer;
        [SerializeField] GameObject slotTemplate;

        private UIFadeAnimation backFade;
        private readonly List<RewardSlot> slots = new List<RewardSlot>();
        private bool isInitialized;
        private SimpleCallback onClosedCallback;

        public bool IsOpened => gameObject.activeSelf;

        private sealed class RewardSlot
        {
            public RectTransform Root;
            public TMP_Text AmountText;
            public TMP_Text StateText;
            public Image Bg;
        }

        public void Init()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            backFade = new UIFadeAnimation(gameObject);

            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);

            backFade.Hide(immediately: true);
            panelScalable.Hide(immediately: true);
        }

        public void Show(SimpleCallback onClosed = null)
        {
            if (IsOpened)
                return;

            onClosedCallback = onClosed;

            DailyRewardController.ResetStreakIfMissed();
            RebuildSlots();

            gameObject.SetActive(true);
            backFade.Show(0.2f, onCompleted: () =>
            {
                panelScalable.Show(immediately: false, duration: 0.3f);
            });

            UIController.OnPopupWindowOpened(this);
        }

        public void Hide()
        {
            backFade.Hide(0.2f);
            panelScalable.Hide(immediately: false, duration: 0.3f, onCompleted: () =>
            {
                gameObject.SetActive(false);
            });

            UIController.OnPopupWindowClosed(this);

            SimpleCallback cb = onClosedCallback;
            onClosedCallback = null;
            cb?.Invoke();
        }

        private void OnClaimClicked()
        {
            if (DailyRewardController.Claim())
            {
                RebuildSlots();
                statusText.text = "已领取今日奖励！";
            }
            else
            {
                statusText.text = "今日奖励已领取";
            }
        }

        private void RebuildSlots()
        {
            int dayIndex = DailyRewardController.GetCurrentDayIndex();
            bool canClaim = DailyRewardController.CanClaimToday();

            for (int i = 0; i < 7; i++)
            {
                RewardSlot slot = GetOrCreateSlot(i);

                int thisDay = i + 1;
                slot.AmountText.text = DailyRewardController.GetCoinReward(thisDay).ToString();

                bool claimed = (DailyRewardController.Save.ClaimedMask & (1 << i)) != 0;
                bool isToday = thisDay == dayIndex && canClaim;

                if (claimed)
                {
                    slot.StateText.text = "已领";
                    slot.Bg.color = new Color(0.82f, 0.82f, 0.82f, 0.92f);
                }
                else if (isToday)
                {
                    slot.StateText.text = "今日";
                    slot.Bg.color = CandyColors.StrawberryPri;
                }
                else
                {
                    slot.StateText.text = thisDay + "天";
                    slot.Bg.color = CandyColors.MintLt;
                }
            }

            claimButton.interactable = canClaim;
            statusText.text = canClaim ? "登录即领，连续登录奖励更丰厚！" : "今日奖励已领取，明天再来！";
        }

        private RewardSlot GetOrCreateSlot(int index)
        {
            if (index < slots.Count)
                return slots[index];

            GameObject slotGo = Instantiate(slotTemplate, slotsContainer);
            slotGo.SetActive(true);

            RewardSlot slot = new RewardSlot
            {
                Root = slotGo.GetComponent<RectTransform>(),
                AmountText = slotGo.transform.Find("Amount")?.GetComponent<TMP_Text>(),
                StateText = slotGo.transform.Find("State")?.GetComponent<TMP_Text>(),
                Bg = slotGo.GetComponent<Image>()
            };

            slots.Add(slot);
            return slot;
        }
    }
}
