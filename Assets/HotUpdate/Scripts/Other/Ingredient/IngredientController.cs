using System;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 原料采购（规划 TAB1）：面粉/车厘子/彩虹糖针，积分购买获得持续增益
    /// （临时提升货架产出倍率）。消耗富余积分，催回访采购。
    /// </summary>
    public static class IngredientController
    {
        private const string SAVE_NAME = "ingredient";

        private static IngredientSave save;
        private static bool isInitialized;

        public static event Action OnIngredientChanged;

        public enum IngredientType
        {
            Flour = 0,        // 面粉：+25% 产出，30 分钟
            Cherry = 1,       // 车厘子：+50% 产出，30 分钟
            Sprinkles = 2,    // 彩虹糖针：+100% 产出，30 分钟
        }

        public static string[] Names = { "面粉", "车厘子", "彩虹糖针" };
        public static int[] Costs = { 50, 100, 200 };
        public static float[] Multipliers = { 1.25f, 1.5f, 2f };
        public static float[] Durations = { 30 * 60f, 30 * 60f, 30 * 60f }; // 秒

        public static IngredientSave Save => save;
        public static bool IsInitialized => isInitialized;

        public static void Init()
        {
            if (isInitialized)
                return;

            isInitialized = true;
            save = SaveController.GetSaveObject<IngredientSave>(SAVE_NAME);
            save.EnsureInit(Names.Length);
        }

        /// <summary>购买原料激活增益。返回 true 成功。</summary>
        public static bool Buy(IngredientType type)
        {
            EnsureInit();

            int cost = Costs[(int)type];
            if (!CurrencyController.HasAmount(CurrencyType.Coins, cost))
                return false;

            CurrencyController.Substract(CurrencyType.Coins, cost);
            CustomAnalytics.TrackCurrencySpend("coins", cost, "ingredient_buy");

            // 激活增益（叠加时长）：解码当前过期时间与 now 比较（避免 ToBinary 负数符号比较问题）
            DateTime existing = DateTime.FromBinary(save.ActiveUntil[(int)type]);
            DateTime baseTime = existing > DateTime.Now ? existing : DateTime.Now;
            save.ActiveUntil[(int)type] = baseTime.AddSeconds(Durations[(int)type]).ToBinary();
            SaveController.MarkAsSaveIsRequired();

            OnIngredientChanged?.Invoke();
            return true;
        }

        /// <summary>当前是否生效。</summary>
        public static bool IsActive(IngredientType type)
        {
            EnsureInit();

            DateTime until = DateTime.FromBinary(save.ActiveUntil[(int)type]);
            return until > DateTime.Now;
        }

        /// <summary>剩余秒数。</summary>
        public static double GetRemainingSeconds(IngredientType type)
        {
            EnsureInit();

            DateTime until = DateTime.FromBinary(save.ActiveUntil[(int)type]);
            double remain = (until - DateTime.Now).TotalSeconds;
            return Math.Max(0, remain);
        }

        /// <summary>总产出倍率（所有生效原料叠加）。</summary>
        public static float GetTotalMultiplier()
        {
            EnsureInit();

            float total = 1f;
            for (int i = 0; i < Names.Length; i++)
            {
                if (IsActive((IngredientType)i))
                    total *= Multipliers[i];
            }

            return total;
        }

        private static void EnsureInit()
        {
            if (!isInitialized)
                Init();
        }
    }
}
