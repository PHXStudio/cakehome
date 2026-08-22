using UnityEditor;
using System.Collections.Generic;
using System.Text;

#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build;
#endif

namespace Watermelon
{
    // Batches define add/remove operations and applies them across every synced BuildTargetGroup
    // (DefineManager.GetSyncedTargetGroups), not just the active one — so auto-detected module
    // defines stay correct on platforms that aren't currently selected.
    public class DefineString
    {
        private readonly Dictionary<BuildTargetGroup, string> baseDefineLines = new Dictionary<BuildTargetGroup, string>();
        private readonly Dictionary<BuildTargetGroup, List<string>> definesByGroup = new Dictionary<BuildTargetGroup, List<string>>();

        public DefineString()
        {
            foreach (BuildTargetGroup group in DefineManager.GetSyncedTargetGroups())
            {
                string line = GetSymbols(group);

                baseDefineLines[group] = line;
                definesByGroup[group] = new List<string>(line.Split(';'));
            }
        }

        // True only if the define is already present on every synced group.
        public bool HasDefine(string define)
        {
            if (definesByGroup.Count == 0)
                return false;

            foreach (List<string> defines in definesByGroup.Values)
            {
                if (defines.FindIndex(x => x == define) == -1)
                    return false;
            }

            return true;
        }

        public void RemoveDefine(string define)
        {
            foreach (List<string> defines in definesByGroup.Values)
            {
                int defineIndex = defines.FindIndex(x => x == define);
                if (defineIndex != -1)
                    defines.RemoveAt(defineIndex);
            }
        }

        public void AddDefine(string define)
        {
            foreach (List<string> defines in definesByGroup.Values)
            {
                if (defines.FindIndex(x => x == define) == -1)
                    defines.Add(define);
            }
        }

        public bool HasChanges()
        {
            foreach (BuildTargetGroup group in definesByGroup.Keys)
            {
                if (baseDefineLines[group] != BuildLine(definesByGroup[group]))
                    return true;
            }

            return false;
        }

        public void ApplyDefines()
        {
            foreach (BuildTargetGroup group in definesByGroup.Keys)
            {
                string newDefineLine = BuildLine(definesByGroup[group]);

                if (baseDefineLines[group] != newDefineLine)
                    SetSymbols(group, newDefineLine);
            }
        }

        private static string BuildLine(List<string> defines)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string define in defines)
            {
                if (string.IsNullOrEmpty(define))
                    continue;

                sb.Append(define);
                sb.Append(";");
            }

            return sb.ToString();
        }

        private static string GetSymbols(BuildTargetGroup group)
        {
#if UNITY_6000_0_OR_NEWER
            return PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif
        }

        private static void SetSymbols(BuildTargetGroup group, string line)
        {
#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), line);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, line);
#endif
        }
    }
}
