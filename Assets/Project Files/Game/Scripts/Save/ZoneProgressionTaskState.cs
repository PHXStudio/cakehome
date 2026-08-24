using System;
using System.Collections.Generic;

namespace Watermelon
{
    // Tracks how far the zone's ZoneProgressionStep task queue has advanced. SequentialAdded and
    // RandomSpawnCount are counters/cursors only — which steps have been reached is re-derived
    // from BuildingController's upgrade count every time, same as BuildingSave's own counter.
    [Serializable]
    public class ZoneProgressionTaskState
    {
        public int          SequentialAdded;
        public int          RandomSpawnCount;
        public List<string> PendingBatchTaskIds = new List<string>();
    }
}
