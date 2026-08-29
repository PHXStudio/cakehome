using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace Watermelon
{
    public class TaskController : MonoBehaviour
    {
        public static TaskController Instance { get; private set; }

        public static event Action OnTasksChanged;
        public static event Action OnAllTasksComplete;

        private static bool isProgressionLocked;
        private bool isEvaluatingProgression;

        private readonly List<ITask> activeTasks = new List<ITask>();
        private SpawnerRewardQueue spawnerQueue;
        private TaskSave save;
        private SpawnerQueueSave spawnerQueueSave;

        public SpawnerRewardQueue SpawnerQueue => spawnerQueue;
        public IReadOnlyList<ITask> ActiveTasks => activeTasks;

        public void Init()
        {
            Instance    = this;
            spawnerQueue = new SpawnerRewardQueue();

            spawnerQueueSave = SaveController.GetSaveObject<SpawnerQueueSave>("SpawnerQueue");
            spawnerQueue.Restore(spawnerQueueSave.Entries);
            spawnerQueue.OnChanged += SyncSpawnerQueueSave;

            MergeGrid.OnFieldChanged += NotifyFieldChanged;

            LoadZone(ZoneController.CurrentZone);
            EvaluateProgressionQueue();

            ZoneController.OnZoneChanged += HandleZoneChanged;
            BuildingController.OnUpgradeCompleted += HandleUpgradeCompleted;
        }

        private void OnDestroy()
        {
            spawnerQueue.OnChanged -= SyncSpawnerQueueSave;
            MergeGrid.OnFieldChanged -= NotifyFieldChanged;
            ZoneController.OnZoneChanged -= HandleZoneChanged;
            BuildingController.OnUpgradeCompleted -= HandleUpgradeCompleted;
            Instance = null;
        }

        // ─── Zone save/restore ───────────────────────────────────────────────────

        private void LoadZone(ZoneData zone)
        {
            save = SaveController.GetSaveObject<TaskSave>($"Tasks_{zone.ZoneId}");

            foreach (ClientOrderTaskSaveData data in save.ClientOrders.ToList())
            {
                CharacterData character = CharacterController.GetCharacter(data.CharacterId);
                List<OrderItem> items = data.Items.ConvertAll(i => new OrderItem { typeId = i.TypeId, grade = i.Grade });
                AddTask(new ClientOrderTask(data.TaskId, character, items, data.CoinsReward));
            }
        }

        public void SyncSaveData()
        {
            save.ClientOrders.Clear();

            foreach (ITask task in activeTasks)
            {
                if (task is ClientOrderTask order)
                {
                    save.ClientOrders.Add(new ClientOrderTaskSaveData
                    {
                        TaskId      = order.TaskId,
                        CharacterId = order.Character != null ? order.Character.CharacterId : null,
                        CoinsReward = order.CoinsReward,
                        // Only the requirement definition (typeId/grade) is saved — collected
                        // state and the live bound instance are rebuilt by OnFieldChanged once
                        // the zone's board is restored, exactly like a fresh field rescan.
                        Items = order.Items.ConvertAll(i => new OrderItemSaveData { TypeId = i.typeId, Grade = i.grade })
                    });
                }
            }

            SaveController.MarkAsSaveIsRequired();
        }

        private void HandleZoneChanged(ZoneData newZone)
        {
            SyncSaveData();

            foreach (ITask task in activeTasks)
                task.OnProgressChanged -= SyncSaveData;
            activeTasks.Clear();

            LoadZone(newZone);
            EvaluateProgressionQueue();

            OnTasksChanged?.Invoke();
        }

        // ─── Spawner queue ───────────────────────────────────────────────────────

        public static void EnqueueSpawner(string spawnerTypeId, int grade = 1)
        {
            if (string.IsNullOrEmpty(spawnerTypeId))
            {
                Debug.LogError("[Tasks] EnqueueSpawner called with an empty typeId — check the reward config (SpawnerQueueReward).");
                return;
            }

            Instance?.spawnerQueue.Push(spawnerTypeId, Mathf.Max(1, grade));
        }

        private void SyncSpawnerQueueSave()
        {
            spawnerQueueSave.Entries.Clear();
            spawnerQueueSave.Entries.AddRange(spawnerQueue.Entries);
            SaveController.MarkAsSaveIsRequired();
        }

        // ─── Task management ─────────────────────────────────────────────────────

        public void AddTask(ITask task)
        {
            activeTasks.Add(task);
            task.OnProgressChanged += SyncSaveData;

            if (task is IFieldAwareTask fieldAware && MergeController.Instance != null)
                fieldAware.OnFieldChanged(MergeController.Instance.Grid);

            OnTasksChanged?.Invoke();
            SyncSaveData();
        }

        public void RemoveTask(ITask task)
        {
            task.OnProgressChanged -= SyncSaveData;
            activeTasks.Remove(task);
            OnTasksChanged?.Invoke();
            SyncSaveData();

            if (activeTasks.Count == 0)
                OnAllTasksComplete?.Invoke();

            EvaluateProgressionQueue();
        }

        // ─── Typed factories ─────────────────────────────────────────────────────

        // 跨订单共享的认领集合：同一棋盘活物不能被两张订单同时显示为"可交付"。
        // 此前 ClientOrderTask 的排除集只含本订单已绑实例，两单需求撞车时会同时亮 Give。
        public HashSet<MergeFieldObject> GetClaimedInstances(ClientOrderTask except)
        {
            var claimed = new HashSet<MergeFieldObject>();
            foreach (ITask task in activeTasks)
            {
                if (ReferenceEquals(task, except) || task is not ClientOrderTask order) continue;
                foreach (OrderItem item in order.Items)
                    if (item.collected && item.boundObject != null)
                        claimed.Add(item.boundObject);
            }
            return claimed;
        }

        public static ClientOrderTask AddClientOrder(string taskId, CharacterData character, List<OrderItem> items, int coinsReward)
        {
            var task = new ClientOrderTask(taskId, character, items, coinsReward);
            Instance?.AddTask(task);
            return task;
        }

        // ─── Zone progression task queue ────────────────────────────────────────

        private void HandleUpgradeCompleted(ZoneData zone, string buildingId, int totalUpgradesInZone)
        {
            ZoneData currentZone = ZoneController.CurrentZone;
            if (zone == null || currentZone == null || zone.ZoneId != currentZone.ZoneId) return;

            EvaluateProgressionQueue();
        }

        // Advances ZoneProgressionStep's sequential/random task queue for the current zone.
        // Sequential entries are added one gated batch at a time (a Sequential entry waits for
        // the previous batch to fully complete; consecutive Parallel entries join that same
        // batch). Once the sequential queue is drained, random tasks from the latest reached
        // step's weighted random pool are kept topped up to pool.MaxSimultaneousRandomTasks —
        // completing one immediately draws its replacement, forever.
        // External hold on the progression queue (e.g. while onboarding runs its injected order).
        // Lock before Init() to also suppress the boot-time evaluation; unlocking re-evaluates.
        public static void SetProgressionLocked(bool locked)
        {
            if (isProgressionLocked == locked) return;

            isProgressionLocked = locked;
            if (!locked) Instance?.EvaluateProgressionQueue();
        }

        private void EvaluateProgressionQueue()
        {
            if (isProgressionLocked || isEvaluatingProgression) return;
            if (save == null) return;

            ZoneData zone = ZoneController.CurrentZone;
            if (zone == null) return;

            isEvaluatingProgression = true;
            try
            {
                EvaluateProgressionQueueInternal(zone);
            }
            finally
            {
                isEvaluatingProgression = false;
            }
        }

        private void EvaluateProgressionQueueInternal(ZoneData zone)
        {
            ZoneProgressionTaskState state = save.ProgressionTasks;

            if (state.PendingBatchTaskIds.Count > 0)
            {
                bool stillPending = state.PendingBatchTaskIds.Exists(id => activeTasks.Exists(t => t.TaskId == id));
                if (stillPending) return;

                state.PendingBatchTaskIds.Clear();
            }

            List<ProgressionTaskEntry> queue = BuildSequentialQueue(zone);

            if (state.SequentialAdded < queue.Count)
            {
                RemovePlaceholder();

                int index = state.SequentialAdded;
                AddSequentialEntry(zone, queue[index], index, state);
                index++;

                while (index < queue.Count && queue[index].AddMode == ProgressionTaskAddMode.Parallel)
                {
                    AddSequentialEntry(zone, queue[index], index, state);
                    index++;
                }

                state.SequentialAdded = index;
                SaveController.MarkAsSaveIsRequired();
                return;
            }

            RandomProgressionTaskPool pool = FindActiveRandomPool(zone);
            if (pool == null)
            {
                RemovePlaceholder();
                return;
            }

            if (IsSpawnGated(pool, zone))
            {
                AddPlaceholderIfNeeded(zone, pool);
                return;
            }

            RemovePlaceholder();

            // Random tasks top up individually — completing one immediately spawns its
            // replacement, instead of waiting for the whole batch to clear like sequential does.
            string randomTaskPrefix = $"zoneprog_rand_{zone.ZoneId}_";
            int activeRandomCount = activeTasks.Count(t => t.TaskId.StartsWith(randomTaskPrefix));
            int neededCount = pool.MaxSimultaneousRandomTasks - activeRandomCount;

            bool spawnedAny = false;

            // Characters already in play among active random tasks — PickCharacter avoids these
            // (and whatever this loop adds below) as long as the pool has enough to go around.
            var usedCharacters = new HashSet<CharacterData>(activeTasks
                .Where(t => t.TaskId.StartsWith(randomTaskPrefix))
                .OfType<ClientOrderTask>()
                .Select(t => t.Character)
                .Where(c => c != null));

            for (int i = 0; i < neededCount; i++)
            {
                ClientOrderTaskDefinition picked = pool.PickTask();
                if (picked == null) break;

                string taskId = $"{randomTaskPrefix}{state.RandomSpawnCount++}";
                CharacterData character = pool.PickCharacter(usedCharacters);
                if (character != null) usedCharacters.Add(character);

                AddTask(picked.CreateTask(taskId, character));
                spawnedAny = true;
            }

            if (spawnedAny) SaveController.MarkAsSaveIsRequired();
        }

        private static bool IsSpawnGated(RandomProgressionTaskPool pool, ZoneData zone)
        {
            if (pool.StopSpawnCoinMultiplier <= 0f) return false;

            int minCost = BuildingController.GetMinAvailableUpgradeCost(zone);
            if (minCost < 0) return false;

            return CurrencyController.Get(CurrencyType.Coins) >= pool.StopSpawnCoinMultiplier * minCost;
        }

        private void AddPlaceholderIfNeeded(ZoneData zone, RandomProgressionTaskPool pool)
        {
            if (activeTasks.Exists(t => t is PlaceholderTask)) return;

            AddTask(new PlaceholderTask($"placeholder_{zone.ZoneId}", pool.PickCharacter()));
        }

        private void RemovePlaceholder()
        {
            ITask placeholder = activeTasks.Find(t => t is PlaceholderTask);
            if (placeholder != null) RemoveTask(placeholder);
        }

        private void AddSequentialEntry(ZoneData zone, ProgressionTaskEntry entry, int index, ZoneProgressionTaskState state)
        {
            if (entry.Task == null) return;

            string taskId = $"zoneprog_seq_{zone.ZoneId}_{index}";
            AddTask(entry.Task.CreateTask(taskId, null));
            state.PendingBatchTaskIds.Add(taskId);
        }

        // Flattens every ZoneProgressionStep's sequential entries reached so far (AtUpgrade <=
        // the zone's current upgrade count), ordered by AtUpgrade — so a later step's entries
        // continue queuing behind whatever an earlier step left in progress.
        private List<ProgressionTaskEntry> BuildSequentialQueue(ZoneData zone)
        {
            var result = new List<ProgressionTaskEntry>();
            if (zone.Progression == null) return result;

            int upgradeCount = BuildingController.GetZoneUpgradeCount(zone.ZoneId);

            foreach (ZoneProgressionStep step in zone.Progression.OrderBy(s => s.AtUpgrade))
            {
                if (step.AtUpgrade > upgradeCount) continue;
                if (step.SequentialTasks == null) continue;

                result.AddRange(step.SequentialTasks);
            }

            return result;
        }

        // Latest-reached step (by AtUpgrade) that defines a non-empty random pool — a later
        // step's pool supersedes an earlier one's once reached.
        private RandomProgressionTaskPool FindActiveRandomPool(ZoneData zone)
        {
            if (zone.Progression == null) return null;

            int upgradeCount = BuildingController.GetZoneUpgradeCount(zone.ZoneId);

            RandomProgressionTaskPool best = null;
            int bestAtUpgrade = -1;

            foreach (ZoneProgressionStep step in zone.Progression)
            {
                if (step.AtUpgrade > upgradeCount) continue;
                if (step.RandomTasks == null || !step.RandomTasks.HasTasks) continue;
                if (step.AtUpgrade <= bestAtUpgrade) continue;

                bestAtUpgrade = step.AtUpgrade;
                best = step.RandomTasks;
            }

            return best;
        }

        // ─── Field-changed notification (from MergeGrid — spawn/remove anywhere on the grid) ──

        // Reaching IsComplete here does NOT auto-remove the task — a field-aware task
        // (ClientOrderTask) only finishes when the player presses its Give button;
        // IsComplete just unlocks that button.
        private void NotifyFieldChanged()
        {
            if (MergeController.Instance == null) return;
            MergeGrid grid = MergeController.Instance.Grid;

            foreach (ITask task in activeTasks)
            {
                if (task is IFieldAwareTask fieldAware)
                    fieldAware.OnFieldChanged(grid);
            }
        }
    }
}
