using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Abstract base class for all tween animations.
    /// Encapsulates lifecycle (start, pause, resume, kill, complete), easing, delay,
    /// progress tracking, and callback registration.
    ///
    /// <para><b>Lifecycle:</b>
    /// <list type="number">
    ///   <item><term>Create</term><description>Instantiate a concrete subclass and configure it with the fluent builder methods.</description></item>
    ///   <item><term>Start</term><description>Call <see cref="StartTween"/> to register the tween with <see cref="Tween"/> and begin updating.</description></item>
    ///   <item><term>Update</term><description><see cref="TweensHolder"/> calls <see cref="UpdateState"/> and <see cref="Invoke"/> each frame.</description></item>
    ///   <item><term>End</term><description>Either <see cref="Complete"/> (applies the final value and fires callbacks) or <see cref="Kill"/> (no final value, no callbacks).</description></item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Implementing a custom tween:</b>
    /// Override <see cref="Invoke"/> to interpolate the value each frame,
    /// <see cref="DefaultComplete"/> to snap to the final value on completion, and
    /// <see cref="Validate"/> to return <c>false</c> when the target object is no longer valid.
    /// </para>
    /// </summary>
    public abstract class TweenCase
    {
        // Managed by TweensHolder — internal set only
        /// <summary>Index of this tween inside its <see cref="TweensHolder"/> array. -1 when inactive.</summary>
        public int ActiveID  { get; internal set; }

        /// <summary>Whether the tween is currently registered and updating in a <see cref="TweensHolder"/>.</summary>
        public bool IsActive { get; internal set; }

        // Private: only read/written inside TweenCase
        private float currentDelay;
        private float delay;
        protected float duration;
        private int   updateMethodIndex;
        private bool  isPaused;
        private bool  isUnscaled;
        private bool  isCompleted;
        private bool  isKilling;
        private Ease.IEasingFunction easeFunction;
        private SimpleCallback completedCallback;
        private SimpleCallback terminatedCallback;
        private List<CallbackData> callbackData;

        // Protected: written by concrete case constructors
        /// <summary>Optional target object. Used by <see cref="Validate"/> in subclasses to detect destroyed Unity objects.</summary>
        protected GameObject parentObject;

        // Protected: read by concrete Invoke() implementations via Interpolate(state)
        /// <summary>
        /// Normalized progress of the tween in the range [0, 1].
        /// Read by concrete <see cref="Invoke"/> implementations via <see cref="Interpolate"/>.
        /// </summary>
        protected float state;

        // Public read-only surface
        /// <summary>Elapsed delay time (seconds) since the tween started. Counts up until it reaches <see cref="Delay"/>.</summary>
        public float CurrentDelay     => currentDelay;

        /// <summary>Initial delay (seconds) before the tween begins animating.</summary>
        public float Delay            => delay;

        /// <summary>Normalized progress [0, 1]. Advances each frame after the delay has elapsed.</summary>
        public float State            => state;

        /// <summary>Total animation duration in seconds (excluding delay).</summary>
        public float Duration         => duration;

        /// <summary>Whether the tween is currently paused. Paused tweens are skipped by <see cref="TweensHolder"/> but remain registered.</summary>
        public bool  IsPaused         => isPaused;

        /// <summary>When <c>true</c>, the tween uses <c>Time.unscaledDeltaTime</c> and is unaffected by <c>Time.timeScale</c>.</summary>
        public bool  IsUnscaled       => isUnscaled;

        /// <summary>Whether the tween has finished animating (state reached 1, or <see cref="Complete"/> was called).</summary>
        public bool  IsCompleted      => isCompleted;

        /// <summary>
        /// Whether the tween has been marked for removal but not yet physically removed from the array.
        /// While <c>true</c>, <see cref="Restart"/> is a no-op. Cleared by <see cref="ResetKillingState"/> after removal.
        /// </summary>
        public bool  IsKilling        => isKilling;

        /// <summary>Array index of the owning <see cref="TweensHolder"/> inside <see cref="TweenManager.Holders"/>.</summary>
        public int   UpdateMethodIndex => updateMethodIndex;

        /// <summary>Unity update loop in which this tween runs.</summary>
        public UpdateMethod UpdateMethod => (UpdateMethod)updateMethodIndex;

        /// <summary>The GameObject this tween is attached to, or <c>null</c> for system (non-transform) tweens.</summary>
        public GameObject ParentObject => parentObject;

        /// <summary>Initialises the tween with a <see cref="Ease.Type.Linear"/> easing function.</summary>
        public TweenCase()
        {
            SetEasing(Ease.Type.Linear);
        }

        /// <summary>Registers the tween with the global <see cref="Tween"/> system and begins updating. Returns <c>this</c> for chaining.</summary>
        public virtual TweenCase StartTween()
        {
            Tween.AddTween(this);
            return this;
        }

        /// <summary>
        /// Called each frame to check whether the tween's target object is still valid.
        /// Return <c>false</c> to automatically kill the tween (e.g. when a GameObject has been destroyed).
        /// </summary>
        public abstract bool Validate();

        // -----------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------

        /// <summary>
        /// Stops and removes the tween without applying the final value or firing <see cref="OnComplete"/>.
        /// Fires the internal terminated callback used by <see cref="TweenCaseCollection"/>.
        /// Idempotent — safe to call multiple times.
        /// </summary>
        public TweenCase Kill()
        {
            if (!isKilling)
            {
                IsActive = false;
                Tween.MarkForKilling(this);
                isKilling = true;
                completedCallback = null;
                terminatedCallback?.Invoke();
                terminatedCallback = null;
            }
            return this;
        }

        /// <summary>
        /// Immediately completes the tween: snaps to the final value via <see cref="DefaultComplete"/>,
        /// fires the <see cref="OnComplete"/> callback, then calls <see cref="Kill"/>.
        /// Idempotent — subsequent calls are no-ops.
        /// </summary>
        public TweenCase Complete()
        {
            if (isKilling || isCompleted) return this;

            if (isPaused) isPaused = false;

            state = 1;
            isCompleted = true;

            DefaultComplete();
            InvokeCompleteEvent();
            Kill();

            return this;
        }

        /// <summary>
        /// Restarts the tween from the beginning (resets progress and delay to 0).
        /// If the tween has already ended, re-registers it with the system via <see cref="StartTween"/>.
        /// Has no effect while <see cref="Kill"/> is mid-processing (<see cref="IsKilling"/> is <c>true</c>).
        /// </summary>
        public TweenCase Restart()
        {
            if (isKilling) return this;

            state = 0;
            currentDelay = 0;
            isCompleted = false;
            isPaused = false;

            if (!IsActive)
                StartTween();

            return this;
        }

        /// <summary>Pauses the tween. The tween remains registered but is skipped by the update loop until <see cref="Resume"/> is called.</summary>
        public TweenCase Pause()
        {
            isPaused = true;
            return this;
        }

        /// <summary>Resumes a paused tween so it continues updating from where it stopped.</summary>
        public TweenCase Resume()
        {
            isPaused = false;
            return this;
        }

        // -----------------------------------------------------------
        // Builder (fluent API)
        // -----------------------------------------------------------

        /// <summary>Sets a delay (seconds) before the tween begins animating. Must be called before <see cref="StartTween"/>.</summary>
        public TweenCase SetDelay(float delay)
        {
            this.delay = delay;
            currentDelay = 0;
            return this;
        }

        /// <summary>
        /// Sets which Unity update loop drives this tween (<see cref="UpdateMethod.Update"/>,
        /// <see cref="UpdateMethod.FixedUpdate"/>, or <see cref="UpdateMethod.LateUpdate"/>).
        /// Must be called before <see cref="StartTween"/>.
        /// </summary>
        public TweenCase SetUpdateMethod(UpdateMethod updateMethod)
        {
            updateMethodIndex = (int)updateMethod;
            return this;
        }

        /// <summary>
        /// When <c>true</c>, the tween uses <c>Time.unscaledDeltaTime</c> and ignores <c>Time.timeScale</c>.
        /// Useful for UI animations that must run during paused gameplay.
        /// </summary>
        public TweenCase SetUnscaledMode(bool isUnscaled)
        {
            this.isUnscaled = isUnscaled;
            return this;
        }

        /// <summary>Sets one of the built-in easing curves from <see cref="Ease.Type"/>. Default is <see cref="Ease.Type.Linear"/>.</summary>
        public TweenCase SetEasing(Ease.Type ease)
        {
            easeFunction = Ease.GetFunction(ease);
            return this;
        }

        /// <summary>Sets a custom easing curve via an <see cref="AnimationCurve"/>. The curve is sampled over its total time range.</summary>
        public TweenCase SetCurveEasing(AnimationCurve easingCurve)
        {
            easeFunction = new AnimationCurveEasingFunction(easingCurve);
            return this;
        }

        /// <summary>Sets a fully custom easing function implementing <see cref="Ease.IEasingFunction"/>.</summary>
        public TweenCase SetCustomEasing(Ease.IEasingFunction easeFunction)
        {
            this.easeFunction = easeFunction;
            return this;
        }

        /// <summary>Sets the total animation duration in seconds (excluding delay).</summary>
        public TweenCase SetDuration(float duration)
        {
            this.duration = duration;
            return this;
        }

        // -----------------------------------------------------------
        // Callbacks
        // -----------------------------------------------------------

        /// <summary>
        /// Registers a callback invoked when the tween completes naturally or via <see cref="Complete"/>.
        /// Only one callback is stored — each call replaces the previous one (last call wins).
        /// <b>Not</b> fired when the tween is stopped via <see cref="Kill"/>.
        /// </summary>
        public TweenCase OnComplete(SimpleCallback callback)
        {
            completedCallback = callback;
            return this;
        }

        /// <summary>
        /// Registers a callback fired when the tween ends for any reason — both <see cref="Complete"/> and <see cref="Kill"/>.
        /// Used internally by <see cref="TweenCaseCollection"/>. Always has at most one subscriber.
        /// </summary>
        internal TweenCase OnTerminated(SimpleCallback callback)
        {
            terminatedCallback = callback;
            return this;
        }

        /// <summary>
        /// Registers a one-shot callback fired when the normalized progress reaches <paramref name="t"/> (range 0–1).
        /// Multiple thresholds can be registered; each fires exactly once and is then discarded.
        /// </summary>
        /// <param name="t">Normalized time threshold in [0, 1].</param>
        /// <param name="callback">The callback to invoke when the threshold is crossed.</param>
        public TweenCase OnTimeReached(float t, SimpleCallback callback)
        {
            callbackData ??= new List<CallbackData>();
            callbackData.Add(new CallbackData(t, callback));
            return this;
        }

        // -----------------------------------------------------------
        // Internal — called only by TweensHolder or TweenCase itself
        // -----------------------------------------------------------

        /// <summary>
        /// Applies the easing function to the raw progress value <paramref name="p"/>.
        /// Call this inside <see cref="Invoke"/> to obtain the eased interpolation factor.
        /// </summary>
        /// <param name="p">Raw normalized progress in [0, 1].</param>
        /// <returns>Eased value, typically in [0, 1] (may overshoot for elastic/back easings).</returns>
        public float Interpolate(float p) => easeFunction.Interpolate(p);

        /// <summary>Advances the delay timer by <paramref name="deltaTime"/>. Called by <see cref="TweensHolder"/> while the delay has not yet elapsed.</summary>
        internal void UpdateDelay(float deltaTime)
        {
            currentDelay += deltaTime;
        }

        /// <summary>
        /// Advances <see cref="State"/> by <c>deltaTime / duration</c>,
        /// fires any <see cref="OnTimeReached"/> callbacks whose threshold was crossed,
        /// and sets <see cref="IsCompleted"/> when state reaches 1.
        /// Called by <see cref="TweensHolder"/> every frame after the delay has elapsed.
        /// </summary>
        internal void UpdateState(float deltaTime)
        {
            state += Mathf.Min(1.0f, deltaTime / duration);

            // Check OnTimeReached callbacks independently — even if state jumped past 1.
            if (!callbackData.IsNullOrEmpty())
            {
                for (int i = 0; i < callbackData.Count; i++)
                {
                    var data = callbackData[i];
                    if (state >= data.t)
                    {
                        data.callback?.Invoke();
                        callbackData.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (state >= 1)
                isCompleted = true;
        }

        /// <summary>Invokes the registered <see cref="OnComplete"/> callback. Called by <see cref="TweensHolder"/> after the tween completes naturally.</summary>
        internal void InvokeCompleteEvent()
        {
            completedCallback?.Invoke();
        }

        /// <summary>
        /// Called by <see cref="TweensHolder"/> after the tween has been fully removed from its array.
        /// Resets <see cref="IsKilling"/> so the tween can be safely <see cref="Restart"/>ed.
        /// </summary>
        internal void ResetKillingState()
        {
            isKilling = false;
        }

        // -----------------------------------------------------------
        // Abstract — implemented by concrete tween cases
        // -----------------------------------------------------------

        /// <summary>
        /// Called every frame while the tween is active and not paused (after any delay has elapsed).
        /// Implement the actual value interpolation here using <see cref="Interpolate"/>(<see cref="state"/>).
        /// </summary>
        /// <param name="deltaTime">Elapsed time since the last frame (scaled or unscaled, matching <see cref="IsUnscaled"/>).</param>
        public abstract void Invoke(float deltaTime);

        /// <summary>
        /// Called when the tween finishes naturally or via <see cref="Complete"/>.
        /// Snap the animated value to its final state here to guarantee the animation reaches its target.
        /// <b>Not</b> called when the tween is stopped via <see cref="Kill"/>.
        /// </summary>
        public abstract void DefaultComplete();

        // -----------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------

        private readonly struct CallbackData
        {
            public readonly float t;
            public readonly SimpleCallback callback;

            public CallbackData(float t, SimpleCallback callback)
            {
                this.t = t;
                this.callback = callback;
            }
        }
    }
}
