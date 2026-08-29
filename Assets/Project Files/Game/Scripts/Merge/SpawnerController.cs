using System;
using UnityEngine;

namespace Watermelon
{
    public class SpawnerController : MonoBehaviour
    {
        public static SpawnerController Instance { get; private set; }

        [SerializeField] MergeGrid   mergeGrid;
        [SerializeField] MergeDatabase database;

        [UnpackNested]
        [BoxFoldout("Flight Settings", order: 10)]
        [SerializeField] MergeFlyingIcon.FlightSettings flightSettings;

        // Tutorial-only override — while set, every TryActivate deterministically produces this
        // item instead of rolling the spawner's weighted pool, so a guided step can rely on a
        // known result. Cleared via ClearForcedSpawn() once the tutorial step no longer needs it.
        private static string forcedTypeId;
        private static int forcedGrade;

        // Tutorial-only gate — while locked, TryActivate is a no-op, so guided merge steps can't
        // be derailed by tapping a teapot spawner mid-script (a stray random item breaks the
        // scripted board layout the step cell configs rely on).
        private static bool isSpawningLocked;

        public static void SetSpawningLocked(bool locked)
        {
            isSpawningLocked = locked;
        }

        // Fired after any successful TryActivate — forced (tutorial) or the normal weighted-random
        // path — so tutorial steps can react to "a spawn just happened" without caring which.
        public static event Action OnSpawnCompleted;

        public static void SetForcedSpawn(string typeId, int grade)
        {
            forcedTypeId = typeId;
            forcedGrade = grade;
        }

        public static void ClearForcedSpawn()
        {
            forcedTypeId = null;
        }

        public void Init()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        public bool TryActivate(SpawnerObject spawner, Vector2Int spawnerPos)
        {
            if (isSpawningLocked)
            {
                Debug.Log("[Spawn] TryActivate: aborted, spawning is locked by tutorial");
                return false;
            }

            if (!spawner.CanSpawn)
            {
                Debug.Log("[Spawn] TryActivate: aborted, spawner has no spawns remaining");
                return false;
            }

            bool isForced = !string.IsNullOrEmpty(forcedTypeId);

            // 强制产出（SetForcedSpawn：教程/测试用）应绕过空池检查，否则 g1 生成器（池空设计）下强制产出永远失败
            if (!isForced && spawner.IsPoolEmpty)
            {
                Debug.Log("[Spawn] TryActivate: aborted, spawn pool is empty");
                return false;
            }

            SpawnEntry entry;
            float chancePercent;

            if (isForced)
            {
                entry = new SpawnEntry { typeId = forcedTypeId, grade = forcedGrade };
                chancePercent = 0f;
            }
            else
            {
                (entry, chancePercent) = spawner.GetRandomSpawnWithChance();
            }

            if (entry == null)
            {
                Debug.Log("[Spawn] TryActivate: aborted, GetRandomSpawnWithChance() returned null (SpawnPool missing/empty on grade config)");
                return false;
            }

            MergeItemData data = database.GetItem(entry.typeId);
            if (data == null)
            {
                Debug.Log($"[Spawn] TryActivate: aborted, database has no item for typeId='{entry.typeId}'");
                return false;
            }

            MergeCell emptyCell = mergeGrid.FindNearestEmpty(spawnerPos.x, spawnerPos.y);
            if (emptyCell == null)
            {
                FloatingTextController.SpawnFloatingText("spawner_hint", "No Space!", spawner.transform.position);
                return false;
            }

            // Everything that can fail is resolved — energy is the last gate before commit.
            if (spawner.EnergyCost > 0 && !EnergyController.TrySpend(spawner.EnergyCost))
            {
                UIRecoverEnergy.Show();

                return false;
            }

            MergeFieldObject spawned = mergeGrid.SpawnObject(data, entry.grade, emptyCell.Position.x, emptyCell.Position.y, startVisible: false);
            FlySpawnedObject(spawned, spawner.transform.position, spawnerPos, emptyCell.Position, chancePercent);

            AudioController.PlaySound(AudioController.GetClip("spawner_activate"));

            spawner.Bounce();
            spawner.OnSpawned();

            MergeController.Instance?.SyncSaveData();

            Checkpoint.Log($"Spawner produced {entry.typeId} grade {entry.grade}", gameObject);

            OnSpawnCompleted?.Invoke();

            return true;
        }

        // Spawned object is already placed in its final cell (visual hidden via SpawnObject's
        // startVisible: false) — fly a temporary icon from the spawner to that cell, mirroring
        // MergeController's displace animation.
        private void FlySpawnedObject(MergeFieldObject spawned, Vector3 spawnerWorldPosition, Vector2Int from, Vector2Int to, float chancePercent)
        {
            if (MergeDatabase.IsRareChance(chancePercent) && !MergeController.IsCelebrationTextSuppressed)
            {
                Vector3 midPosition = Vector3.Lerp(spawnerWorldPosition, spawned.transform.position, 0.5f);
                Color color = MergeController.Instance != null ? MergeController.Instance.GreatItemTextColor : Color.white;

                FloatingTextController.SpawnFloatingText("spawner_great", "Great!", midPosition, Quaternion.identity, 1f, color);
            }

            MergeFlyingIcon flyIcon = mergeGrid.SpawnFlyingIcon();
            flyIcon.Show(spawned, mergeGrid.GetIconAnchoredPosition(from));

            AudioController.PlaySound(AudioController.GetClip("item_flying"));

            flyIcon.FlyTo(mergeGrid.GetIconAnchoredPosition(to), flightSettings, () =>
            {
                if (spawned == null) return;
                spawned.SetVisualVisible(true);
                ClientOrderHighlightController.Instance?.Refresh();
                ParticlesController.PlayParticle("Appear")?.SetPosition(spawned.transform.position);

                if (MergeController.Instance != null && MergeController.Instance.TryMarkItemSeen(spawned.TypeId, spawned.Grade))
                    MergeController.ShowNewItemFloatingText(spawned.transform.position);
            });
        }
    }
}
