using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// An <see cref="Ease.IEasingFunction"/> that evaluates a Unity <see cref="AnimationCurve"/>.
    /// Created automatically by <see cref="TweenCase.SetCurveEasing"/> — use that method to attach
    /// a custom curve to any tween without manually instantiating this class.
    ///
    /// <para>The curve is sampled over its full time range (from 0 to the time of the last keyframe),
    /// so you do not need to normalise the curve's time axis to [0, 1] before passing it.</para>
    /// </summary>
    public class AnimationCurveEasingFunction : Ease.IEasingFunction
    {
        private AnimationCurve easingCurve;
        private float totalEasingTime;

        /// <summary>
        /// Wraps <paramref name="easingCurve"/> for use as a tween easing function.
        /// Pre-computes the total curve time to avoid repeated key array access during playback.
        /// </summary>
        /// <param name="easingCurve">The animation curve to use. Must have at least one keyframe.</param>
        public AnimationCurveEasingFunction(AnimationCurve easingCurve)
        {
            this.easingCurve = easingCurve;

            totalEasingTime = easingCurve.keys[easingCurve.keys.Length - 1].time;
        }

        /// <summary>
        /// Evaluates the curve at <c>p * totalEasingTime</c>, mapping the normalised progress
        /// <paramref name="p"/> to the curve's full time range.
        /// </summary>
        /// <param name="p">Normalised input progress in [0, 1].</param>
        /// <returns>The curve's output value at the corresponding time.</returns>
        public float Interpolate(float p)
        {
            return easingCurve.Evaluate(p * totalEasingTime);
        }
    }
}
