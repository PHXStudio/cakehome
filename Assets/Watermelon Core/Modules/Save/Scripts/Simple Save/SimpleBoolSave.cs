using UnityEngine;

namespace Watermelon
{
    /// <summary>Minimal <see cref="ISaveObject"/> that persists a single <see langword="bool"/> value.</summary>
    [System.Serializable]
    public class SimpleBoolSave : ISaveObject
    {
        [SerializeField] bool value;
        public virtual bool Value
        {
            get => value;
            set => this.value = value;
        }

        public virtual void OnBeforeSave() { }
    }
}