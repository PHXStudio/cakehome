using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Manages a fullscreen overlay panel used for scene transitions and loading screens.
    /// Finds an <see cref="IOverlayPanel"/> among direct children of the provided GameObject,
    /// or creates a <see cref="DummyOverlayPanel"/> as a fallback.
    /// Initialized via <see cref="OverlayPreInitializer"/>; accessed through the static <see cref="Show"/> and <see cref="Hide"/> methods.
    /// </summary>
    public class Overlay
    {
        private const string NOT_INITIALIZED_ERROR = "[Overlay]: Not initialized. Add the OverlayPreInitializer component to the Initializer prefab.";

        private static Overlay instance;

        private IOverlayPanel panel;

        public Overlay(GameObject parentObject)
        {
            instance = this;

            panel = FindOverlayPanel(parentObject.transform);

            if(panel == null)
                panel = CreateDummyOverlay(parentObject.transform);

            panel.Init();

            panel.SetState(false);
            panel.SetLoadingState(false);
        }

        /// <summary>
        /// Fades the overlay in over <paramref name="duration"/> seconds, then invokes <paramref name="onCompleted"/>.
        /// Has no effect if the overlay is already active.
        /// </summary>
        public static void Show(float duration, SimpleCallback onCompleted, bool showLoadingAnimation = false)
        {
            if (instance == null) { Debug.LogError(NOT_INITIALIZED_ERROR); return; }

            IOverlayPanel panel = instance.panel;
            if (panel == null) return;
            if (panel.IsActive) return;

            panel.SetState(true);
            panel.Show(duration, onCompleted);

            if(showLoadingAnimation)
                panel.SetLoadingState(true);
        }

        /// <summary>
        /// Fades the overlay out over <paramref name="duration"/> seconds, then invokes <paramref name="onCompleted"/>
        /// and disables the loading animation. Has no effect if the overlay is not active.
        /// </summary>
        public static void Hide(float duration, SimpleCallback onCompleted = null)
        {
            if (instance == null) { Debug.LogError(NOT_INITIALIZED_ERROR); return; }

            IOverlayPanel panel = instance.panel;
            if (panel == null) return;
            if (!panel.IsActive) return;

            panel.Hide(duration, () =>
            {
                panel.SetState(false);
                panel.SetLoadingState(false);

                onCompleted?.Invoke();
            });
        }

        private static IOverlayPanel FindOverlayPanel(Transform parentTransform)
        {
            foreach (Transform child in parentTransform)
            {
                Component component = child.GetComponent(typeof(IOverlayPanel));
                if (component != null)
                    return (IOverlayPanel)component;
            }

            return null;
        }

        private IOverlayPanel CreateDummyOverlay(Transform parentTransform)
        {
            GameObject canvasObject = new("[TEMP OVERLAY]");
            canvasObject.transform.SetParent(parentTransform);
            canvasObject.transform.ResetLocal();
            canvasObject.layer = LayerMask.NameToLayer("UI");

            RectTransform rt = canvasObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.sizeDelta = Vector2.zero;

            Canvas overlayCanvas = canvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 999;

            canvasObject.AddComponent<GraphicRaycaster>();

            return canvasObject.AddComponent<DummyOverlayPanel>();
        }

        public void Unload()
        {
            panel?.Clear();
            panel = null;

            instance = null;
        }
    }
}
