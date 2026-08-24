using System.Collections;
using UnityEngine;

namespace Watermelon
{
    public abstract class InitModule : ScriptableObject
    {
        public abstract string ModuleName { get; }

        /// <summary>
        /// Async initialization of this module.
        /// Return a coroutine that will be executed and awaited by ProjectInitSettings.
        /// Use 'yield return' to wait for async operations (loading, network requests, etc).
        /// Guaranteed to complete before the next module initializes.
        /// </summary>
        public abstract IEnumerator InitAsync(GameObject owner);

        /// <summary>
        /// Called when the module should be unloaded (e.g. on scene exit or app quit).
        /// Override to release resources and clear the static instance.
        /// </summary>
        public virtual void Unload() { }
    }
}
