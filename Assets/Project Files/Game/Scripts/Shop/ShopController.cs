using System;
using UnityEngine;

namespace Watermelon
{
    [StaticUnload]
    public static class ShopController
    {
        private static ShopSave save;
        private static CakeCatalog catalog;
        private static ShopConfig config;
        private static bool isInitialized;

        private static float onlineAccumulator;
        private static DateTime lastOnlineTick;

        public static bool IsInitialized => isInitialized;
        public static CakeCatalog Catalog => catalog;
        public static ShopConfig Config => config;
        public static ShopSave Save => save;

        public static event SimpleCallback StateChanged;
        public static event CurrencyCallback Harvested;

        public static void Init(CakeCatalog cakeCatalog, ShopConfig shopConfig)
        {
            if (isInitialized)
                return;

            catalog = cakeCatalog != null ? cakeCatalog : CakeCatalog.CreateDefaultRuntimeCatalog();
            config = shopConfig != null ? shopConfig : ShopConfig.CreateDefaultRuntimeConfig();

            save = SaveController.GetSaveObject<ShopSave>("shop");
            if (save.UnlockedShelfCount < config.InitialShelfCount)
                save.UnlockedShelfCount = config.InitialShelfCount;

            save.EnsureShelfSize(config.MaxShelfCount);

            if (save.Cakes == null)
                save.Cakes = new System.Collections.Generic.List<OwnedCake>();

            if (save.LastSettleBinary == 0)
                save.LastSettleBinary = DateTime.Now.ToBinary();

            ShopDailyTheme.Refresh(save, catalog);
            SettleOffline();

            lastOnlineTick = DateTime.Now;
            onlineAccumulator = 0f;
            isInitialized = true;
        }

        public static void EnsureInitialized()
        {
            if (isInitialized)
                return;

            CakeCatalog loadedCatalog = Resources.Load<CakeCatalog>("Shop/Cake Catalog");
            ShopConfig loadedConfig = Resources.Load<ShopConfig>("Shop/Shop Config");
            Init(loadedCatalog, loadedConfig);
        }

        public static OwnedCake GetCake(int cakeIndex)
        {
            if (cakeIndex < 0 || cakeIndex >= save.Cakes.Count)
                return null;

            return save.Cakes[cakeIndex];
        }

        public static OwnedCake GetCakeOnShelf(int shelfIndex)
        {
            if (!IsValidUnlockedShelf(shelfIndex))
                return null;

            int cakeIndex = save.ShelfSlots[shelfIndex];
            return GetCake(cakeIndex);
        }

        public static int FindCakeIndex(string instanceId)
        {
            for (int i = 0; i < save.Cakes.Count; i++)
            {
                if (save.Cakes[i].InstanceId == instanceId)
                    return i;
            }

            return -1;
        }

        public static CakeDefinition GetDefinition(OwnedCake cake)
        {
            if (cake == null)
                return null;

            return catalog.GetById(cake.DefinitionId);
        }

        public static bool IsShelfUnlocked(int shelfIndex)
        {
            return shelfIndex >= 0 && shelfIndex < save.UnlockedShelfCount;
        }

        public static bool IsValidUnlockedShelf(int shelfIndex)
        {
            return IsShelfUnlocked(shelfIndex) && shelfIndex < save.ShelfSlots.Length;
        }

        public static int FindEmptyShelf()
        {
            for (int i = 0; i < save.UnlockedShelfCount; i++)
            {
                if (save.ShelfSlots[i] == ShopSave.EMPTY_SLOT)
                    return i;
            }

            return -1;
        }

        public static OwnedCake GrantCake(string definitionId)
        {
            EnsureInitialized();

            CakeDefinition definition = catalog.GetById(definitionId);
            if (definition == null)
            {
                Debug.LogWarning($"[Shop] Unknown cake definition: {definitionId}");
                return null;
            }

            string instanceId = $"cake_{save.NextInstanceCounter++}";
            OwnedCake cake = new OwnedCake(instanceId, definitionId);
            save.Cakes.Add(cake);

            int empty = FindEmptyShelf();
            if (empty >= 0)
            {
                PlaceCakeInternal(save.Cakes.Count - 1, empty);
            }

            MarkDirtyAndNotify();
            return cake;
        }

        public static OwnedCake GrantRandomCake()
        {
            EnsureInitialized();

            if (catalog.Count == 0)
                return null;

            int index = UnityEngine.Random.Range(0, catalog.Count);
            CakeDefinition def = catalog.GetByIndex(index);
            return GrantCake(def.Id);
        }

        public static bool PlaceCake(string instanceId, int shelfIndex)
        {
            EnsureInitialized();

            int cakeIndex = FindCakeIndex(instanceId);
            if (cakeIndex < 0 || !IsValidUnlockedShelf(shelfIndex))
                return false;

            // Settle before rearranging so rates stay fair
            SettleOnlinePartial();

            PlaceCakeInternal(cakeIndex, shelfIndex);
            MarkDirtyAndNotify();
            return true;
        }

        public static bool UnshelveToFreezer(string instanceId)
        {
            EnsureInitialized();

            int cakeIndex = FindCakeIndex(instanceId);
            if (cakeIndex < 0)
                return false;

            SettleOnlinePartial();

            for (int i = 0; i < save.ShelfSlots.Length; i++)
            {
                if (save.ShelfSlots[i] == cakeIndex)
                    save.ShelfSlots[i] = ShopSave.EMPTY_SLOT;
            }

            save.Cakes[cakeIndex].ClearPlacement();
            MarkDirtyAndNotify();
            return true;
        }

        public static bool SwapShelves(int shelfA, int shelfB)
        {
            EnsureInitialized();

            if (!IsValidUnlockedShelf(shelfA) || !IsValidUnlockedShelf(shelfB))
                return false;

            SettleOnlinePartial();

            int temp = save.ShelfSlots[shelfA];
            save.ShelfSlots[shelfA] = save.ShelfSlots[shelfB];
            save.ShelfSlots[shelfB] = temp;

            // Refresh placed timestamps for cakes that moved onto shelves
            RefreshPlacementTimestamps();
            MarkDirtyAndNotify();
            return true;
        }

        public static bool UnlockNextShelf()
        {
            EnsureInitialized();

            if (save.UnlockedShelfCount >= config.MaxShelfCount)
                return false;

            int cost = config.GetExpandCost(save.UnlockedShelfCount);
            if (!CurrencyController.HasAmount(CurrencyType.BakingCredits, cost))
                return false;

            CurrencyController.Substract(CurrencyType.BakingCredits, cost);
            save.UnlockedShelfCount++;
            save.EnsureShelfSize(config.MaxShelfCount);
            MarkDirtyAndNotify();
            return true;
        }

        public static int GetNextExpandCost()
        {
            EnsureInitialized();

            if (save.UnlockedShelfCount >= config.MaxShelfCount)
                return -1;

            return config.GetExpandCost(save.UnlockedShelfCount);
        }

        public static bool IsManagerRecommended(OwnedCake cake, DateTime now)
        {
            if (cake == null || !cake.IsOnShelf)
                return false;

            return cake.GetHoursOnShelf(now) >= config.ManagerRecommendHours;
        }

        public static bool IsHotThemeMatch(OwnedCake cake)
        {
            CakeDefinition definition = GetDefinition(cake);
            if (definition == null)
                return false;

            return definition.HasElement(ShopDailyTheme.CurrentTheme);
        }

        public static float GetShelfRate(int shelfIndex)
        {
            EnsureInitialized();

            OwnedCake cake = GetCakeOnShelf(shelfIndex);
            if (cake == null)
                return 0f;

            return GetCakeRate(cake, DateTime.Now);
        }

        public static float GetCakeRate(OwnedCake cake, DateTime now)
        {
            CakeDefinition definition = GetDefinition(cake);
            float baseRate = definition != null ? definition.CreditsPerHour : config.DefaultCreditsPerHour;

            float multiplier = 1f;
            if (IsManagerRecommended(cake, now))
                multiplier *= config.ManagerRecommendMultiplier;

            if (IsHotThemeMatch(cake))
                multiplier *= config.HotThemeMultiplier;

            return baseRate * multiplier;
        }

        public static float GetTotalCreditsPerHour()
        {
            EnsureInitialized();

            float total = 0f;
            DateTime now = DateTime.Now;
            for (int i = 0; i < save.UnlockedShelfCount; i++)
            {
                OwnedCake cake = GetCakeOnShelf(i);
                if (cake != null)
                    total += GetCakeRate(cake, now);
            }

            return total;
        }

        public static double PendingCredits => save != null ? save.PendingCredits : 0;

        public static void TickOnline(float deltaSeconds)
        {
            if (!isInitialized)
                return;

            float cph = GetTotalCreditsPerHour();
            if (cph <= 0f)
            {
                lastOnlineTick = DateTime.Now;
                return;
            }

            onlineAccumulator += cph * (deltaSeconds / 3600f);
            if (onlineAccumulator >= 0.01f)
            {
                save.PendingCredits += onlineAccumulator;
                onlineAccumulator = 0f;
                save.LastSettleBinary = DateTime.Now.ToBinary();
                SaveController.MarkAsSaveIsRequired();
                StateChanged?.Invoke();
            }

            lastOnlineTick = DateTime.Now;
        }

        public static void SettleOffline()
        {
            if (save == null)
                return;

            DateTime now = DateTime.Now;
            DateTime last = DateTime.FromBinary(save.LastSettleBinary);
            if (last > now)
                last = now;

            double hours = (now - last).TotalHours;
            if (hours < 0.0001)
            {
                save.LastSettleBinary = now.ToBinary();
                return;
            }

            hours = Math.Min(hours, config.MaxOfflineHours);

            double earned = 0;
            for (int i = 0; i < save.UnlockedShelfCount; i++)
            {
                OwnedCake cake = GetCakeOnShelf(i);
                if (cake == null)
                    continue;

                // Approximate: use current multipliers (manager recommend based on now)
                earned += GetCakeRate(cake, now) * hours;
            }

            if (earned > 0)
                save.PendingCredits += earned;

            save.LastSettleBinary = now.ToBinary();
            onlineAccumulator = 0f;
            SaveController.MarkAsSaveIsRequired();
        }

        public static void OnShopOpened()
        {
            EnsureInitialized();
            ShopDailyTheme.Refresh(save, catalog);
            SettleOffline();
            lastOnlineTick = DateTime.Now;
            StateChanged?.Invoke();
        }

        public static int Harvest()
        {
            EnsureInitialized();
            SettleOnlinePartial();

            int amount = Mathf.FloorToInt((float)save.PendingCredits);
            if (amount <= 0)
                return 0;

            save.PendingCredits -= amount;
            CurrencyController.Add(CurrencyType.BakingCredits, amount);
            MarkDirtyAndNotify();

            Harvested?.Invoke(CurrencyController.GetCurrency(CurrencyType.BakingCredits), amount);
            return amount;
        }

        public static System.Collections.Generic.List<OwnedCake> GetFreezerCakes()
        {
            EnsureInitialized();

            System.Collections.Generic.List<OwnedCake> freezer = new System.Collections.Generic.List<OwnedCake>();
            for (int i = 0; i < save.Cakes.Count; i++)
            {
                if (!IsCakeOnAnyShelf(i))
                    freezer.Add(save.Cakes[i]);
            }

            return freezer;
        }

        private static bool IsCakeOnAnyShelf(int cakeIndex)
        {
            for (int i = 0; i < save.ShelfSlots.Length; i++)
            {
                if (save.ShelfSlots[i] == cakeIndex)
                    return true;
            }

            return false;
        }

        private static void PlaceCakeInternal(int cakeIndex, int shelfIndex)
        {
            // If shelf occupied, move previous cake to freezer
            int existing = save.ShelfSlots[shelfIndex];
            if (existing != ShopSave.EMPTY_SLOT && existing != cakeIndex)
                save.Cakes[existing].ClearPlacement();

            // Remove cake from any other shelf
            for (int i = 0; i < save.ShelfSlots.Length; i++)
            {
                if (save.ShelfSlots[i] == cakeIndex)
                    save.ShelfSlots[i] = ShopSave.EMPTY_SLOT;
            }

            save.ShelfSlots[shelfIndex] = cakeIndex;
            save.Cakes[cakeIndex].PlaceNow();
        }

        private static void RefreshPlacementTimestamps()
        {
            for (int i = 0; i < save.ShelfSlots.Length; i++)
            {
                int cakeIndex = save.ShelfSlots[i];
                if (cakeIndex == ShopSave.EMPTY_SLOT)
                    continue;

                if (!save.Cakes[cakeIndex].IsOnShelf)
                    save.Cakes[cakeIndex].PlaceNow();
            }
        }

        private static void SettleOnlinePartial()
        {
            if (!isInitialized)
                return;

            DateTime now = DateTime.Now;
            float seconds = (float)(now - lastOnlineTick).TotalSeconds;
            if (seconds > 0f)
                TickOnline(seconds);
        }

        private static void MarkDirtyAndNotify()
        {
            SaveController.MarkAsSaveIsRequired();
            StateChanged?.Invoke();
        }

        private static void UnloadStatic()
        {
            save = null;
            catalog = null;
            config = null;
            isInitialized = false;
            onlineAccumulator = 0f;
            StateChanged = null;
            Harvested = null;
        }
    }
}
