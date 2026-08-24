using UnityEngine;
using System.Collections;

namespace Watermelon
{
    /// <summary>
    /// MonoBehaviour facade for the tween system.
    /// Hosts Unity lifecycle hooks (Update / FixedUpdate / LateUpdate) and coroutine support.
    /// All state and logic lives in <see cref="TweenManager"/>; this class is intentionally thin.
    ///
    /// <para>Usage: call <see cref="Init"/> once (typically from <see cref="TweenInitModule"/>), then use
    /// the static extension methods on Unity objects (e.g. <c>transform.DOMove(...)</c>) or the static
    /// factory helpers (<see cref="DelayedCall"/>, <see cref="NextFrame"/>, etc.).</para>
    /// </summary>
    public class Tween : MonoBehaviour
    {
        private static Tween instance;
        private static TweenManager manager;

        /// <summary>
        /// Direct access to the underlying <see cref="TweensHolder"/> array (indexed by <see cref="UpdateMethod"/>).
        /// Primarily used by tests and advanced tooling; prefer the static API for normal usage.
        /// </summary>
        public static TweensHolder[] Tweens => manager?.Holders;

        /// <summary>
        /// Initialises the tween system with pre-allocated arrays for each Unity update loop.
        /// Must be called exactly once before any tweens are created (done automatically by <see cref="TweenInitModule"/>).
        /// </summary>
        /// <param name="tweensUpdateCount">Initial capacity for <see cref="UpdateMethod.Update"/> tweens.</param>
        /// <param name="tweensFixedUpdateCount">Initial capacity for <see cref="UpdateMethod.FixedUpdate"/> tweens.</param>
        /// <param name="tweensLateUpdateCount">Initial capacity for <see cref="UpdateMethod.LateUpdate"/> tweens.</param>
        public void Init(int tweensUpdateCount, int tweensFixedUpdateCount, int tweensLateUpdateCount)
        {
            instance = this;
            manager = new TweenManager(tweensUpdateCount, tweensFixedUpdateCount, tweensLateUpdateCount);

            // DontDestroyOnLoad is only valid in Play Mode.
            // Edit Mode tests (NUnit) skip this without affecting runtime behaviour.
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
        }

        // -----------------------------------------------------------
        // Unity lifecycle — delegate to manager
        // -----------------------------------------------------------

        private void Update()       => manager?.UpdateRegular(Time.deltaTime, Time.unscaledDeltaTime);
        private void FixedUpdate()  => manager?.UpdateFixed(Time.fixedDeltaTime, Time.fixedUnscaledDeltaTime);
        private void LateUpdate()   => manager?.UpdateLate(Time.deltaTime, Time.unscaledDeltaTime);

        private void OnDestroy()
        {
            manager?.Unload();
            manager = null;
            instance = null;
        }

        // -----------------------------------------------------------
        // Static facade — all delegate to manager
        // -----------------------------------------------------------

        /// <summary>Registers a tween with the system. Called internally by <see cref="TweenCase.StartTween"/>.</summary>
        public static void AddTween(TweenCase tween)       => manager?.AddTween(tween);

        /// <summary>Enqueues a tween for deferred removal. Called internally by <see cref="TweenCase.Kill"/>.</summary>
        public static void MarkForKilling(TweenCase tween) => manager?.MarkForKilling(tween);

        /// <summary>Pauses all tweens in the specified <paramref name="tweenType"/> update loop.</summary>
        public static void Pause(UpdateMethod tweenType)   => manager?.Pause(tweenType);

        /// <summary>Pauses all active tweens across every update loop.</summary>
        public static void PauseAll()                      => manager?.PauseAll();

        /// <summary>Resumes all tweens in the specified <paramref name="tweenType"/> update loop.</summary>
        public static void Resume(UpdateMethod tweenType)  => manager?.Resume(tweenType);

        /// <summary>Resumes all paused tweens across every update loop.</summary>
        public static void ResumeAll()                     => manager?.ResumeAll();

        /// <summary>Kills all tweens in the specified <paramref name="tweenType"/> update loop.</summary>
        public static void Remove(UpdateMethod tweenType)  => manager?.Remove(tweenType);

        /// <summary>Kills all active tweens across every update loop. OnComplete callbacks are <b>not</b> fired.</summary>
        public static void RemoveAll()                     => manager?.RemoveAll();
        
        /// <summary>
        /// Immediately clears all tweens and resets internal state without destroying the MonoBehaviour or its manager.
        /// Use this before scene reloads instead of <see cref="RemoveAll"/> — <see cref="RemoveAll"/> defers cleanup
        /// to the next Update, which can cause index errors if new tweens are added before that update runs.
        /// </summary>
        public static void KillAll()                       => manager?.Unload();
        
        /// <summary>
        /// Begins collecting every subsequently created tween into a single <see cref="TweenCaseCollection"/>.
        /// Call <see cref="EndTweenCaseCollection"/> to stop collecting.
        /// </summary>
        /// <returns>The collection that will receive all tweens started before <see cref="EndTweenCaseCollection"/> is called.</returns>
        public static TweenCaseCollection BeginTweenCaseCollection() => manager?.BeginCollection();

        /// <summary>Stops populating the active batch collection started by <see cref="BeginTweenCaseCollection"/>.</summary>
        public static void EndTweenCaseCollection()                   => manager?.EndCollection();

        /// <summary>
        /// Schedules <paramref name="callback"/> to be invoked after <paramref name="delay"/> seconds.
        /// Returns <c>null</c> if <paramref name="delay"/> &lt;= 0 (the callback is invoked immediately instead).
        /// Use the <c>.KillActive()</c> extension on the result to safely cancel without a null check.
        /// </summary>
        public static TweenCase DelayedCall(float delay, SimpleCallback callback, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
            => manager?.DelayedCall(delay, callback, unscaledTime, tweenType);

        /// <summary>Interpolates a <see cref="Color"/> value and invokes <paramref name="callback"/> each frame.</summary>
        public static TweenCase DoColor(Color startValue, Color resultValue, float time, SystemTweenCases.ColorCase.TweenColorCallback callback, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
            => manager?.DoColor(startValue, resultValue, time, callback, unscaledTime, tweenType);

        /// <summary>Interpolates a <see cref="float"/> value and invokes <paramref name="callback"/> each frame.</summary>
        public static TweenCase DoFloat(float startValue, float resultValue, float time, SystemTweenCases.Float.TweenFloatCallback callback, float delay = 0.0f, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
            => manager?.DoFloat(startValue, resultValue, time, callback, delay, unscaledTime, tweenType);

        /// <summary>
        /// Runs <paramref name="callback"/> every frame until it calls <c>tweenCase.Complete()</c>.
        /// Useful for polling a condition without a fixed duration.
        /// </summary>
        public static TweenCase DoWaitForCondition(SystemTweenCases.Condition.TweenConditionCallback callback, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
            => manager?.DoWaitForCondition(callback, unscaledTime, tweenType);

        /// <summary>
        /// Invokes <paramref name="callback"/> after <paramref name="framesOffset"/> frames have elapsed.
        /// Uses unscaled time by default so it fires regardless of <c>Time.timeScale</c>.
        /// </summary>
        public static TweenCase NextFrame(SimpleCallback callback, int framesOffset = 1, bool unscaledTime = true, UpdateMethod updateMethod = UpdateMethod.Update)
            => manager?.NextFrame(callback, framesOffset, unscaledTime, updateMethod);

        /// <summary>Starts a coroutine on the <see cref="Tween"/> MonoBehaviour, allowing non-MonoBehaviour scripts to run coroutines.</summary>
        public static Coroutine InvokeCoroutine(IEnumerator enumerator)
        {
            if (instance == null) return null;
            return instance.StartCoroutine(enumerator);
        }

        /// <summary>Stops a coroutine previously started via <see cref="InvokeCoroutine"/>.</summary>
        public static void StopCustomCoroutine(Coroutine coroutine)
        {
            if (instance == null) return;
            instance.StopCoroutine(coroutine);
        }

        /// <summary>Stops all coroutines and unloads all tween holders. Used for manual cleanup (e.g. before scene reload).</summary>
        public static void DestroyObject()
        {
            if (instance != null)
                instance.StopAllCoroutines();

            manager?.Unload();
        }
    }
}
