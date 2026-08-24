using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    // Full-screen dimmer with an animatable "hole" that leaves one rectangular region of the
    // screen undarkened and lets raycasts pass through to whatever is underneath — used to
    // spotlight board cells / UI elements across FirstStartTutorial's steps. A null bounds Show()
    // degrades to a flat full-screen dim with no cutout (raycasts always blocked), the same
    // shape TutorialCanvasController's own fadeCanvasGroup dim takes for its (separate) use case.
    public class TutorialSpotlightMaskController : MaskableGraphic, ICanvasRaycastFilter, ITutorialCanvasElement
    {
        private static readonly int HoleRectId = Shader.PropertyToID("_HoleRect");
        private static readonly int HasHoleId = Shader.PropertyToID("_HasHole");
        private static readonly int CornerRadiusId = Shader.PropertyToID("_CornerRadius");
        private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
        private static readonly int DimColorId = Shader.PropertyToID("_DimColor");

        [SerializeField] private Canvas hostCanvas;
        [SerializeField] private float cornerRadius = 24f;
        [SerializeField] private float softness = 12f;
        [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.75f);

        private static TutorialSpotlightMaskController instance;

        public static RectTransform RectTransform => instance != null ? instance.rectTransform : null;

        private Material materialInstance;
        private Rect currentHoleLocal;
        private bool hasHole;
        private TweenCase morphTweenCase;

        // Called by TutorialCanvasController.Init instead of Awake — keeps setup order
        // deterministic and avoids creating the material in edit mode (Graphic is ExecuteAlways).
        public void Init()
        {
            instance = this;
            raycastTarget = true;

            if (hostCanvas == null)
                hostCanvas = GetComponentInParent<Canvas>();

            Shader shader = Shader.Find("Watermelon/UI/TutorialSpotlightMask");
            materialInstance = new Material(shader);
            material = materialInstance;

            materialInstance.SetColor(DimColorId, dimColor);
            materialInstance.SetFloat(CornerRadiusId, cornerRadius);
            materialInstance.SetFloat(SoftnessId, softness);
            materialInstance.SetFloat(HasHoleId, 0f);

            enabled = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            morphTweenCase.KillActive();

            if (instance == this)
                instance = null;

            if (materialInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(materialInstance);
                else
                    DestroyImmediate(materialInstance);
            }
        }

        public static void Show(Rect? holeLocalRect, bool animate = true, float duration = 0.35f)
        {
            if (instance == null) return;
            instance.ShowInternal(holeLocalRect, animate, duration);
        }

        public static void Hide()
        {
            if (instance == null) return;
            instance.HideInternal();
        }

        private void ShowInternal(Rect? holeLocalRect, bool animate, float duration)
        {
            if (hostCanvas != null)
                hostCanvas.enabled = true;

            enabled = true;
            morphTweenCase.KillActive();

            if (holeLocalRect == null)
            {
                hasHole = false;
                materialInstance.SetFloat(HasHoleId, 0f);
                SetVerticesDirty();
                return;
            }

            Rect targetRect = holeLocalRect.Value;

            if (!animate || !hasHole)
            {
                hasHole = true;
                currentHoleLocal = targetRect;
                ApplyHoleRect(currentHoleLocal);
                return;
            }

            Rect startRect = currentHoleLocal;
            hasHole = true;

            morphTweenCase = Tween.DoFloat(0f, 1f, duration, t =>
            {
                currentHoleLocal = LerpRect(startRect, targetRect, t);
                ApplyHoleRect(currentHoleLocal);
            });
        }

        private void HideInternal()
        {
            morphTweenCase.KillActive();
            hasHole = false;
            enabled = false;
        }

        private void ApplyHoleRect(Rect rect)
        {
            materialInstance.SetFloat(HasHoleId, 1f);
            materialInstance.SetVector(HoleRectId, new Vector4(rect.xMin, rect.yMin, rect.xMax, rect.yMax));
            SetVerticesDirty();
        }

        private static Rect LerpRect(Rect a, Rect b, float t)
        {
            return Rect.MinMaxRect(
                Mathf.Lerp(a.xMin, b.xMin, t),
                Mathf.Lerp(a.yMin, b.yMin, t),
                Mathf.Lerp(a.xMax, b.xMax, t),
                Mathf.Lerp(a.yMax, b.yMax, t));
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (!hasHole) return true; // no hole => this graphic is hit => swallows input everywhere

            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 local);
            return !currentHoleLocal.Contains(local); // true = blocks; false = passes through
        }
    }
}
