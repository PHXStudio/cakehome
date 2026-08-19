#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    public static class ShopDebugMenu
    {
        [MenuItem("Actions/Shop/Grant Random Cake")]
        public static void GrantRandomCake()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Shop] Enter Play Mode first.");
                return;
            }

            ShopController.EnsureInitialized();
            OwnedCake cake = ShopController.GrantRandomCake();
            Debug.Log(cake != null ? $"[Shop] Granted {cake.DefinitionId} ({cake.InstanceId})" : "[Shop] Grant failed.");
        }

        [MenuItem("Actions/Shop/Add 500 Coins")]
        public static void AddCredits()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Shop] Enter Play Mode first.");
                return;
            }

            CurrencyController.Add(CurrencyType.Coins, 500);
            Debug.Log("[Shop] +500 Coins");
        }

        [MenuItem("Actions/Shop/Force Settle Offline")]
        public static void ForceSettle()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Shop] Enter Play Mode first.");
                return;
            }

            ShopController.EnsureInitialized();
            ShopController.SettleOffline();
            Debug.Log($"[Shop] Pending credits: {ShopController.PendingCredits}");
        }

        [MenuItem("Actions/Shop/Force Manager Recommend On Shelves")]
        public static void ForceRecommend()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Shop] Enter Play Mode first.");
                return;
            }

            ShopController.EnsureInitialized();
            ShopSave save = ShopController.Save;
            System.DateTime past = System.DateTime.Now.AddHours(-ShopController.Config.ManagerRecommendHours - 0.1f);
            for (int i = 0; i < save.UnlockedShelfCount; i++)
            {
                OwnedCake cake = ShopController.GetCakeOnShelf(i);
                if (cake != null)
                    cake.PlacedAtBinary = past.ToBinary();
            }

            SaveController.MarkAsSaveIsRequired();
            ShopWorld.Refresh();
            Debug.Log("[Shop] Forced manager recommend on occupied shelves.");
        }
    }
}
#endif
