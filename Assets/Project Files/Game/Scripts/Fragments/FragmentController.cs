using System;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Cake-fragment inventory (配方碎片). Lives in Game.Scripts so the order system
    /// (TaskController/UIClientOrderCard) can grant fragments without bridging;
    /// HotUpdate systems (level drops, exchange UI) access it the same way.
    /// </summary>
    public static class FragmentController
    {
        private const string SAVE_KEY = "Fragments";

        private static FragmentSave save;
        private static bool initialized;

        public static event Action OnChanged;

        public static int Count => save?.Count ?? 0;

        public static void Init()
        {
            if (initialized) return;

            save = SaveController.GetSaveObject<FragmentSave>(SAVE_KEY);
            initialized = true;
        }

        public static void Add(int amount)
        {
            if (!initialized || amount <= 0) return;

            save.Count += amount;
            SaveController.MarkAsSaveIsRequired();

            OnChanged?.Invoke();
        }

        public static bool Has(int amount) => Count >= amount;

        public static bool Spend(int amount)
        {
            if (!initialized || amount <= 0 || save.Count < amount) return false;

            save.Count -= amount;
            SaveController.MarkAsSaveIsRequired();

            OnChanged?.Invoke();

            return true;
        }
    }
}
