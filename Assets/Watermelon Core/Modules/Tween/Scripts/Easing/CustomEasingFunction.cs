using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// A named, serialisable <see cref="Ease.IEasingFunction"/> backed by an <see cref="AnimationCurve"/>.
    /// Custom easing functions are created in the Inspector (via <see cref="TweenInitModule"/>),
    /// registered at startup with <see cref="Ease.Init"/>, and retrieved at runtime by name or hash
    /// via <see cref="Ease.GetCustomEasingFunction(string)"/>.
    ///
    /// <para>Unlike <see cref="AnimationCurveEasingFunction"/>, this class is serialisable so it can be
    /// configured as a reusable project-wide asset rather than created per-tween.</para>
    /// </summary>
    [System.Serializable]
    public class CustomEasingFunction : Ease.IEasingFunction
    {
        [SerializeField] string name;

        /// <summary>Unique identifier used to look up this function via <see cref="Ease.GetCustomEasingFunction(string)"/>.</summary>
        public string Name => name;

        [SerializeField] AnimationCurve easingCurve;

        private float totalEasingTime;

        /// <summary>
        /// Creates a custom easing function with the given <paramref name="name"/> and <paramref name="easingCurve"/>.
        /// Automatically calls <see cref="Init"/> to pre-compute the curve length.
        /// </summary>
        public CustomEasingFunction(string name, AnimationCurve easingCurve)
        {
            this.name = name;
            this.easingCurve = easingCurve;

            Init();
        }

        /// <summary>
        /// Pre-computes the total duration of the <see cref="easingCurve"/> from its last keyframe time.
        /// Must be called after deserialisation (done automatically by <see cref="Ease.Init"/>).
        /// </summary>
        public void Init()
        {
            totalEasingTime = easingCurve.keys[easingCurve.keys.Length - 1].time;
        }

        /// <summary>
        /// Evaluates the easing curve at <c>p * totalEasingTime</c>, mapping the normalised progress
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
