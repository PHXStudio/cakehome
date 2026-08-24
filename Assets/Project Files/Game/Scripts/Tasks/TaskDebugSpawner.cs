#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class TaskDebugSpawner : MonoBehaviour, IDebugPersistentComponent
    {
        public enum DebugTaskType { ClientOrder, SpawnEntry }

        [SerializeField] DebugTaskType taskType;

        // ClientOrder
        [SerializeField] CharacterData character;
        [SerializeField] List<OrderItem> orderItems = new List<OrderItem>();
        [SerializeField] int coinsReward = 100;

        // SpawnEntry
        [SerializeField] string spawnerTypeId = "Kettle Spawner";
        [SerializeField] int spawnerGrade = 1;

        private static int debugCounter;

        public void AddSelectedTask()
        {
            string id = $"debug_{taskType}_{++debugCounter}";

            switch (taskType)
            {
                case DebugTaskType.ClientOrder:
                    // Deep-copy each OrderItem — it's a mutable class, so a shallow list copy would
                    // leave every debug-spawned task sharing (and silently rewriting) the same
                    // requirement objects whenever the Inspector fields are edited between spawns.
                    var items = orderItems.ConvertAll(o => new OrderItem { typeId = o.typeId, grade = o.grade });
                    TaskController.AddClientOrder(id, character, items, coinsReward);
                    break;
                case DebugTaskType.SpawnEntry:
                    TaskController.EnqueueSpawner(spawnerTypeId, spawnerGrade);
                    break;
            }
        }
    }
}
#endif
