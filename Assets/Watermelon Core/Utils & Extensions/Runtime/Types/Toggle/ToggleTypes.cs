using UnityEngine;
using UnityEngine.Serialization;

namespace Watermelon
{
    [System.Serializable]
    public abstract class ToggleType<T>
    {
        [SerializeField] bool enabled;
        public bool Enabled => enabled;

        [FormerlySerializedAs("newValue")]
        [SerializeField] T value;
        public T Value => value;

        public ToggleType(bool enabled, T value)
        {
            this.enabled = enabled;
            this.value = value;
        }

        public T Handle(T value)
        {
            if (enabled)
            {
                return this.value;
            }
            else
            {
                return value;
            }
        }
    }
}