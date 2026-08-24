using System;
using System.IO;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// File system save wrapper for Windows, macOS, Android, and iOS.
    /// Uses atomic write (write to <c>.tmp</c>, then <c>File.Replace</c>) to prevent save corruption on crash.
    /// </summary>
    public sealed class DefaultSaveWrapper : ISaveWrapper
    {
        // Cached on construction (main thread) so background threads can safely use it.
        private string persistentDataPath;

        /// <summary>Caches <see cref="Application.persistentDataPath"/> so background threads can access it safely.</summary>
        public void Init()
        {
            persistentDataPath = Application.persistentDataPath;
        }

        private string JsonPath(string fileName) => Path.Combine(persistentDataPath, fileName + ".save");

        // Pre-".save" save files used this extension. Kept only so installs that updated from an older
        // build can still load their existing data; new saves are never written here.
        private string LegacyJsonPath(string fileName) => Path.Combine(persistentDataPath, fileName + ".json");

        /// <summary>Loads the save file from <c>persistentDataPath/{fileName}.save</c>; auto-recovers from a <c>.tmp</c> file if the primary is missing, and falls back to a legacy <c>.json</c> save on first load after an update.</summary>
        public SaveFile Load(string fileName)
        {
            string jsonPath = JsonPath(fileName);
            string tmpPath  = jsonPath + ".tmp";

            // Recovery: if .save is missing but .tmp exists, a previous save was interrupted
            // between WriteAllText and File.Replace — recover from the temp file.
            if (!File.Exists(jsonPath) && File.Exists(tmpPath))
            {
                try { File.Move(tmpPath, jsonPath); }
                catch (Exception ex) { LogManager.LogWarning($"[Save]: Could not recover '{fileName}' from .tmp: {ex.Message}", LogCategory.Systems); }
            }

            if (!File.Exists(jsonPath))
            {
                SaveFile migrated = LoadLegacyJson(fileName);
                if (migrated != null)
                    return migrated;
            }

            if (File.Exists(jsonPath))
            {
                try
                {
                    SaveFile loaded = SaveJson.FromJson<SaveFile>(File.ReadAllText(jsonPath));
                    if (loaded != null)
                    {
                        loaded.Init();
                        return loaded;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Save]: Failed to read save '{fileName}': {ex.Message}");
                }
            }

            SaveFile empty = new();
            empty.Init();
            return empty;
        }

        /// <summary>Reads a pre-update <c>.json</c> save (recovering its own <c>.tmp</c> if needed) and deletes it once migrated; returns null if no legacy save exists.</summary>
        private SaveFile LoadLegacyJson(string fileName)
        {
            string legacyPath    = LegacyJsonPath(fileName);
            string legacyTmpPath = legacyPath + ".tmp";

            if (!File.Exists(legacyPath) && File.Exists(legacyTmpPath))
            {
                try { File.Move(legacyTmpPath, legacyPath); }
                catch (Exception ex) { LogManager.LogWarning($"[Save]: Could not recover legacy '{fileName}' from .tmp: {ex.Message}", LogCategory.Systems); }
            }

            if (!File.Exists(legacyPath))
                return null;

            try
            {
                SaveFile loaded = SaveJson.FromJson<SaveFile>(File.ReadAllText(legacyPath));
                if (loaded != null)
                {
                    loaded.Init();
                    try { File.Delete(legacyPath); } catch (Exception ex) { LogManager.LogWarning($"[Save]: Could not remove migrated legacy save '{fileName}': {ex.Message}", LogCategory.Systems); }
                    return loaded;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save]: Failed to read legacy save '{fileName}': {ex.Message}");
            }

            return null;
        }

        /// <summary>Writes JSON to a <c>.tmp</c> file then atomically replaces the final file using <c>File.Replace</c>.</summary>
        public void SaveRaw(string fileName, string json)
        {
            string finalPath = JsonPath(fileName);
            string tmpPath   = finalPath + ".tmp";

            try
            {
                File.WriteAllText(tmpPath, json);

                // File.Replace atomically replaces finalPath with tmpPath on Windows (ReplaceFile API).
                // Fall back to Move on first save when finalPath doesn't exist yet.
                if (File.Exists(finalPath))
                    File.Replace(tmpPath, finalPath, destinationBackupFileName: null);
                else
                    File.Move(tmpPath, finalPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save]: Failed to write save '{fileName}': {ex.Message}");
            }
        }

        /// <summary>Deletes the <c>.save</c>/<c>.tmp</c> files for the given save file name, plus any leftover pre-update <c>.json</c>/<c>.tmp</c> files.</summary>
        public void Delete(string fileName)
        {
            string jsonPath = JsonPath(fileName);
            string tmpPath  = jsonPath + ".tmp";

            if (File.Exists(jsonPath)) File.Delete(jsonPath);
            if (File.Exists(tmpPath))  File.Delete(tmpPath);

            string legacyPath    = LegacyJsonPath(fileName);
            string legacyTmpPath = legacyPath + ".tmp";

            if (File.Exists(legacyPath))    File.Delete(legacyPath);
            if (File.Exists(legacyTmpPath)) File.Delete(legacyTmpPath);
        }

        /// <summary>Returns <c>true</c>; file I/O can be safely offloaded to a <see cref="System.Threading.ThreadPool"/> thread.</summary>
        public bool UseThreads() => true;

        public void Configure(SaveWrapperConfig config)
        {
            // DefaultSaveWrapper doesn't require any configuration
        }
    }
}
