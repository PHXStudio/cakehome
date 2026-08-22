using System;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    // Shared enter/exit animation for task cards — small overshoot scale-in, scale-to-zero out.
    // Kept as a static helper so every ITaskCardView implementation animates consistently.
    public static class TaskCardAnimation
    {
        public const float EnterDuration = 0.3f;
        private const float ExitDuration = 0.2f;

        public static void PlayEnter(Transform transform)
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(1f, EnterDuration).SetEasing(Ease.Type.BackOut);
        }

        public static void PlayExit(Transform transform, Action onComplete)
        {
            transform.DOScale(0f, ExitDuration)
                .SetEasing(Ease.Type.CubicIn)
                .OnComplete(() => onComplete?.Invoke());
        }

        // Same scale animation, plus a synced Preferred Width tween so cards that resize
        // based on content (e.g. item count) grow/shrink smoothly instead of popping.
        public static void PlayEnter(Transform transform, LayoutElement layoutElement, float targetWidth)
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(1f, EnterDuration).SetEasing(Ease.Type.BackOut);

            if (layoutElement != null)
            {
                layoutElement.preferredWidth = 0f;
                Tween.DoFloat(0f, targetWidth, EnterDuration, value => layoutElement.preferredWidth = value)
                    .SetEasing(Ease.Type.BackOut);
            }
        }

        public static void PlayExit(Transform transform, LayoutElement layoutElement, Action onComplete)
        {
            transform.DOScale(0f, ExitDuration)
                .SetEasing(Ease.Type.CubicIn)
                .OnComplete(() => onComplete?.Invoke());

            if (layoutElement != null)
            {
                Tween.DoFloat(layoutElement.preferredWidth, 0f, ExitDuration, value => layoutElement.preferredWidth = value)
                    .SetEasing(Ease.Type.CubicIn);
            }
        }
    }
}
