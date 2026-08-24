using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class ProgressionTaskEntry
    {
        [SerializeField] ProgressionTaskAddMode   addMode = ProgressionTaskAddMode.Sequential;
        [SerializeField] ClientOrderTaskDefinition task;

        public ProgressionTaskAddMode     AddMode => addMode;
        public ClientOrderTaskDefinition  Task    => task;
    }
}
