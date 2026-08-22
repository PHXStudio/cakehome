using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Serializable container for all save objects belonging to one save file.
    /// Holds a flat array of <see cref="SaveFileContainer"/> entries (serialized) and a dictionary for fast runtime access.
    /// </summary>
    [Serializable]
    public class SaveFile
    {
        [SerializeField] SaveFileContainer[] containers;
        [SerializeField] long lastSavedUnix;
        [SerializeField] int saveCount = 0;  // Version number for cloud sync - increments on each flush

        private List<SaveFileContainer> containerList;
        private Dictionary<string, SaveFileContainer> containerByKey;
        private bool isDirty;

        /// <summary>True when in-memory state has changed since the last flush to disk.</summary>
        public bool IsDirty => isDirty;

        /// <summary>Last save timestamp as local <see cref="DateTime"/>; <see cref="DateTime.MinValue"/> if never saved.</summary>
        public DateTime LastSavedAt => lastSavedUnix > 0 ? DateTimeOffset.FromUnixTimeSeconds(lastSavedUnix).LocalDateTime : DateTime.MinValue;

        /// <summary>
        /// Last saved timestamp in Unix seconds. Used for cloud sync metadata.
        /// </summary>
        public long LastSavedUnix => lastSavedUnix;

        /// <summary>
        /// Save count / version number. Increments on each flush when updateLastSaved is true.
        /// Used for smart conflict resolution with cloud saves.
        /// </summary>
        public int SaveCount => saveCount;

        /// <summary>Populates runtime structures (list and dictionary) from the deserialized containers array. Must be called after deserialization.</summary>
        public void Init()
        {
            containerList = containers != null
                ? new List<SaveFileContainer>(containers)
                : new List<SaveFileContainer>();

            containerByKey = new Dictionary<string, SaveFileContainer>(containerList.Count);
            foreach (SaveFileContainer c in containerList)
                containerByKey[c.Key] = c;

            isDirty = false;
        }

        /// <summary>Serializes all containers to JSON and optionally updates <see cref="LastSavedUnix"/> and <see cref="SaveCount"/>. Clears the dirty flag.</summary>
        public void Flush(bool updateLastSaved)
        {
            containers = containerList.ToArray();

            foreach (SaveFileContainer container in containerList)
                container.Flush();

            if (updateLastSaved)
            {
                lastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                saveCount++;  // Increment version number on each save
            }

            isDirty = false;
        }

        /// <summary>
        /// Returns a save object for type T.
        /// Key is resolved via [SaveKey] attribute if present, otherwise typeof(T).FullName.
        /// Prefer [SaveKey("stable_key")] on your class to avoid data loss on rename/refactor.
        /// </summary>
        public T GetSaveObject<T>() where T : ISaveObject, new()
        {
            if (!SaveKeyMap.TryGet(typeof(T), out string key))
            {
                // Type not registered — map needs regeneration (Tools/Save/Regenerate Key Map).
                // Fall back to runtime attribute lookup so the game still works correctly.
                SaveKeyAttribute attr = typeof(T).GetCustomAttribute<SaveKeyAttribute>();
                key = attr != null ? attr.Key : typeof(T).FullName;
            }

            return GetSaveObject<T>(key);
        }

        /// <summary>Returns the save object stored under the given key, creating and registering a new instance if none exists.</summary>
        public T GetSaveObject<T>(string key) where T : ISaveObject, new()
        {
            if (!containerByKey.TryGetValue(key, out SaveFileContainer container))
            {
                container = new SaveFileContainer(key, new T());
                containerList.Add(container);
                containerByKey[key] = container;
            }
            else if (!container.Restored)
            {
                container.Restore<T>();
            }

            return (T)container.SaveObject;
        }

        /// <summary>Removes the container with the given key from this save file and marks it dirty.</summary>
        public void RemoveContainer(string key)
        {
            containerList.RemoveAll(c => c.Key == key);
            containerByKey.Remove(key);
            isDirty = true;
        }

        /// <summary>Removes all containers from this save file and marks it dirty.</summary>
        public void Clear()
        {
            containerList.Clear();
            containerByKey.Clear();
            isDirty = true;
        }

        /// <summary>Marks this file as dirty so it is included in the next <see cref="SaveManager.Save"/> call.</summary>
        public void MarkAsDirty() => isDirty = true;

        /// <summary>Logs last saved time and all container keys to the Unity console.</summary>
        public void Info()
        {
            LogManager.Log($"Last Saved: {LastSavedAt}", LogCategory.Systems);
            foreach (SaveFileContainer container in containerList)
                LogManager.Log($"Key: {container.Key} | SaveObject: {container.SaveObject}", LogCategory.Systems);
        }
    }
}
