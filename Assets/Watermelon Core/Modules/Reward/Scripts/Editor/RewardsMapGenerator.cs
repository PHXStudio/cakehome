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
    /// <summary>
    /// Editor-only code generator that produces <c>MD_GeneratedRewardsMap.cs</c>.
    /// On every domain reload it scans all player-assembly types decorated with
    /// <see cref="RegisterRewardAttribute"/>, writes the registration file if the content changed,
    /// and immediately populates <see cref="RewardsMap"/> for the editor domain
    /// (since <c>[RuntimeInitializeOnLoadMethod]</c> only executes at runtime).
    /// Trigger manually via <b>Tools &gt; Rewards &gt; Regenerate Map</b>.
    /// </summary>
    [InitializeOnLoad]
    public static class RewardsMapGenerator
    {
        static RewardsMapGenerator()
        {
            EditorApplication.delayCall += Generate;

            // RuntimeInitializeOnLoadMethod runs only at runtime, so populate the map
            // here for the editor domain (custom editors, drawers, validation).
            foreach ((Type reward, Type view) in BuildEntries())
                RewardsMap.Register(reward, view);
        }

        public static void Generate()
        {
            List<(Type reward, Type view)> entries = BuildEntries();

            if (AreMapsEqual(entries))
                return;

            string nl = Environment.NewLine;
            string registerLines = string.Join(nl, entries.Select(e =>
                $"            RewardsMap.Register(typeof({CSharpName(e.reward)}), typeof({CSharpName(e.view)}));"));

            string code = $@"// Auto-generated file. Do not edit.
// Regenerate via: Tools/Rewards/Regenerate Map
using UnityEngine;

namespace Watermelon
{{
    static class GeneratedRewardsMap
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
            File.WriteAllText(path, code);
            AssetDatabase.Refresh();

            Debug.Log($"[RewardsMapGenerator] Generated {entries.Count} reward mappings.");
        }

        private static List<(Type reward, Type view)> BuildEntries()
        {
            HashSet<string> playerAssemblies = new HashSet<string>(
                CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                    .Select(a => a.name));

            TypeCache.TypeCollection rewardTypes = TypeCache.GetTypesDerivedFrom<Reward>();

            return rewardTypes
                .Where(t => !t.IsAbstract
                         && !t.IsGenericTypeDefinition
                         && playerAssemblies.Contains(t.Assembly.GetName().Name))
                .Select(t =>
                {
                    RegisterRewardAttribute attr = t.GetCustomAttribute<RegisterRewardAttribute>();
                    if (attr == null || attr.ViewType == null) return (null, null);
                    return (reward: t, view: attr.ViewType);
                })
                .Where(x => x.reward != null && x.view != null)
                .OrderBy(e => e.reward.FullName)
                .ToList();
        }

        /// <summary>
        /// Compares discovered entries against the currently generated file content to skip unnecessary writes.
        /// </summary>
        private static bool AreMapsEqual(List<(Type reward, Type view)> entries)
        {
            string path = GetTargetPath();
            if (!File.Exists(path)) return false;

            string current = File.ReadAllText(path);
            foreach ((Type reward, Type view) in entries)
            {
                string expected = $"RewardsMap.Register(typeof({CSharpName(reward)}), typeof({CSharpName(view)}));";
                if (!current.Contains(expected)) return false;
            }

            int registrationCount = current.Split(new[] { "RewardsMap.Register(" }, StringSplitOptions.None).Length - 1;
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
            MonoScript script = EditorUtils.GetAsset<MonoScript>("MD_GeneratedRewardsMap");
            if (script != null)
                return AssetDatabase.GetAssetPath(script);

            return Path.Combine("Assets", "Project Files", "Modules", "Reward", "MD_GeneratedRewardsMap.cs");
        }
    }
}
