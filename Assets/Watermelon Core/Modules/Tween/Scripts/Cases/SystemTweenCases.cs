using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Tween cases and extension methods for non-component targets: generic actions, conditions,
    /// float/color value interpolation, frame-delayed callbacks, and async operation tracking.
    /// </summary>
    public static class SystemTweenCases
    {
        #region Extensions
        /// <summary>
        /// Interpolates a generic value of type <typeparamref name="T"/> and passes it to <paramref name="action"/> each frame.
        /// The action signature is <c>(startValue, resultValue, easedProgress)</c>.
        /// </summary>
        public static TweenCase DOAction<T>(this object tweenObject, System.Action<T, T, float> action, T startValue, T resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new Action<T>(startValue, resultValue, action).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Attaches <paramref name="onCompleted"/> to fire when the <see cref="AsyncOperation"/> finishes.
        /// Uses <see cref="AsyncOperation.isDone"/> for reliable completion detection.
        /// </summary>
        public static TweenCase OnCompleted(this AsyncOperation tweenObject, SimpleCallback onCompleted)
        {
            return new AsyncOperationTweenCase(tweenObject).SetUnscaledMode(true).SetUpdateMethod(UpdateMethod.Update).OnComplete(onCompleted).StartTween();
        }
        #endregion

        /// <summary>
        /// A no-op <see cref="TweenCase"/> used as a timer. Used internally by <see cref="TweenManager.DelayedCall"/>
        /// to run a callback after a fixed duration without animating any value.
        /// </summary>
        public class Default : TweenCase
        {
            public override void DefaultComplete() { }
            public override void Invoke(float deltaTime) { }

            public override bool Validate()
            {
                return true;
            }
        }

        /// <summary>
        /// Runs a user-supplied callback every frame until the callback decides to call <c>tweenCase.Complete()</c>.
        /// Useful for polling conditions or driving custom per-frame logic with tween lifecycle management.
        /// </summary>
        public class Condition : TweenCase
        {
            private readonly TweenConditionCallback callback;

            public Condition(TweenConditionCallback callback)
            {
                this.callback = callback;
            }

            public override void DefaultComplete() { }

            public override void Invoke(float deltaTime)
            {
                callback.Invoke(this);
            }

            public override bool Validate()
            {
                return true;
            }

            /// <summary>Delegate invoked every frame. Call <c>tweenCase.Complete()</c> on the received instance to end the tween.</summary>
            public delegate void TweenConditionCallback(Condition tweenCase);
        }

        /// <summary>
        /// Interpolates between <c>startValue</c> and <c>resultValue</c> of type <typeparamref name="T"/> and
        /// invokes <c>action(start, end, easedProgress)</c> each frame. Used by <see cref="DOAction{T}"/>.
        /// </summary>
        public class Action<T> : TweenCase
        {
            private System.Action<T, T, float> action;

            private T startValue;
            private T resultValue;

            public Action(T startValue, T resultValue, System.Action<T, T, float> action)
            {
                this.startValue = startValue;
                this.resultValue = resultValue;

                this.action = action;
            }

            public override bool Validate()
            {
                return true;
            }

            public override void DefaultComplete()
            {
                action.Invoke(startValue, resultValue, 1);
            }

            public override void Invoke(float deltaTime)
            {
                action.Invoke(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>
        /// Fires a callback after a specified number of frames have elapsed.
        /// The internal counter is set to <c>framesOffset + 1</c> to account for the frame in which the tween is created
        /// (which may already be mid-Update when called outside the update loop).
        /// </summary>
        public class NextFrame : TweenCase
        {
            private readonly SimpleCallback callback;
            private int framesOffset;

            public NextFrame(SimpleCallback callback, int framesOffset)
            {
                this.callback = callback;
                this.framesOffset = framesOffset + 1;
            }

            public override void Invoke(float deltaTime)
            {
                framesOffset--;

                if (framesOffset <= 0)
                    Complete();
            }

            public override void DefaultComplete()
            {
                callback.Invoke();
            }

            public override bool Validate()
            {
                return true;
            }
        }

        /// <summary>
        /// Interpolates a <see cref="float"/> value from <c>startValue</c> to <c>resultValue</c>
        /// and invokes <see cref="TweenFloatCallback"/> each frame with the current interpolated value.
        /// </summary>
        public class Float : TweenCase
        {
            private readonly float startValue;
            private readonly float resultValue;

            private readonly TweenFloatCallback callback;

            public Float(float startValue, float resultValue, TweenFloatCallback callback)
            {
                this.startValue = startValue;
                this.resultValue = resultValue;

                this.callback = callback;
            }

            public override bool Validate()
            {
                return true;
            }

            public override void DefaultComplete()
            {
                callback.Invoke(resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                callback.Invoke(Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }

            /// <summary>Invoked each frame with the current interpolated float value.</summary>
            public delegate void TweenFloatCallback(float value);
        }

        /// <summary>
        /// Interpolates a <see cref="Color"/> value from <c>startValue</c> to <c>resultValue</c>
        /// and invokes <see cref="TweenColorCallback"/> each frame with the current interpolated color.
        /// </summary>
        public class ColorCase : TweenCase
        {
            private readonly Color startValue;
            private readonly Color resultValue;

            private readonly TweenColorCallback callback;

            public ColorCase(Color startValue, Color resultValue, TweenColorCallback callback)
            {
                this.startValue = startValue;
                this.resultValue = resultValue;

                this.callback = callback;
            }

            public override bool Validate()
            {
                return true;
            }

            public override void DefaultComplete()
            {
                callback.Invoke(resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                callback.Invoke(Color.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }

            /// <summary>Invoked each frame with the current interpolated color value.</summary>
            public delegate void TweenColorCallback(Color color);
        }

        /// <summary>
        /// Tracks an <see cref="AsyncOperation"/> (e.g. from <c>SceneManager.LoadSceneAsync</c>) and
        /// calls <see cref="TweenCase.Complete"/> when <see cref="AsyncOperation.isDone"/> becomes <c>true</c>.
        /// Use <c>.OnCompleted(callback)</c> extension or the <see cref="TweenCase.OnComplete"/> builder method to react to completion.
        /// </summary>
        public class AsyncOperationTweenCase : TweenCase
        {
            /// <summary>The tracked Unity async operation.</summary>
            public AsyncOperation asyncOperation;

            /// <summary>Current load progress reported by the async operation (0–1, stops at 0.9 when <c>allowSceneActivation</c> is <c>false</c>).</summary>
            public float Progress => asyncOperation.progress;

            /// <summary>Whether the async operation has finished. Equivalent to <see cref="AsyncOperation.isDone"/>.</summary>
            public bool IsOperationDone => asyncOperation.isDone;

            public AsyncOperationTweenCase(AsyncOperation asyncOperation)
            {
                this.asyncOperation = asyncOperation;

                SetDuration(float.MaxValue);
            }

            public override void DefaultComplete() { }

            public override void Invoke(float deltaTime)
            {
                if (asyncOperation.isDone)
                    Complete();
            }

            public override bool Validate()
            {
                return true;
            }
        }
    }
}
