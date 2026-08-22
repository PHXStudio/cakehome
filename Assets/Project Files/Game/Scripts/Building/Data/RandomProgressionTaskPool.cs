using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class RandomProgressionTaskPool
    {
        [SerializeField] WeightedProgressionTaskEntry[] tasks;
        [SerializeField] CharacterData[]                characters;
        // 0 = disabled. Otherwise random spawn stops once coins >= this * cheapest available
        // building upgrade cost (BuildingController.GetMinAvailableUpgradeCost).
        [SerializeField] float                          stopSpawnCoinMultiplier;
        // How many random tasks are kept in the pool at once. Once the whole batch is
        // completed, this many new random tasks are spawned together.
        [SerializeField] int                            maxSimultaneousRandomTasks = 1;

        public bool  HasTasks                    => !tasks.IsNullOrEmpty();
        public float StopSpawnCoinMultiplier      => stopSpawnCoinMultiplier;
        public int   MaxSimultaneousRandomTasks   => Mathf.Max(1, maxSimultaneousRandomTasks);

        // Not serialized into the asset — just tracks the last roll for this play session so the
        // same order can't spawn twice in a row (including back-to-back picks within one batch).
        [System.NonSerialized] ClientOrderTaskDefinition lastPickedTask;

        // Cumulative-weight roll — same algorithm as WeightedList<T>.GetRandomItemWithChance,
        // with the last picked task excluded (when the pool has another option) to avoid repeats.
        public ClientOrderTaskDefinition PickTask()
        {
            if (tasks.IsNullOrEmpty()) return null;

            bool excludeLast = tasks.Length > 1;

            float totalWeight = 0f;
            foreach (WeightedProgressionTaskEntry entry in tasks)
            {
                if (excludeLast && entry.Task == lastPickedTask) continue;
                totalWeight += entry.Weight;
            }

            // Every non-repeat candidate has zero weight — repeat is unavoidable, fall back to full roll.
            if (totalWeight <= 0f)
            {
                excludeLast = false;

                foreach (WeightedProgressionTaskEntry entry in tasks)
                    totalWeight += entry.Weight;

                if (totalWeight <= 0f) return tasks[0].Task;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (WeightedProgressionTaskEntry entry in tasks)
            {
                if (excludeLast && entry.Task == lastPickedTask) continue;

                currentWeight += entry.Weight;
                if (currentWeight >= randomValue)
                {
                    lastPickedTask = entry.Task;
                    return entry.Task;
                }
            }

            lastPickedTask = tasks[tasks.Length - 1].Task;
            return lastPickedTask;
        }

        // Avoids characters in `exclude` (e.g. already used by other currently-active random
        // tasks) as long as the pool has an alternative — falls back to a full roll otherwise.
        public CharacterData PickCharacter(ICollection<CharacterData> exclude = null)
        {
            if (characters.IsNullOrEmpty()) return null;

            CharacterData[] candidates = (exclude == null || exclude.Count == 0)
                ? characters
                : characters.Where(c => !exclude.Contains(c)).ToArray();

            if (candidates.IsNullOrEmpty()) candidates = characters;

            return candidates[Random.Range(0, candidates.Length)];
        }
    }
}
