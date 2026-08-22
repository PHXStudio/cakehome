namespace Watermelon
{
    /// <summary>
    /// Persisted haptic preferences. Loaded and managed by <see cref="Haptic"/>.
    /// </summary>
    [System.Serializable]
    public class HapticSave : ISaveObject
    {
        /// <summary>Whether haptic is enabled. Defaults to <c>true</c>.</summary>
        public bool IsActive = true;

        public void OnBeforeSave()
        {

        }
    }
}
