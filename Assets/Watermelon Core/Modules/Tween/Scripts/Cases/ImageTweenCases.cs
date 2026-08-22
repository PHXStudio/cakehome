using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Tween extensions for <see cref="Image"/> components.
    /// </summary>
    public static class ImageTweenCases
    {
        #region Extensions
        /// <summary>Animates <see cref="Image.fillAmount"/> from its current value to <paramref name="resultValue"/> (range 0–1).</summary>
        public static TweenCase DOFillAmount(this Image tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new ImageFill(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        /// <summary>Interpolates <see cref="Image.fillAmount"/> from its starting value to <c>resultValue</c>.</summary>
        public class ImageFill : TweenCaseFunction<Image, float>
        {
            public ImageFill(Image tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.fillAmount;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.fillAmount = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.fillAmount = startValue + (resultValue - startValue) * Interpolate(state);
            }
        }
    }
}
