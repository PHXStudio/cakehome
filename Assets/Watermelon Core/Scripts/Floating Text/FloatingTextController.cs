using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class FloatingTextController : MonoBehaviour
    {
        private static FloatingTextController floatingTextController;

        [SerializeField] FloatingTextCase[] floatingTextCases;
        private Dictionary<int, FloatingTextCase> floatingTextLink;

        [Tooltip("Minimal gap between a UI floating text's rendered bounds and the screen edge — spawn positions are snapped inward so the whole text stays visible. In canvas units.")]
        [SerializeField] float screenEdgeMargin = 24f;

        public void Init()
        {
            floatingTextController = this;

            floatingTextLink = new Dictionary<int, FloatingTextCase>();
            for (int i = 0; i < floatingTextCases.Length; i++)
            {
                FloatingTextCase floatingText = floatingTextCases[i];
                if(string.IsNullOrEmpty(floatingText.Name))
                {
                    Debug.LogError("[Floating Text]: Floating Text initialization failed. A unique name (ID) must be provided. Please ensure the 'name' field is not empty before proceeding.", this);

                    continue;
                }

                if (floatingText.FloatingTextBehavior == null)
                {
                    Debug.LogError(string.Format("Floating Text ({0}) initialization failed. No Floating Text Behavior linked. Please assign a valid Floating Text Behavior before proceeding.", floatingText.Name), this);

                    continue;
                }

                floatingText.Init();

                floatingTextLink.Add(floatingText.Name.GetHashCode(), floatingText);
            }
        }

        private void OnDestroy()
        {
            if(!floatingTextCases.IsNullOrEmpty())
            {
                for (int i = 0; i < floatingTextCases.Length; i++)
                {
                    PoolManager.DestroyPool(floatingTextCases[i].FloatingTextPool);
                }
            }
        }

        public static FloatingTextBaseBehavior SpawnFloatingText(string floatingTextName, Vector3 position)
        {
            return SpawnFloatingText(floatingTextName.GetHashCode(), string.Empty, position, Quaternion.identity, 1.0f, Color.white);
        }

        public static FloatingTextBaseBehavior SpawnFloatingText(int floatingTextNameHash, Vector3 position)
        {
            return SpawnFloatingText(floatingTextNameHash, string.Empty, position, Quaternion.identity, 1.0f, Color.white);
        }

        public static FloatingTextBaseBehavior SpawnFloatingText(string floatingTextName, string text, Vector3 position)
        {
            return SpawnFloatingText(floatingTextName.GetHashCode(), text, position, Quaternion.identity, 1.0f, Color.white);
        }

        public static FloatingTextBaseBehavior SpawnFloatingText(int floatingTextNameHash, string text, Vector3 position)
        {
            return SpawnFloatingText(floatingTextNameHash, text, position, Quaternion.identity, 1.0f, Color.white);
        }

        public static FloatingTextBaseBehavior SpawnFloatingText(string floatingTextName, string text, Vector3 position, Quaternion rotation)
        {
            return SpawnFloatingText(floatingTextName.GetHashCode(), text, position, rotation, 1.0f, Color.white);
        }

        public static FloatingTextBaseBehavior SpawnFloatingText(int floatingTextNameHash, string text, Vector3 position, Quaternion rotation)
        {
            return SpawnFloatingText(floatingTextNameHash, text, position, rotation, 1.0f, Color.white);
        }

        public static FloatingTextBaseBehavior SpawnFloatingText(string floatingTextName, string text, Vector3 position, Quaternion rotation, float scaleMultiplier)
        {
            return SpawnFloatingText(floatingTextName.GetHashCode(), text, position, rotation, scaleMultiplier, Color.white);
        }

        public static FloatingTextBaseBehavior SpawnFloatingText(int floatingTextNameHash, string text, Vector3 position, Quaternion rotation, float scaleMultiplier)
        {
            return SpawnFloatingText(floatingTextNameHash, text, position, rotation, scaleMultiplier, Color.white);
        }

        public static FloatingTextBaseBehavior SpawnFloatingText(string floatingTextName, string text, Vector3 position, Quaternion rotation, float scaleMultiplier, Color color)
        {
            return SpawnFloatingText(floatingTextName.GetHashCode(), text, position, rotation, scaleMultiplier, color);
        }

        public static FloatingTextBaseBehavior SpawnFloatingText(int floatingTextNameHash, string text, Vector3 position, Quaternion rotation, float scaleMultiplier, Color color)
        {
            if (floatingTextController.floatingTextLink.ContainsKey(floatingTextNameHash))
            {
                FloatingTextCase floatingTextCase = floatingTextController.floatingTextLink[floatingTextNameHash];

                GameObject floatingTextObject = floatingTextCase.FloatingTextPool.GetPooledObject();
                FloatingTextBaseBehavior floatingTextBehavior = floatingTextObject.GetComponent<FloatingTextBaseBehavior>();

                Vector2 textSize = floatingTextBehavior.GetTextSize(text, scaleMultiplier);
                floatingTextObject.transform.position = ClampToScreen(floatingTextCase, position, textSize);
                floatingTextObject.transform.rotation = rotation;
                floatingTextObject.SetActive(true);

                floatingTextBehavior.Activate(text, scaleMultiplier, color);

                return floatingTextBehavior;
            }

            return null;
        }

        // Snaps a UI floating text's spawn position inside the root canvas rect (== the screen)
        // so texts requested near the screen edges don't render partly off-screen. The margin
        // accounts for the text's rendered half-size (position is its center) plus the
        // configured edge gap. Clamped against the root canvas rather than the pool container —
        // containers may be sized differently than the screen. World-space cases (no container)
        // pass through unchanged.
        private static Vector3 ClampToScreen(FloatingTextCase floatingTextCase, Vector3 position, Vector2 textSize)
        {
            Transform container = floatingTextCase.ObjectsContainer;
            if (container == null) return position;

            Canvas canvas = container.GetComponentInParent<Canvas>();
            if (canvas == null) return position;

            RectTransform canvasRect = (RectTransform)canvas.rootCanvas.transform;
            float marginX = floatingTextController.screenEdgeMargin + textSize.x * 0.5f;
            float marginY = floatingTextController.screenEdgeMargin + textSize.y * 0.5f;
            Rect rect = canvasRect.rect;
            Vector3 local = canvasRect.InverseTransformPoint(position);
            local.x = Mathf.Clamp(local.x, rect.xMin + marginX, rect.xMax - marginX);
            local.y = Mathf.Clamp(local.y, rect.yMin + marginY, rect.yMax - marginY);
            return canvasRect.TransformPoint(local);
        }

        public static FloatingTextBaseBehavior GetFloatingText(string floatingTextName)
        {
            return GetFloatingText(floatingTextName.GetHashCode());
        }

        public static FloatingTextBaseBehavior GetFloatingText(int floatingTextNameHash)
        {
            if (floatingTextController.floatingTextLink.ContainsKey(floatingTextNameHash))
            {
                FloatingTextCase floatingTextCase = floatingTextController.floatingTextLink[floatingTextNameHash];

                GameObject floatingTextObject = floatingTextCase.FloatingTextPool.GetPooledObject();

                return floatingTextObject.GetComponent<FloatingTextBaseBehavior>();
            }

            return null;
        }

        public static void Unload()
        {
            FloatingTextCase[] floatingTextCases = floatingTextController.floatingTextCases;
            for (int i = 0; i < floatingTextCases.Length; i++)
            {
                floatingTextCases[i].FloatingTextPool.ReturnToPoolEverything(true);
            }
        }
    }
}