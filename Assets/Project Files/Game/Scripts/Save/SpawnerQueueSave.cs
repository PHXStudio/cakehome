using System;
using System.Collections.Generic;

namespace Watermelon
{
    // Global (not per-zone) — pending spawner rewards stay claimable across zone switches,
    // so the queue lives in its own save object instead of TaskSave's $"Tasks_{zoneId}".
    [Serializable]
    public class SpawnerQueueSave : ISaveObject
    {
        public List<SpawnerQueueEntry> Entries = new List<SpawnerQueueEntry>();

        public void OnBeforeSave() { }
    }
}
