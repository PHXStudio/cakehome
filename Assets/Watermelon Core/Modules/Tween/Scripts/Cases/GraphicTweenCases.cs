using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Tween extensions for <see cref="Graphic"/> components (e.g. <see cref="UnityEngine.UI.Image"/>, <see cref="UnityEngine.UI.Text"/>).
    /// </summary>
    public static class GraphicTweenCases
    {
        #region Extensions
        /// <summary>Animates the full <see cref="Graphic.color"/> (RGBA) to <paramref name="resultValue"/>.</summary>
        public static TweenCase DOColor(this Graphic tweenObject, Color resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new GraphicColor(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates only the alpha channel of <see cref="Graphic.color"/> to <paramref name="resultValue"/> (0 = transparent, 1 = opaque).</summary>
        public static TweenCase DOFade(this Graphic tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Fade(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }
        #endregion

        /// <summary>Interpolates the full RGBA color of a <see cref="Graphic"/> to <c>resultValue</c>.</summary>
        public class GraphicColor : TweenCaseFunction<Graphic, Color>
        {
            public GraphicColor(Graphic tweenObject, Color resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.color;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.color = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.color = Color.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>Interpolates only the alpha channel of a <see cref="Graphic"/>'s color, leaving RGB unchanged.</summary>
        public class Fade : TweenCaseFunction<Graphic, float>
        {
            public Fade(Graphic tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.color.a;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.color = new Color(tweenObject.color.r, tweenObject.color.g, tweenObject.color.b, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.color = new Color(tweenObject.color.r, tweenObject.color.g, tweenObject.color.b, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }
        }
    }
}
