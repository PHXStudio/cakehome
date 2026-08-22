using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Tween extensions for <see cref="RectTransform"/> and <see cref="Graphic"/> components in UI (canvas) space.
    /// Covers anchored-position movement, shake, and <see cref="RectTransform.sizeDelta"/> scaling.
    /// All position values are in local canvas units (anchored coordinates), not world space.
    /// </summary>
    public static class RectTransformTweenCases
    {
        #region Extensions
        /// <summary>Animates <see cref="RectTransform.anchoredPosition"/> to the 2D <paramref name="resultValue"/>.</summary>
        public static TweenCase DOAnchoredPosition(this RectTransform tweenObject, Vector2 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnchoredPosition(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>Animates <see cref="RectTransform.anchoredPosition3D"/> to the 3D <paramref name="resultValue"/>.</summary>
        public static TweenCase DOAnchoredPosition(this RectTransform tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnchoredPosition3D(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>Animates the underlying <see cref="Graphic.rectTransform"/>'s <see cref="RectTransform.anchoredPosition"/> to <paramref name="resultValue"/>.</summary>
        public static TweenCase DOAnchoredPosition(this Graphic tweenObject, Vector2 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnchoredPosition(tweenObject.rectTransform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>Animates the underlying <see cref="Graphic.rectTransform"/>'s <see cref="RectTransform.anchoredPosition3D"/> to <paramref name="resultValue"/>.</summary>
        public static TweenCase DOAnchoredPosition(this Graphic tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnchoredPosition3D(tweenObject.rectTransform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Animates <see cref="RectTransform.anchoredPosition"/> to <paramref name="resultValue"/> while adding a vertical offset
        /// shaped by <paramref name="verticalOffset"/> (<see cref="AnimationCurve"/> evaluated over normalised progress 0–1).
        /// Useful for arc or bounce trajectories in UI space.
        /// </summary>
        public static TweenCase DOAnchoredPositionWithVerticalOffset(this RectTransform tweenObject, Vector2 resultValue, AnimationCurve verticalOffset, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnchoredPositionWithVerticalOffset(tweenObject, resultValue, verticalOffset).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Shakes <see cref="RectTransform.anchoredPosition"/> randomly within a circle of radius <paramref name="magnitude"/>.
        /// The element returns to its original position when the tween completes.
        /// </summary>
        public static TweenCase DOAnchoredPositionShake(this RectTransform tweenObject, float magnitude, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new Shake(tweenObject, magnitude).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Shakes the underlying <see cref="Graphic.rectTransform"/>'s <see cref="RectTransform.anchoredPosition"/> randomly within a circle of radius <paramref name="magnitude"/>.
        /// The element returns to its original position when the tween completes.
        /// </summary>
        public static TweenCase DOAnchoredPositionShake(this Graphic tweenObject, float magnitude, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new Shake(tweenObject.rectTransform, magnitude).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Scales <see cref="RectTransform.sizeDelta"/> uniformly by <paramref name="resultValue"/> relative to its current size
        /// (e.g. <c>2f</c> doubles the rect, <c>0.5f</c> halves it).
        /// </summary>
        public static TweenCase DOSizeScale(this RectTransform tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new SizeScale(tweenObject, tweenObject.sizeDelta * resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Scales the <see cref="Graphic.rectTransform"/>'s <see cref="RectTransform.sizeDelta"/> uniformly by <paramref name="resultValue"/> relative to its current size.
        /// </summary>
        public static TweenCase DOSizeScale(this Graphic tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new SizeScale(tweenObject.rectTransform, tweenObject.rectTransform.sizeDelta * resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>Animates <see cref="RectTransform.sizeDelta"/> to the absolute <paramref name="resultValue"/>.</summary>
        public static TweenCase DOSize(this RectTransform tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new SizeScale(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>Animates the <see cref="Graphic.rectTransform"/>'s <see cref="RectTransform.sizeDelta"/> to the absolute <paramref name="resultValue"/>.</summary>
        public static TweenCase DOSize(this Graphic tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new SizeScale(tweenObject.rectTransform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        /// <summary>Interpolates <see cref="RectTransform.anchoredPosition"/> (2D) from its starting value to <c>resultValue</c>.</summary>
        public class AnchoredPosition : TweenCaseFunction<RectTransform, Vector2>
        {
            public AnchoredPosition(RectTransform tweenObject, Vector2 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.anchoredPosition;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.anchoredPosition = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.anchoredPosition = Vector2.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>Interpolates <see cref="RectTransform.anchoredPosition3D"/> (3D) from its starting value to <c>resultValue</c>.</summary>
        public class AnchoredPosition3D : TweenCaseFunction<RectTransform, Vector3>
        {
            public AnchoredPosition3D(RectTransform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.anchoredPosition3D;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.anchoredPosition3D = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.anchoredPosition3D = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>
        /// Interpolates <see cref="RectTransform.anchoredPosition"/> to <c>resultValue</c> while modulating the Y component
        /// with an <see cref="AnimationCurve"/> evaluated at each normalised progress value.
        /// Useful for parabolic or custom-shaped UI movement arcs.
        /// </summary>
        public class AnchoredPositionWithVerticalOffset : TweenCaseFunction<RectTransform, Vector2>
        {
            private AnimationCurve verticalOffset;

            public AnchoredPositionWithVerticalOffset(RectTransform tweenObject, Vector2 resultValue, AnimationCurve verticalOffset) : base(tweenObject, resultValue)
            {
                this.verticalOffset = verticalOffset;

                parentObject = tweenObject.gameObject;

                startValue = tweenObject.anchoredPosition;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.anchoredPosition = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.anchoredPosition = Vector2.LerpUnclamped(startValue, new Vector2(resultValue.x, resultValue.y * verticalOffset.Evaluate(state)), Interpolate(state));
            }
        }

        /// <summary>
        /// Interpolates <see cref="RectTransform.sizeDelta"/> from its starting value to <c>resultValue</c>.
        /// Used by both <c>DOSizeScale</c> (relative) and <c>DOSize</c> (absolute) extension methods.
        /// </summary>
        public class SizeScale : TweenCaseFunction<RectTransform, Vector2>
        {
            public SizeScale(RectTransform tweenObject, Vector2 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.sizeDelta;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.sizeDelta = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.sizeDelta = Vector2.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>
        /// Shakes <see cref="RectTransform.anchoredPosition"/> each frame by a random offset inside a unit circle
        /// scaled by <c>magnitude × easedProgress</c>. Restores the original position on completion.
        /// </summary>
        public class Shake : TweenCase
        {
            private RectTransform tweenObject;
            private Vector2 startPosition;
            private float magnitude;

            public Shake(RectTransform tweenObject, float magnitude)
            {
                this.tweenObject = tweenObject;
                this.magnitude = magnitude;

                parentObject = tweenObject.gameObject;

                startPosition = tweenObject.anchoredPosition;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.anchoredPosition = startPosition;
            }

            public override void Invoke(float timeDelta)
            {
                tweenObject.anchoredPosition = startPosition + Random.insideUnitCircle * magnitude * Interpolate(state);
            }
        }
    }
}
