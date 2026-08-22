using UnityEngine;

namespace Watermelon
{
    // Shared by SpawnerGradeConfig and ChestGradeConfig — lets SpawnerObject/UIInfoWindow read
    // the pool without caring which concrete config type produced it, while keeping the
    // type-specific fields (EnergyCost / MaxSpawns) separate so a designer filling out one type
    // doesn't see an irrelevant field for the other.
    [System.Serializable]
    public abstract class SpawnPoolGradeConfig : MergeGradeConfig
    {
        [SerializeField] WeightedList<SpawnEntry> spawnPool;

        public WeightedList<SpawnEntry> SpawnPool => spawnPool;
    }
}
