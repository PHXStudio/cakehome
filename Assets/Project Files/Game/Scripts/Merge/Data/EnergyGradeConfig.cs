using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class EnergyGradeConfig : MergeGradeConfig
    {
        [SerializeField] int amount; // energy given on pickup

        public int Amount => amount;
    }
}
