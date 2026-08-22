using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Common interface for all pool types (Pool, PoolGeneric, PoolMultiple).
    /// </summary>
    public interface IPool
    {
        /// <summary>Unique name used to identify this pool in PoolManager.</summary>
        public string Name { get; }

        /// <summary>Registers the pool in PoolManager. Called automatically by constructors.</summary>
        public void Init();

        /// <summary>Returns an inactive pooled object, activating it. Creates a new instance if none are available.</summary>
        public GameObject GetPooledObject();

        /// <summary>Pre-warms the pool by ensuring at least <paramref name="count"/> objects exist.</summary>
        public void CreatePoolObjects(int count);

        /// <summary>Deactivates all active objects, returning them to the pool.</summary>
        public void ReturnToPoolEverything(bool resetParent = false);

        /// <summary>Destroys all pooled objects and unregisters the pool. Note: this method is performance heavy.</summary>
        public void Clear();
    }
}