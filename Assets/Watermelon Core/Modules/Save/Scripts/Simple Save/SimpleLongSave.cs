using UnityEngine;

namespace Watermelon
{
    /// <summary>Minimal <see cref="ISaveObject"/> that persists a single <see langword="long"/> value.</summary>
    [System.Serializable]
    public class SimpleLongSave : ISaveObject
    {
        [SerializeField] long value;
        public virtual long Value
        {
            get => value; set
            {
                this.value = value;
            }
        }

        public virtual void OnBeforeSave() { }
    }
}