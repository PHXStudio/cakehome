using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Bottom navigation chrome only. Tab gameplay is owned by HubModuleRouter modules.
    /// </summary>
    public class UIBottomNavBar : MonoBehaviour
    {
        private static UIBottomNavBar instance;

        [SerializeField] GameObject root;
        [SerializeField] Button shopButton;
        [SerializeField] Button camperButton;
        [SerializeField] Button profileButton;
        [SerializeField] Graphic shopSelected;
        [SerializeField] Graphic camperSelected;
        [SerializeField] Graphic profileSelected;
        [SerializeField] float navHeight = BottomNavLayout.Height;
        [SerializeField] Color selectedColor = new Color(0.2f, 0.55f, 0.35f, 1f);
        [SerializeField] Color normalColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        [Header("Shop Nudge (onboarding)")]
        [SerializeField] string nudgeText = "咖啡店有顾客在等!";
        [SerializeField] Color nudgeColor = new Color(0.9f, 0.25f, 0.2f, 1f);

        private MainHubTab currentTab = MainHubTab.Camper;
        private bool isVisible;

        // 门店引导气泡/红点：首胜后出现，玩家点过 Shop Tab 后永久消失。
        // 出现条件：教程开启 + 门店教程未完成 + 已过至少一关 + 当前不在门店 Tab。
        private Image shopBadge;
        private RectTransform shopBadgeRect;
        private GameObject shopBubble;
        private RectTransform shopBubbleRect;
        private bool nudgeActive;
        private float nudgeTime;

        private const string NUDGE_PREFS_KEY = "ShopTabNudgeDismissed";

        // PlayerPrefs 不能在静态/字段初始化器里读（Unity 限制）——改成惰性加载
        private static bool? nudgeDismissedCached;

        private static bool NudgeDismissed
        {
            get
            {
                if (!nudgeDismissedCached.HasValue)
                    nudgeDismissedCached = PlayerPrefs.GetInt(NUDGE_PREFS_KEY, 0) == 1;
                return nudgeDismissedCached.Value;
            }
        }

        public static float NavHeight => instance != null ? instance.navHeight : BottomNavLayout.Height;

        public static MainHubTab CurrentTab => instance != null ? instance.currentTab : MainHubTab.Camper;
        public static bool IsVisible => instance != null && instance.isVisible;

        private void Awake()
        {
            instance = this;

            if (root == null)
                root = gameObject;

            HubModuleRouter.EnsureInitialized();
            EnsureNavRendersAboveWorld();
            ApplyLayout();

            if (shopButton != null)
                shopButton.onClick.AddListener(() => SelectTab(MainHubTab.Shop));

            if (camperButton != null)
                camperButton.onClick.AddListener(() => SelectTab(MainHubTab.Camper));

            if (profileButton != null)
                profileButton.onClick.AddListener(() => SelectTab(MainHubTab.Profile));

            BuildShopNudge();

            isVisible = root.activeSelf;
            RefreshSelectedVisuals();
        }

        private void Start()
        {
            // 延迟到 Start：Awake 阶段存档系统可能未就绪（IsOnboardingCompleted 要读档）
            RefreshNudge();
        }

        private void EnsureNavRendersAboveWorld()
        {
            Canvas navCanvas = root != null ? root.GetComponent<Canvas>() : GetComponent<Canvas>();
            if (navCanvas == null)
                navCanvas = GetComponentInChildren<Canvas>(true);

            if (navCanvas == null)
                return;

            navCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            navCanvas.overrideSorting = true;
            navCanvas.sortingOrder = 600;
        }

        private void ApplyLayout()
        {
            RectTransform barRect = (root != null ? root : gameObject).GetComponent<RectTransform>();
            if (barRect != null)
                barRect.sizeDelta = new Vector2(barRect.sizeDelta.x, navHeight);

            ApplyTabLayout(shopButton, shopSelected);
            ApplyTabLayout(camperButton, camperSelected);
            ApplyTabLayout(profileButton, profileSelected);
        }

        private static void ApplyTabLayout(Button button, Graphic selectedBg)
        {
            if (button == null)
                return;

            Transform tab = button.transform;
            for (int i = 0; i < tab.childCount; i++)
            {
                Transform child = tab.GetChild(i);
                RectTransform childRect = child as RectTransform;
                if (childRect == null)
                    continue;

                if (child.name == "Icon")
                {
                    childRect.sizeDelta = new Vector2(BottomNavLayout.IconSize, BottomNavLayout.IconSize);
                    childRect.anchoredPosition = new Vector2(0f, BottomNavLayout.IconYOffset);
                }
                else if (child.name == "Selected Bg")
                {
                    childRect.sizeDelta = new Vector2(BottomNavLayout.SelectedBgSize, BottomNavLayout.SelectedBgSize);
                    childRect.anchoredPosition = new Vector2(0f, BottomNavLayout.IconYOffset);
                }
            }

            if (selectedBg != null)
            {
                RectTransform selectedRect = selectedBg.rectTransform;
                selectedRect.sizeDelta = new Vector2(BottomNavLayout.SelectedBgSize, BottomNavLayout.SelectedBgSize);
                selectedRect.anchoredPosition = new Vector2(0f, BottomNavLayout.IconYOffset);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        // ─── Shop Nudge（轻引导：C.6 观察 1）────────────────────────────────────

        /// <summary>通关后调用（GameController.OnLevelCompleted），让首胜后的红点出现。</summary>
        public static void NotifyLevelCompleted()
        {
            if (EnsureInstance())
                instance.RefreshNudge();
        }

        private void RefreshNudge()
        {
            bool should = !NudgeDismissed
                && MergeViewController.TutorialsEnabled
                && !MergeViewController.IsOnboardingCompleted()
                && LevelController.MaxReachedLevelIndex > 0
                && currentTab != MainHubTab.Shop;

            nudgeActive = should;
            nudgeTime = 0f;

            if (shopBadge != null) shopBadge.gameObject.SetActive(should);
            if (shopBubble != null) shopBubble.SetActive(should);
        }

        private void DismissNudge()
        {
            if (NudgeDismissed) return;

            nudgeDismissedCached = true;
            PlayerPrefs.SetInt(NUDGE_PREFS_KEY, 1);
            PlayerPrefs.Save();

            RefreshNudge();
        }

        private void Update()
        {
            if (!nudgeActive || shopBadgeRect == null) return;

            // 红点呼吸 + 气泡轻浮动
            nudgeTime += Time.unscaledDeltaTime;
            float pulse = 1f + Mathf.Sin(nudgeTime * 4f) * 0.15f;
            shopBadgeRect.localScale = Vector3.one * pulse;

            if (shopBubbleRect != null)
            {
                Vector2 pos = shopBubbleRect.anchoredPosition;
                pos.y = shopBubbleBaseY + Mathf.Sin(nudgeTime * 2.2f) * 6f;
                shopBubbleRect.anchoredPosition = pos;
            }
        }

        private float shopBubbleBaseY;

        private void BuildShopNudge()
        {
            if (shopButton == null) return;

            // 红点：图标右上角，运行时生成软圆贴图（不依赖美术资源）
            GameObject badgeGo = new GameObject("Shop Badge", typeof(RectTransform), typeof(Image));
            badgeGo.layer = 5;
            badgeGo.transform.SetParent(shopButton.transform, false);

            shopBadgeRect = (RectTransform)badgeGo.transform;
            shopBadgeRect.anchorMin = shopBadgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            shopBadgeRect.pivot = new Vector2(0.5f, 0.5f);
            shopBadgeRect.anchoredPosition = new Vector2(BottomNavLayout.IconSize * 0.38f, BottomNavLayout.IconSize * 0.38f + BottomNavLayout.IconYOffset);
            shopBadgeRect.sizeDelta = new Vector2(36f, 36f);

            shopBadge = badgeGo.GetComponent<Image>();
            shopBadge.sprite = GetSoftCircleSprite();
            shopBadge.color = nudgeColor;
            shopBadge.raycastTarget = false;

            // 气泡：Shop Tab 正上方，白底圆角 + 中文提示（legacy Text + ChineseFont）
            GameObject bubbleGo = new GameObject("Shop Bubble", typeof(RectTransform), typeof(Image));
            bubbleGo.layer = 5;
            bubbleGo.transform.SetParent(shopButton.transform, false);

            shopBubbleRect = (RectTransform)bubbleGo.transform;
            shopBubbleRect.anchorMin = shopBubbleRect.anchorMax = new Vector2(0.5f, 1f);
            shopBubbleRect.pivot = new Vector2(0.5f, 0f);
            shopBubbleBaseY = 14f;
            shopBubbleRect.anchoredPosition = new Vector2(0f, shopBubbleBaseY);
            shopBubbleRect.sizeDelta = new Vector2(340f, 84f);

            Image bubbleBg = bubbleGo.GetComponent<Image>();
            bubbleBg.sprite = GetRoundedRectSprite();
            bubbleBg.type = Image.Type.Sliced;  // sprite 带 40px 边框，拉伸时圆角不变形
            bubbleBg.color = Color.white;
            bubbleBg.raycastTarget = false;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.layer = 5;
            textGo.transform.SetParent(bubbleGo.transform, false);

            RectTransform textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            Text label = textGo.GetComponent<Text>();
            Font font = Resources.Load<Font>("ChineseFont");
            label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = nudgeText;
            label.fontSize = 34;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.25f, 0.2f, 0.18f, 1f);
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            shopBubble = bubbleGo;
        }

        private static Sprite softCircleSprite;

        private static Sprite GetSoftCircleSprite()
        {
            if (softCircleSprite != null) return softCircleSprite;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                    float alpha = Mathf.Clamp01((center - 2f - dist) / 3f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            softCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return softCircleSprite;
        }

        private static Sprite roundedRectSprite;

        // 气泡背景：运行时生成圆角矩形（内置 UISprite.psd 在当前渲染管线下不可用）
        private static Sprite GetRoundedRectSprite()
        {
            if (roundedRectSprite != null) return roundedRectSprite;

            const int size = 128;
            const float radius = 28f;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius), 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            roundedRectSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(40f, 40f, 40f, 40f));
            return roundedRectSprite;
        }


        public static void Show()
        {
            if (!EnsureInstance())
                return;

            instance.EnsureNavRendersAboveWorld();
            instance.SetVisible(true);
            instance.RefreshNudge();
        }

        public static void Hide(bool immediate = false)
        {
            if (!EnsureInstance())
                return;

            instance.SetVisible(false);
        }

        public static void SelectTab(MainHubTab tab, bool force = false)
        {
            if (!EnsureInstance())
                return;

            instance.SelectTabInternal(tab, force);
        }

        private static bool EnsureInstance()
        {
            if (instance != null)
                return true;

            instance = Object.FindObjectOfType<UIBottomNavBar>(true);
            if (instance == null)
                return false;

            if (!instance.gameObject.activeSelf)
                instance.gameObject.SetActive(true);

            return instance != null;
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            root.SetActive(visible);
        }

        private void SelectTabInternal(MainHubTab tab, bool force)
        {
            if (!force && currentTab == tab && HubModuleRouter.IsModuleActive(tab))
            {
                RefreshSelectedVisuals();
                return;
            }

            // Hub tabs always keep the nav bar for switching between gameplay pillars.
            Show();
            HubModuleRouter.SwitchTo(tab, force);

            currentTab = tab;
            RefreshSelectedVisuals();

            // 玩家点过 Shop Tab → 门店已被发现，引导红点永久收起
            if (tab == MainHubTab.Shop)
                DismissNudge();
            else
                RefreshNudge();
        }

        /// <summary>
        /// Exit all hub gameplay modules (used when entering a level).
        /// </summary>
        public static void HideAllHubPages()
        {
            HubModuleRouter.ExitAll();
        }

        private void RefreshSelectedVisuals()
        {
            ApplySelected(shopSelected, shopButton, currentTab == MainHubTab.Shop);
            ApplySelected(camperSelected, camperButton, currentTab == MainHubTab.Camper);
            ApplySelected(profileSelected, profileButton, currentTab == MainHubTab.Profile);
        }

        private void ApplySelected(Graphic selectedGraphic, Button button, bool selected)
        {
            if (selectedGraphic != null)
            {
                Color c = selectedColor;
                c.a = selected ? 0.18f : 0f;
                selectedGraphic.color = c;
            }

            if (button != null)
            {
                Graphic icon = button.targetGraphic;
                if (icon != null)
                {
                    // 彩色图标保持原色，选中态由 Selected Bg 圆点表达；
                    // 纯色染色只作为 sprite 缺失时的占位回退。
                    Image iconImage = icon as Image;
                    icon.color = iconImage != null && iconImage.sprite != null
                        ? Color.white
                        : (selected ? selectedColor : normalColor);
                }
            }
        }
    }
}
