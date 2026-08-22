using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Watermelon
{
    [InitializeOnLoad]
    public static class SaveKeyMapGenerator
    {
        static SaveKeyMapGenerator()
        {
            EditorApplication.delayCall += Generate;
        }

        public static void Generate()
        {
            List<(Type type, string key)> entries = BuildEntries();

            if (AreMapsEqual(entries))
                return;

            // Detect duplicate keys — two types sharing the same key will silently overwrite each other.
            var seen = new Dictionary<string, Type>();
            foreach ((Type type, string key) in entries)
            {
                if (seen.TryGetValue(key, out Type existing))
                    Debug.LogWarning($"[SaveKeyMapGenerator]: Duplicate save key '{key}' used by {type.FullName} and {existing.FullName}. Add [SaveKey(\"unique_key\")] to one of them.");
                else
                    seen[key] = type;
            }

            string nl = Environment.NewLine;
            string registerLines = string.Join(nl, entries.Select(e =>
                $"            SaveKeyMap.Register(typeof({CSharpName(e.type)}), \"{e.key}\");"));

            string code = $@"// Auto-generated file. Do not edit.
// Regenerate via: Tools/Save/Regenerate Key Map
using UnityEngine;

namespace Watermelon
{{
    static class GeneratedSaveKeyRegistration
    {{
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {{
{registerLines}
        }}
    }}
}}";
            code = NormalizeEol(code, nl);

            string path = GetTargetPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, code);
            AssetDatabase.Refresh();

            Debug.Log($"[SaveKeyMapGenerator]: Generated {entries.Count} save key registrations.");
        }

        private static List<(Type type, string key)> BuildEntries()
        {
            // Only include types from assemblies that go into the player build.
            // PlayerWithoutTestAssemblies excludes test assemblies that TypeCache would otherwise include.
            HashSet<string> playerAssemblies = new HashSet<string>(
                CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                    .Select(a => a.name));

            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<ISaveObject>();

            return types
                .Where(t => !t.IsAbstract
                         && !t.IsGenericTypeDefinition
                         && playerAssemblies.Contains(t.Assembly.GetName().Name))
                .Select(t =>
                {
                    SaveKeyAttribute attr = t.GetCustomAttribute<SaveKeyAttribute>();
                    string key = attr != null ? attr.Key : t.FullName;
                    return (type: t, key);
                })
                .OrderBy(e => e.key)
                .ToList();
        }

        /// <summary>
        /// Compares discovered entries against the currently registered map to skip unnecessary writes.
        /// </summary>
        private static bool AreMapsEqual(List<(Type type, string key)> entries)
        {
            // Read the current generated file and compare line count as a quick heuristic,
            // then verify each type/key pair is already registered correctly.
            // Since the map is only readable at runtime (RuntimeInitializeOnLoadMethod), we
            // compare against the generated file content instead.
            string path = GetTargetPath();
            if (!File.Exists(path)) return false;

            string current = File.ReadAllText(path);
            foreach ((Type type, string key) in entries)
            {
                string expected = $"SaveKeyMap.Register(typeof({CSharpName(type)}), \"{key}\");";
                if (!current.Contains(expected)) return false;
            }

            // Also check count — if entries were removed the file would still pass the above check.
            int registrationCount = current.Split(new[] { "SaveKeyMap.Register(" }, StringSplitOptions.None).Length - 1;
            return registrationCount == entries.Count;
        }

        /// <summary>
        /// Converts a Type to a valid C# fully-qualified name for use in typeof().
        /// Type.FullName uses '+' for nested types; C# syntax requires '.'.
        /// </summary>
        private static string CSharpName(Type t)
        {
            return t.FullName?.Replace('+', '.') ?? t.Name;
        }

        private static string NormalizeEol(string s, string newline)
        {
            s = s.Replace("\r\n", "\n").Replace("\r", "\n");
            return s.Replace("\n", newline);
        }

        private static string GetTargetPath()
        {
            MonoScript script = EditorUtils.GetAsset<MonoScript>("MD_GeneratedSaveKeyRegistration");
            if (script != null)
                return AssetDatabase.GetAssetPath(script);

            return Path.Combine("Assets", "Project Files", "Modules", "Save", "MD_GeneratedSaveKeyRegistration.cs");
        }
    }
}
