using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Tween extensions for <see cref="Renderer"/> components using <see cref="MaterialPropertyBlock"/>.
    /// Property-block updates avoid creating material instances, making them GPU-efficient for instanced rendering.
    /// </summary>
    public static class RendererTweenCases
    {
        #region Extensions
        /// <summary>
        /// Animates a color property in <paramref name="materialPropertyBlock"/> on <paramref name="tweenObject"/>
        /// identified by <paramref name="colorID"/> (from <c>Shader.PropertyToID</c>).
        /// </summary>
        public static TweenCase DOPropertyBlockColor(this Renderer tweenObject, int colorID, MaterialPropertyBlock materialPropertyBlock, Color resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new PropertyBlockColor(colorID, materialPropertyBlock, tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Animates a float property in <paramref name="materialPropertyBlock"/> on <paramref name="tweenObject"/>
        /// identified by <paramref name="floatID"/> (from <c>Shader.PropertyToID</c>).
        /// </summary>
        public static TweenCase DOPropertyBlockFloat(this Renderer tweenObject, int floatID, MaterialPropertyBlock materialPropertyBlock, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new PropertyBlockFloat(floatID, materialPropertyBlock, tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        /// <summary>Interpolates a float property in a <see cref="MaterialPropertyBlock"/> and applies it to a <see cref="Renderer"/> each frame.</summary>
        public class PropertyBlockFloat : TweenCaseFunction<Renderer, float>
        {
            private MaterialPropertyBlock materialPropertyBlock;

            private int floatID;

            public PropertyBlockFloat(int floatID, MaterialPropertyBlock materialPropertyBlock, Renderer tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                this.parentObject = tweenObject.gameObject;
                this.materialPropertyBlock = materialPropertyBlock;

                this.floatID = floatID;
                this.startValue = materialPropertyBlock.GetFloat(floatID);
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetFloat(floatID, resultValue);
                tweenObject.SetPropertyBlock(materialPropertyBlock);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetFloat(floatID, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
                tweenObject.SetPropertyBlock(materialPropertyBlock);
            }
        }

        /// <summary>Interpolates a color property in a <see cref="MaterialPropertyBlock"/> and applies it to a <see cref="Renderer"/> each frame.</summary>
        public class PropertyBlockColor : TweenCaseFunction<Renderer, Color>
        {
            private MaterialPropertyBlock materialPropertyBlock;

            private int colorID;

            public PropertyBlockColor(int colorID, MaterialPropertyBlock materialPropertyBlock, Renderer tweenObject, Color resultValue) : base(tweenObject, resultValue)
            {
                this.parentObject = tweenObject.gameObject;
                this.materialPropertyBlock = materialPropertyBlock;

                this.colorID = colorID;
                this.startValue = materialPropertyBlock.GetColor(colorID);
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetColor(colorID, resultValue);
                tweenObject.SetPropertyBlock(materialPropertyBlock);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetColor(colorID, Color.LerpUnclamped(startValue, resultValue, Interpolate(state)));
                tweenObject.SetPropertyBlock(materialPropertyBlock);
            }
        }
    }
}
