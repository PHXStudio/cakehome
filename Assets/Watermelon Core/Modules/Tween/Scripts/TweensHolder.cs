using UnityEngine;
using System.Collections.Generic;
using System;

namespace Watermelon
{
    /// <summary>
    /// Manages a flat array of <see cref="TweenCase"/> objects for a single Unity update loop
    /// (<see cref="UpdateMethod.Update"/>, <see cref="UpdateMethod.FixedUpdate"/>, or <see cref="UpdateMethod.LateUpdate"/>).
    ///
    /// <para><b>Design highlights:</b>
    /// <list type="bullet">
    ///   <item>Killed tweens are collected in a deferred list and removed <em>after</em> the update loop
    ///         to avoid modifying the array during iteration.</item>
    ///   <item>The array is compacted lazily — gaps left by removed tweens are filled only when a new tween
    ///         is added or at the start of the next update.</item>
    ///   <item>The backing array doubles in capacity when full, avoiding frequent reallocations.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class TweensHolder
    {
        protected TweenCase[] tweens;

        /// <summary>
        /// Raw backing array of active tweens. Slots may be <c>null</c> if a tween was recently removed
        /// but the array has not yet been compacted.
        /// </summary>
        public TweenCase[] Tweens => tweens;

        protected int tweensCount;

        protected bool hasActiveTweens = false;

        protected bool requiresActiveReorganization = false;
        protected int reorganizeFromID = -1;
        protected int maxActiveLookupID = -1;

        protected List<TweenCase> killingTweens = new List<TweenCase>();

#if UNITY_EDITOR
        protected int maxTweensAmount = 0;
#endif

        protected UpdateMethod updateMethod;

        /// <summary>
        /// Creates a new holder for the specified update loop with an initial backing array of size <paramref name="defaultAmount"/>.
        /// The array doubles automatically when capacity is exceeded.
        /// </summary>
        /// <param name="updateMethod">Unity update loop this holder is tied to.</param>
        /// <param name="defaultAmount">Initial capacity of the tween array. Tune to the expected peak active count to avoid reallocations.</param>
        public TweensHolder(UpdateMethod updateMethod, int defaultAmount)
        {
            this.updateMethod = updateMethod;

            tweens = new TweenCase[defaultAmount];
        }

        /// <summary>
        /// Registers a <see cref="TweenCase"/> in this holder and marks it as active.
        /// Doubles the backing array if it is full, and compacts any pending gaps before inserting.
        /// </summary>
        public void AddTween(TweenCase tween)
        {
            if (tweensCount >= tweens.Length)
            {
                Array.Resize(ref tweens, tweens.Length * 2);

                LogManager.LogWarning("[Tween]: The amount of the tweens (" + updateMethod + ") was adjusted. Current size - " + tweens.Length + ". Change the default amount to prevent performance leak!", LogCategory.Systems);
            }

            if (requiresActiveReorganization)
                ReorganizeUpdateActiveTweens();

            tween.IsActive = true;
            tween.ActiveID = (maxActiveLookupID = tweensCount);

            tweens[tweensCount] = tween;
            tweensCount++;

            hasActiveTweens = true;

#if UNITY_EDITOR
            if (maxTweensAmount < tweensCount)
                maxTweensAmount = tweensCount;
#endif
        }

        /// <summary>
        /// Compacts the tween array by shifting active tweens left to fill gaps left by removed tweens.
        /// Called lazily before <see cref="AddTween"/> or at the start of <see cref="Update"/> when required.
        /// </summary>
        private void ReorganizeUpdateActiveTweens()
        {
            if (tweensCount <= 0)
            {
                maxActiveLookupID = -1;
                reorganizeFromID = -1;
                requiresActiveReorganization = false;

                return;
            }

            if (reorganizeFromID == maxActiveLookupID)
            {
                maxActiveLookupID--;
                reorganizeFromID = -1;
                requiresActiveReorganization = false;

                return;
            }

            int defaultOffset = 1;
            int tweensTempCount = maxActiveLookupID + 1;

            maxActiveLookupID = reorganizeFromID - 1;

            for (int i = reorganizeFromID + 1; i < tweensTempCount; i++)
            {
                TweenCase tween = tweens[i];
                if (tween != null)
                {
                    tween.ActiveID = (maxActiveLookupID = i - defaultOffset);

                    tweens[i - defaultOffset] = tween;
                    tweens[i] = null;
                }
                else
                {
                    defaultOffset++;
                }
            }

            requiresActiveReorganization = false;
            reorganizeFromID = -1;
        }

        /// <summary>
        /// Advances all active, non-paused tweens by the appropriate delta time, validates each tween,
        /// and processes natural completions. At the end of the loop, all tweens marked for killing are removed.
        /// </summary>
        /// <param name="deltaTime">Scaled delta time (<c>Time.deltaTime</c> or <c>Time.fixedDeltaTime</c>).</param>
        /// <param name="unscaledDeltaTime">Unscaled delta time (<c>Time.unscaledDeltaTime</c> or <c>Time.fixedUnscaledDeltaTime</c>).</param>
        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (!hasActiveTweens)
                return;

            if (requiresActiveReorganization)
                ReorganizeUpdateActiveTweens();

            for (int i = 0; i < tweensCount; i++)
            {
                TweenCase tween = tweens[i];
                if (tween != null)
                {
                    if (!tween.Validate())
                    {
                        tween.Kill();
                    }
                    else
                    {
                        if (tween.IsActive && !tween.IsPaused)
                        {
                            if (!tween.IsUnscaled)
                            {
                                if (Time.timeScale == 0)
                                    continue;

                                if (tween.Delay > 0 && tween.Delay > tween.CurrentDelay)
                                {
                                    tween.UpdateDelay(deltaTime);
                                }
                                else
                                {
                                    tween.UpdateState(deltaTime);

                                    tween.Invoke(deltaTime);
                                }
                            }
                            else
                            {
                                if (tween.Delay > 0 && tween.Delay > tween.CurrentDelay)
                                {
                                    tween.UpdateDelay(unscaledDeltaTime);
                                }
                                else
                                {
                                    tween.UpdateState(unscaledDeltaTime);

                                    tween.Invoke(unscaledDeltaTime);
                                }
                            }

                            // !IsKilling: skip if Complete() was already called inside Invoke()
                            if (tween.IsCompleted && !tween.IsKilling)
                            {
                                tween.DefaultComplete();

                                tween.InvokeCompleteEvent();

                                tween.Kill();
                            }
                        }
                    }
                }
            }

            int killingTweensCount = killingTweens.Count;
            for (int i = 0; i < killingTweensCount; i++)
            {
                RemoveActiveTween(killingTweens[i]);
            }
            killingTweens.Clear();
        }

        /// <summary>
        /// Physically removes a tween from the array, decrements the active count, schedules a compaction pass,
        /// and calls <see cref="TweenCase.ResetKillingState"/> so the tween can be restarted if needed.
        /// </summary>
        private void RemoveActiveTween(TweenCase tween)
        {
            int activeId = tween.ActiveID;
            if (activeId < 0) return;

            tween.ActiveID = -1;
            tween.ResetKillingState();

            requiresActiveReorganization = true;

            if (reorganizeFromID == -1 || reorganizeFromID > activeId)
            {
                reorganizeFromID = activeId;
            }

            tweens[activeId] = null;

            tweensCount--;
            hasActiveTweens = (tweensCount > 0);
        }

        /// <summary>Pauses all currently active tweens in this holder.</summary>
        public void Pause()
        {
            for (int i = 0; i < tweensCount; i++)
            {
                TweenCase tween = tweens[i];
                if (tween != null)
                {
                    tween.Pause();
                }
            }
        }

        /// <summary>Resumes all paused tweens in this holder.</summary>
        public void Resume()
        {
            for (int i = 0; i < tweensCount; i++)
            {
                TweenCase tween = tweens[i];
                if (tween != null)
                {
                    tween.Resume();
                }
            }
        }

        /// <summary>Kills all active tweens in this holder. OnComplete callbacks are <b>not</b> fired.</summary>
        public void Kill()
        {
            for (int i = 0; i < tweensCount; i++)
            {
                TweenCase tween = tweens[i];
                if (tween != null)
                {
                    tween.Kill();
                }
            }
        }

        /// <summary>
        /// Enqueues <paramref name="tween"/> for removal at the end of the current <see cref="Update"/> call.
        /// Deferred removal prevents modifying the array while it is being iterated.
        /// Called by <see cref="TweenCase.Kill"/>.
        /// </summary>
        public void MarkForKilling(TweenCase tween)
        {
            killingTweens.Add(tween);
        }

        /// <summary>
        /// Clears all tween references and resets all counters, returning the holder to its initial state.
        /// Called when the scene unloads or the <see cref="Tween"/> MonoBehaviour is destroyed.
        /// </summary>
        public void Unload()
        {
            if(!tweens.IsNullOrEmpty())
            {
                int clearCount = maxActiveLookupID + 1;
                for (int i = 0; i < clearCount; i++)
                {
                    if (tweens[i] != null)
                    {
                        // Reset so any deferred Kill() calls (e.g. from OnDisable after KillAll)
                        // hit the activeId < 0 early-return in RemoveActiveTween and don't corrupt
                        // the next session's tween array.
                        tweens[i].IsActive = false;
                        tweens[i].ActiveID = -1;
                        tweens[i] = null;
                    }
                }
            }

            killingTweens.Clear();

            tweensCount = 0;
            hasActiveTweens = false;

            requiresActiveReorganization = false;
            reorganizeFromID = -1;
            maxActiveLookupID = -1;
        }
    }
}
