using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class BounceSettings
    {
        [SerializeField] float duration = 0.3f;

        [Space]
        [SerializeField] AnimationCurve scaleX = DefaultCurve();
        [SerializeField] AnimationCurve scaleY = DefaultCurve();
        [SerializeField] AnimationCurve scaleZ = DefaultCurve();

        public float Duration => duration;

        public Vector3 Evaluate(float t) => new Vector3(scaleX.Evaluate(t), scaleY.Evaluate(t), scaleZ.Evaluate(t));

        private static AnimationCurve DefaultCurve() => new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.3f, 1.15f),
            new Keyframe(0.65f, 0.95f),
            new Keyframe(1f, 1f));
    }
}
