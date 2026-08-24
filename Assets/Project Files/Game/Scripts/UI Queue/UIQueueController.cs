using System;
using System.Collections.Generic;

namespace Watermelon
{
    // Runs UI actions (dialog, popup, etc.) one priority group at a time so they never overlap
    // across groups. Callers Enqueue a priority (lower runs first among entries waiting at once),
    // an optional delay, and an execute callback that must invoke the "onDone" argument it
    // receives once it's finished/closed. Entries sharing the same priority run concurrently as
    // one group; the next (lower-priority-value) group only starts once all of them call onDone.
    public class UIQueueController
    {
        private static UIQueueController instance;

        private readonly List<QueueEntry> pending = new List<QueueEntry>();
        private bool isBusy;

        public UIQueueController()
        {
            instance = this;
        }

        public static void Enqueue(int priority, Action<Action> execute, float delay = 0f)
        {
            instance?.EnqueueInternal(priority, execute, delay);
        }

        private void EnqueueInternal(int priority, Action<Action> execute, float delay)
        {
            pending.Add(new QueueEntry(priority, execute, delay));
            TryRunNext();
        }

        private void TryRunNext()
        {
            if (isBusy || pending.Count == 0) return;

            // Entries sharing the same priority were queued for the same moment (e.g. an exp-fly
            // and a reward's spawner-fly triggered by the same level-up) and are meant to play
            // together rather than one after another — batch them and run all at once, holding
            // the turn until every one of them has called its own onDone.
            int bestPriority = pending[0].Priority;
            for (int i = 1; i < pending.Count; i++)
                if (pending[i].Priority < bestPriority)
                    bestPriority = pending[i].Priority;

            List<QueueEntry> batch = new List<QueueEntry>();
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (pending[i].Priority == bestPriority)
                {
                    batch.Add(pending[i]);
                    pending.RemoveAt(i);
                }
            }

            isBusy = true;

            int remaining = batch.Count;
            foreach (QueueEntry entry in batch)
                Tween.DelayedCall(entry.Delay, () => entry.Execute(() =>
                {
                    remaining--;
                    if (remaining == 0) OnEntryDone();
                }));
        }

        private void OnEntryDone()
        {
            isBusy = false;
            TryRunNext();
        }

        private readonly struct QueueEntry
        {
            public readonly int Priority;
            public readonly float Delay;
            public readonly Action<Action> Execute;

            public QueueEntry(int priority, Action<Action> execute, float delay)
            {
                Priority = priority;
                Execute = execute;
                Delay = delay;
            }
        }

        public void Unload()
        {
            instance = null;
            pending.Clear();
            isBusy = false;
        }
    }
}
