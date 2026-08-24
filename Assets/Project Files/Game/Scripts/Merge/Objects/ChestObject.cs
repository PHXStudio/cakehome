using System;
using UnityEngine;

namespace Watermelon
{
    // Same as SpawnerObject (weighted spawn pool, tap-to-spawn) but free to use and with a
    // limited number of spawns — once RemainingSpawns hits 0 it removes itself from the grid.
    public class ChestObject : SpawnerObject
    {
        [Serializable]
        private struct ChestSaveData
        {
            public int RemainingSpawns;
        }

        public int MaxSpawns => GradeData?.GetConfig<ChestGradeConfig>()?.MaxSpawns ?? 0;
        public int RemainingSpawns { get; private set; }

        public override int EnergyCost => 0;
        public override bool CanSpawn => RemainingSpawns > 0;

        public override void Init(MergeItemData data, int grade)
        {
            base.Init(data, grade);
            RemainingSpawns = MaxSpawns;
        }

        public override object OnBeforeSave() => new ChestSaveData { RemainingSpawns = RemainingSpawns };

        public override void OnAfterLoad(string customDataJson)
        {
            RemainingSpawns = JsonUtility.FromJson<ChestSaveData>(customDataJson).RemainingSpawns;
        }

        public override void OnSpawned()
        {
            RemainingSpawns = Mathf.Max(0, RemainingSpawns - 1);

            if (RemainingSpawns <= 0)
                MergeController.Instance?.RemoveFromGridAnimated(this);
        }
    }
}
