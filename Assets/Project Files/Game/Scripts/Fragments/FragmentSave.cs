using System;

namespace Watermelon
{
    /// <summary>
    /// Save for <see cref="FragmentController"/>. Stores the cake-fragment inventory —
    /// fragments drop in the tile-match levels and are granted by client orders,
    /// then exchanged for merge-side resources (energy, spawners).
    /// </summary>
    [Serializable]
    public class FragmentSave : ISaveObject
    {
        public int Count;

        public void OnBeforeSave() { }
    }
}
