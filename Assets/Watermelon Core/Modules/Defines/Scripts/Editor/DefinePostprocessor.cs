using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    public class DefinePostprocessor : AssetPostprocessor
    {
        // SessionState, not EditorPrefs: this flags "a .cs/.dll changed in this postprocess batch"
        // for the deferred check a few lines down. EditorPrefs is a machine-wide registry key shared
        // by every Unity project the user has open — with multiple projects built off this same
        // template running side by side, one project's import could flip the flag and another
        // project's unrelated postprocess pass could consume/clear it first, silently dropping the
        // original CheckAutoDefines. SessionState is scoped to this Editor process/project only.
        private const string PREFS_KEY = "DefinesCheck";

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AssemblyReload()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || string.IsNullOrEmpty(CoreEditor.FOLDER_CORE))
            {
                EditorApplication.delayCall += AssemblyReload;
                return;
            }

            EditorApplication.delayCall += () =>
            {
                DefineManager.RebuildCache();
                DefineManager.CheckAutoDefines();
            };
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            HandleDeletedModuleDefines(deletedAssets);

            ValidateRequirement(importedAssets, deletedAssets);

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || string.IsNullOrEmpty(CoreEditor.FOLDER_CORE))
            {
                EditorApplication.delayCall += () => OnPostprocessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths, didDomainReload);
                return;
            }

            if (SessionState.GetBool(PREFS_KEY, false))
            {
                DefineManager.CheckAutoDefines(deletedAssets);
                SessionState.SetBool(PREFS_KEY, false);
            }
        }

        // When a ModuleDefine asset is deleted, proactively remove its asmdef from any
        // module that declared it as an optionalDependency — before Unity recompiles.
        private static void HandleDeletedModuleDefines(string[] deletedAssets)
        {
            if (deletedAssets.IsNullOrEmpty()) return;

            ModuleDefineCache cache = ModuleDefineCache.Load();
            bool cacheChanged = false;

            foreach (string path in deletedAssets)
            {
                if (!path.EndsWith(".asset")) continue;

                ModuleDefineCache.Entry deletedEntry = cache.FindByPath(path);
                if (deletedEntry == null) continue;

                // Find all modules that reference the deleted module as an optional dependency
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (ModuleDefineCache.Entry entry in cache.Entries)
                    {
                        if (entry == deletedEntry) continue;
                        if (!entry.optionalDependencies.Contains(deletedEntry.define)) continue;

                        AsmdefPatcher.Patch(entry.moduleAsmdefGuid, deletedEntry.moduleAsmdefGuid, false);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                DefineManager.DisableDefineForAllPlatforms(deletedEntry.define);
                cache.Remove(deletedEntry);
                cacheChanged = true;

                Debug.Log($"[Define Manager]: Module '{deletedEntry.define}' removed. Unlinked from dependents.");
            }

            if (cacheChanged)
                cache.Save();
        }

        private static void ValidateRequirement(string[] importedAssets, string[] deletedAssets)
        {
            if (!importedAssets.IsNullOrEmpty())
            {
                foreach (string str in importedAssets)
                {
                    if (str.EndsWith(".cs") || str.EndsWith(".dll"))
                    {
                        SessionState.SetBool(PREFS_KEY, true);
                        return;
                    }
                }
            }

            if (!deletedAssets.IsNullOrEmpty())
            {
                foreach (string str in deletedAssets)
                {
                    if (str.EndsWith(".cs") || str.EndsWith(".dll"))
                    {
                        SessionState.SetBool(PREFS_KEY, true);
                        return;
                    }
                }
            }
        }
    }
}
