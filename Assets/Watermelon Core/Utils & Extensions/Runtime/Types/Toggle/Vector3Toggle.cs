using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class Vector3Toggle : ToggleType<Vector3>
    {
        public Vector3Toggle(bool enabled, Vector3 value) : base(enabled, value) { }
    }
}