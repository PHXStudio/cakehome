#if UNITY_EDITOR
using UnityEngine;

namespace Watermelon
{
    // Debug-only: places an item (itemTypeId + grade) straight onto the first empty merge cell.
    public class MergeDebugSpawner : MonoBehaviour, IDebugPersistentComponent
    {
        [SerializeField] string itemTypeId = "Coffee";
        [SerializeField] int grade = 1;

        public void SpawnObject()
        {
            MergeGrid grid = MergeController.Instance?.Grid;
            MergeItemData data = MergeDatabase.Instance?.GetItem(itemTypeId);
            MergeCell cell = grid?.FindAnyEmpty();

            if (grid == null || data == null || cell == null)
            {
                Debug.LogWarning($"[MergeDebugSpawner] SpawnObject failed: grid={grid != null}, item '{itemTypeId}'={data != null}, emptyCell={cell != null}");
                return;
            }

            grid.SpawnObject(data, grade, cell.Position.x, cell.Position.y);
            MergeController.Instance.SyncSaveData();
        }
    }
}
#endif
