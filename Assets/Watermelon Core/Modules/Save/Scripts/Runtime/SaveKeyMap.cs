using System;
using System.Collections.Generic;

namespace Watermelon
{
    /// <summary>
    /// Runtime registry that maps ISaveObject types to their storage keys.
    ///
    /// Populated before scene load by MD_GeneratedSaveKeyRegistration (auto-generated).
    /// Regenerate via: Tools/Save/Regenerate Key Map
    ///
    /// If a type is not registered (map needs regeneration), SaveFile falls back
    /// to runtime reflection — [SaveKey] attribute or typeof(T).FullName.
    /// </summary>
    public static class SaveKeyMap
    {
        private static readonly Dictionary<Type, string> map = new();

        /// <summary>Registers a type-to-key mapping; called by the auto-generated registration file at startup.</summary>
        public static void Register(Type type, string key) => map[type] = key;

        /// <summary>Looks up the storage key for the given type; returns false if the type has not been registered.</summary>
        public static bool TryGet(Type type, out string key) => map.TryGetValue(type, out key);
    }
}
