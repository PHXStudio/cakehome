using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Tween extensions for <see cref="ParticleSystem"/> components.
    /// </summary>
    public static class ParticleTweenCases
    {
        #region Extensions
        /// <summary>Returns a tween that completes when the <see cref="ParticleSystem"/> (and all its sub-emitters) are no longer alive.</summary>
        public static TweenCase WaitForEnd(this ParticleSystem tweenObject, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Wait(tweenObject).SetDelay(delay).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }
        #endregion

        /// <summary>
        /// Polls <see cref="ParticleSystem.IsAlive"/> each frame and calls <see cref="TweenCase.Complete"/> when the system has fully finished.
        /// Automatically killed if the particle system object is destroyed.
        /// </summary>
        public class Wait : TweenCase
        {
            private readonly ParticleSystem particleSystem;

            public Wait(ParticleSystem particleSystem)
            {
                this.particleSystem = particleSystem;

                SetDuration(float.MaxValue);
            }

            public override void DefaultComplete() { }

            public override void Invoke(float deltaTime)
            {
                if (!particleSystem.IsAlive())
                    Complete();
            }

            public override bool Validate()
            {
                return particleSystem != null;
            }
        }
    }
}
