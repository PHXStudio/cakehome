using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Pure C# save system core. Manages save files, cloud sync, and data access.
    /// Fully testable without MonoBehaviour — inject ISaveWrapper via constructor.
    /// SaveController is the thin MonoBehaviour adapter that drives coroutines and Unity lifecycle.
    /// </summary>
    public class SaveManager
    {
        private readonly ISaveWrapper wrapper;
        private readonly CloudSaveManager cloudManager;

        private Dictionary<string, SaveFile> loadedFiles;
        private Dictionary<string, object> fileLocks;
        private readonly HashSet<string> pendingCloudSync = new();
        private SaveFile defaultFile;
        private string defaultFileName;
        private bool initForced;

        /// <summary>True once the default save file has been loaded (and cloud sync has resolved or timed out).</summary>
        public bool IsSaveLoaded { get; private set; }
        /// <summary>The platform wrapper used for all file I/O.</summary>
        public ISaveWrapper Wrapper => wrapper;

        public SaveManager(ISaveWrapper wrapper)
        {
            this.wrapper      = wrapper;
            this.cloudManager = new CloudSaveManager();
        }

        /// <summary>Registers the cloud save handler and calls its <see cref="ICloudSaveHandler.Init"/> method.</summary>
        public void SetCloudHandler(ICloudSaveHandler handler)
        {
            cloudManager.SetHandler(handler);
        }

        /// <summary>Passes configuration to the underlying <see cref="ISaveWrapper"/> (e.g. WebGL prefix).</summary>
        public void Configure(SaveWrapperConfig config)
        {
            wrapper.Configure(config);
        }

        /// <summary>
        /// Loads the default save file and performs cloud sync.
        /// onComplete fires when IsSaveLoaded becomes true.
        /// With no cloud handler the callback is synchronous — IsSaveLoaded is true before Init returns.
        /// With a real cloud handler the callback fires asynchronously after the network response.
        /// namedFiles: additional file names declared by the project to cloud-sync on startup.
        /// </summary>
        public void Init(string fileName, string[] namedFiles, Action onComplete)
        {
            defaultFileName = fileName;
            loadedFiles     = new Dictionary<string, SaveFile>();
            fileLocks       = new Dictionary<string, object>();

            wrapper.Init();

            defaultFile = wrapper.Load(fileName);
            defaultFile.Init();

            // Register immediately so Save() works even while cloud sync is in flight.
            loadedFiles[fileName] = defaultFile;

            cloudManager.SyncFromCloud(fileName, defaultFile, result =>
            {
                // Guard: if ForceCompleteInit() already fired (timeout), the game is running
                // with local data. Applying a late cloud result would silently replace in-use
                // objects and create a split-brain state — discard instead.
                if (initForced)
                {
                    LogManager.LogWarning("[Save Manager]: Cloud sync completed after init timeout — result discarded to avoid overwriting in-use data.", LogCategory.Systems);
                    return;
                }

                if (result != null && result.success && result.resolvedSave != null)
                {
                    defaultFile = result.resolvedSave;

                    if (result.resolution == CloudSyncResult.CloudConflictResolution.CloudPreferred)
                    {
                        LogManager.Log("[Save Manager]: Loaded cloud save (cloud was newer)", LogCategory.Systems);
                        defaultFile.Flush(updateLastSaved: false);
                        WriteRawLocked(fileName, SaveJson.ToJson(defaultFile));
                        // Cloud applied locally — no re-upload needed
                    }
                    else if (result.resolution == CloudSyncResult.CloudConflictResolution.LocalPreferred)
                    {
                        LogManager.Log("[Save Manager]: Kept local save (local was newer)", LogCategory.Systems);
                        pendingCloudSync.Add(fileName); // upload local so cloud stays in sync
                    }
                    else if (result.resolution == CloudSyncResult.CloudConflictResolution.NoConflict)
                    {
                        // Cloud has no save yet — upload local to populate it
                        pendingCloudSync.Add(fileName);
                    }
                }
                else
                {
                    LogManager.Log("[Save Manager]: Cloud sync skipped or failed, using local", LogCategory.Systems);
                }

                loadedFiles[fileName] = defaultFile;

                SyncNamedFilesFromCloud(namedFiles, () =>
                {
                    IsSaveLoaded = true;
                    onComplete?.Invoke();
                });
            });
        }

        /// <summary>
        /// Forces initialization to complete immediately.
        /// Used when cloud sync times out — local save is already loaded, only cloud result is pending.
        /// </summary>
        public void ForceCompleteInit()
        {
            if (!IsSaveLoaded)
            {
                initForced   = true;
                IsSaveLoaded = true;
                LogManager.LogWarning("[Save Manager]: Init forcibly completed — cloud sync may not have finished.", LogCategory.Systems);
            }
        }

        /// <summary>
        /// Downloads declared named files from cloud in parallel.
        /// If cloud is not configured or namedFiles is empty, calls onComplete immediately.
        /// </summary>
        private void SyncNamedFilesFromCloud(string[] namedFiles, Action onComplete)
        {
            if (!cloudManager.IsConfigured || namedFiles == null || namedFiles.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            int remaining = namedFiles.Length;

            foreach (string namedFileName in namedFiles)
            {
                string capturedName = namedFileName; // safe closure capture
                SaveFile localFile  = wrapper.Load(capturedName);
                localFile.Init();

                cloudManager.SyncFromCloud(capturedName, localFile, namedResult =>
                {
                    // NOTE: ICloudSaveHandler contract requires callbacks on main thread.
                    // Interlocked.Decrement is used defensively in case an implementation violates this.
                    SaveFile resolved = localFile;

                    if (namedResult != null && namedResult.success && namedResult.resolvedSave != null)
                    {
                        resolved = namedResult.resolvedSave;

                        if (namedResult.resolution == CloudSyncResult.CloudConflictResolution.CloudPreferred)
                        {
                            LogManager.Log($"[Save Manager]: Loaded cloud save for '{capturedName}' (cloud was newer)", LogCategory.Systems);
                            resolved.Flush(updateLastSaved: false);
                            WriteRawLocked(capturedName, SaveJson.ToJson(resolved));
                            // Cloud applied locally — no re-upload needed
                        }
                        else if (namedResult.resolution == CloudSyncResult.CloudConflictResolution.LocalPreferred)
                        {
                            LogManager.Log($"[Save Manager]: Kept local save for '{capturedName}' (local was newer)", LogCategory.Systems);
                            pendingCloudSync.Add(capturedName); // upload local so cloud stays in sync
                        }
                        else if (namedResult.resolution == CloudSyncResult.CloudConflictResolution.NoConflict)
                        {
                            // Cloud has no save yet — upload local to populate it
                            pendingCloudSync.Add(capturedName);
                        }
                    }
                    else
                    {
                        LogManager.Log($"[Save Manager]: Cloud sync skipped or failed for '{capturedName}', using local", LogCategory.Systems);
                    }

                    loadedFiles[capturedName] = resolved;

                    if (Interlocked.Decrement(ref remaining) == 0)
                        onComplete?.Invoke();
                });
            }
        }

        // --- Default file accessors ---

        /// <summary>Returns the save object for type T from the default save file, resolving the key via <see cref="SaveKeyMap"/> or the <see cref="SaveKeyAttribute"/>.</summary>
        public T GetSaveObject<T>() where T : ISaveObject, new()
        {
            if (!IsSaveLoaded)
            {
                Debug.LogError("[Save Manager]: Not initialized.");
                return default;
            }

            return defaultFile.GetSaveObject<T>(); // SaveKey attribute resolved inside SaveFile
        }

        /// <summary>Returns the save object stored under the given explicit key in the default save file.</summary>
        public T GetSaveObject<T>(string key) where T : ISaveObject, new()
        {
            if (!IsSaveLoaded)
            {
                Debug.LogError("[Save Manager]: Not initialized.");
                return default;
            }

            return defaultFile.GetSaveObject<T>(key);
        }

        /// <summary>
        /// Returns a save object from a named save file.
        /// <paramref name="key"/> is optional — if omitted, resolved via [SaveKey] attribute or typeof(T).FullName.
        /// </summary>
        public T GetSaveObject<T>(string fileName, string key = null) where T : ISaveObject, new()
        {
            if (!IsSaveLoaded)
            {
                Debug.LogError("[Save Manager]: Not initialized.");
                return default;
            }

            SaveFile file = GetFile(fileName);
            return key != null ? file.GetSaveObject<T>(key) : file.GetSaveObject<T>();
        }

        /// <summary>
        /// Marks all currently loaded save files as dirty so they are included in the next Save() call.
        /// </summary>
        public void MarkAllAsDirty()
        {
            if (loadedFiles == null) return;
            foreach (SaveFile file in loadedFiles.Values)
                file.MarkAsDirty();
        }

        // --- Multiple file support ---

        /// <summary>Returns a named save file; loads it from disk on first access (lazy load).</summary>
        public SaveFile GetFile(string fileName)
        {
            if (loadedFiles == null)
            {
                Debug.LogError("[Save Manager]: Not initialized.");
                return null;
            }

            if (!loadedFiles.TryGetValue(fileName, out SaveFile file))
            {
                file = wrapper.Load(fileName);
                file.Init();
                loadedFiles[fileName] = file;
            }

            return file;
        }

        // --- Save ---

        /// <summary>Flushes and writes all dirty save files to disk; pass <c>forceSave: true</c> to write regardless of dirty state.</summary>
        public void Save(bool forceSave = false, bool useThreads = true)
        {
            if (loadedFiles == null) return;

            foreach (KeyValuePair<string, SaveFile> kvp in loadedFiles)
            {
                if (forceSave || kvp.Value.IsDirty)
                    WriteFile(kvp.Key, kvp.Value, useThreads);
            }
        }

        /// <summary>Flushes and serializes one save file, then writes it to disk — on a background thread if supported.</summary>
        private void WriteFile(string fileName, SaveFile file, bool useThreads)
        {
            file.Flush(updateLastSaved: true);

            string json = SaveJson.ToJson(file);

            if (useThreads && wrapper.UseThreads())
            {
                object fileLock = GetFileLock(fileName);

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    lock (fileLock)
                    {
                        wrapper.SaveRaw(fileName, json);
                        LogManager.Log($"[Save Manager]: Saved '{fileName}'.", LogCategory.Systems);
                    }
                });
            }
            else
            {
                WriteRawLocked(fileName, json);
                LogManager.Log($"[Save Manager]: Saved '{fileName}'.", LogCategory.Systems);
            }

            // Debounced: mark for cloud upload without uploading immediately.
            // Call SyncPendingToCloud() on focus loss, pause, or periodic timer.
            if (cloudManager.IsConfigured)
                pendingCloudSync.Add(fileName);
        }

        /// <summary>Returns the lock object for a given save file name, creating it on first use. Must only be called from the main thread.</summary>
        private object GetFileLock(string fileName)
        {
            if (!fileLocks.TryGetValue(fileName, out object fileLock))
            {
                fileLock = new object();
                fileLocks[fileName] = fileLock;
            }

            return fileLock;
        }

        /// <summary>Writes to disk under the per-file lock so this can never race with a concurrent <see cref="WriteFile"/> for the same file name.</summary>
        private void WriteRawLocked(string fileName, string json)
        {
            lock (GetFileLock(fileName))
            {
                wrapper.SaveRaw(fileName, json);
            }
        }

        /// <summary>
        /// Uploads all locally-saved files that haven't been synced to cloud yet.
        /// Call this on focus loss, app pause, or a low-frequency timer (e.g. every 5 min).
        /// </summary>
        public void SyncPendingToCloud()
        {
            if (!cloudManager.IsConfigured || pendingCloudSync.Count == 0) return;

            // Snapshot and clear before dispatching so re-entrant calls don't double-upload.
            string[] pending = new string[pendingCloudSync.Count];
            pendingCloudSync.CopyTo(pending);
            pendingCloudSync.Clear();

            foreach (string fileName in pending)
            {
                if (loadedFiles.TryGetValue(fileName, out SaveFile file))
                {
                    string capturedName = fileName;
                    cloudManager.SyncToCloud(capturedName, file, success =>
                    {
                        // Re-queue on failure so the next sync attempt retries.
                        if (!success) pendingCloudSync.Add(capturedName);
                    });
                }
            }
        }

        // --- Delete ---

        /// <summary>Removes the named save file from memory, disk, and cloud storage.</summary>
        public void DeleteFile(string fileName)
        {
            loadedFiles?.Remove(fileName);
            fileLocks?.Remove(fileName);
            pendingCloudSync?.Remove(fileName);
            wrapper.Delete(fileName);

            if (cloudManager.IsConfigured)
                cloudManager.DeleteCloud(fileName, _ => { });
        }

        /// <summary>Removes the default save file from memory, disk, and cloud storage.</summary>
        public void DeleteDefaultFile()
        {
            loadedFiles?.Remove(defaultFileName);
            fileLocks?.Remove(defaultFileName);
            pendingCloudSync?.Remove(defaultFileName);
            wrapper.Delete(defaultFileName);

            if (cloudManager.IsConfigured)
                cloudManager.DeleteCloud(defaultFileName, _ => { });
        }

        // --- Utilities ---

        /// <summary>
        /// Exports a snapshot of the default save file to an arbitrary absolute path; used for preset authoring in the editor.
        /// Writes directly to <paramref name="presetPath"/> — unlike <see cref="ISaveWrapper.SaveRaw"/>, does not resolve
        /// against persistentDataPath or append the named-save-file extension.
        /// </summary>
        public void PresetsSave(string presetPath)
        {
            if (defaultFile == null) return;
            defaultFile.Flush(updateLastSaved: false);

            string directory = Path.GetDirectoryName(presetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(presetPath, SaveJson.ToJson(defaultFile));
        }

        /// <summary>Returns a deep copy of the default save file; reflects in-memory changes even if the file hasn't been flushed to disk yet.</summary>
        public SaveFile GetDefaultFileCopy()
        {
            // If there are unsaved in-memory changes, disk is stale — serialize from memory.
            if (defaultFile != null && defaultFile.IsDirty)
            {
                defaultFile.Flush(updateLastSaved: false);
                string json = SaveJson.ToJson(defaultFile);
                SaveFile copy = SaveJson.FromJson<SaveFile>(json);
                copy.Init();
                return copy;
            }

            return wrapper.Load(defaultFileName); // Load() now calls Init() internally
        }

        /// <summary>
        /// Writes saveFile to disk under the given fileName (defaults to the default save file).
        /// Does not update lastSaved or saveCount.
        /// </summary>
        public void SaveCustom(SaveFile saveFile, string fileName = null)
        {
            if (saveFile == null) return;
            string targetFile = fileName ?? defaultFileName;
            saveFile.Flush(updateLastSaved: false);
            WriteRawLocked(targetFile, SaveJson.ToJson(saveFile));
        }

        /// <summary>Logs default save file info (last saved time, container keys) to the Unity console.</summary>
        public void Info() => defaultFile?.Info();
    }
}
