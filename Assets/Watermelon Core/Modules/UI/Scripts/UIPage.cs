using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Abstract base class for all UI screens (pages). Each page is a full-screen canvas with its own
    /// <see cref="UnityEngine.UI.Canvas"/> and <see cref="UnityEngine.UI.GraphicRaycaster"/>.
    /// Subclasses implement <see cref="Init"/>, <see cref="OnShow"/>, and <see cref="OnHide"/>;
    /// at the end of the show/hide animation they must call <see cref="NotifyOpened"/>
    /// or <see cref="NotifyClosed"/> respectively.
    /// </summary>
    [RequireComponent(typeof(Canvas)), RequireComponent(typeof(GraphicRaycaster))]
    public abstract class UIPage : MonoBehaviour, ISceneSavingReceiver
    {
        [Hide]
        [SerializeField] Component[] registeredElements;

        protected bool isPageDisplayed;
        /// <summary>Returns <c>true</c> if the page canvas is currently enabled and visible.</summary>
        public bool IsPageDisplayed => isPageDisplayed;

        protected Canvas canvas;
        /// <summary>The <see cref="UnityEngine.UI.Canvas"/> component on this page's root GameObject.</summary>
        public Canvas Canvas => canvas;

        protected GraphicRaycaster graphicRaycaster;
        /// <summary>The <see cref="UnityEngine.UI.GraphicRaycaster"/> component on this page's root GameObject.</summary>
        public GraphicRaycaster GraphicRaycaster => graphicRaycaster;

        private string defaultName;

        private IUIPageElement[] pageElements;

        protected bool isCached;
        /// <summary>Returns <c>true</c> if this page was instantiated from a cached prefab.</summary>
        public bool IsCached => isCached;

        /// <summary>Override and return <c>true</c> to mark this page as a popup window tracked by <see cref="UIController"/>.</summary>
        public virtual bool IsPopup => false;
        /// <summary>Override and return <c>true</c> to pause the game (<c>Time.timeScale = 0</c>) while this popup is open. Requires <see cref="IsPopup"/> to also return <c>true</c>.</summary>
        public virtual bool IsPausePopup => false;

        internal void CacheComponents()
        {
            defaultName = name;

            canvas = GetComponent<Canvas>();
            graphicRaycaster = GetComponent<GraphicRaycaster>();
        }

        internal void PreparePage()
        {
            isPageDisplayed = false;
            canvas.enabled = false;

            pageElements = new IUIPageElement[registeredElements.Length];
            for (int i = 0; i < pageElements.Length; i++)
            {
                pageElements[i] = (IUIPageElement)registeredElements[i];
                pageElements[i].Init(this);
            }
        }

        /// <summary>
        /// Called by <see cref="UIController.InitPages"/> after all game modules are initialized.
        /// Override to subscribe to events, bind UI elements, or perform any setup that requires module access.
        /// </summary>
        public abstract void Init();

        internal void EnableCanvas()
        {
            isPageDisplayed = true;

            canvas.enabled = true;

            if(!pageElements.IsNullOrEmpty())
            {
                for (int i = 0; i < pageElements.Length; i++)
                {
                    pageElements[i]?.OnPageStateChanged(true);
                }
            }

#if UNITY_EDITOR
            name = string.Format("{0} (Active)", defaultName);
#endif
        }

        internal void DisableCanvas()
        {
            isPageDisplayed = false;

            canvas.enabled = false;

            if (!pageElements.IsNullOrEmpty())
            {
                for (int i = 0; i < pageElements.Length; i++)
                {
                    pageElements[i]?.OnPageStateChanged(false);
                }
            }

#if UNITY_EDITOR
            name = defaultName;
#endif
        }

        /// <summary>Override to play the show animation. Call <see cref="NotifyOpened"/> when complete.</summary>
        protected abstract void OnShow();
        /// <summary>Override to play the hide animation. Call <see cref="NotifyClosed"/> when complete.</summary>
        protected abstract void OnHide();
        /// <summary>Override to run custom cleanup before the page is unloaded. No need to call base.</summary>
        protected virtual void OnUnload() { }

        internal void TriggerShow() => OnShow();
        internal void TriggerHide() => OnHide();

        internal void TriggerUnload()
        {
            OnUnload();

            isPageDisplayed = false;
            canvas.enabled = false;
        }

        /// <summary>Call at the end of <see cref="OnShow"/> to signal the controller that the page is open.</summary>
        protected void NotifyOpened() => UIController.OnPageOpened(this);
        /// <summary>Call at the end of <see cref="OnHide"/> to signal the controller that the page is closed.</summary>
        protected void NotifyClosed() => UIController.OnPageClosed(this);

        internal void MarkAsCached()
        {
            isCached = true;
        }

        public bool OnPrefabSaving()
        {
            IUIPageElement[] current = GetComponentsInChildren<IUIPageElement>();
            if (!NeedsElementsUpdate(current)) return false;

            registeredElements = ToComponentArray(current);
            return true;
        }

        public void OnSceneSaving()
        {
            IUIPageElement[] current = GetComponentsInChildren<IUIPageElement>();
            if (!NeedsElementsUpdate(current)) return;

            registeredElements = ToComponentArray(current);
            RuntimeEditorUtils.SetDirty(this);
        }

        private bool NeedsElementsUpdate(IUIPageElement[] current)
        {
            if (registeredElements == null || registeredElements.Length != current.Length)
                return true;

            for (int i = 0; i < registeredElements.Length; i++)
            {
                if (registeredElements[i] == null)
                    return true;
            }

            for (int i = 0; i < current.Length; i++)
            {
                if (!ReferenceEquals(registeredElements[i], current[i] as Component))
                    return true;
            }

            return false;
        }

        private static Component[] ToComponentArray(IUIPageElement[] elements)
        {
            Component[] result = new Component[elements.Length];
            for (int i = 0; i < elements.Length; i++)
                result[i] = elements[i] as Component;
            return result;
        }
    }
}