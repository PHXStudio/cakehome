using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Wraps a single <see cref="ISaveObject"/> together with its serialized JSON and stable string key inside a <see cref="SaveFile"/>.
    /// </summary>
    [System.Serializable]
    public class SaveFileContainer
    {
        [SerializeField] string key;
        [SerializeField] string json;

        /// <summary>Stable string key used to look up this container in <see cref="SaveFile"/>.</summary>
        public string Key => key;

        [System.NonSerialized] bool restored = false;
        /// <summary>True once the save object has been deserialized from JSON or freshly constructed.</summary>
        public bool Restored => restored;

        [System.NonSerialized] ISaveObject saveObject;
        /// <summary>The live in-memory save object instance.</summary>
        public ISaveObject SaveObject => saveObject;

        /// <summary>Creates a new container for a freshly constructed save object; marks it as already restored.</summary>
        public SaveFileContainer(string key, ISaveObject saveObject)
        {
            this.key = key;
            this.saveObject = saveObject;

            restored = true;
        }

        // Parameterless — used only by SaveJson (Newtonsoft) when deserializing a "containers" array entry.
        // Leaves `restored` false so SaveFile.GetSaveObject falls through to Restore<T>().
        private SaveFileContainer() { }

        /// <summary>Calls <see cref="ISaveObject.OnBeforeSave"/> then serializes the save object to the stored JSON field.</summary>
        public void Flush()
        {
            if (saveObject != null && restored)
            {
                saveObject.OnBeforeSave();

                json = SaveJson.ToJson(saveObject);
            }
        }

        /// <summary>Deserializes the stored JSON into a typed instance; falls back to <c>new T()</c> if JSON is null or malformed.</summary>
        public void Restore<T>() where T : ISaveObject, new()
        {
            saveObject = SaveJson.FromJson<T>(json) ?? new T();
            restored = true;
        }
    }
}
