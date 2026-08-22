using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class ObjectToggle : ToggleType<GameObject>
    {
        public ObjectToggle(bool enabled, GameObject value) : base(enabled, value) { }
    }
}