using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Tween extensions for core uGUI layout and scroll components.
    /// </summary>
    public static class UITweenCases
    {
        #region Extensions
        /// <summary>Animates <see cref="LayoutElement.preferredHeight"/> from its current value to <paramref name="resultValue"/>. Useful for expand/collapse animations.</summary>
        public static TweenCase DOPreferredHeight(this LayoutElement tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new LayoutElementPrefferedHeight(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>Animates <see cref="CanvasGroup.alpha"/> from its current value to <paramref name="resultValue"/> (0 = transparent, 1 = opaque).</summary>
        public static TweenCase DOFade(this CanvasGroup tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new CanvasGroupFade(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>Animates <see cref="ScrollRect.horizontalNormalizedPosition"/> from its current value to <paramref name="resultValue"/> (0 = left, 1 = right).</summary>
        public static TweenCase DOHorizontalNormalizedPos(this ScrollRect tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new ScrollRectNormPosHorizontal(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>Animates <see cref="ScrollRect.verticalNormalizedPosition"/> from its current value to <paramref name="resultValue"/> (0 = bottom, 1 = top).</summary>
        public static TweenCase DOVerticalNormalizedPos(this ScrollRect tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new ScrollRectNormPosVertical(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        /// <summary>Interpolates <see cref="LayoutElement.preferredHeight"/> from its starting value to <c>resultValue</c>.</summary>
        public class LayoutElementPrefferedHeight : TweenCaseFunction<LayoutElement, float>
        {
            public LayoutElementPrefferedHeight(LayoutElement tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.preferredHeight;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.preferredHeight = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.preferredHeight = Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>Interpolates <see cref="CanvasGroup.alpha"/> from its starting value to <c>resultValue</c>.</summary>
        public class CanvasGroupFade : TweenCaseFunction<CanvasGroup, float>
        {
            public CanvasGroupFade(CanvasGroup tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.alpha;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.alpha = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.alpha = Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>Interpolates <see cref="ScrollRect.horizontalNormalizedPosition"/> from its starting value to <c>resultValue</c>.</summary>
        public class ScrollRectNormPosHorizontal : TweenCaseFunction<ScrollRect, float>
        {
            public ScrollRectNormPosHorizontal(ScrollRect tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.horizontalNormalizedPosition;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.horizontalNormalizedPosition = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.horizontalNormalizedPosition = Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>Interpolates <see cref="ScrollRect.verticalNormalizedPosition"/> from its starting value to <c>resultValue</c>.</summary>
        public class ScrollRectNormPosVertical : TweenCaseFunction<ScrollRect, float>
        {
            public ScrollRectNormPosVertical(ScrollRect tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.verticalNormalizedPosition;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.verticalNormalizedPosition = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.verticalNormalizedPosition = Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }
    }
}
