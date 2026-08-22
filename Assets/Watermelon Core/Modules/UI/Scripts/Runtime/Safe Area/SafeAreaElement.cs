using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Add to any RectTransform inside a <see cref="UIPage"/> to automatically register it with
    /// <see cref="SafeAreaAdapter"/>. The transform's anchors will be adjusted to respect the device safe area
    /// whenever the page becomes visible or the safe area changes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaElement : MonoBehaviour, IUIPageElement
    {
        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            SafeAreaAdapter.RegisterRectTransform(rectTransform);
        }

        private void OnDisable()
        {
            SafeAreaAdapter.UnregisterRectTransform(rectTransform);
        }

        void IUIPageElement.Init(UIPage page) { }

        void IUIPageElement.OnPageStateChanged(bool state)
        {
            if (state)
                SafeAreaAdapter.Refresh(true);
        }
    }
}
