using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    public static class AsmdefPatcher
    {
        // Matches "references": [...] — strings only, no nested brackets
        private static readonly Regex ReferencesRegex =
            new Regex(@"""references""\s*:\s*\[([^\[]*?)\]", RegexOptions.Singleline);

        private static readonly Regex StringEntryRegex =
            new Regex(@"""([^""]+)""");

        public static void Patch(string asmdefGuid, string referenceGuid, bool add)
        {
            if (string.IsNullOrEmpty(asmdefGuid) || string.IsNullOrEmpty(referenceGuid))
                return;

            string path = AssetDatabase.GUIDToAssetPath(asmdefGuid);
            if (string.IsNullOrEmpty(path))
            {
                LogManager.LogWarning($"[AsmdefPatcher]: Cannot resolve path for GUID {asmdefGuid}", LogCategory.Systems);
                return;
            }

            PatchPath(path, referenceGuid, add);
        }

        private static void PatchPath(string asmdefPath, string guid, bool add)
        {
            if (!File.Exists(asmdefPath))
            {
                LogManager.LogWarning($"[AsmdefPatcher]: File not found: {asmdefPath}", LogCategory.Systems);
                return;
            }

            string json = File.ReadAllText(asmdefPath);
            string entry = $"GUID:{guid}";

            Match match = ReferencesRegex.Match(json);
            if (!match.Success)
            {
                LogManager.LogWarning($"[AsmdefPatcher]: No 'references' field in {asmdefPath}", LogCategory.Systems);
                return;
            }

            List<string> refs = StringEntryRegex.Matches(match.Groups[1].Value)
                .Cast<Match>()
                .Select(m => m.Groups[1].Value)
                .ToList();

            bool changed = false;
            if (add && !refs.Contains(entry))
            {
                refs.Add(entry);
                changed = true;
            }
            else if (!add && refs.Remove(entry))
            {
                changed = true;
            }

            if (!changed) return;

            string newBlock = refs.Count == 0
                ? "\"references\": []"
                : "\"references\": [\n" + string.Join(",\n", refs.Select(r => $"        \"{r}\"")) + "\n    ]";

            string newJson = json.Substring(0, match.Index) + newBlock + json.Substring(match.Index + match.Length);

            File.WriteAllText(asmdefPath, newJson);
            AssetDatabase.ImportAsset(asmdefPath);

            LogManager.Log($"[AsmdefPatcher]: {(add ? "Linked" : "Unlinked")} GUID:{guid} {(add ? "in" : "from")} {System.IO.Path.GetFileName(asmdefPath)}", LogCategory.Systems);
        }
    }
}
