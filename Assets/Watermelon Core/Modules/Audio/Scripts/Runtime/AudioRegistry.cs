using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Audio Registry", menuName = "Data/Core/Audio Registry")]
    public class AudioRegistry : ScriptableObject
    {
        [SerializeField] private AudioRegistryEntry[] entries = new AudioRegistryEntry[0];

        [Tooltip(
            "Patterns to ignore during collection.\n" +
            "  MD_*          — clips whose name starts with MD_\n" +
            "  /Assets/Mus/* — all clips inside that folder path\n" +
            "  *_temp        — clips whose name ends with _temp\n" +
            "Starts with '/' → matched against asset path. Otherwise → matched against clip name.\n" +
            "'*' matches any sequence of characters.")]
        [SerializeField] private string[] ignorePatterns = new string[0];

        [NonSerialized] private Dictionary<string, AudioClip> lookup;

        public IReadOnlyList<string> IgnorePatterns => ignorePatterns;
        public IReadOnlyList<AudioRegistryEntry> Entries => entries;

        private void OnEnable()
        {
            BuildRuntimeLookup();
        }

        public void BuildRuntimeLookup()
        {
            lookup = new Dictionary<string, AudioClip>(entries != null ? entries.Length : 0);

            if (entries == null) return;

            foreach (AudioRegistryEntry entry in entries)
            {
                if (entry == null || entry.Clip == null) continue;
                lookup[entry.ClipName] = entry.Clip;
            }
        }

        public AudioClip GetClip(string name)
        {
            if (lookup == null)
                BuildRuntimeLookup();

            if (lookup.TryGetValue(name, out AudioClip clip))
                return clip;

            Debug.LogWarning($"[AudioRegistry]: Clip '{name}' not found in registry.");
            return null;
        }

#if UNITY_EDITOR
        public void SetEntries(AudioRegistryEntry[] newEntries)
        {
            entries = newEntries;
            BuildRuntimeLookup();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }

    [Serializable]
    public class AudioRegistryEntry
    {
        [SerializeField] private string clipName;
        [SerializeField] private string assetPath;
        [SerializeField] private AudioClip clip;

        public string ClipName => clipName;
        public string AssetPath => assetPath;
        public AudioClip Clip => clip;

        public AudioRegistryEntry(string clipName, string assetPath, AudioClip clip)
        {
            this.clipName = clipName;
            this.assetPath = assetPath;
            this.clip = clip;
        }
    }
}
