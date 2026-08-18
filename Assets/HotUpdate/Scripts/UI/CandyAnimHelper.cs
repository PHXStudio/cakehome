using UnityEngine;

namespace Watermelon
{
    public static class CandyAnimHelper
    {
        // Bounce a transform in with elastic overshoot
        public static TweenCase BounceIn(this Transform t, float delay = 0f)
        {
            t.localScale = Vector3.one * 1.4f;
            return t.DOScale(Vector3.one, 0.6f).SetDelay(delay).SetEasing(Ease.Type.ElasticOut);
        }

        // Shrink and fade out
        public static TweenCase PopOut(this Transform t, float delay = 0f)
        {
            return t.DOScale(Vector3.one * 0.3f, 0.35f).SetDelay(delay).SetEasing(Ease.Type.BackIn);
        }

        // "Squish" press animation
        public static TweenCase PressSquish(this Transform t)
        {
            return t.DOScale(Vector3.one * 0.85f, 0.08f).SetEasing(Ease.Type.QuadIn);
        }

        // "Pop" release animation
        public static TweenCase ReleasePop(this Transform t)
        {
            return t.DOScale(Vector3.one, 0.25f).SetEasing(Ease.Type.BackOut);
        }

        // Coin / reward sparkle ping
        public static TweenCase SparklePing(this Transform t)
        {
            return t.DOPushScale(Vector3.one * 1.25f, Vector3.one, 0.15f, 0.3f);
        }
    }
}