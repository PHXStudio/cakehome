using UnityEngine;

namespace Watermelon
{
    /// <summary>Minimal <see cref="ISaveObject"/> that persists a single <see langword="float"/> value.</summary>
    [System.Serializable]
    public class SimpleFloatSave : ISaveObject
    {
        [SerializeField] float value;
        public float Value { get => value; set => this.value = value; }

        public virtual void OnBeforeSave() { }
    }
}