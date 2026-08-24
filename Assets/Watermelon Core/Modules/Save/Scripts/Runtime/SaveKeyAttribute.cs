using System;

namespace Watermelon
{
    /// <summary>
    /// Pins a stable storage key to a save object class.
    /// Without this attribute the key defaults to typeof(T).FullName,
    /// which silently breaks saves when a class or namespace is renamed.
    ///
    /// Usage:
    ///   [SaveKey("player_progress")]
    ///   public class PlayerProgress : ISaveObject { ... }
    ///
    /// The key must be unique across all save objects in the same SaveFile.
    /// Once set, never change the key — changing it is equivalent to deleting the save.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SaveKeyAttribute : Attribute
    {
        /// <summary>The stable storage key assigned to the decorated class.</summary>
        public string Key { get; }

        public SaveKeyAttribute(string key)
        {
            Key = key;
        }
    }
}
