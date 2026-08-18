using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UIShopPage : UIPage
    {
        private const string HUD_ROOT_NAME = "ShopHudChrome";

        [SerializeField] RectTransform safeAreaRectTransform;
        [SerializeField] Text themeText;
        [SerializeField] Text rateText;
        [SerializeField] Text pendingText;
        [SerializeField] Text creditsText;
        [SerializeField] Text expandText;
        [SerializeField] Button harvestButton;
        [SerializeField] Button freezerButton;
        [SerializeField] Button expandButton;
        [SerializeField] Button grantCakeButton;
        [SerializeField] UIShopFreezerPanel freezerPanel;

        public override void Init()
        {
            EnsureHud();
            EnsureShopHudDoesNotBlockNav();

            if (safeAreaRectTransform != null)
                NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);

            WireButtons();
            ShopController.StateChanged += RefreshHud;
            RefreshHud();
        }

        private void OnDestroy()
        {
            ShopController.StateChanged -= RefreshHud;
        }

        public override void PlayShowAnimation()
        {
            // World visibility is owned by ShopHubModule; this page only refreshes HUD.
            ShopController.EnsureInitialized();
            EnsureHud();
            EnsureShopHudDoesNotBlockNav();
            WireButtons();
            RefreshHud();
            UIController.OnPageOpened(this);
        }

        public override void PlayHideAnimation()
        {
            if (freezerPanel != null)
                freezerPanel.Hide();

            UIController.OnPageClosed(this);
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

            if (grantCakeButton != null)
            {
                grantCakeButton.onClick.RemoveAllListeners();
                grantCakeButton.onClick.AddListener(OnGrantCakeClicked);
            }
        }

        /// <summary>
        /// Idle shop hub: transparent center for 3D dollhouse, bottom strip free for nav.
        /// </summary>
        private void EnsureShopHudDoesNotBlockNav()
        {
            Canvas pageCanvas = canvas != null ? canvas : GetComponent<Canvas>();
            if (pageCanvas != null)
            {
                pageCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                pageCanvas.overrideSorting = true;
                pageCanvas.sortingOrder = 50;
            }

            RemoveBackground();

            if (safeAreaRectTransform != null)
            {
                Vector2 offsetMin = safeAreaRectTransform.offsetMin;
                offsetMin.y = Mathf.Max(offsetMin.y, UIBottomNavBar.NavHeight);
                safeAreaRectTransform.offsetMin = offsetMin;
            }
        }

        /// <summary>
        /// Shop page must not fill the screen — remove any full-page Background UI.
        /// </summary>
        private void RemoveBackground()
        {
            // Direct child named Background
            Transform bg = transform.Find("Background");
            if (bg != null)
            {
                if (Application.isPlaying)
                    Destroy(bg.gameObject);
                else
                    DestroyImmediate(bg.gameObject);
            }

            // Any leftover full-stretch Image on page root (old solid panel)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null || child.name != "Background")
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private void Update()
        {
            if (!IsPageDisplayed || !ShopController.IsInitialized)
                return;

            if (pendingText != null)
                BottomNavTextUtil.Apply(pendingText, $"待收获 {Mathf.FloorToInt((float)ShopController.PendingCredits)}", 26);

            if (rateText != null)
                BottomNavTextUtil.Apply(rateText, $"{ShopController.GetTotalCreditsPerHour():0.#}/时", 26);
        }

        private void RefreshHud()
        {
            if (!ShopController.IsInitialized)
                return;

            if (themeText != null)
            {
                string themeName = ShopDailyTheme.GetDisplayName(ShopDailyTheme.CurrentTheme);
                BottomNavTextUtil.Apply(themeText, $"今日热门 {themeName} ×2", 28);
            }

            if (rateText != null)
                BottomNavTextUtil.Apply(rateText, $"{ShopController.GetTotalCreditsPerHour():0.#}/时", 26);

            if (pendingText != null)
                BottomNavTextUtil.Apply(pendingText, $"待收获 {Mathf.FloorToInt((float)ShopController.PendingCredits)}", 26);

            if (creditsText != null)
            {
                int credits = 0;
                try
                {
                    credits = CurrencyController.Get(CurrencyType.BakingCredits);
                }
                catch (System.Exception)
                {
                    credits = 0;
                }

                BottomNavTextUtil.Apply(creditsText, $"积分 {credits}", 30);
            }

            if (expandText != null)
            {
                int cost = ShopController.GetNextExpandCost();
                if (cost < 0)
                    BottomNavTextUtil.Apply(expandText, "展位已满", 22);
                else
                    BottomNavTextUtil.Apply(expandText, $"{cost}", 22);
            }

            if (freezerPanel != null && freezerPanel.gameObject.activeInHierarchy)
                freezerPanel.Rebuild();

            ShopWorld.Refresh();
        }

        private void OnHarvestClicked()
        {
            int amount = ShopController.Harvest();
            RefreshHud();

            if (amount > 0 && pendingText != null)
                BottomNavTextUtil.Apply(pendingText, $"收获 +{amount}", 26);
        }

        private void OnFreezerClicked()
        {
            if (freezerPanel != null)
                freezerPanel.Toggle();
        }

        private void OnExpandClicked()
        {
            ShopController.UnlockNextShelf();
            RefreshHud();
        }

        private void OnGrantCakeClicked()
        {
            ShopController.GrantRandomCake();
            RefreshHud();
        }

        private void EnsureHud()
        {
            Transform page = transform;
            Image bgImg = page.Find("Background")?.GetComponent<Image>();
            if (bgImg != null)
            {
                if (Application.isPlaying)
                    Destroy(bgImg.gameObject);
                else
                    DestroyImmediate(bgImg.gameObject);
            }

            RectTransform safe = safeAreaRectTransform;
            if (safe == null)
            {
                Transform existing = page.Find("Safe Area");
                if (existing != null)
                    safe = existing as RectTransform;
            }

            if (safe == null)
            {
                GameObject safeGo = new GameObject("Safe Area", typeof(RectTransform));
                safeGo.transform.SetParent(page, false);
                safe = safeGo.GetComponent<RectTransform>();
                Stretch(safe);
            }

            safe.offsetMin = new Vector2(0f, BottomNavLayout.Height);
            safe.offsetMax = Vector2.zero;
            safeAreaRectTransform = safe;

            Transform oldTitle = safe.Find("Title");
            if (oldTitle != null)
                oldTitle.gameObject.SetActive(false);
            Transform oldSubtitle = safe.Find("Subtitle");
            if (oldSubtitle != null)
                oldSubtitle.gameObject.SetActive(false);

            Transform chrome = safe.Find(HUD_ROOT_NAME);
            if (chrome != null)
                Destroy(chrome.gameObject);

            GameObject hudRoot = new GameObject(HUD_ROOT_NAME, typeof(RectTransform));
            hudRoot.transform.SetParent(safe, false);
            RectTransform hudRect = hudRoot.GetComponent<RectTransform>();
            Stretch(hudRect);

            // Center stays empty — no raycast blocker — 3D dollhouse display shows through.
            // Top info strip
            creditsText = CreateChip(hudRoot.transform, "Credits", new Vector2(0.02f, 0.90f), new Vector2(0.28f, 0.98f), "积分 0", 30, new Color(1f, 1f, 1f, 0.88f));
            themeText = CreateChip(hudRoot.transform, "Theme", new Vector2(0.30f, 0.90f), new Vector2(0.72f, 0.98f), "今日热门", 28, new Color(1f, 1f, 1f, 0.88f));
            rateText = CreateChip(hudRoot.transform, "Rate", new Vector2(0.74f, 0.90f), new Vector2(0.98f, 0.98f), "0/时", 26, new Color(1f, 1f, 1f, 0.88f));

            // Side / corner play buttons — leave center clear for 3D stage
            freezerButton = CreateSideButton(hudRoot.transform, "FreezerButton", new Vector2(0.02f, 0.72f), new Vector2(0.22f, 0.86f), "冰柜", new Color(0.4f, 0.68f, 0.88f));
            harvestButton = CreateSideButton(hudRoot.transform, "HarvestButton", new Vector2(0.78f, 0.72f), new Vector2(0.98f, 0.86f), "收获", new Color(0.95f, 0.55f, 0.35f));
            pendingText = CreateChip(hudRoot.transform, "Pending", new Vector2(0.78f, 0.64f), new Vector2(0.98f, 0.72f), "待收获 0", 22, new Color(1f, 1f, 1f, 0.82f));

            expandButton = CreateSideButton(hudRoot.transform, "ExpandButton", new Vector2(0.02f, 0.18f), new Vector2(0.22f, 0.32f), "扩建", new Color(0.42f, 0.72f, 0.48f));
            expandText = CreateChip(hudRoot.transform, "ExpandCost", new Vector2(0.02f, 0.10f), new Vector2(0.22f, 0.18f), "100", 22, new Color(1f, 1f, 1f, 0.82f));
            grantCakeButton = CreateSideButton(hudRoot.transform, "GrantCakeButton", new Vector2(0.78f, 0.18f), new Vector2(0.98f, 0.32f), "领蛋糕", new Color(0.72f, 0.52f, 0.85f));

            freezerPanel = BuildFreezerPanel(hudRoot.transform);
        }

        private static UIShopFreezerPanel BuildFreezerPanel(Transform parent)
        {
            GameObject panel = new GameObject("FreezerPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            // Dock to left side so center display stays visible
            panelRect.anchorMin = new Vector2(0.02f, 0.34f);
            panelRect.anchorMax = new Vector2(0.42f, 0.70f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.24f, 0.94f);

            GameObject title = new GameObject("Title", typeof(RectTransform));
            title.transform.SetParent(panel.transform, false);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.85f);
            titleRect.anchorMax = new Vector2(0.7f, 1f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            Text titleText = title.AddComponent<Text>();
            BottomNavTextUtil.Apply(titleText, "冷藏冰柜", 30);
            titleText.color = Color.white;

            GameObject closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panel.transform, false);
            RectTransform closeRect = closeGo.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.78f, 0.86f);
            closeRect.anchorMax = new Vector2(0.96f, 0.97f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;
            closeGo.GetComponent<Image>().color = new Color(0.9f, 0.4f, 0.4f);
            Button closeButton = closeGo.GetComponent<Button>();
            GameObject closeLabelGo = new GameObject("Text", typeof(RectTransform));
            closeLabelGo.transform.SetParent(closeGo.transform, false);
            Stretch(closeLabelGo.GetComponent<RectTransform>());
            Text closeLabel = closeLabelGo.AddComponent<Text>();
            BottomNavTextUtil.Apply(closeLabel, "关", 22);
            closeLabel.color = Color.white;

            GameObject empty = new GameObject("Empty", typeof(RectTransform));
            empty.transform.SetParent(panel.transform, false);
            RectTransform emptyRect = empty.GetComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0.1f, 0.35f);
            emptyRect.anchorMax = new Vector2(0.9f, 0.7f);
            emptyRect.offsetMin = Vector2.zero;
            emptyRect.offsetMax = Vector2.zero;
            Text emptyText = empty.AddComponent<Text>();
            BottomNavTextUtil.Apply(emptyText, "冰柜是空的", 26);
            emptyText.color = new Color(1f, 1f, 1f, 0.7f);

            GameObject scroll = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            scroll.transform.SetParent(panel.transform, false);
            RectTransform scrollRect = scroll.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.04f, 0.05f);
            scrollRect.anchorMax = new Vector2(0.96f, 0.84f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = scroll.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            ContentSizeFitter fitter = scroll.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UIShopFreezerPanel component = panel.AddComponent<UIShopFreezerPanel>();
            component.Configure(panel, scroll.transform, closeButton, emptyText);
            panel.SetActive(false);
            return component;
        }

        private static Text CreateChip(Transform parent, string name, Vector2 min, Vector2 max, string value, int size, Color bgColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = bgColor;
            go.GetComponent<Image>().raycastTarget = false;

            GameObject textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>());
            Text text = textGo.AddComponent<Text>();
            BottomNavTextUtil.Apply(text, value, size);
            text.color = new Color(0.18f, 0.22f, 0.2f);
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateSideButton(Transform parent, string name, Vector2 min, Vector2 max, string label, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            Button button = go.GetComponent<Button>();

            GameObject textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            Stretch(textGo.GetComponent<RectTransform>());
            Text text = textGo.AddComponent<Text>();
            BottomNavTextUtil.Apply(text, label, 28);
            text.color = Color.white;
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
