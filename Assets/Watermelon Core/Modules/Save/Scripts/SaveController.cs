using System.Collections;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// MonoBehaviour facade over <see cref="SaveManager"/>. Manages Unity lifecycle (auto-save, cloud sync coroutines,
    /// focus/pause hooks) and exposes a static API for game code.
    /// Created automatically by <see cref="SaveInitModule"/> on the Initializer's GameObject.
    /// </summary>
    public class SaveController : MonoBehaviour
    {
        /// <summary>File name used for the primary save file (without extension).</summary>
        public const string DEFAULT_FILE_NAME = "save";

        private const float INIT_TIMEOUT_SECONDS = 15f;
#if UNITY_EDITOR
        private const float CLOUD_SYNC_INTERVAL = 30f;   // shorter for editor testing
#else
        private const float CLOUD_SYNC_INTERVAL = 300f;  // 5 minutes in production
#endif

        private static SaveController instance;

        // Created at field-init so Configure() can be called before InitAsync() (see SaveInitModule)
        private readonly SaveManager manager = new SaveManager(GetWrapper());

        /// <summary>Fires once when the save system finishes loading (including cloud sync or timeout fallback).</summary>
        public static event SimpleCallback OnSaveLoaded;

        /// <summary>
        /// Initializes the save system: auto-discovers a cloud handler, loads the default save file,
        /// performs cloud sync (with a timeout fallback), then starts auto-save and cloud sync coroutines.
        /// </summary>
        public IEnumerator InitAsync(float autoSaveDelay, string[] namedFiles = null)
        {
            instance = this;

            // Auto-discover cloud save handler — search self + children of the Initializer GameObject.
            // Scoped to this hierarchy; faster and more predictable than FindObjectOfType.
            CloudSaveBehavior cloudBehavior = GetComponentInChildren<CloudSaveBehavior>(true);
            if (cloudBehavior != null)
            {
                ICloudSaveHandler handler = cloudBehavior.GetConfiguredHandler();
                if (handler != null)
                {
                    manager.SetCloudHandler(handler);
                    LogManager.Log("[Save Controller]: Cloud handler auto-discovered and configured", LogCategory.Systems);
                }
            }

            manager.Init(DEFAULT_FILE_NAME, namedFiles, null);

            // Wait for cloud sync with a timeout — prevents infinite hang if SDK never fires callback
            float elapsed = 0f;
            while (!manager.IsSaveLoaded && elapsed < INIT_TIMEOUT_SECONDS)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!manager.IsSaveLoaded)
            {
                Debug.LogError($"[Save Controller]: Init timed out after {INIT_TIMEOUT_SECONDS}s. Falling back to local save.");
                manager.ForceCompleteInit();
            }

            // Upload local save to cloud if it was determined to be newer during init
            // (covers LocalPreferred and NoConflict cases where cloud needs to be updated)
            manager.SyncPendingToCloud();

            LogManager.Log("[Save Controller]: Save loaded.", LogCategory.Systems);
            OnSaveLoaded?.Invoke();

            if (autoSaveDelay > 0f)
                StartCoroutine(AutoSaveCoroutine(autoSaveDelay));

            StartCoroutine(CloudSyncCoroutine());
        }

        /// <summary>Applies platform-specific wrapper configuration (e.g. WebGL prefix) before initialization.</summary>
        public void Configure(SaveWrapperConfig config) => manager.Configure(config);

        private void OnDestroy()
        {
#if UNITY_EDITOR
            // Synchronous save on editor stop to avoid lost data
            manager.Save(forceSave: true, useThreads: false);
#endif
            OnSaveLoaded = null;
            instance = null;
        }

#if !UNITY_EDITOR
        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                manager.Save();
                manager.SyncPendingToCloud();
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                manager.Save();
                manager.SyncPendingToCloud();
            }
        }
#endif

        private IEnumerator AutoSaveCoroutine(float saveDelay)
        {
            var wait = new WaitForSeconds(saveDelay);

            while (true)
            {
                yield return wait;
                manager.Save();
            }
        }

        private IEnumerator CloudSyncCoroutine()
        {
            var wait = new WaitForSeconds(CLOUD_SYNC_INTERVAL);

            while (true)
            {
                yield return wait;
                manager.SyncPendingToCloud();
            }
        }

        // --- Static API (delegates to manager) ---

        /// <summary>Returns the save object for type T from the default save file, resolving the key automatically.</summary>
        public static T GetSaveObject<T>() where T : ISaveObject, new()
            => instance.manager.GetSaveObject<T>();

        /// <summary>Returns the save object stored under the given explicit key in the default save file.</summary>
        public static T GetSaveObject<T>(string key) where T : ISaveObject, new()
            => instance.manager.GetSaveObject<T>(key);

        /// <summary>Returns the save object from a named save file; key is optional and resolved automatically if omitted.</summary>
        public static T GetSaveObject<T>(string fileName, string key = null) where T : ISaveObject, new()
            => instance != null ? instance.manager.GetSaveObject<T>(fileName, key) : default;

        /// <summary>Marks all loaded save files as dirty so they are written on the next <see cref="Save"/> call.</summary>
        public static void MarkAsSaveIsRequired()
        {
            if (instance != null) instance.manager.MarkAllAsDirty();
        }

        /// <summary>Returns a named save file by its file name, loading it from disk on first access.</summary>
        public static SaveFile GetFile(string fileName)
        {
            if (instance == null) return null;
            return instance.manager.GetFile(fileName);
        }

        /// <summary>Flushes all dirty save files to disk; pass <c>forceSave: true</c> to write regardless of dirty state.</summary>
        public static void Save(bool forceSave = false, bool useThreads = true)
        {
            if (instance != null) instance.manager.Save(forceSave, useThreads);
        }

        /// <summary>Uploads all locally-saved files that are pending cloud sync.</summary>
        public static void SyncToCloud()
        {
            if (instance != null) instance.manager.SyncPendingToCloud();
        }

        /// <summary>Deletes a named save file from disk and cloud.</summary>
        public static void DeleteFile(string fileName)
        {
            if (instance != null) instance.manager.DeleteFile(fileName);
        }

        /// <summary>Deletes the default save file from disk and cloud.</summary>
        public static void DeleteSaveFile()
        {
            if (instance != null) { instance.manager.DeleteDefaultFile(); return; }
            
#if UNITY_EDITOR
            DefaultSaveWrapper wrapper = new DefaultSaveWrapper();
            wrapper.Init();
            wrapper.Delete(DEFAULT_FILE_NAME);
#endif
        }

        /// <summary>Returns a deep copy of the current default save file; reflects in-memory changes even before the next flush.</summary>
        public static SaveFile GetSaveFileCopy()
        {
            if (instance != null) return instance.manager.GetDefaultFileCopy();
#if UNITY_EDITOR
            DefaultSaveWrapper wrapper = new DefaultSaveWrapper();
            wrapper.Init();
            return wrapper.Load(DEFAULT_FILE_NAME);
#else
            return null;
#endif
        }

        /// <summary>Writes a custom <see cref="SaveFile"/> to disk without updating its <c>lastSaved</c> or <c>saveCount</c> metadata.</summary>
        public static void SaveCustom(SaveFile saveFile, string fileName = null)
        {
            if (instance != null) { instance.manager.SaveCustom(saveFile, fileName); return; }
#if UNITY_EDITOR
            if (saveFile == null) return;
            string targetFile = fileName ?? DEFAULT_FILE_NAME;
            DefaultSaveWrapper wrapper = new DefaultSaveWrapper();
            wrapper.Init();
            saveFile.Flush(updateLastSaved: false);
            wrapper.SaveRaw(targetFile, SaveJson.ToJson(saveFile));
#endif
        }

        /// <summary>Exports the default save file to an arbitrary path on disk, typically used for preset authoring in the editor.</summary>
        public static void PresetsSave(string presetPath)
        {
            if (instance != null) instance.manager.PresetsSave(presetPath);
        }

        /// <summary>Logs save file info (last saved time, container keys) to the Unity console.</summary>
        public static void Info()
        {
            if (instance != null) instance.manager.Info();
        }

        /// <summary>Returns the appropriate <see cref="ISaveWrapper"/> for the current platform: <see cref="WebGLSaveWrapper"/> on WebGL, <see cref="DefaultSaveWrapper"/> otherwise.</summary>
        public static ISaveWrapper GetWrapper()
        {
#if UNITY_EDITOR
            return new DefaultSaveWrapper();
#elif UNITY_WEBGL
            return new WebGLSaveWrapper();
#else
            return new DefaultSaveWrapper();
#endif
        }
    }
}
