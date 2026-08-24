using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Core tween engine. Owns all <see cref="TweensHolder"/> instances and orchestrates
    /// tween registration, update routing, pause/resume, and factory helpers.
    /// <see cref="Tween"/> (the MonoBehaviour) is a thin facade that delegates every call here.
    /// </summary>
    public class TweenManager
    {
        private TweensHolder[] holders;

        /// <summary>
        /// One <see cref="TweensHolder"/> per <see cref="UpdateMethod"/> (index 0 = Update, 1 = FixedUpdate, 2 = LateUpdate).
        /// </summary>
        public TweensHolder[] Holders => holders;

        private TweenCaseCollection activeCollection;
        private bool isCollectionEnabled;

        /// <summary>
        /// Creates a manager with three <see cref="TweensHolder"/> instances — one per Unity update loop.
        /// </summary>
        /// <param name="updateCount">Initial capacity for <see cref="UpdateMethod.Update"/> tweens.</param>
        /// <param name="fixedCount">Initial capacity for <see cref="UpdateMethod.FixedUpdate"/> tweens.</param>
        /// <param name="lateCount">Initial capacity for <see cref="UpdateMethod.LateUpdate"/> tweens.</param>
        public TweenManager(int updateCount, int fixedCount, int lateCount)
        {
            holders = new TweensHolder[]
            {
                new TweensHolder(UpdateMethod.Update, updateCount),
                new TweensHolder(UpdateMethod.FixedUpdate, fixedCount),
                new TweensHolder(UpdateMethod.LateUpdate, lateCount)
            };
        }

        // -----------------------------------------------------------
        // Core tween management
        // -----------------------------------------------------------

        /// <summary>
        /// Registers <paramref name="tween"/> in the appropriate <see cref="TweensHolder"/> based on its
        /// <see cref="TweenCase.UpdateMethodIndex"/>. If a batch collection is active (see <see cref="BeginCollection"/>),
        /// the tween is also added to it.
        /// </summary>
        public void AddTween(TweenCase tween)
        {
            holders[tween.UpdateMethodIndex].AddTween(tween);

            if (isCollectionEnabled)
                activeCollection.AddTween(tween);
        }

        /// <summary>
        /// Forwards the kill request to the appropriate <see cref="TweensHolder"/> for deferred removal.
        /// Called by <see cref="TweenCase.Kill"/>.
        /// </summary>
        public void MarkForKilling(TweenCase tween)
        {
            holders[tween.UpdateMethodIndex].MarkForKilling(tween);
        }

        // -----------------------------------------------------------
        // Pause / Resume / Remove
        // -----------------------------------------------------------

        /// <summary>Pauses all tweens running in the specified <paramref name="tweenType"/> loop.</summary>
        public void Pause(UpdateMethod tweenType)   => holders[(int)tweenType].Pause();

        /// <summary>Resumes all tweens running in the specified <paramref name="tweenType"/> loop.</summary>
        public void Resume(UpdateMethod tweenType)  => holders[(int)tweenType].Resume();

        /// <summary>Kills all tweens running in the specified <paramref name="tweenType"/> loop.</summary>
        public void Remove(UpdateMethod tweenType)  => holders[(int)tweenType].Kill();

        /// <summary>Pauses all tweens across every update loop.</summary>
        public void PauseAll()  { foreach (var h in holders) h.Pause(); }

        /// <summary>Resumes all tweens across every update loop.</summary>
        public void ResumeAll() { foreach (var h in holders) h.Resume(); }

        /// <summary>Kills all tweens across every update loop. OnComplete callbacks are <b>not</b> fired.</summary>
        public void RemoveAll() { foreach (var h in holders) h.Kill(); }

        // -----------------------------------------------------------
        // Collection batch tracking
        // -----------------------------------------------------------

        /// <summary>
        /// Starts collecting every subsequently created tween into a new <see cref="TweenCaseCollection"/>.
        /// Call <see cref="EndCollection"/> to stop collecting.
        /// </summary>
        /// <returns>The newly created collection that will receive tweens until <see cref="EndCollection"/> is called.</returns>
        public TweenCaseCollection BeginCollection()
        {
            isCollectionEnabled = true;
            activeCollection = new TweenCaseCollection();
            return activeCollection;
        }

        /// <summary>Stops populating the active batch collection. Tweens created after this call are not added to any collection.</summary>
        public void EndCollection()
        {
            isCollectionEnabled = false;
            activeCollection = null;
        }

        // -----------------------------------------------------------
        // Update — called by Tween MonoBehaviour each Unity event
        // -----------------------------------------------------------

        /// <summary>Drives the <see cref="UpdateMethod.Update"/> holder. Called from <c>MonoBehaviour.Update</c>.</summary>
        public void UpdateRegular(float dt, float udt) => holders[0].Update(dt, udt);

        /// <summary>Drives the <see cref="UpdateMethod.FixedUpdate"/> holder. Called from <c>MonoBehaviour.FixedUpdate</c>.</summary>
        public void UpdateFixed(float dt, float udt)   => holders[1].Update(dt, udt);

        /// <summary>Drives the <see cref="UpdateMethod.LateUpdate"/> holder. Called from <c>MonoBehaviour.LateUpdate</c>.</summary>
        public void UpdateLate(float dt, float udt)    => holders[2].Update(dt, udt);

        // -----------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------

        /// <summary>
        /// Unloads all holders and resets collection state.
        /// Called when the <see cref="Tween"/> MonoBehaviour is destroyed.
        /// </summary>
        public void Unload()
        {
            foreach (var h in holders)
                h.Unload();

            activeCollection = null;
            isCollectionEnabled = false;
        }

        // -----------------------------------------------------------
        // Factory helpers (mirrors Tween static API)
        // -----------------------------------------------------------

        /// <summary>
        /// Schedules <paramref name="callback"/> to be invoked after <paramref name="delay"/> seconds.
        /// Returns <c>null</c> if delay is &lt;= 0 (the callback is invoked immediately instead).
        /// Use the <c>.KillActive()</c> extension on the result to safely cancel without a null check.
        /// </summary>
        public TweenCase DelayedCall(float delay, SimpleCallback callback, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            if (delay <= 0)
            {
                callback?.Invoke();
                return null;
            }

            return new SystemTweenCases.Default()
                .SetDuration(delay)
                .SetUnscaledMode(unscaledTime)
                .OnComplete(callback)
                .SetUpdateMethod(tweenType)
                .StartTween();
        }

        /// <summary>Interpolates a <see cref="Color"/> value from <paramref name="startValue"/> to <paramref name="resultValue"/> over <paramref name="time"/> seconds, invoking <paramref name="callback"/> each frame.</summary>
        public TweenCase DoColor(Color startValue, Color resultValue, float time, SystemTweenCases.ColorCase.TweenColorCallback callback, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new SystemTweenCases.ColorCase(startValue, resultValue, callback)
                .SetDuration(time)
                .SetUnscaledMode(unscaledTime)
                .SetUpdateMethod(tweenType)
                .StartTween();
        }

        /// <summary>Interpolates a <see cref="float"/> value from <paramref name="startValue"/> to <paramref name="resultValue"/> over <paramref name="time"/> seconds, invoking <paramref name="callback"/> each frame.</summary>
        public TweenCase DoFloat(float startValue, float resultValue, float time, SystemTweenCases.Float.TweenFloatCallback callback, float delay = 0.0f, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new SystemTweenCases.Float(startValue, resultValue, callback)
                .SetDelay(delay)
                .SetDuration(time)
                .SetUnscaledMode(unscaledTime)
                .SetUpdateMethod(tweenType)
                .StartTween();
        }

        /// <summary>
        /// Runs <paramref name="callback"/> every frame until it calls <c>tweenCase.Complete()</c>.
        /// Useful for polling conditions that don't have a fixed duration.
        /// </summary>
        public TweenCase DoWaitForCondition(SystemTweenCases.Condition.TweenConditionCallback callback, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new SystemTweenCases.Condition(callback)
                .SetDuration(float.MaxValue)
                .SetUnscaledMode(unscaledTime)
                .SetUpdateMethod(tweenType)
                .StartTween();
        }

        /// <summary>
        /// Invokes <paramref name="callback"/> after <paramref name="framesOffset"/> frames have passed.
        /// Uses unscaled time by default so it works regardless of <c>Time.timeScale</c>.
        /// </summary>
        public TweenCase NextFrame(SimpleCallback callback, int framesOffset = 1, bool unscaledTime = true, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new SystemTweenCases.NextFrame(callback, framesOffset)
                .SetDuration(float.MaxValue)
                .SetUnscaledMode(unscaledTime)
                .SetUpdateMethod(updateMethod)
                .StartTween();
        }
    }
}
