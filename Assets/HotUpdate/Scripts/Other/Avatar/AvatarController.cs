using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// TAB3 我的（个人中心）：Avatar 换装 + 专属称号 + 甜品展馆。
    /// 承接富余积分的情感溢价消费（规划 TAB3 回收站）。
    /// </summary>
    public static class AvatarController
    {
        private const string SAVE_NAME = "avatar";

        private static AvatarSave save;
        private static bool isInitialized;

        public static event Action OnAvatarChanged;

        public static AvatarSave Save => save;
        public static bool IsInitialized => isInitialized;

        // ------------------------------------------------------------------ 换装槽位

        public enum AvatarSlot
        {
            Outfit = 0,   // 服装（烘焙服）
            Hat = 1,      // 头饰（独角兽）
            Prop = 2,     // 手持（打蛋器）
        }

        public static string[] SlotItems = { "烘焙服", "独角兽头饰", "打蛋器" };
        public static int[] SlotCosts = { 300, 200, 150 };

        // ------------------------------------------------------------------ 称号

        public static string[] Titles = { "绝味蛋糕师", "扩张大师", "甜品收藏家" };

        public static void Init()
        {
            if (isInitialized)
                return;

            isInitialized = true;
            save = SaveController.GetSaveObject<AvatarSave>(SAVE_NAME);
            save.EnsureInit(SlotItems.Length);
        }

        // ------------------------------------------------------------------ 换装

        public static bool IsOwned(AvatarSlot slot)
        {
            EnsureInit();
            return save.OwnedSlots[(int)slot];
        }

        public static bool IsEquipped(AvatarSlot slot)
        {
            EnsureInit();
            return save.EquippedSlot == (int)slot;
        }

        /// <summary>购买并装备换装项。返回 true 成功。</summary>
        public static bool BuyAndEquip(AvatarSlot slot)
        {
            EnsureInit();

            int cost = SlotCosts[(int)slot];
            if (!CurrencyController.HasAmount(CurrencyType.Coins, cost))
                return false;

            CurrencyController.Substract(CurrencyType.Coins, cost);
            CustomAnalytics.TrackCurrencySpend("coins", cost, "avatar_buy");

            save.OwnedSlots[(int)slot] = true;
            save.EquippedSlot = (int)slot;
            SaveController.MarkAsSaveIsRequired();

            OnAvatarChanged?.Invoke();
            return true;
        }

        public static void Equip(AvatarSlot slot)
        {
            EnsureInit();

            if (!IsOwned(slot))
                return;

            save.EquippedSlot = (int)slot;
            SaveController.MarkAsSaveIsRequired();
            OnAvatarChanged?.Invoke();
        }

        // ------------------------------------------------------------------ 称号

        public static int GetUnlockedTitleCount()
        {
            EnsureInit();
            return save.UnlockedTitles;
        }

        public static bool IsTitleUnlocked(int index)
        {
            EnsureInit();
            return (save.UnlockedTitles & (1 << index)) != 0;
        }

        /// <summary>刷新称号解锁状态（由达成条件触发，如配方全解封/货架满）。</summary>
        public static void RefreshTitles()
        {
            EnsureInit();

            // 绝味蛋糕师：全部配方已解封
            if (!IsTitleUnlocked(0) && AllRecipesUnlocked())
                UnlockTitle(0);

            // 扩张大师：货架满格
            if (!IsTitleUnlocked(1) && ShopController.Save != null &&
                ShopController.Save.UnlockedShelfCount >= ShopController.Config.MaxShelfCount)
                UnlockTitle(1);

            // 甜品收藏家：拥有全部蛋糕类型
            if (!IsTitleUnlocked(2) && AllCakeTypesOwned())
                UnlockTitle(2);
        }

        private static void UnlockTitle(int index)
        {
            save.UnlockedTitles |= (1 << index);
            SaveController.MarkAsSaveIsRequired();
            OnAvatarChanged?.Invoke();
        }

        private static bool AllRecipesUnlocked()
        {
            RecipeDefinition[] recipes = RecipeController.Recipes;
            if (recipes == null || recipes.Length == 0)
                return false;

            for (int i = 0; i < recipes.Length; i++)
            {
                if (!RecipeController.IsUnlocked(recipes[i].Id))
                    return false;
            }

            return true;
        }

        private static bool AllCakeTypesOwned()
        {
            CakeCatalog catalog = ShopController.Catalog;
            if (catalog == null || catalog.Count == 0)
                return false;

            System.Collections.Generic.HashSet<string> owned = new System.Collections.Generic.HashSet<string>();
            var cakes = ShopController.Save.Cakes;
            if (cakes == null)
                return false;

            for (int i = 0; i < cakes.Count; i++)
                owned.Add(cakes[i].DefinitionId);

            for (int i = 0; i < catalog.Count; i++)
            {
                if (!owned.Contains(catalog.GetByIndex(i).Id))
                    return false;
            }

            return true;
        }

        // ------------------------------------------------------------------ 展馆

        /// <summary>展馆蛋糕数量（拥有的不同蛋糕类型数）。</summary>
        public static int GetGalleryCount()
        {
            EnsureInit();

            System.Collections.Generic.HashSet<string> owned = new System.Collections.Generic.HashSet<string>();
            var cakes = ShopController.Save.Cakes;
            if (cakes != null)
            {
                for (int i = 0; i < cakes.Count; i++)
                    owned.Add(cakes[i].DefinitionId);
            }

            return owned.Count;
        }

        private static void EnsureInit()
        {
            if (!isInitialized)
                Init();
        }
    }
}
