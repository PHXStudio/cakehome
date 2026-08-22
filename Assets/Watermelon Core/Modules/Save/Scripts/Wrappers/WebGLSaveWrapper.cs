using System.Runtime.InteropServices;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Save wrapper for WebGL builds that stores data in the browser's <c>localStorage</c> via a JavaScript bridge.
    /// No-op in the Unity Editor; active only under <c>UNITY_WEBGL &amp;&amp; !UNITY_EDITOR</c>.
    /// Does not support background thread writes — <see cref="UseThreads"/> returns <c>false</c>.
    /// </summary>
    public class WebGLSaveWrapper : ISaveWrapper
    {
        /// <summary>No-op; WebGL initialization is performed by <see cref="Configure"/> when a prefix is set.</summary>
        public void Init() {}

        /// <summary>Loads the save file from <c>localStorage</c> by key; returns an empty <see cref="SaveFile"/> if the key does not exist.</summary>
        public SaveFile Load(string fileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string json = load(fileName);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    SaveFile loaded = SaveJson.FromJson<SaveFile>(json);
                    if (loaded != null)
                    {
                        loaded.Init();
                        return loaded;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Save]: Failed to load WebGL save '{fileName}': {ex.Message}");
                }
            }
#endif
            SaveFile empty = new();
            empty.Init();
            return empty;
        }

        /// <summary>Stores the JSON string in <c>localStorage</c> under the given key.</summary>
        public void SaveRaw(string fileName, string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            save(fileName, json);
#endif
        }

        /// <summary>Removes the entry from <c>localStorage</c> for the given key.</summary>
        public void Delete(string fileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            deleteItem(fileName);
#endif
        }

        /// <summary>Returns <c>false</c>; <c>localStorage</c> is synchronous and must be called on the main thread.</summary>
        public bool UseThreads() => false;

        /// <summary>Passes the <see cref="SaveWrapperConfig.WebGLPrefix"/> to the JS <c>init()</c> function to namespace all <c>localStorage</c> keys.</summary>
        public void Configure(SaveWrapperConfig config)
        {
            if(config == null) return;

            if (!string.IsNullOrEmpty(config.WebGLPrefix))
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                init(config.WebGLPrefix);
#endif
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string init(string prefix);

        [DllImport("__Internal")]
        private static extern string load(string keyName);

        [DllImport("__Internal")]
        private static extern void save(string keyName, string data);

        [DllImport("__Internal")]
        private static extern void deleteItem(string keyName);
#endif
    }
}
