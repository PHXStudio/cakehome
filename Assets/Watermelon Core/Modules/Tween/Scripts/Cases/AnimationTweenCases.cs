using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Tween extensions for <see cref="Animation"/> and <see cref="Animator"/> components.
    /// </summary>
    public static class AnimationTweenCases
    {
        #region Extensions
        /// <summary>Returns a tween that completes when the legacy <see cref="Animation"/> component stops playing its current clip.</summary>
        public static TweenCase WaitForEnd(this Animation tweenObject, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new Wait(tweenObject).SetDelay(delay).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>Animates the blend weight of the Animator layer identified by <paramref name="layerName"/>.</summary>
        public static TweenCase DOLayerWeight(this Animator tweenObject, string layerName, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnimatorWeight(tweenObject, resultValue, tweenObject.GetLayerIndex(layerName)).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>Animates the blend weight of the Animator layer at index <paramref name="layerID"/>.</summary>
        public static TweenCase DOLayerWeight(this Animator tweenObject, int layerID, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnimatorWeight(tweenObject, resultValue, layerID).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        /// <summary>Waits until the legacy <see cref="Animation"/> component is no longer playing, then calls <see cref="TweenCase.Complete"/>.</summary>
        public class Wait : TweenCase
        {
            public Animation animation;

            public Wait(Animation animation)
            {
                this.animation = animation;

                SetDuration(float.MaxValue);
            }

            public override void DefaultComplete() { }

            public override void Invoke(float deltaTime)
            {
                if (!animation.isPlaying)
                    Complete();
            }

            public override bool Validate()
            {
                return true;
            }
        }

        /// <summary>Interpolates the blend weight of a single <see cref="Animator"/> layer from its current value to <c>resultValue</c>.</summary>
        public class AnimatorWeight : TweenCaseFunction<Animator, float>
        {
            private int layerID;

            public AnimatorWeight(Animator tweenObject, float resultValue, int layerID) : base(tweenObject, resultValue)
            {
                this.layerID = layerID;

                parentObject = tweenObject.gameObject;

                startValue = tweenObject.GetLayerWeight(layerID);
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.SetLayerWeight(layerID, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.SetLayerWeight(layerID, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }
        }
    }
}
