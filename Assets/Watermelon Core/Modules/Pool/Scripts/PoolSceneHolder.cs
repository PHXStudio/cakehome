using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// MonoBehaviour that initializes a set of pools when a scene loads and destroys them when it unloads.
    /// Runs before default script execution order (-5) to ensure pools are ready for other Awake calls.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    public class PoolSceneHolder : MonoBehaviour
    {
        [SerializeField] Pool[] pools;

        private void Awake()
        {
            foreach(Pool pool in pools)
            {
                pool.Init();
            }
        }

        private void OnDestroy()
        {
            foreach (Pool pool in pools)
            {
                PoolManager.DestroyPool(pool);
            }
        }
    }
}