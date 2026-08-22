using UnityEngine;
using System.Collections.Generic;

namespace Watermelon
{
    /// <summary>
    /// Class that manages all pool operations.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager instance;

        private const string OBJECT_FORMAT = "{0} e{1}";

        /// <summary>
        /// List of all existing pools.
        /// </summary>
        private List<IPool> poolsList;

        /// <summary>
        /// Dictionary which allows to access Pool by name.
        /// </summary>
        private Dictionary<int, IPool> poolsDictionary;

        private Transform defaultContainer;

        public static Transform DefaultContainer => instance != null ? instance.defaultContainer : null;

        public static IReadOnlyList<IPool> Pools => instance != null ? instance.poolsList : null;

        private void Awake()
        {
            Init();

            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Initializes the PoolManager instance.
        /// Called automatically by Awake; invoke explicitly after AddComponent in tests
        /// (EditMode does not trigger Awake).
        /// </summary>
        public void Init()
        {
            instance = this;
            poolsList = new List<IPool>();
            poolsDictionary = new Dictionary<int, IPool>();
        }

        private void OnDestroy()
        {
            poolsList.Clear();
            poolsDictionary.Clear();

            instance = null;
        }

        public static void ReturnToPool()
        {
            if (instance == null) return;

            if (!instance.poolsList.IsNullOrEmpty())
            {
                for (int i = 0; i < instance.poolsList.Count; i++)
                {
                    instance.poolsList[i].ReturnToPoolEverything(true);
                }
            }
        }

        /// <summary>
        /// Returns reference to Pool by it's name.
        /// </summary>
        /// <param name="poolName">Name of Pool which should be returned.</param>
        /// <returns>Reference to Pool.</returns>
        public static IPool GetPoolByName(string poolName)
        {
            if (instance == null) return null;

            int poolHash = poolName.GetHashCode();

            if (instance.poolsDictionary.ContainsKey(poolHash))
            {
                return instance.poolsDictionary[poolHash];
            }

            Debug.LogError("[Pool] Not found pool with name: '" + poolName + "'");

            return null;
        }

        public static void AddPool(IPool pool)
        {
            if (instance == null)
            {
                Debug.LogError("[Pool]: Attempted to add a pool but PoolManager is not initialized.");

                return;
            }

            if (pool == null)
            {
                Debug.LogError("[Pool]: Attempted to add a null pool reference. Please ensure a valid IPool instance is provided.");

                return;
            }

            int poolHash = pool.Name.GetHashCode();

            if (instance.poolsDictionary.ContainsKey(poolHash))
            {
                Debug.LogError("[Pool] Adding a new pool failed. Name \"" + pool.Name + "\" already exists.");

                return;
            }

            instance.poolsDictionary.Add(poolHash, pool);
            instance.poolsList.Add(pool);
        }

        public static bool HasPool(string name)
        {
            if (instance == null) return false;

            return instance.poolsDictionary.ContainsKey(name.GetHashCode());
        }

        public static void DestroyPool(IPool pool)
        {
            if (instance == null) return;

            if (pool == null)
            {
                Debug.LogError("[Pool]: Attempted to destroy a null pool reference. Please ensure a valid IPool instance is provided.");

                return;
            }

            pool.Clear();

            instance.poolsDictionary.Remove(pool.Name.GetHashCode());
            instance.poolsList.Remove(pool);
        }

        public static Transform GetContainer(Transform poolContainer)
        {
#if UNITY_EDITOR
            if (poolContainer == null && instance != null)
            {
                if (instance.defaultContainer == null)
                {
                    // Create container object
                    GameObject containerObject = new("[POOL OBJECTS]");
                    instance.defaultContainer = containerObject.transform;
                    instance.defaultContainer.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    instance.defaultContainer.localScale = Vector3.one;

                    DontDestroyOnLoad(instance.defaultContainer);
                }

                return instance.defaultContainer;
            }
#endif

            return poolContainer;
        }

        public static string FormatName(string name, int elementIndex)
        {
            return string.Format(OBJECT_FORMAT, name, elementIndex);
        }
    }
}
