using System;

namespace Watermelon
{
    // Joins UIQueueController behind every other entry and fires a callback once it's this
    // entry's turn — i.e. once the queue has fully drained everything else queued around it
    // (dialogs, level-up popups, building progression, ...).
    //
    // Deferred one frame before joining: without the defer, a caller reacting synchronously to
    // the same event that triggers a followup enqueue (e.g. BuildingController enqueuing its
    // progression dialog right after firing OnUpgradeCompleted) would join the empty queue first
    // and claim its "busy" slot before that followup entry even exists, running before it
    // instead of after.
    public static class UIQueueTail
    {
        public static void WaitForIdle(Action onReady)
        {
            Tween.NextFrame(() =>
            {
                UIQueueController.Enqueue(int.MaxValue, onDone =>
                {
                    onReady?.Invoke();
                    onDone();
                });
            });
        }
    }
}
