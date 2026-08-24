using UnityEngine;
using System.Collections.Generic;

namespace Watermelon
{
    /// <summary>
    /// Applies the device's <see cref="Screen.safeArea"/> to any registered <see cref="RectTransform"/> elements
    /// by adjusting their anchor min/max values. Supports named pixel offsets from external systems (e.g. banner ads).
    /// <para>Initialized by <see cref="UIController.InitPages"/>; accessed via static methods.</para>
    /// </summary>
    public class SafeAreaAdapter
    {
        private const string NOT_INITIALIZED_WARNING = "[SafeAreaAdapter]: Not initialized. Make sure UIController exists in the scene and that InitPages has been called.";

        private Vector2 virtualResolution;

        private readonly HashSet<RectTransform> registeredTransforms = new();
        private readonly Dictionary<string, Vector2> extraOffsets = new();

        private Rect lastSafeArea = Rect.zero;
        private Vector2Int lastScreenSize = Vector2Int.zero;
        private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

        /// <summary>Virtual-pixel inset from the top edge after applying the safe area and any extra offsets.</summary>
        public float TopOffset { get; private set; }
        /// <summary>Virtual-pixel inset from the bottom edge after applying the safe area and any extra offsets.</summary>
        public float BottomOffset { get; private set; }

        private static SafeAreaAdapter instance;

        public SafeAreaAdapter(Vector2 virtualResolution)
        {
            instance = this;

            this.virtualResolution = virtualResolution;

            RefreshInternal(true);
        }

        // ---- Instance ----

        private void RegisterInternal(RectTransform rectTransform)
        {
            if (registeredTransforms.Add(rectTransform))
                RefreshInternal(true);
        }

        private void UnregisterInternal(RectTransform rectTransform)
        {
            registeredTransforms.Remove(rectTransform);
        }

        private void SetOffsetInternal(string key, float bottom, float top)
        {
            extraOffsets[key] = new Vector2(bottom, top);
            RefreshInternal(true);
        }

        private void RemoveOffsetInternal(string key)
        {
            if (extraOffsets.Remove(key))
                RefreshInternal(true);
        }

        private void RefreshInternal(bool force = false)
        {
            Rect safeArea = Screen.safeArea;

            if (!force
                && safeArea == lastSafeArea
                && Screen.width == lastScreenSize.x
                && Screen.height == lastScreenSize.y
                && Screen.orientation == lastOrientation)
            {
                return;
            }

            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            ApplySafeArea(safeArea);
        }

        private void ApplySafeArea(Rect rect)
        {
            float totalBottom = 0;
            float totalTop = 0;

            foreach (Vector2 offset in extraOffsets.Values)
            {
                totalBottom += offset.x;
                totalTop += offset.y;
            }

            rect.y += totalBottom;
            rect.height -= totalBottom + totalTop;

            // Check for invalid screen startup state on some Samsung devices
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 anchorMin = rect.position;
            Vector2 anchorMax = rect.position + rect.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            // Fix for some Samsung devices (e.g. Note 10+, A71, S20) where Refresh gets called
            // twice and the first time returns NaN anchor coordinates
            if (anchorMin.x < 0 || anchorMin.y < 0 || anchorMax.x < 0 || anchorMax.y < 0) return;

            TopOffset = virtualResolution.y * (1f - anchorMax.y);
            BottomOffset = virtualResolution.y * anchorMin.y;

            registeredTransforms.RemoveWhere(t => t == null);

            foreach (RectTransform t in registeredTransforms)
            {
                t.anchorMin = anchorMin;
                t.anchorMax = anchorMax;
            }
        }

        // ---- Static facade ----

        /// <summary>Registers a <see cref="RectTransform"/> to be kept within the safe area. Triggers an immediate refresh.</summary>
        public static void RegisterRectTransform(RectTransform rectTransform)
        {
            if (instance == null) { LogManager.LogWarning(NOT_INITIALIZED_WARNING, LogCategory.Systems); return; }
            instance.RegisterInternal(rectTransform);
        }

        /// <summary>Unregisters a <see cref="RectTransform"/> so it is no longer updated on safe area changes.</summary>
        public static void UnregisterRectTransform(RectTransform rectTransform)
        {
            if (instance == null) { LogManager.LogWarning(NOT_INITIALIZED_WARNING, LogCategory.Systems); return; }
            instance.UnregisterInternal(rectTransform);
        }

        /// <summary>
        /// Adds or updates a named pixel offset applied to the safe area before anchor calculation.
        /// <param name="bottom">Pixels to shrink from the bottom edge.</param>
        /// <param name="top">Pixels to shrink from the top edge.</param>
        /// </summary>
        public static void SetOffset(string key, float bottom, float top = 0)
        {
            if (instance == null) { LogManager.LogWarning(NOT_INITIALIZED_WARNING, LogCategory.Systems); return; }
            instance.SetOffsetInternal(key, bottom, top);
        }

        public static void RemoveOffset(string key)
        {
            if (instance == null) { LogManager.LogWarning(NOT_INITIALIZED_WARNING, LogCategory.Systems); return; }
            instance.RemoveOffsetInternal(key);
        }

        /// <summary>
        /// Re-evaluates the safe area and updates all registered transforms.
        /// Pass <c>force = true</c> to skip the change-detection check (e.g. after a screen rotation).
        /// </summary>
        public static void Refresh(bool force = false)
        {
            if (instance == null) { LogManager.LogWarning(NOT_INITIALIZED_WARNING, LogCategory.Systems); return; }
            instance.RefreshInternal(force);
        }

        public static float GetTopOffset() => instance != null ? instance.TopOffset : 0;
        public static float GetBottomOffset() => instance != null ? instance.BottomOffset : 0;

        public void Unload()
        {
            instance = null;
        }
    }
}
