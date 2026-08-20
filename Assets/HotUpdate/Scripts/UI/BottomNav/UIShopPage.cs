using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// 商店（店铺挂机）页面：由 ShopUIBuilder 构建的标准 prefab UI。
    /// 序列化引用 + 事件驱动刷新 + 入场/按钮动画 + 收获飞币。
    /// 中央透明保留 3D 展柜透出，底部避让导航条。
    /// </summary>
    public class UIShopPage : UIPage
    {
        [SerializeField] RectTransform safeAreaRectTransform;

        [Space]
        [SerializeField] CurrencyUIPanelSimple coinsPanel;
        [SerializeField] TMP_Text themeText;
        [SerializeField] TMP_Text rateText;
        [SerializeField] TMP_Text pendingText;
        [SerializeField] TMP_Text freshnessText;
        [SerializeField] TMP_Text expandCostText;

        [Space]
        [SerializeField] Button harvestButton;
        [SerializeField] RectTransform harvestButtonRect;
        [SerializeField] Button freezerButton;
        [SerializeField] Button expandButton;

        [Space]
        [SerializeField] UIShopFreezerPanel freezerPanel;
        [SerializeField] UIShopExpandConfirmPopUp expandConfirmPopUp;
        [SerializeField] Button recipeButton;
        [SerializeField] UIRecipePanel recipePanel;
        [SerializeField] Button ingredientButton;
        [SerializeField] UIIngredientPanel ingredientPanel;
        [SerializeField] Button customerButton;
        [SerializeField] TMP_Text customerStatusText;

        [Header("Animations")]
        [SerializeField] RectTransform[] hudElements;

        private TweenCase[] entranceTweens;
        private TweenCase pendingPulseTween;

        public override void Init()
        {
            if (coinsPanel != null)
                coinsPanel.Init();

            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
            ApplyBottomNavPadding();

            WireButtons();

            if (freezerPanel != null)
                freezerPanel.Init();
            if (expandConfirmPopUp != null)
                expandConfirmPopUp.Init();
            if (recipePanel != null)
                recipePanel.Init();
            if (ingredientPanel != null)
                ingredientPanel.Init();

            ShopController.StateChanged += RefreshHud;
            RefreshHud();
        }

        private void OnDestroy()
        {
            ShopController.StateChanged -= RefreshHud;
        }

        public override void PlayShowAnimation()
        {
            ShopController.EnsureInitialized();
            ApplyBottomNavPadding();

            if (freezerPanel != null)
                freezerPanel.Hide(immediately: true);

            PlayEntranceAnimation();
            RefreshHud();

            UIController.OnPageOpened(this);
        }

        public override void PlayHideAnimation()
        {
            entranceTweens.KillActive();
            pendingPulseTween.KillActive();

            if (freezerPanel != null)
                freezerPanel.Hide(immediately: true);

            UIController.OnPageClosed(this);
        }

        private void ApplyBottomNavPadding()
        {
            if (safeAreaRectTransform == null)
                return;

            Vector2 offsetMin = safeAreaRectTransform.offsetMin;
            offsetMin.y = Mathf.Max(offsetMin.y, UIBottomNavBar.NavHeight);
            safeAreaRectTransform.offsetMin = offsetMin;
        }

        private void WireButtons()
        {
            if (harvestButton != null)
            {
                harvestButton.onClick.RemoveAllListeners();
                harvestButton.onClick.AddListener(OnHarvestClicked);
            }

            if (freezerButton != null)
            {
                freezerButton.onClick.RemoveAllListeners();
                freezerButton.onClick.AddListener(OnFreezerClicked);
            }

            if (expandButton != null)
            {
                expandButton.onClick.RemoveAllListeners();
                expandButton.onClick.AddListener(OnExpandClicked);
            }

            if (recipeButton != null)
            {
                recipeButton.onClick.RemoveAllListeners();
                recipeButton.onClick.AddListener(OnRecipeClicked);
            }

            if (ingredientButton != null)
            {
                ingredientButton.onClick.RemoveAllListeners();
                ingredientButton.onClick.AddListener(OnIngredientClicked);
            }

            if (customerButton != null)
            {
                customerButton.onClick.RemoveAllListeners();
                customerButton.onClick.AddListener(OnCustomerClicked);
            }
        }

        private void OnCustomerClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            // 找有蛋糕的货架（优先最新上架的）
            int target = -1;
            for (int i = 0; i < ShopController.Save.UnlockedShelfCount; i++)
            {
                if (ShopController.GetCakeOnShelf(i) != null)
                {
                    target = i;
                    break;
                }
            }

            if (target < 0)
            {
                if (customerStatusText != null)
                    customerStatusText.text = "货架没有蛋糕";
                return;
            }

            int amount = ShopController.CustomerBuys(target);
            if (amount > 0)
            {
                if (customerStatusText != null)
                    customerStatusText.text = $"顾客买走了蛋糕 +{amount}";

                // 飞币演出
                if (harvestButtonRect != null && coinsPanel != null)
                {
                    FloatingCloud.SpawnCurrency("Coins", harvestButtonRect, coinsPanel.RectTransform,
                        Mathf.Clamp(amount, 1, 8), null, RefreshHud);
                }
            }
            else
            {
                if (customerStatusText != null)
                    customerStatusText.text = "蛋糕刚上架";
            }

            RefreshHud();
        }

        private void OnRecipeClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            if (recipePanel != null)
                recipePanel.Toggle();
        }

        private void OnIngredientClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            if (ingredientPanel != null)
                ingredientPanel.Toggle();
        }

        private void PlayEntranceAnimation()
        {
            entranceTweens.KillActive();

            if (hudElements == null || hudElements.Length == 0)
                return;

            entranceTweens = new TweenCase[hudElements.Length];
            for (int i = 0; i < hudElements.Length; i++)
            {
                RectTransform element = hudElements[i];
                if (element == null)
                    continue;

                element.localScale = Vector3.zero;
                entranceTweens[i] = element.DOScale(Vector3.one, 0.3f, i * 0.05f).SetEasing(Ease.Type.BackOut);
            }
        }

        private void RefreshHud()
        {
            if (!ShopController.IsInitialized)
                return;

            if (themeText != null)
                themeText.text = $"今日热门 {ShopDailyTheme.GetDisplayName(ShopDailyTheme.CurrentTheme)} ×2";

            if (rateText != null)
                rateText.text = $"{ShopController.GetTotalCreditsPerHour():0.#}/时";

            if (pendingText != null)
                pendingText.text = $"待收获 {Mathf.FloorToInt((float)ShopController.PendingCredits)}";

            if (freshnessText != null)
                freshnessText.text = ShopController.GetOverallFreshnessLabel();

            if (expandCostText != null)
            {
                int cost = ShopController.GetNextExpandCost();
                expandCostText.text = cost < 0 ? "展位已满" : cost.ToString();
            }

            if (expandButton != null)
                expandButton.interactable = ShopController.GetNextExpandCost() >= 0;

            if (freezerPanel != null)
                freezerPanel.RefreshIfOpen();

            ShopWorld.Refresh();
            UpdatePendingPulse();
        }

        private void UpdatePendingPulse()
        {
            if (pendingText == null)
                return;

            bool hasPending = ShopController.PendingCredits > 0;
            if (hasPending)
            {
                if (pendingPulseTween == null || !pendingPulseTween.IsActive)
                {
                    pendingPulseTween = pendingText.transform.DOPingPongScale(1.0f, 1.06f, 1.2f, Ease.Type.QuadIn, Ease.Type.QuadOut);
                }
            }
            else
            {
                pendingPulseTween.KillActive();
                pendingText.transform.localScale = Vector3.one;
            }
        }

        private void OnHarvestClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            int amount = ShopController.Harvest();
            if (amount > 0)
            {
                PlayPressPunch(harvestButton.transform);

                if (harvestButtonRect != null && coinsPanel != null)
                {
                    FloatingCloud.SpawnCurrency("Coins", harvestButtonRect, coinsPanel.RectTransform,
                        Mathf.Clamp(amount, 1, 8), null, RefreshHud);
                }
            }

            RefreshHud();
        }

        private void OnFreezerClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            PlayPressPunch(freezerButton.transform);

            if (freezerPanel != null)
                freezerPanel.Toggle();
        }

        private void OnExpandClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            int cost = ShopController.GetNextExpandCost();
            if (cost < 0)
                return;

            if (expandConfirmPopUp != null)
            {
                expandConfirmPopUp.Show(cost, () =>
                {
                    if (ShopController.UnlockNextShelf())
                        RefreshHud();
                });
            }
            else
            {
                if (ShopController.UnlockNextShelf())
                    RefreshHud();
            }
        }

        private void PlayPressPunch(Transform target)
        {
            if (target == null)
                return;

            target.DOPushScale(Vector3.one * 1.12f, Vector3.one, 0.08f, 0.12f, Ease.Type.QuadOut, Ease.Type.QuadIn);
        }
    }
}
