using UnityEngine;

namespace Watermelon
{
    /// <summary>Minimal <see cref="ISaveObject"/> that persists a single <see langword="int"/> value.</summary>
    [System.Serializable]
    public class SimpleIntSave : ISaveObject
    {
        [SerializeField] int value;
        public virtual int Value
        {
            get => value; set
            {
                this.value = value;
            }
        }

        public virtual void OnBeforeSave() { }
    }
}