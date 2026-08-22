using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class MergeFlyingIcon : MonoBehaviour
    {
        private Image image;
        private Image shadowImage;
        private RectTransform rectTransform;
        private TweenCase flightTween;

        // Icon lives on a child ("Icon") rather than this root GameObject so "Shadow" can be
        // its sibling, placed earlier in the hierarchy — in uGUI a later sibling draws on top
        // of an earlier one, so this is what keeps the shadow behind the icon instead of over it.
        private void Awake()
        {
            rectTransform = (RectTransform)transform;

            image = transform.Find("Icon").GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;

            Transform shadowTransform = transform.Find("Shadow");
            if (shadowTransform != null)
                shadowImage = shadowTransform.GetComponent<Image>();
        }

        public void Show(Sprite sprite, Vector2 size, Vector2 anchoredPosition)
        {
            flightTween.KillActive();

            image.sprite = sprite;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = anchoredPosition;
            transform.SetAsLastSibling();

            if (shadowImage != null)
                shadowImage.enabled = false;
        }

        // Mirrors source's live shadow (sprite/offset/scale, including any CustomShadow
        // override) onto this icon's own shadow child, so the shadow flies along with it
        // instead of being left behind on the field object (which hides its shadow too —
        // see MergeFieldObject.OnVisualsStateChanged).
        public void Show(MergeFieldObject source, Vector2 anchoredPosition)
        {
            Show(source.CurrentSprite, ((RectTransform)source.transform).rect.size, anchoredPosition);

            if (shadowImage != null)
                source.CopyShadowVisualTo(shadowImage);
        }

        public Vector2 AnchoredPosition => rectTransform.anchoredPosition;

        public void SetAnchoredPosition(Vector2 pos) => rectTransform.anchoredPosition = pos;
        public void SetScale(float scale) => rectTransform.localScale = Vector3.one * scale;

        public void FlyTo(Vector2 target, float duration, Ease.Type ease, SimpleCallback onComplete)
        {
            flightTween.KillActive();
            flightTween = rectTransform.DOAnchoredPosition(target, duration)
                .SetEasing(ease)
                .OnComplete(() => { Release(); onComplete?.Invoke(); });
        }

        // Flies a dug arc that undershoots the target, then a small secondary
        // arc ("bounce") covers the remaining distance to land on it exactly.
        public void FlyTo(Vector2 target, FlightSettings settings, SimpleCallback onComplete)
        {
            flightTween.KillActive();

            Vector2 startPos = rectTransform.anchoredPosition;
            float distance = Vector2.Distance(startPos, target);
            float arcHeight = Mathf.Clamp(distance * settings.ArcHeightRatio, settings.MinArcHeight, settings.MaxArcHeight);
            Vector2 undershootPos = Vector2.LerpUnclamped(startPos, target, settings.UndershootRatio);

            flightTween = Tween.DoFloat(0f, 1f, settings.MainDuration, t =>
            {
                Vector2 pos = Vector2.LerpUnclamped(startPos, undershootPos, t);
                float heightFraction = settings.ArcCurve.Evaluate(t);
                pos.y += heightFraction * arcHeight;
                rectTransform.anchoredPosition = pos;

                rectTransform.localScale = Vector3.one * settings.ScaleCurve.Evaluate(heightFraction);
            })
            .OnComplete(() =>
            {
                flightTween = Tween.DoFloat(0f, 1f, settings.BounceDuration, t =>
                {
                    Vector2 pos = Vector2.LerpUnclamped(undershootPos, target, Mathf.SmoothStep(0f, 1f, t));
                    float heightFraction = settings.BounceCurve.Evaluate(t) * settings.BounceHeightRatio;
                    pos.y += heightFraction * arcHeight;
                    rectTransform.anchoredPosition = pos;

                    rectTransform.localScale = Vector3.one * settings.ScaleCurve.Evaluate(heightFraction);
                })
                .OnComplete(() => { Release(); onComplete?.Invoke(); });
            });
        }

        [System.Serializable]
        public class FlightSettings
        {
            [Header("Arc")]
            [SerializeField] float mainDuration = 0.32f;
            [SerializeField] AnimationCurve arcCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
            [SerializeField] float arcHeightRatio = 0.35f;
            [SerializeField] float minArcHeight = 40f;
            [SerializeField] float maxArcHeight = 160f;

            [Header("Landing Bounce")]
            [Range(0.5f, 1f)] [SerializeField] float undershootRatio = 0.85f;
            [SerializeField] float bounceDuration = 0.2f;
            [SerializeField] AnimationCurve bounceCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
            [SerializeField] float bounceHeightRatio = 0.12f;

            [Header("Scale")]
            [Tooltip("X = current arc height as a fraction of the main arc's peak height (0 = ground, 1 = peak).")]
            [SerializeField] AnimationCurve scaleCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1.15f));

            public float MainDuration => mainDuration;
            public AnimationCurve ArcCurve => arcCurve;
            public float ArcHeightRatio => arcHeightRatio;
            public float MinArcHeight => minArcHeight;
            public float MaxArcHeight => maxArcHeight;

            public float UndershootRatio => undershootRatio;
            public float BounceDuration => bounceDuration;
            public AnimationCurve BounceCurve => bounceCurve;
            public float BounceHeightRatio => bounceHeightRatio;

            public AnimationCurve ScaleCurve => scaleCurve;
        }

        // Plain straight-line flight (no arc/bounce) — for UI-originated flights (e.g. task cards),
        // pairs with the simple FlyTo(target, duration, ease, onComplete) overload.
        [System.Serializable]
        public class SimpleFlightSettings
        {
            [SerializeField] float duration = 0.3f;
            [SerializeField] Ease.Type easing = Ease.Type.CubicOut;

            public float Duration => duration;
            public Ease.Type Easing => easing;
        }

        // Inactive == available for reuse by the pool — mirrors FloatingTextController's lifecycle.
        public void Release()
        {
            flightTween.KillActive();
            rectTransform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }
    }
}
