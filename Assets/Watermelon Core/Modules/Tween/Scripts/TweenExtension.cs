using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Null-safe extension helpers for <see cref="TweenCase"/> and <see cref="TweenCaseCollection"/>,
    /// plus <see cref="ScrollRect"/> snapping utilities.
    ///
    /// <para>The <c>KillActive</c> / <c>CompleteActive</c> family is the recommended way to stop tweens
    /// stored in fields, because they handle both the <c>null</c> case (tween never started or returned null
    /// from <see cref="Tween.DelayedCall"/>) and the already-completed case without requiring extra null checks.</para>
    /// </summary>
    public static class TweenExtensions
    {
        /// <summary>Returns <c>true</c> if <paramref name="tweenCase"/> is not <c>null</c> and is currently active.</summary>
        public static bool ExistsAndActive(this TweenCase tweenCase)
        {
            return tweenCase != null && tweenCase.IsActive;
        }

        /// <summary>
        /// Kills <paramref name="tweenCase"/> if it is not <c>null</c> and is currently active.
        /// Returns <c>true</c> if the tween was killed, <c>false</c> if it was already inactive or <c>null</c>.
        /// </summary>
        public static bool KillActive(this TweenCase tweenCase)
        {
            if (tweenCase != null && tweenCase.IsActive)
            {
                tweenCase.Kill();

                return true;
            }

            return false;
        }

        /// <summary>Kills every active tween in <paramref name="tweenCases"/>, skipping <c>null</c> entries and already-inactive tweens.</summary>
        public static void KillActive(this TweenCase[] tweenCases)
        {
            if(tweenCases != null)
            {
                foreach (TweenCase tweenCase in tweenCases)
                {
                    if (tweenCase != null && tweenCase.IsActive)
                    {
                        tweenCase.Kill();
                    }
                }
            }
        }

        /// <summary>
        /// Kills <paramref name="tweenCase"/> collection if it is not <c>null</c> and is not yet complete.
        /// Returns <c>true</c> if the collection was killed.
        /// </summary>
        public static bool KillActive(this TweenCaseCollection tweenCase)
        {
            if (tweenCase != null && !tweenCase.IsComplete())
            {
                tweenCase.Kill();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Completes <paramref name="tweenCase"/> if it is not <c>null</c> and has not yet finished.
        /// Returns <c>true</c> if the tween was completed.
        /// </summary>
        public static bool CompleteActive(this TweenCase tweenCase)
        {
            if (tweenCase != null && !tweenCase.IsCompleted)
            {
                tweenCase.Complete();

                return true;
            }

            return false;
        }

        /// <summary>Forces immediate completion of every active tween in <paramref name="tweenCases"/>, skipping <c>null</c> entries.</summary>
        public static void CompleteActive(this TweenCase[] tweenCases)
        {
            if (tweenCases != null)
            {
                foreach (TweenCase tweenCase in tweenCases)
                {
                    if (tweenCase != null && tweenCase.IsActive)
                    {
                        tweenCase.Complete();
                    }
                }
            }
        }

        /// <summary>
        /// Smoothly scrolls the <see cref="ScrollRect"/> so that the bottom edge of <paramref name="target"/>
        /// is visible within the viewport.
        /// </summary>
        /// <param name="scrollRect">The scroll rect to animate.</param>
        /// <param name="target">The child <see cref="RectTransform"/> to bring into view.</param>
        /// <param name="duration">Scroll animation duration in seconds.</param>
        /// <param name="offsetX">Additional horizontal offset applied to the target position.</param>
        /// <param name="offsetY">Additional vertical offset applied to the target position.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenCase DoSnapTargetBottom(this ScrollRect scrollRect, RectTransform target, float duration, float offsetX = 0, float offsetY = 0)
        {
            var targetPosition = target.position + Vector3.up * (scrollRect.viewport.rect.height / 2 + target.sizeDelta.y);
            return scrollRect.SnapToTarget(targetPosition, duration, offsetX, offsetY);
        }

        /// <summary>
        /// Smoothly scrolls the <see cref="ScrollRect"/> so that the top edge of <paramref name="target"/>
        /// is aligned with the top of the viewport.
        /// </summary>
        /// <param name="scrollRect">The scroll rect to animate.</param>
        /// <param name="target">The child <see cref="RectTransform"/> to bring into view.</param>
        /// <param name="duration">Scroll animation duration in seconds.</param>
        /// <param name="offsetX">Additional horizontal offset applied to the target position.</param>
        /// <param name="offsetY">Additional vertical offset applied to the target position.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenCase DoSnapTargetTop(this ScrollRect scrollRect, RectTransform target, float duration, float offsetX = 0, float offsetY = 0)
        {
            return scrollRect.SnapToTarget(target.position, duration, offsetX, offsetY);
        }

        /// <summary>
        /// Smoothly scrolls the <see cref="ScrollRect"/> so that a world-space <paramref name="target"/> position
        /// is visible, respecting the scroll rect's horizontal/vertical constraints.
        /// </summary>
        /// <param name="scrollRect">The scroll rect to animate.</param>
        /// <param name="target">World-space position to scroll towards.</param>
        /// <param name="duration">Scroll animation duration in seconds.</param>
        /// <param name="offsetX">Additional horizontal offset in viewport space.</param>
        /// <param name="offsetY">Additional vertical offset in viewport space.</param>
        public static TweenCase SnapToTarget(this ScrollRect scrollRect, Vector3 target, float duration, float offsetX = 0, float offsetY = 0)
        {
            Vector2 contentPosition = scrollRect.viewport.InverseTransformPoint(scrollRect.content.position);
            Vector2 newPosition = scrollRect.viewport.InverseTransformPoint(target);
            newPosition = new Vector2(newPosition.x + offsetX, newPosition.y + offsetY);

            if (!scrollRect.horizontal)
                newPosition.x = contentPosition.x;

            if (!scrollRect.vertical)
                newPosition.y = contentPosition.y;

            return scrollRect.content.DOAnchoredPosition(contentPosition - newPosition, duration);
        }
    }
}
