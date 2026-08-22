using UnityEngine;

namespace Watermelon
{
    /// <summary>Minimal <see cref="ISaveObject"/> that persists a single <see langword="string"/> value.</summary>
    [System.Serializable]
    public class SimpleStringSave : ISaveObject
    {
        [SerializeField] string value;
        public virtual string Value
        {
            get => value;
            set => this.value = value;
        }

        public virtual void OnBeforeSave() { }
    }
}