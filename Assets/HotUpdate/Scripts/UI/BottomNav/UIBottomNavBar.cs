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

        private MainHubTab currentTab = MainHubTab.Camper;
        private bool isVisible;

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

            isVisible = root.activeSelf;
            RefreshSelectedVisuals();
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

        public static void Show()
        {
            if (!EnsureInstance())
                return;

            instance.EnsureNavRendersAboveWorld();
            instance.SetVisible(true);
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
