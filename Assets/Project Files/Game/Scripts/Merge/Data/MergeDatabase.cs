using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Merge Database", menuName = "Game/Merge Database")]
    public class MergeDatabase : ScriptableObject
    {
        [SerializeField] MergeItemData[] items;
        [SerializeField] float rareSpawnChancePercent = 20f;

        [SerializeField] AudioClip appearAudioClip;
        [SerializeField] AudioClip collectAudioClip;

        private static MergeDatabase instance;
        private Dictionary<string, MergeItemData> lookup;

        public static MergeDatabase Instance => instance;

        // A draw counts as "rare" when its own odds within the pool it was drawn from are at or
        // below this threshold — spawner-independent, so a spawner whose pool happens to favor
        // high grades doesn't trigger the rare callout on every spawn.
        public static bool IsRareChance(float chancePercent) =>
            instance != null && chancePercent <= instance.rareSpawnChancePercent;

        public IReadOnlyList<MergeItemData> Items => items;

        public AudioClip AppearAudioClip  => appearAudioClip;
        public AudioClip CollectAudioClip => collectAudioClip;

        public void Init()
        {
            instance = this;
            lookup = new Dictionary<string, MergeItemData>(items.Length);
            foreach (MergeItemData item in items)
            {
                if (!string.IsNullOrEmpty(item.TypeId))
                    lookup[item.TypeId] = item;
            }
        }

        public MergeItemData GetItem(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
            {
                Debug.LogError("[MergeDatabase] GetItem: null returned, typeId is null or empty.");

                return null;
            }

            if (!lookup.TryGetValue(typeId, out MergeItemData data))
                Debug.LogError($"[MergeDatabase] GetItem: null returned, no item found with typeId: {typeId}");

            return data;
        }

        public bool HasItem(string typeId) => !string.IsNullOrEmpty(typeId) && lookup.ContainsKey(typeId);

#if UNITY_EDITOR
        // Duplicating an Inspector array element copies the [SerializeReference] id, so several
        // grades end up sharing one MergeGradeConfig instance and edits leak between them —
        // replace aliased configs with deep copies.
        private void OnValidate()
        {
            if (items == null) return;

            HashSet<MergeGradeConfig> seen = new HashSet<MergeGradeConfig>();
            bool changed = false;

            foreach (MergeItemData item in items)
            {
                MergeGradeData[] grades = item?.Grades;
                if (grades == null) continue;

                foreach (MergeGradeData grade in grades)
                {
                    MergeGradeConfig config = grade?.Config;
                    if (config == null) continue;

                    if (!seen.Add(config))
                    {
                        MergeGradeConfig clone = (MergeGradeConfig)JsonUtility.FromJson(
                            JsonUtility.ToJson(config), config.GetType());
                        grade.SetConfigEditor(clone);
                        changed = true;
                    }
                }
            }

            if (changed)
                UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
