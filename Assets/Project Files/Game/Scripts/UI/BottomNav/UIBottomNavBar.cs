using UnityEngine;
using UnityEngine.UI;
using Watermelon.Map;

namespace Watermelon
{
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

            // Inactive objects skip Awake until activated once.
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
            if (!force && currentTab == tab && IsHubPageDisplayed(tab))
            {
                RefreshSelectedVisuals();
                return;
            }

            HideHubPagesExcept(tab);

            currentTab = tab;
            RefreshSelectedVisuals();

            switch (tab)
            {
                case MainHubTab.Shop:
                    MapBehavior.SetMapVisible(false);
                    MapBehavior.DisableScroll();
                    if (UIController.GetPage<UIShopPage>() != null)
                        UIController.ShowPage<UIShopPage>();
                    break;

                case MainHubTab.Profile:
                    MapBehavior.SetMapVisible(false);
                    MapBehavior.DisableScroll();
                    if (UIController.GetPage<UIProfilePage>() != null)
                        UIController.ShowPage<UIProfilePage>();
                    break;

                case MainHubTab.Camper:
                default:
                    MapBehavior.SetMapVisible(true);
                    MapBehavior.EnableScroll();
                    if (UIController.GetPage<UIMainMenu>() != null)
                        UIController.ShowPage<UIMainMenu>();
                    break;
            }
        }

        private static bool IsHubPageDisplayed(MainHubTab tab)
        {
            switch (tab)
            {
                case MainHubTab.Shop:
                    return UIController.IsDisplayed<UIShopPage>();
                case MainHubTab.Profile:
                    return UIController.IsDisplayed<UIProfilePage>();
                default:
                    return UIController.IsDisplayed<UIMainMenu>();
            }
        }

        private static void HideHubPagesExcept(MainHubTab keep)
        {
            if (UIController.IsDisplayed<Watermelon.IAPStore.UIStore>())
                UIController.HidePage<Watermelon.IAPStore.UIStore>();

            if (keep != MainHubTab.Shop && UIController.IsDisplayed<UIShopPage>())
                UIController.HidePage<UIShopPage>();

            if (keep != MainHubTab.Profile && UIController.IsDisplayed<UIProfilePage>())
                UIController.HidePage<UIProfilePage>();

            if (keep != MainHubTab.Camper && UIController.IsDisplayed<UIMainMenu>())
                UIController.HidePage<UIMainMenu>();
        }

        public static void HideAllHubPages()
        {
            if (UIController.IsDisplayed<Watermelon.IAPStore.UIStore>())
                UIController.HidePage<Watermelon.IAPStore.UIStore>();

            if (UIController.IsDisplayed<UIShopPage>())
                UIController.HidePage<UIShopPage>();

            if (UIController.IsDisplayed<UIProfilePage>())
                UIController.HidePage<UIProfilePage>();

            if (UIController.IsDisplayed<UIMainMenu>())
                UIController.HidePage<UIMainMenu>();
        }

        private void RefreshSelectedVisuals()
        {
            ApplySelected(shopSelected, shopButton, currentTab == MainHubTab.Shop);
            ApplySelected(camperSelected, camperButton, currentTab == MainHubTab.Camper);
            ApplySelected(profileSelected, profileButton, currentTab == MainHubTab.Profile);
        }

        private void ApplySelected(Graphic selectedGraphic, Button button, bool selected)
        {
            // selectedGraphic is the "Selected Bg" image under the tab button.
            // selected: fill with selectedColor; unselected: fully transparent.
            if (selectedGraphic != null)
            {
                Color c = selectedColor;
                c.a = selected ? 0.18f : 0f;
                selectedGraphic.color = c;
            }

            // Tint the Icon image too for extra contrast.
            if (button != null)
            {
                Graphic icon = button.targetGraphic;
                if (icon != null)
                    icon.color = selected ? selectedColor : normalColor;
            }
        }
    }
}
