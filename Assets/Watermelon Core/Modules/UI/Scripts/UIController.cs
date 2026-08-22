using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Central manager for all UI pages and popup windows.
    /// Provides a static facade for showing, hiding, and querying pages by type.
    /// <para>Startup order: <see cref="Init"/> (caches refs, discovers pages) →
    /// game modules initialize → <see cref="InitPages"/> (initializes pages so they can access modules).</para>
    /// </summary>
    public class UIController : MonoBehaviour
    {
        private static UIController uiController;

        [SerializeField] CachedPages cachedPages;

        private SafeAreaAdapter notchSafeArea;

        [Space]
        [SerializeField] bool usePausePopups = true;

        private List<UIPage> pages;
        /// <summary>All pages registered under this controller (scene-resident + instantiated cached pages).</summary>
        public IReadOnlyList<UIPage> Pages => pages;

        private Dictionary<Type, UIPage> pagesLink = new Dictionary<Type, UIPage>();
        /// <summary>Fast type-to-page lookup for all registered pages.</summary>
        public IReadOnlyDictionary<Type, UIPage> PagesLink => pagesLink;

        private HashSet<UIPage> popupWindows;
        /// <summary>Returns <c>true</c> if at least one popup page is currently open.</summary>
        public static bool IsPopupOpened
        {
            get
            {
                if(uiController == null)
                {
                    Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                    return false;
                }

                return uiController.popupWindows.Count > 0;
            }
        }

        private List<UIPage> pausePopups = new List<UIPage>();
        /// <summary>All currently open pages with <see cref="UIPage.IsPausePopup"/> set to <c>true</c>.</summary>
        public IReadOnlyList<UIPage> PausePopups => pausePopups;

        /// <summary>Returns <c>true</c> if at least one pause-popup page is currently open.</summary>
        public static bool IsPausePopupOpened => uiController != null && uiController.pausePopups.Count > 0;

        private static Canvas mainCanvas;
        /// <summary>The root <see cref="Canvas"/> component attached to this controller's GameObject.</summary>
        public static Canvas MainCanvas => mainCanvas;
        /// <summary>The <see cref="CanvasScaler"/> component attached to this controller's GameObject.</summary>
        public static CanvasScaler CanvasScaler { get; private set; }
        /// <summary>
        /// Uniform scale factor between the physical screen and the canvas reference resolution.
        /// Computed in <see cref="InitPages"/> and used for safe area calculations.
        /// </summary>
        public static float ScaleFactor { get; private set; } = 1.0f;

        private static Camera mainCamera;

        private Dictionary<UIPage, SimpleCallback> pageClosedCallbacks = new Dictionary<UIPage, SimpleCallback>();

        /// <summary>Fired when any page finishes its show animation and becomes active.</summary>
        public static event PageCallback PageOpened;
        /// <summary>Fired when any page finishes its hide animation and becomes inactive.</summary>
        public static event PageCallback PageClosed;

        /// <summary>Fired when a popup page (IsPopup == true) finishes opening.</summary>
        public static event PopupCallback PopupOpened;
        /// <summary>Fired when a popup page (IsPopup == true) finishes closing.</summary>
        public static event PopupCallback PopupClosed;

        /// <summary>
        /// Phase 1 of initialization. Caches component references, adjusts the canvas scaler for tablet/phone,
        /// and discovers all child <see cref="UIPage"/> components. Must be called before game modules initialize.
        /// </summary>
        public void Init()
        {
            uiController = this;

            mainCanvas = GetComponent<Canvas>();
            CanvasScaler = GetComponent<CanvasScaler>();

            mainCamera = Camera.main;

            CanvasScaler.matchWidthOrHeight = UIUtils.IsTablet() ? 1 : 0;

            popupWindows = new HashSet<UIPage>();
            pausePopups = new List<UIPage>();

            pages = new List<UIPage>();
            pagesLink = new Dictionary<Type, UIPage>();
            for (int i = 0; i < transform.childCount; i++)
            {
                UIPage uiPage = transform.GetChild(i).GetComponent<UIPage>();
                if(uiPage != null)
                {
                    uiPage.CacheComponents();

                    if(pagesLink.ContainsKey(uiPage.GetType()))
                    {
                        Debug.LogWarning($"[UI Controller] Page {uiPage.GetType()} is already added to the UIController. Please remove the duplicate object to resolve this issue.");

                        continue;
                    }

                    pagesLink.Add(uiPage.GetType(), uiPage);

                    pages.Add(uiPage);
                }
            }

            cachedPages.Init(this);
        }

        private void RecalculateScaleFactor()
        {
            Vector2 refRes = CanvasScaler.referenceResolution;

            float screenRatio = (float)Screen.width / Screen.height;
            float targetRatio = refRes.x / refRes.y;

            ScaleFactor = screenRatio > targetRatio
                ? Screen.height / refRes.y
                : Screen.width / refRes.x;
        }

        /// <summary>
        /// Phase 2 of initialization. Recalculates the scale factor, initializes the safe area adapter,
        /// and calls <see cref="UIPage.PreparePage"/> + <see cref="UIPage.Init"/> on every registered page.
        /// Must be called after all game modules are initialized so pages can safely reference them.
        /// </summary>
        public void InitPages()
        {
            RecalculateScaleFactor();

            notchSafeArea = new SafeAreaAdapter(new Vector2(Screen.width / ScaleFactor, Screen.height / ScaleFactor));

            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].PreparePage();
                pages[i].Init();
            }
        }

        /// <summary>Calls <see cref="UIPage.Unload"/> on every page that is currently displayed.</summary>
        public static void ResetPages()
        {
            UIController controller = uiController;
            if (controller != null)
            {
                List<UIPage> pages = controller.pages;
                for (int i = 0; i < pages.Count; i++)
                {
                    if (pages[i].IsPageDisplayed)
                    {
                        pages[i].TriggerUnload();
                    }
                }
            }
        }

        /// <summary>
        /// Shows the page of type <typeparamref name="T"/>. If the page is not in the scene,
        /// it is instantiated from the cached prefab. Has no effect if the page is already displayed.
        /// </summary>
        public static void ShowPage<T>() where T : UIPage
        {
            if(uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return;
            }

            Type pageType = typeof(T);

            Dictionary<Type, UIPage> pagesLink = uiController.pagesLink;

            if(!pagesLink.TryGetValue(pageType, out UIPage page))
            {
                if(!uiController.cachedPages.HasPage(pageType))
                {
                    Debug.LogError($"[UIController]: Page {pageType} not found in the scene and not linked as a cached page. Ensure it's instantiated or registered before showing.");

                    return;
                }

                page = uiController.cachedPages.InstantiateCachedPage(pageType);
            }

            if (!page.IsPageDisplayed)
            {
                page.GraphicRaycaster.enabled = true;
                page.TriggerShow();
                page.EnableCanvas();
            }
        }

        /// <summary>
        /// Shows the given <paramref name="page"/> directly. Has no effect if the page is already displayed.
        /// </summary>
        public static void ShowPage(UIPage page)
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return;
            }

            if(page == null)
            {
                Debug.LogError("[UIController]: Provided page reference cannot be null!");

                return;
            }

            if (!page.IsPageDisplayed)
            {
                page.GraphicRaycaster.enabled = true;
                page.TriggerShow();
                page.EnableCanvas();
            }
        }

        /// <summary>
        /// Starts the hide animation for the page of type <typeparamref name="T"/>.
        /// <paramref name="onPageClosed"/> is invoked once the page finishes hiding.
        /// If the page is not displayed, the callback is invoked immediately.
        /// </summary>
        public static void HidePage<T>(SimpleCallback onPageClosed = null) where T : UIPage
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                onPageClosed?.Invoke();

                return;
            }

            Dictionary<Type, UIPage> pagesLink = uiController.pagesLink;
            Type pageType = typeof(T);

            if (!pagesLink.TryGetValue(pageType, out UIPage page))
            {
                Debug.LogError($"[UIController]: Page type {pageType.Name} was not found in the registered pages!");

                onPageClosed?.Invoke();

                return;
            }

            if (!page.IsPageDisplayed)
            {
                onPageClosed?.Invoke();

                return;
            }

            if (onPageClosed != null)
                uiController.pageClosedCallbacks[page] = onPageClosed;

            page.GraphicRaycaster.enabled = false;
            page.TriggerHide();
        }

        /// <summary>
        /// Starts the hide animation for the given <paramref name="page"/>.
        /// <paramref name="onPageClosed"/> is invoked once the page finishes hiding.
        /// If the page is not displayed, the callback is invoked immediately.
        /// </summary>
        public static void HidePage(UIPage page, SimpleCallback onPageClosed = null)
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                onPageClosed?.Invoke();

                return;
            }

            if (page == null)
            {
                Debug.LogError("[UIController]: Provided page reference cannot be null!");

                onPageClosed?.Invoke();

                return;
            }

            if (!page.IsPageDisplayed)
            {
                onPageClosed?.Invoke();

                return;
            }

            if (onPageClosed != null)
                uiController.pageClosedCallbacks[page] = onPageClosed;

            page.GraphicRaycaster.enabled = false;
            page.TriggerHide();
        }

        /// <summary>
        /// Immediately disables the canvas of the page of type <typeparamref name="T"/> without playing a hide animation.
        /// Fires <see cref="PageClosed"/> and handles popup/pause-popup cleanup.
        /// </summary>
        public static void DisablePage<T>()
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return;
            }

            Dictionary<Type, UIPage> pagesLink = uiController.pagesLink;
            Type pageType = typeof(T);

            if (!pagesLink.TryGetValue(pageType, out UIPage page))
            {
                Debug.LogError($"[UIController]: Page type {pageType.Name} was not found in the registered pages!");

                return;
            }

            if (page.IsPageDisplayed)
            {
                OnPageClosed(page);
            }
        }

        /// <summary>Returns <c>true</c> if the page of type <typeparamref name="T"/> is currently visible.</summary>
        public static bool IsDisplayed<T>() where T : UIPage
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return false;
            }

            Dictionary<Type, UIPage> pagesLink = uiController.pagesLink;
            Type pageType = typeof(T);

            if (!pagesLink.TryGetValue(pageType, out UIPage page))
            {
                Debug.LogError($"[UIController]: Page type {pageType.Name} was not found in the registered pages!");

                return false;
            }

            return page.IsPageDisplayed;
        }

        /// <summary>
        /// Called by a page's hide animation when it completes. Disables the canvas, fires events,
        /// handles popup/pause-popup cleanup, invokes the registered close callback, and destroys
        /// auto-destroy cached pages.
        /// </summary>
        internal static void OnPageClosed(UIPage page)
        {
            page.DisableCanvas();

            PageClosed?.Invoke(page, page.GetType());

            if (page.IsPopup && uiController.popupWindows.Remove(page))
            {
                if (page.IsPausePopup)
                {
                    uiController.pausePopups.Remove(page);

                    if (uiController.pausePopups.Count == 0 && uiController.usePausePopups)
                        Time.timeScale = 1.0f;
                }

                PopupClosed?.Invoke(page);
            }

            if (uiController.pageClosedCallbacks.TryGetValue(page, out SimpleCallback pageCallback))
            {
                uiController.pageClosedCallbacks.Remove(page);
                pageCallback.Invoke();
            }

            if(page.IsCached)
            {
                Type pageType = page.GetType();

                CachedPages.PageData data = uiController.cachedPages.GetPageData(pageType);
                if (data != null)
                {
                    if(data.AutoDestroy)
                    {
                        uiController.pagesLink.Remove(pageType);
                        uiController.pages.Remove(page);

                        Destroy(page.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// Called by a page's show animation when it completes. Fires <see cref="PageOpened"/>,
        /// and if <see cref="UIPage.IsPopup"/> is true, registers it and pauses the game
        /// when <see cref="UIPage.IsPausePopup"/> is also true.
        /// </summary>
        internal static void OnPageOpened(UIPage page)
        {
            PageOpened?.Invoke(page, page.GetType());

            if (page.IsPopup && uiController.popupWindows.Add(page))
            {
                if (page.IsPausePopup)
                {
                    uiController.pausePopups.Add(page);

                    if (uiController.usePausePopups)
                        Time.timeScale = 0.0f;
                }

                PopupOpened?.Invoke(page);
            }
        }

        /// <summary>
        /// Returns the page instance of type <typeparamref name="T"/>.
        /// If it is a cached page not yet instantiated, instantiates it first.
        /// </summary>
        public static T GetPage<T>() where T : UIPage
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return null;
            }

            Type pageType = typeof(T);

            Dictionary<Type, UIPage> pagesLink = uiController.pagesLink;
            if (!pagesLink.TryGetValue(pageType, out UIPage page))
            {
                if (!uiController.cachedPages.HasPage(pageType))
                {
                    Debug.LogError($"[UIController]: Page {pageType} not found in the scene and not linked as a cached page. Ensure it's instantiated or registered before showing.");

                    return null;
                }

                page = uiController.cachedPages.InstantiateCachedPage(pageType);
            }

            return (T)page;
        }

        /// <summary>
        /// Returns <c>true</c> if a page of type <typeparamref name="T"/> is registered
        /// (either as a scene-resident page or as a cached prefab).
        /// </summary>
        public static bool HasPage<T>() where T : UIPage
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return false;
            }

            Type pageType = typeof(T);

            if (uiController.pagesLink.ContainsKey(pageType))
                return true;

            if (uiController.cachedPages.HasPage(pageType))
                return true;

            return false;
        }

        /// <summary>
        /// Converts a world-space position (with <paramref name="offset"/>) into a screen-space point,
        /// clamping it so it never falls behind the camera.
        /// </summary>
        public static Vector3 FixUIElementToWorld(Transform target, Vector3 offset)
        {
            Vector3 targPos = target.transform.position + offset;
            Vector3 camForward = mainCamera.transform.forward;

            float distInFrontOfCamera = Vector3.Dot(targPos - (mainCamera.transform.position + camForward), camForward);
            if (distInFrontOfCamera < 0f)
            {
                targPos -= camForward * distInFrontOfCamera;
            }

            return RectTransformUtility.WorldToScreenPoint(mainCamera, targPos);
        }

        internal void RegisterCachedPage(Type pageType, UIPage page)
        {
            pagesLink.Add(pageType, page);
            pages.Add(page);
        }

        private void OnDestroy()
        {
            uiController = null;

            notchSafeArea?.Unload();

            PageOpened = null;
            PageClosed = null;

            PopupOpened = null;
            PopupClosed = null;

            if(usePausePopups)
                Time.timeScale = 1.0f;
        }

        /// <summary>Delegate for page open/close events.</summary>
        public delegate void PageCallback(UIPage page, Type pageType);
        /// <summary>Delegate for popup open/close events.</summary>
        public delegate void PopupCallback(UIPage page);
    }
}
