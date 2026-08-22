namespace Watermelon
{
    /// <summary>
    /// Contract for all serializable save data objects managed by the Save system.
    /// </summary>
    public interface ISaveObject
    {
        /// <summary>Called before serialization. Use to sync runtime state into serializable fields.</summary>
        void OnBeforeSave();
    }
}