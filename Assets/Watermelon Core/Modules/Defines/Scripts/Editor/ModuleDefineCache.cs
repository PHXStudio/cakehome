using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Watermelon
{
    public class ModuleDefineCache
    {
        private const string CACHE_PATH = "Library/WatermelonModules.json";

        public List<Entry> Entries { get; private set; } = new List<Entry>();

        [Serializable]
        public class Entry
        {
            public string define;
            public string detectionType;
            public string filePath;
            public string moduleAsmdefGuid;
            public string settingsAssetPath;
            public List<string> optionalDependencies = new List<string>();
        }

        [Serializable]
        private class Wrapper { public List<Entry> entries = new List<Entry>(); }

        public static ModuleDefineCache Load()
        {
            var cache = new ModuleDefineCache();

            if (!File.Exists(CACHE_PATH))
                return cache;

            try
            {
                Wrapper w = JsonUtility.FromJson<Wrapper>(File.ReadAllText(CACHE_PATH));
                if (w?.entries != null)
                    cache.Entries = w.entries;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ModuleDefineCache]: Failed to read cache, rebuilding. {e.Message}");
            }

            return cache;
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(CACHE_PATH, JsonUtility.ToJson(new Wrapper { entries = Entries }, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[ModuleDefineCache]: Failed to save. {e.Message}");
            }
        }

        public void Rebuild(ModuleDefineSettings[] allSettings)
        {
            Entries.Clear();

            foreach (ModuleDefineSettings settings in allSettings)
            {
                if (settings == null || string.IsNullOrEmpty(settings.Define))
                    continue;

                var entry = new Entry
                {
                    define = settings.Define,
                    detectionType = settings.DetectionType,
                    filePath = settings.FilePath,
                    settingsAssetPath = AssetDatabase.GetAssetPath(settings),
                    optionalDependencies = new List<string>()
                };

                if (settings.ModuleAsmdef != null)
                    entry.moduleAsmdefGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(settings.ModuleAsmdef));
                else if (settings.AutoLinkAsmdef && !string.IsNullOrEmpty(settings.AsmdefName))
                    entry.moduleAsmdefGuid = FindAsmdefGuidByName(settings.AsmdefName);

                if (!settings.OptionalDependencies.IsNullOrEmpty())
                {
                    foreach (string dep in settings.OptionalDependencies)
                    {
                        if (!string.IsNullOrEmpty(dep))
                            entry.optionalDependencies.Add(dep);
                    }
                }

                Entries.Add(entry);
            }
        }

        // Resolves an asmdef by exact filename match, ignoring assets that don't currently exist
        // (e.g. a package that isn't installed) — returns null instead of a broken reference.
        private static string FindAsmdefGuidByName(string asmdefName)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{asmdefName} t:AssemblyDefinitionAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), asmdefName, StringComparison.Ordinal))
                    return guid;
            }

            return null;
        }

        public Entry FindByPath(string assetPath) =>
            Entries.Find(e => string.Equals(e.settingsAssetPath, assetPath, StringComparison.OrdinalIgnoreCase));

        public Entry FindByDefine(string define) =>
            Entries.Find(e => string.Equals(e.define, define, StringComparison.Ordinal));

        public void Remove(Entry entry) => Entries.Remove(entry);
    }
}
