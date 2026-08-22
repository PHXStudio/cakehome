using System;
using System.Collections.Generic;

namespace Watermelon
{
    // LIFO stack of pending spawner rewards — the card always shows/spawns the most recently
    // added one, older entries surface as the newer ones are claimed.
    public class SpawnerRewardQueue
    {
        private readonly List<SpawnerQueueEntry> entries = new List<SpawnerQueueEntry>();

        public bool HasPending => entries.Count > 0;
        public int Count => entries.Count;
        public IReadOnlyList<SpawnerQueueEntry> Entries => entries;

        public event Action OnChanged; // fires on every push/pop

        public void Push(string typeId, int grade)
        {
            entries.Add(new SpawnerQueueEntry { TypeId = typeId, Grade = grade });
            OnChanged?.Invoke();
        }

        public SpawnerQueueEntry Peek() => entries.Count > 0 ? entries[entries.Count - 1] : null;

        public SpawnerQueueEntry Pop()
        {
            if (entries.Count == 0) return null;

            SpawnerQueueEntry entry = entries[entries.Count - 1];
            entries.RemoveAt(entries.Count - 1);
            OnChanged?.Invoke();
            return entry;
        }

        // Save restore — called before the UI subscribes, so no OnChanged.
        public void Restore(List<SpawnerQueueEntry> saved)
        {
            entries.Clear();
            if (saved != null) entries.AddRange(saved);
        }
    }
}
