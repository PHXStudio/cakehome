using System;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Lifetime statistics for the merge gameplay (orders delivered etc.).
    /// Incremented at the order delivery point (UIClientOrderCard).
    /// </summary>
    public static class MergeStatsController
    {
        private const string SAVE_KEY = "MergeStats";

        private static MergeStatsSave save;
        private static bool initialized;

        public static int OrdersCompleted => save?.OrdersCompleted ?? 0;

        public static void Init()
        {
            if (initialized) return;

            save = SaveController.GetSaveObject<MergeStatsSave>(SAVE_KEY);
            initialized = true;
        }

        public static void AddOrdersCompleted(int amount)
        {
            if (!initialized || amount <= 0) return;

            save.OrdersCompleted += amount;
            SaveController.MarkAsSaveIsRequired();
        }
    }
}
