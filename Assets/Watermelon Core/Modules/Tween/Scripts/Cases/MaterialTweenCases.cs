using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Tween extensions for <see cref="Material"/> properties.
    /// Uses shader property IDs (from <c>Shader.PropertyToID</c>) for efficient GPU-side updates.
    /// </summary>
    public static class MaterialTweenCases
    {
        #region Extensions
        /// <summary>Animates a <see cref="Color"/> shader property on <paramref name="tweenObject"/> identified by <paramref name="colorID"/>.</summary>
        public static TweenCase DOColor(this Material tweenObject, int colorID, Color resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new MaterialColor(colorID, tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>Animates a <see cref="float"/> shader property on <paramref name="material"/> identified by <paramref name="floatId"/>.</summary>
        public static TweenCase DoFloat(this Material material, int floatId, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new MaterialFloat(floatId, material, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        /// <summary>Interpolates a color shader property on a <see cref="Material"/> using <see cref="Material.SetColor(int,Color)"/>.</summary>
        public class MaterialColor : TweenCaseFunction<Material, Color>
        {
            private int colorID;

            public MaterialColor(int colorID, Material tweenObject, Color resultValue) : base(tweenObject, resultValue)
            {
                this.colorID = colorID;

                startValue = tweenObject.GetColor(colorID);
            }

            public override bool Validate()
            {
                return true;
            }

            public override void DefaultComplete()
            {
                tweenObject.SetColor(colorID, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.SetColor(colorID, Color.LerpUnclamped(startValue, resultValue, Interpolate(State)));
            }
        }

        /// <summary>Interpolates a float shader property on a <see cref="Material"/> using <see cref="Material.SetFloat(int,float)"/>.</summary>
        public class MaterialFloat : TweenCaseFunction<Material, float>
        {
            private int floatID;

            public MaterialFloat(int floatID, Material tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                this.floatID = floatID;

                startValue = tweenObject.GetFloat(floatID);
            }

            public override bool Validate()
            {
                return true;
            }

            public override void DefaultComplete()
            {
                tweenObject.SetFloat(floatID, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.SetFloat(floatID, startValue + (resultValue - startValue) * Interpolate(State));
            }
        }
    }
}
