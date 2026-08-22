using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Linq;

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build;
#endif

namespace Watermelon
{
    public static class DefineManager
    {
        // Single choke point for reading/writing the active build target's Scripting Define Symbols.
        // HasDefine/EnableDefine/DisableDefine and DefineManagerWindow both go through these instead
        // of each re-implementing the PlayerSettings get/split/join/set dance.
        public static string[] GetActiveDefines()
        {
#if UNITY_6000_0_OR_NEWER
            string definesLine = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)));
#else
            string definesLine = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
#endif
            return definesLine.Split(';').Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

        public static void SetActiveDefines(IEnumerable<string> defines)
        {
            string definesLine = string.Join(";", defines.Where(x => !string.IsNullOrEmpty(x)));

#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)), definesLine);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), definesLine);
#endif
        }

        public static bool HasDefine(string define)
        {
            return GetActiveDefines().Contains(define);
        }

        public static void EnableDefine(string define)
        {
            string[] defines = GetActiveDefines();
            if (defines.Contains(define))
                return;

            SetActiveDefines(defines.Append(define));
        }

        public static void DisableDefine(string define)
        {
            string[] defines = GetActiveDefines();
            if (!defines.Contains(define))
                return;

            SetActiveDefines(defines.Where(x => x != define));
        }

        public static void CheckAutoDefines(string[] deletedAssets = null)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || string.IsNullOrEmpty(CoreEditor.FOLDER_CORE))
            {
                EditorApplication.delayCall += () => { CheckAutoDefines(deletedAssets); };
                return;
            }

            bool CheckDeletedAssets(string filePath)
            {
                if (!string.IsNullOrEmpty(filePath) && !deletedAssets.IsNullOrEmpty())
                {
                    foreach (string deletedAsset in deletedAssets)
                    {
                        if (deletedAsset.EndsWith(filePath, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                return false;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<DefineState> markedDefines = new List<DefineState>();
            List<RegisteredDefine> registeredDefines = GetDynamicDefines();

            foreach (RegisteredDefine registeredDefine in registeredDefines)
            {
                if (CheckDeletedAssets(registeredDefine.FilePath))
                {
                    markedDefines.Add(new DefineState(registeredDefine.Define, false));
                    continue;
                }

                bool defineFound = false;
                foreach (Assembly assembly in assemblies)
                {
                    if (assembly.GetType(registeredDefine.AssemblyType, false) != null)
                    {
                        defineFound = true;
                        markedDefines.Add(new DefineState(registeredDefine.Define, true));
                        break;
                    }
                }

                if (!defineFound)
                    markedDefines.Add(new DefineState(registeredDefine.Define, false));
            }

            ChangeAutoDefinesState(markedDefines);
        }

        public static void ChangeAutoDefinesState(List<DefineState> defineStates)
        {
            if (EditorApplication.isCompiling || defineStates.IsNullOrEmpty())
                return;

            bool definesUpdated = false;

            StringBuilder sb = new StringBuilder();
            sb.Append("[Define Manager]: Dependencies change is detected. Updating Scripting Define Symbols..");
            sb.AppendLine();

            DefineString definesString = new DefineString();
            foreach (DefineState defineState in defineStates)
            {
                if (defineState.State)
                {
                    if (!definesString.HasDefine(defineState.Define))
                    {
                        definesUpdated = true;
                        definesString.AddDefine(defineState.Define);
                        sb.AppendLine();
                        sb.Append(defineState.Define);
                        sb.Append(" - added");
                    }
                }
                else
                {
                    if (definesString.HasDefine(defineState.Define))
                    {
                        definesUpdated = true;
                        definesString.RemoveDefine(defineState.Define);
                        sb.AppendLine();
                        sb.Append(defineState.Define);
                        sb.Append(" - removed");
                    }
                }
            }
            sb.AppendLine();

            if (definesUpdated)
                Debug.Log(sb.ToString());

            definesString.ApplyDefines();

            ReconcileOptionalDependencies(definesString);
        }

        // For each active module, ensures its optional dependency asmdefs are linked/unlinked
        // based on whether those dependency modules are also active.
        private static void ReconcileOptionalDependencies(DefineString definesString)
        {
            ModuleDefineCache cache = ModuleDefineCache.Load();
            if (cache.Entries.IsNullOrEmpty()) return;

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (ModuleDefineCache.Entry entry in cache.Entries)
                {
                    if (string.IsNullOrEmpty(entry.moduleAsmdefGuid) || entry.optionalDependencies.IsNullOrEmpty())
                        continue;

                    // Module is "active" for linking purposes if its asmdef file exists on disk,
                    // not whether its high-level define is set. The define may be off (e.g.
                    // MODULE_IAP without Unity IAP SDK) while the asmdef is still present and
                    // needs its optional references managed.
                    string asmdefPath = AssetDatabase.GUIDToAssetPath(entry.moduleAsmdefGuid);
                    bool moduleAsmdefExists = !string.IsNullOrEmpty(asmdefPath) && File.Exists(asmdefPath);

                    foreach (string depDefine in entry.optionalDependencies)
                    {
                        ModuleDefineCache.Entry depEntry = cache.FindByDefine(depDefine);
                        if (depEntry == null || string.IsNullOrEmpty(depEntry.moduleAsmdefGuid))
                            continue;

                        bool shouldLink = moduleAsmdefExists && definesString.HasDefine(depDefine);
                        AsmdefPatcher.Patch(entry.moduleAsmdefGuid, depEntry.moduleAsmdefGuid, shouldLink);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        // Same as DisableDefine, but clears the symbol on every synced BuildTargetGroup instead of
        // just the active one. Use this for auto-managed defines (module presence/absence) — that
        // state is platform-agnostic, so a define removed on PC must also be removed on Android/iOS/etc.,
        // otherwise it stays stale on any group that isn't the active one at the time of removal.
        public static void DisableDefineForAllPlatforms(string define)
        {
            DefineString definesString = new DefineString();
            definesString.RemoveDefine(define);
            definesString.ApplyDefines();
        }

        public static void EnableDefineForAllPlatforms(string define)
        {
            DefineString definesString = new DefineString();
            definesString.AddDefine(define);
            definesString.ApplyDefines();
        }

        // BuildTargetGroups whose Scripting Define Symbols get kept in sync by the auto-define
        // pipeline. Computed dynamically (not hardcoded) so new platforms added to the project don't
        // need this list touched: every enum value is probed once against the PlayerSettings API and
        // kept only if it doesn't throw (filters out Unknown and obsolete/removed groups). Cached for
        // the lifetime of the domain.
        private static BuildTargetGroup[] cachedSyncedGroups;

        public static BuildTargetGroup[] GetSyncedTargetGroups()
        {
            if (cachedSyncedGroups != null)
                return cachedSyncedGroups;

            List<BuildTargetGroup> groups = new List<BuildTargetGroup>();
            foreach (BuildTargetGroup group in ((BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup))).Distinct())
            {
                if (group == BuildTargetGroup.Unknown)
                    continue;

                try
                {
#if UNITY_6000_0_OR_NEWER
                    PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));
#else
                    PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif
                    groups.Add(group);
                }
                catch (Exception)
                {
                    // Obsolete/unsupported group for this Editor version — skip.
                }
            }

            cachedSyncedGroups = groups.ToArray();
            return cachedSyncedGroups;
        }

        public static void RebuildCache()
        {
            ModuleDefineSettings[] allSettings = GetModuleDefineSettings();
            ModuleDefineCache cache = new ModuleDefineCache();
            cache.Rebuild(allSettings);
            cache.Save();
        }

        public static ModuleDefineSettings[] GetModuleDefineSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:ModuleDefineSettings");
            var result = new List<ModuleDefineSettings>();

            foreach (string guid in guids)
            {
                ModuleDefineSettings settings = AssetDatabase.LoadAssetAtPath<ModuleDefineSettings>(
                    AssetDatabase.GUIDToAssetPath(guid));

                if (settings != null)
                    result.Add(settings);
            }

            return result.ToArray();
        }

        // Every [DefineAttribute] found on any type across all loaded assemblies. Scans the whole
        // AppDomain once — callers that need the raw attribute list (GetDynamicDefines and
        // DefineManagerWindow, which filter it for opposite subsets: has/hasn't an AssemblyType)
        // should share one scan instead of each walking every assembly's types themselves.
        public static List<DefineAttribute> GetAllDefineAttributes()
        {
            List<DefineAttribute> result = new List<DefineAttribute>();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly == null) continue;
                try
                {
                    Type[] types = assembly.GetTypes().Where(m => m.IsDefined(typeof(DefineAttribute), true)).ToArray();
                    foreach (Type type in types)
                        result.AddRange((DefineAttribute[])Attribute.GetCustomAttributes(type, typeof(DefineAttribute)));
                }
                catch (ReflectionTypeLoadException e)
                {
                    Debug.LogException(e);
                }
            }

            return result;
        }

        // defineAttributes: pass an already-scanned list to skip the AppDomain scan (e.g. when the
        // caller already ran GetAllDefineAttributes() for its own purposes); null scans it here.
        public static List<RegisteredDefine> GetDynamicDefines(List<DefineAttribute> defineAttributes = null)
        {
            List<RegisteredDefine> registeredDefines = new List<RegisteredDefine>();
            registeredDefines.AddRange(DefineSettings.STATIC_REGISTERED_DEFINES);

            foreach (DefineAttribute defineAttribute in defineAttributes ?? GetAllDefineAttributes())
            {
                if (!string.IsNullOrEmpty(defineAttribute.AssemblyType))
                {
                    if (registeredDefines.FindIndex(x => x.Define == defineAttribute.Define) == -1)
                        registeredDefines.Add(new RegisteredDefine(defineAttribute));
                }
            }

            foreach (ModuleDefineSettings settings in GetModuleDefineSettings())
            {
                if (string.IsNullOrEmpty(settings.DetectionType)) continue;
                if (registeredDefines.FindIndex(x => x.Define == settings.Define) == -1)
                    registeredDefines.Add(new RegisteredDefine(settings.Define, settings.DetectionType, settings.FilePath));
            }

            return registeredDefines;
        }
    }
}
