using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class ChestGradeConfig : SpawnPoolGradeConfig
    {
        [SerializeField] int maxSpawns = 3;

        public int MaxSpawns => maxSpawns;
    }
}
