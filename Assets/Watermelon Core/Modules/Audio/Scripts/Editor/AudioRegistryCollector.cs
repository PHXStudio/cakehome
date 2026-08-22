using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Watermelon
{
    [InitializeOnLoad]
    public class AudioRegistryCollector : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        static AudioRegistryCollector()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                CollectAll();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            CollectAll();
        }

        public static void CollectAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioRegistry");

            if (guids.Length == 0)
                return;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioRegistry registry = AssetDatabase.LoadAssetAtPath<AudioRegistry>(path);

                if (registry != null)
                    Collect(registry);
            }

            AssetDatabase.SaveAssets();
        }

        private static void Collect(AudioRegistry registry)
        {
            string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip");

            var entries = new List<AudioRegistryEntry>(clipGuids.Length);

            // clipName → first assetPath seen (for duplicate detection)
            var seen = new Dictionary<string, string>();

            foreach (string clipGuid in clipGuids)
            {
                string clipPath = AssetDatabase.GUIDToAssetPath(clipGuid);
                string clipName = Path.GetFileNameWithoutExtension(clipPath);

                if (ShouldIgnore(clipName, clipPath, registry.IgnorePatterns))
                    continue;

                if (seen.TryGetValue(clipName, out string existingPath))
                {
                    Debug.LogError(
                        $"[AudioRegistry]: Duplicate clip name '{clipName}' — skipping second occurrence.\n" +
                        $"  Kept:    {existingPath}\n" +
                        $"  Skipped: {clipPath}");
                    continue;
                }

                seen[clipName] = clipPath;

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                entries.Add(new AudioRegistryEntry(clipName, clipPath, clip));
            }

            registry.SetEntries(entries.ToArray());

            Debug.Log($"[AudioRegistry]: Collected {entries.Count} clips into '{registry.name}'.");
        }

        // ─── Pattern matching ──────────────────────────────────────────────

        private static bool ShouldIgnore(string clipName, string assetPath, IReadOnlyList<string> patterns)
        {
            foreach (string pattern in patterns)
            {
                if (string.IsNullOrEmpty(pattern)) continue;

                bool isPathPattern = pattern[0] == '/';
                string target = isPathPattern ? assetPath : clipName;
                string normalizedPattern = isPathPattern ? pattern.Substring(1) : pattern;

                if (WildcardMatch(target, normalizedPattern))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Case-insensitive wildcard match. '*' matches any sequence of characters,
        /// including path separators.
        /// </summary>
        private static bool WildcardMatch(string text, string pattern)
        {
            int ti = 0, pi = 0;
            int starPI = -1, starTI = 0;

            while (ti < text.Length)
            {
                if (pi < pattern.Length && pattern[pi] == '*')
                {
                    starPI = ++pi;
                    starTI = ti;
                }
                else if (pi < pattern.Length &&
                         char.ToLowerInvariant(text[ti]) == char.ToLowerInvariant(pattern[pi]))
                {
                    ti++;
                    pi++;
                }
                else if (starPI >= 0)
                {
                    pi = starPI;
                    ti = ++starTI;
                }
                else
                {
                    return false;
                }
            }

            while (pi < pattern.Length && pattern[pi] == '*')
                pi++;

            return pi == pattern.Length;
        }
    }
}
