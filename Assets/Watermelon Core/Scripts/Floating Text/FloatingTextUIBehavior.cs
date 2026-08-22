using UnityEngine;

namespace Watermelon
{
    public class FloatingTextUIBehavior : FloatingTextBaseBehavior
    {
        [Space]
        [SerializeField] Vector2 offset;
        [SerializeField] float time;
        [SerializeField] Ease.Type easing;

        [Space]
        [SerializeField] float scaleTime;
        [SerializeField] AnimationCurve scaleAnimationCurve;

        [Space]
        [SerializeField] float fadeDelay;
        [SerializeField] float fadeTime;
        [SerializeField] Ease.Type fadeEasing;

        private RectTransform rectTransform;
        private Vector3 defaultScale;

        private TweenCase scaleTween;
        private TweenCase moveTween;
        private TweenCase fadeTween;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            defaultScale = rectTransform.localScale;
        }

        public override void Activate(string text, float scaleMultiplier, Color color)
        {
            textRef.text = text;

            color.a = 1.0f;
            textRef.color = color;

            rectTransform.localScale = Vector3.zero;
            scaleTween = rectTransform.DOScale(defaultScale * scaleMultiplier, scaleTime).SetCurveEasing(scaleAnimationCurve);

            Vector2 startAnchoredPosition = rectTransform.anchoredPosition;
            moveTween = rectTransform.DOAnchoredPosition(startAnchoredPosition + offset, time).SetEasing(easing).OnComplete(delegate
            {
                gameObject.SetActive(false);

                InvokeCompleteEvent();
            });

            fadeTween = textRef.DOFade(0.0f, fadeTime, fadeDelay).SetEasing(fadeEasing);
        }

        public void Reset()
        {
            scaleTween.KillActive();
            moveTween.KillActive();
            fadeTween.KillActive();
        }
    }
}
