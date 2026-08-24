using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Manages cloud save synchronization logic.
    /// Handles uploading local saves to cloud and downloading/resolving conflicts.
    /// </summary>
    public class CloudSaveManager
    {
        private ICloudSaveHandler handler;
        private Dictionary<string, SaveFileMetadata> localMetadataCache = new();

        /// <summary>True when a cloud handler has been set and reports that the service is ready.</summary>
        public bool IsConfigured => handler != null && handler.IsAvailable;

        /// <summary>Assigns the cloud handler and immediately calls its <see cref="ICloudSaveHandler.Init"/> method.</summary>
        public void SetHandler(ICloudSaveHandler cloudHandler)
        {
            handler = cloudHandler;
            if (handler != null)
            {
                handler.Init();
            }
        }

        /// <summary>
        /// Cache local metadata after a save operation.
        /// Used for next sync comparison.
        /// </summary>
        public void CacheLocalMetadata(string fileName, SaveFileMetadata metadata)
        {
            if (metadata != null)
            {
                localMetadataCache[fileName] = new SaveFileMetadata(metadata.timestampUnix, metadata.saveCount);
            }
        }

        /// <summary>
        /// Upload local save file to cloud after local save.
        /// Should be called after SaveFile.Flush() to ensure metadata is current.
        /// </summary>
        public void SyncToCloud(string fileName, SaveFile localSave, Action<bool> onComplete)
        {
            if (!IsConfigured)
            {
                LogManager.LogWarning("[Cloud Save]: Handler not configured, skipping cloud upload", LogCategory.Systems);
                onComplete?.Invoke(false);
                return;
            }

            if (localSave == null)
            {
                Debug.LogError("[Cloud Save]: Cannot sync null SaveFile to cloud");
                onComplete?.Invoke(false);
                return;
            }

            try
            {
                string json = SaveJson.ToJson(localSave);
                SaveFileMetadata metadata = new SaveFileMetadata(
                    localSave.LastSavedUnix,
                    localSave.SaveCount
                );

                CacheLocalMetadata(fileName, metadata);

                handler.Upload(fileName, json, metadata, success =>
                {
                    if (success)
                    {
                        LogManager.Log($"[Cloud Save]: Uploaded '{fileName}' successfully", LogCategory.Systems);
                    }
                    else
                    {
                        LogManager.LogWarning($"[Cloud Save]: Failed to upload '{fileName}'", LogCategory.Systems);
                    }

                    onComplete?.Invoke(success);
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Cloud Save]: Error preparing upload for '{fileName}': {ex.Message}");
                onComplete?.Invoke(false);
            }
        }

        /// <summary>
        /// Download and resolve cloud save with local save.
        /// Compares metadata and determines which version to use.
        /// Called at game startup to sync with cloud.
        /// </summary>
        public void SyncFromCloud(string fileName, SaveFile localSave, Action<CloudSyncResult> onComplete)
        {
            if (!IsConfigured)
            {
                var result = new CloudSyncResult
                {
                    success = true,
                    resolvedSave = localSave,
                    resolution = CloudSyncResult.CloudConflictResolution.LocalUnchanged,
                    localMetadata = ExtractMetadata(localSave)
                };
                onComplete?.Invoke(result);
                return;
            }

            handler.Download(fileName, (success, cloudJson, cloudMetadata) =>
            {
                ProcessSyncResult(fileName, localSave, success, cloudJson, cloudMetadata, onComplete);
            });
        }

        /// <summary>Compares local and cloud metadata to decide which version wins, then populates and returns a <see cref="CloudSyncResult"/>.</summary>
        private void ProcessSyncResult(
            string fileName,
            SaveFile localSave,
            bool downloadSuccess,
            string cloudJson,
            SaveFileMetadata cloudMetadata,
            Action<CloudSyncResult> onComplete)
        {
            var result = new CloudSyncResult();
            result.localMetadata = ExtractMetadata(localSave);

            if (!downloadSuccess || string.IsNullOrEmpty(cloudJson))
            {
                // No cloud save exists
                result.success = true;
                result.resolvedSave = localSave;
                result.resolution = CloudSyncResult.CloudConflictResolution.NoConflict;
                result.cloudMetadata = null;

                CacheLocalMetadata(fileName, result.localMetadata);
                onComplete?.Invoke(result);
                return;
            }

            // Cloud save exists - compare metadata
            result.cloudMetadata = cloudMetadata;

            SaveFileMetadata localMetadata = result.localMetadata ?? new SaveFileMetadata(0, 0);

            // Smart conflict resolution: saveCount takes priority.
            // cloudMetadata can be null if the handler returned success with no metadata.
            if (cloudMetadata != null && cloudMetadata.IsNewerThan(localMetadata))
            {
                // Cloud is newer - load cloud version
                try
                {
                    SaveFile cloudSave = SaveJson.FromJson<SaveFile>(cloudJson);
                    if (cloudSave != null)
                    {
                        cloudSave.Init(); // populate containerList from deserialized containers array
                        result.success = true;
                        result.resolvedSave = cloudSave;
                        result.resolution = CloudSyncResult.CloudConflictResolution.CloudPreferred;
                        CacheLocalMetadata(fileName, cloudMetadata);

                        LogManager.Log($"[Cloud Save]: Cloud save '{fileName}' is newer. " +
                            $"Local: {localMetadata}, Cloud: {cloudMetadata}", LogCategory.Systems);
                    }
                    else
                    {
                        throw new Exception("Deserialization returned null");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Cloud Save]: Failed to parse cloud save for '{fileName}': {ex.Message}");
                    result.success = false;
                    result.resolution = CloudSyncResult.CloudConflictResolution.SyncFailed;
                }
            }
            else
            {
                // Local is newer or same - keep local
                result.success = true;
                result.resolvedSave = localSave;
                result.resolution = localMetadata.saveCount > 0 || localMetadata.timestampUnix > 0
                    ? CloudSyncResult.CloudConflictResolution.LocalPreferred
                    : CloudSyncResult.CloudConflictResolution.NoConflict;

                if (result.resolution == CloudSyncResult.CloudConflictResolution.LocalPreferred)
                {
                    LogManager.Log($"[Cloud Save]: Local save '{fileName}' is newer. " +
                        $"Local: {localMetadata}, Cloud: {cloudMetadata}", LogCategory.Systems);
                }
            }

            onComplete?.Invoke(result);
        }

        /// <summary>Creates a <see cref="SaveFileMetadata"/> snapshot from the given save file's version fields.</summary>
        private SaveFileMetadata ExtractMetadata(SaveFile saveFile)
        {
            if (saveFile == null)
                return new SaveFileMetadata(0, 0);

            return new SaveFileMetadata(saveFile.LastSavedUnix, saveFile.SaveCount);
        }

        /// <summary>
        /// Delete save from cloud storage.
        /// </summary>
        public void DeleteCloud(string fileName, Action<bool> onComplete)
        {
            if (!IsConfigured)
            {
                onComplete?.Invoke(false);
                return;
            }

            handler.Delete(fileName, success =>
            {
                if (success)
                {
                    localMetadataCache.Remove(fileName);
                    LogManager.Log($"[Cloud Save]: Deleted '{fileName}' from cloud", LogCategory.Systems);
                }
                else
                {
                    LogManager.LogWarning($"[Cloud Save]: Failed to delete '{fileName}' from cloud", LogCategory.Systems);
                }

                onComplete?.Invoke(success);
            });
        }
    }
}
