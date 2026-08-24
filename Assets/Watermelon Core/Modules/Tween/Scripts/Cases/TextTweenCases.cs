using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Tween extensions for legacy <see cref="Text"/>, <see cref="TextMesh"/>, and TextMeshPro (<see cref="TMP_Text"/>) components.
    /// Provides font-size and color animations across all three text APIs.
    /// </summary>
    public static class TextTweenCases
    {
        #region Extensions
        /// <summary>
        /// Animates <see cref="Text.fontSize"/> from its current value to <paramref name="resultValue"/> (integer pixels).
        /// </summary>
        public static TweenCase DOFontSize(this Text tweenObject, int resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new TextFontSize(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Animates <see cref="TextMesh.fontSize"/> from its current value to <paramref name="resultValue"/> (integer pixels).
        /// </summary>
        public static TweenCase DOFontSize(this TextMesh tweenObject, int resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new TextMeshFontSize(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Animates only the alpha channel of <see cref="TextMesh.color"/> to <paramref name="resultValue"/> (0 = transparent, 1 = opaque), leaving RGB unchanged.
        /// </summary>
        public static TweenCase DOFade(this TextMesh tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new TextMeshFade(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Animates the full RGBA <see cref="TextMesh.color"/> to <paramref name="resultValue"/>.
        /// </summary>
        public static TweenCase DOColor(this TextMesh tweenObject, Color resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new TextMeshColor(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        /// <summary>Interpolates <see cref="Text.fontSize"/> (integer) from its starting value to <c>resultValue</c>.</summary>
        public class TextFontSize : TweenCaseFunction<Text, int>
        {
            public TextFontSize(Text tweenObject, int resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.fontSize;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.fontSize = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.fontSize = (int)Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>Interpolates <see cref="TextMesh.fontSize"/> (integer) from its starting value to <c>resultValue</c>.</summary>
        public class TextMeshFontSize : TweenCaseFunction<TextMesh, int>
        {
            public TextMeshFontSize(TextMesh tweenObject, int resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.fontSize;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.fontSize = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.fontSize = (int)Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>Interpolates only the alpha channel of <see cref="TextMesh.color"/>, leaving RGB unchanged.</summary>
        public class TextMeshFade : TweenCaseFunction<TextMesh, float>
        {
            public TextMeshFade(TextMesh tweenObject, float resultValue) : base(tweenObject, resultValue)
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

        /// <summary>Interpolates the full RGBA color of a <see cref="TextMesh"/> to <c>resultValue</c>.</summary>
        public class TextMeshColor : TweenCaseFunction<TextMesh, Color>
        {
            public TextMeshColor(TextMesh tweenObject, Color resultValue) : base(tweenObject, resultValue)
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

        /// <summary>
        /// Interpolates <see cref="TMP_Text.fontSize"/> (float) from its starting value to <c>resultValue</c>.
        /// The result is cast to <c>int</c> each frame, matching TextMeshPro's integer font-size behaviour.
        /// </summary>
        public class TMPFontSize : TweenCaseFunction<TMP_Text, float>
        {
            public TMPFontSize(TMP_Text tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.fontSize;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.fontSize = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.fontSize = (int)Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }
    }
}
