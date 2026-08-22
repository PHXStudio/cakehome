using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class WeightedProgressionTaskEntry
    {
        [SerializeField] float                     weight = 1f;
        [SerializeField] ClientOrderTaskDefinition  task;

        public float                    Weight => weight;
        public ClientOrderTaskDefinition Task   => task;
    }
}
