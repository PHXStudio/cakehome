using System.Collections.Generic;

namespace Watermelon
{
    /// <summary>
    /// Tracks a group of <see cref="TweenCase"/> objects and fires a single <see cref="OnComplete"/> callback
    /// when every tween in the group has finished — either by natural completion or by being killed.
    ///
    /// <para><b>Usage patterns:</b>
    /// <list type="bullet">
    ///   <item>Manual: create a collection, call <see cref="AddTween"/> for each tween, then <see cref="OnComplete"/>.</item>
    ///   <item>Automatic: bracket tween creation with <see cref="Tween.BeginTweenCaseCollection"/> /
    ///         <see cref="Tween.EndTweenCaseCollection"/> — all tweens created in between are auto-collected.</item>
    ///   <item>Operator: use <c>collection += tween</c> to lazily create and populate a collection.</item>
    /// </list>
    /// </para>
    ///
    /// <para><b>Kill-path correctness:</b>
    /// The collection subscribes to each tween's internal <c>OnTerminated</c> hook, which fires for both
    /// <see cref="TweenCase.Complete"/> and <see cref="TweenCase.Kill"/>. This ensures the
    /// <see cref="OnComplete"/> callback is always invoked regardless of how individual tweens end.
    /// </para>
    /// </summary>
    public class TweenCaseCollection
    {
        private List<TweenCase> tweenCases = new List<TweenCase>();

        /// <summary>All tweens registered in this collection.</summary>
        public List<TweenCase> TweenCases => tweenCases;

        private SimpleCallback tweensCompleted;

        /// <summary>
        /// Adds <paramref name="tweenCase"/> to the collection and subscribes to its termination event.
        /// The termination hook fires for both <see cref="TweenCase.Complete"/> and <see cref="TweenCase.Kill"/>.
        /// </summary>
        public void AddTween(TweenCase tweenCase)
        {
            // OnTerminated fires for both Complete() and Kill() — covers all termination paths
            tweenCase.OnTerminated(OnTweenCaseTerminated);

            tweenCases.Add(tweenCase);
        }

        /// <summary>
        /// Returns <c>true</c> if every tween in the collection is no longer active
        /// (i.e. all have been completed or killed). An empty collection is considered complete.
        /// </summary>
        public bool IsComplete()
        {
            // IsActive becomes false immediately on Kill() or Complete()->Kill(), so this is the
            // single reliable "still running" check regardless of how the tween ended.
            for (int i = 0; i < tweenCases.Count; i++)
            {
                if (tweenCases[i].IsActive)
                    return false;
            }

            return true;
        }

        /// <summary>Forces immediate completion of all tweens in the collection (applies final values and fires per-tween callbacks).</summary>
        public void Complete()
        {
            for (int i = 0; i < tweenCases.Count; i++)
            {
                tweenCases[i].Complete();
            }
        }

        /// <summary>Kills all tweens in the collection. Per-tween OnComplete callbacks are <b>not</b> fired, but the collection's <see cref="OnComplete"/> callback is.</summary>
        public void Kill()
        {
            for (int i = 0; i < tweenCases.Count; i++)
            {
                tweenCases[i].Kill();
            }
        }

        /// <summary>
        /// Registers a callback to be invoked when all tweens in the collection finish (regardless of whether they complete or are killed).
        /// Multiple calls accumulate callbacks (<c>+=</c> semantics).
        /// </summary>
        public void OnComplete(SimpleCallback callback)
        {
            tweensCompleted += callback;
        }

        /// <summary>
        /// Called by each tween when it terminates. Checks whether all tweens have finished
        /// and fires <see cref="tweensCompleted"/> if so.
        /// </summary>
        private void OnTweenCaseTerminated()
        {
            for (int i = 0; i < tweenCases.Count; i++)
            {
                if (tweenCases[i].IsActive)
                    return;
            }

            tweensCompleted?.Invoke();
        }

        /// <summary>
        /// Convenience operator: adds <paramref name="tweenCase"/> to <paramref name="caseCollection"/>,
        /// creating a new collection first if <paramref name="caseCollection"/> is <c>null</c>.
        /// <example>
        /// <code>
        /// TweenCaseCollection group = null;
        /// group += transform.DOMove(target, 0.5f);
        /// group += spriteRenderer.DOFade(0f, 0.5f);
        /// group.OnComplete(() => Debug.Log("all done"));
        /// </code>
        /// </example>
        /// </summary>
        public static TweenCaseCollection operator +(TweenCaseCollection caseCollection, TweenCase tweenCase)
        {
            if(caseCollection == null)
                caseCollection = new TweenCaseCollection();

            caseCollection.AddTween(tweenCase);

            return caseCollection;
        }
    }
}
