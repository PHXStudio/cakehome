using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class SpawnerObject : MergeFieldObject
    {
        private const int ENERGY_COST = 1;

        [SerializeField] Image energyImage;
        [SerializeField] ParticleSystem spawnerParticle;

        // Tutorial-only gate — while suppressed, the energy icon and idle particle stay hidden so
        // early guided-merge steps don't draw attention to spawners before the spawn step unlocks
        // them (mirrors SpawnerController.SetSpawningLocked).
        private static bool energyVisualsSuppressed;
        private static readonly List<SpawnerObject> activeSpawners = new List<SpawnerObject>();

        public static void SetEnergyVisualsSuppressed(bool suppressed)
        {
            energyVisualsSuppressed = suppressed;

            for (int i = 0; i < activeSpawners.Count; i++)
                activeSpawners[i].RefreshEnergyVisuals();
        }

        public virtual int EnergyCost => ENERGY_COST;
        public WeightedList<SpawnEntry> SpawnPool => GradeData?.GetConfig<SpawnPoolGradeConfig>()?.SpawnPool;

        // Gate checked by SpawnerController before spending energy/rolling the pool — true means
        // unlimited for a plain Spawner; ChestObject overrides this with its remaining charge count.
        public virtual bool CanSpawn => true;

        // Hook called by SpawnerController after a successful spawn — no-op for a plain Spawner;
        // ChestObject overrides this to consume a charge and remove itself once empty.
        public virtual void OnSpawned() { }

        public (SpawnEntry Entry, float ChancePercent) GetRandomSpawnWithChance() =>
            !IsPoolEmpty ? SpawnPool.GetRandomItemWithChance() : (null, 0f);

        // Checked by SpawnerController before spending energy/rolling the pool — an empty pool
        // (e.g. all items removed from the grade config) must not spend energy or hit
        // WeightedList's own "can't be empty" error log.
        public bool IsPoolEmpty => SpawnPool == null || SpawnPool.Items.IsNullOrEmpty();

        public override string GetDisplayName() => GradeData?.DisplayName ?? name;
        public override bool CanDrag() => !IsHalfLocked;

        public override ActionButtonConfig GetActionButton() =>
            new ActionButtonConfig(ActionButtonType.Spawn, "Spawn", $"{EnergyCost}", activateOnTap: true);

        public void Bounce() => BounceAnimation.Bounce(transform);

        private void OnEnable()
        {
            activeSpawners.Add(this);
            RefreshEnergyVisuals();
        }

        private void OnDisable()
        {
            activeSpawners.Remove(this);
        }

        public override void Init(MergeItemData data, int grade)
        {
            base.Init(data, grade);

            RefreshEnergyVisuals();
        }

        protected override void OnVisualsStateChanged(bool visible)
        {
            base.OnVisualsStateChanged(visible);

            RefreshEnergyVisuals();
        }

        protected override void OnHalfLockedStateChanged(bool locked)
        {
            base.OnHalfLockedStateChanged(locked);

            RefreshEnergyVisuals();
        }

        private void RefreshEnergyVisuals()
        {
            bool active = !energyVisualsSuppressed && !IsPoolEmpty && IsVisualVisible && !IsHalfLocked;

            if (energyImage != null)
                energyImage.enabled = active;

            UpdateParticleState(active);
        }

        private void UpdateParticleState(bool active)
        {
            if (spawnerParticle == null)
                return;

            if (active)
            {
                if (!spawnerParticle.isPlaying)
                    spawnerParticle.Play();
            }
            else
            {
                spawnerParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
