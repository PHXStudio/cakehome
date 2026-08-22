#pragma warning disable 0649

using System.Collections;
using UnityEngine;

namespace Watermelon
{
    public class ProjectInitSettings : ScriptableObject
    {
        [SerializeField] InitModule[] modules;
        public InitModule[] Modules => modules;

        /// <summary>
        /// Initialize all modules sequentially, waiting for each to complete.
        /// This is a coroutine that should be started from Initializer.
        /// </summary>
        public IEnumerator InitAsync(GameObject owner, MonoBehaviour coroutineRunner)
        {
            if (modules == null || modules.Length == 0)
            {
                Debug.LogError("[ProjectInitSettings]: Modules list is empty.");
                yield break;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] == null)
                {
                    LogManager.LogWarning($"[ProjectInitSettings]: Module at index {i} is null — slot is empty.", LogCategory.Systems);
                    
                    continue;
                }

                LogManager.Log($"[ProjectInitSettings]: Initializing module \"{modules[i].ModuleName}\".", LogCategory.Systems);
                // Start the module's async initialization and wait for it to complete
                yield return coroutineRunner.StartCoroutine(modules[i].InitAsync(owner));
                LogManager.Log($"[ProjectInitSettings]: Module \"{modules[i].ModuleName}\" initialized.", LogCategory.Systems);
            }

            LogManager.Log($"[ProjectInitSettings]: All {modules.Length} modules initialized.", LogCategory.Systems);
        }

        /// <summary>
        /// Legacy sync version for backward compatibility.
        /// Deprecated: Use InitAsync instead.
        /// </summary>
        public void Init(GameObject owner)
        {
            // This would need to be called from a coroutine runner
            // For now, we'll keep it but it won't wait for async operations
            LogManager.LogWarning("[ProjectInitSettings]: Init() is deprecated. Use InitAsync() instead.", LogCategory.Systems);

            if (modules == null || modules.Length == 0)
            {
                LogManager.LogWarning("[ProjectInitSettings]: Modules list is empty.", LogCategory.Systems);
                return;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] == null)
                {
                    LogManager.LogWarning($"[ProjectInitSettings]: Module at index {i} is null — slot is empty.", LogCategory.Systems);
                    continue;
                }

                LogManager.Log($"[ProjectInitSettings]: Initializing module \"{modules[i].ModuleName}\".", LogCategory.Systems);
            }

            LogManager.Log($"[ProjectInitSettings]: All {modules.Length} modules initialized (sync mode - async operations not awaited).", LogCategory.Systems);
        }

        public void Unload()
        {
            if (modules == null || modules.Length == 0)
            {
                LogManager.LogWarning("[ProjectInitSettings]: Modules list is empty.", LogCategory.Systems);
                return;
            }

            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] == null)
                {
                    LogManager.LogWarning($"[ProjectInitSettings]: Module at index {i} is null — slot is empty.", LogCategory.Systems);
                    continue;
                }

                modules[i].Unload();
            }
        }

        public T GetModule<T>() where T : InitModule
        {
            foreach (var module in modules)
            {
                if (module != null && module is T)
                {
                    return (T)module;
                }
            }

            return null;
        }
    }
}
