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

        public static string[] Titles = { "订单达人", "装修大师", "区域开拓者" };

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

        /// <summary>刷新称号解锁状态（由达成条件触发，如订单数/装修升级/区域解锁）。</summary>
        public static void RefreshTitles()
        {
            EnsureInit();

            // 订单达人：累计完成 50 个顾客订单
            if (!IsTitleUnlocked(0) && MergeStatsController.OrdersCompleted >= 50)
                UnlockTitle(0);

            // 装修大师：店铺建筑累计升级 10 级
            if (!IsTitleUnlocked(1) && BuildingController.TotalUpgrades >= 10)
                UnlockTitle(1);

            // 区域开拓者：全部区域已解锁
            if (!IsTitleUnlocked(2) && AllZonesUnlocked())
                UnlockTitle(2);
        }

        private static void UnlockTitle(int index)
        {
            save.UnlockedTitles |= (1 << index);
            SaveController.MarkAsSaveIsRequired();
            OnAvatarChanged?.Invoke();
        }

        private static bool AllZonesUnlocked()
        {
            ZoneData[] zones = ZoneController.AllZones;
            if (zones == null || zones.Length == 0)
                return false;

            for (int i = 0; i < zones.Length; i++)
            {
                if (!ZoneController.IsUnlocked(zones[i]))
                    return false;
            }

            return true;
        }

        // ------------------------------------------------------------------ 展馆

        /// <summary>个人页统计：累计完成订单数。</summary>
        public static int GetOrdersCompletedCount()
        {
            return MergeStatsController.OrdersCompleted;
        }

        private static void EnsureInit()
        {
            if (!isInitialized)
                Init();
        }
    }
}
