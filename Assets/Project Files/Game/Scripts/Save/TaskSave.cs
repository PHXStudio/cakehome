using System;
using System.Collections.Generic;

namespace Watermelon
{
    // Keyed per zone ($"Tasks_{zoneId}") — active tasks only ever apply to whichever zone's
    // board is currently live, same reasoning as MergeSave.
    [Serializable]
    public class TaskSave : ISaveObject
    {
        public List<ClientOrderTaskSaveData> ClientOrders = new List<ClientOrderTaskSaveData>();
        public ZoneProgressionTaskState       ProgressionTasks = new ZoneProgressionTaskState();

        public void OnBeforeSave() { }
    }
}
