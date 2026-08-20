using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 配方系统（规划 M2 经济管道）：
    /// 消消乐消除时按概率掉落配方碎片 → 攒够碎片 + 支付积分解封配方 →
    /// 门店对应蛋糕 2× 售价回收积分，打通「进局→赚分→解封配方」闭环。
    /// 配方与 CakeCatalog 蛋糕一一对应（按蛋糕 Id）。
    /// </summary>
    public static class RecipeController
    {
        private const string SAVE_NAME = "recipe";
        private const string CONFIG_PATH = "Recipe/Recipe Config";

        private static RecipeConfig config;
        private static RecipeSave save;
        private static bool isInitialized;
        private static int currentLevelRecipeIndex = -1;

        public static event Action<string> OnFragmentCollected;   // recipeId
        public static event Action<string> OnRecipeUnlocked;      // recipeId

        public static RecipeSave Save => save;
        public static bool IsInitialized => isInitialized;
        public static int FragmentsToUnlock => config != null ? config.FragmentsToUnlock : 10;
        public static int UnlockCost => config != null ? config.UnlockCost : 200;
        public static float DropChance => config != null ? config.DropChance : 0.15f;
        public static float UnlockedMultiplier => config != null ? config.UnlockedMultiplier : 2f;

        // ------------------------------------------------------------------ 配方定义

        public static RecipeDefinition[] Recipes { get; private set; }

        public static void Init()
        {
            if (isInitialized)
                return;

            isInitialized = true;

            // 数值配置：从 Resources 加载，缺失时用代码兜底默认值
            config = Resources.Load<RecipeConfig>(CONFIG_PATH);
            if (config == null)
                config = RecipeConfig.CreateDefaultRuntimeConfig();

            // 配方 = 门店蛋糕目录（6 种蛋糕即 6 个配方）
            BuildRecipes();

            save = SaveController.GetSaveObject<RecipeSave>(SAVE_NAME);
            save.EnsureInit(Recipes.Length);
        }

        private static void BuildRecipes()
        {
            CakeCatalog catalog = ShopController.Catalog;
            if (catalog == null || catalog.Count == 0)
            {
                Recipes = new RecipeDefinition[0];
                return;
            }

            Recipes = new RecipeDefinition[catalog.Count];
            for (int i = 0; i < catalog.Count; i++)
            {
                CakeDefinition cake = catalog.GetByIndex(i);
                Recipes[i] = new RecipeDefinition(cake.Id, cake.DisplayName);
            }
        }

        /// <summary>当前关卡的关联配方索引（按关卡号循环选择，保证每关可收集的配方确定且分散）。</summary>
        public static void SetLevelContext(int levelIndex)
        {
            if (Recipes == null || Recipes.Length == 0)
            {
                currentLevelRecipeIndex = -1;
                return;
            }

            currentLevelRecipeIndex = levelIndex % Recipes.Length;
        }

        // ------------------------------------------------------------------ 碎片

        /// <summary>消除事件回调：按概率掉落当前关卡关联配方的碎片。</summary>
        public static void OnMatchCombined(List<ISlotable> match)
        {
            if (currentLevelRecipeIndex < 0 || currentLevelRecipeIndex >= Recipes.Length)
                return;

            if (UnityEngine.Random.value > DropChance)
                return;

            AddFragment(Recipes[currentLevelRecipeIndex].Id);
        }

        public static void AddFragment(string recipeId)
        {
            EnsureInit();

            int index = FindRecipeIndex(recipeId);
            if (index < 0)
                return;

            save.Fragments[index] = Mathf.Min(save.Fragments[index] + 1, FragmentsToUnlock);
            SaveController.MarkAsSaveIsRequired();

            CustomAnalytics.TrackFragmentCollect(recipeId, save.Fragments[index]);
            OnFragmentCollected?.Invoke(recipeId);
        }

        public static int GetFragments(string recipeId)
        {
            EnsureInit();

            int index = FindRecipeIndex(recipeId);
            if (index < 0)
                return 0;

            return save.Fragments[index];
        }

        public static int GetFragmentsByIndex(int recipeIndex)
        {
            EnsureInit();

            if (recipeIndex < 0 || recipeIndex >= save.Fragments.Length)
                return 0;

            return save.Fragments[recipeIndex];
        }

        public static bool IsUnlocked(string recipeId)
        {
            EnsureInit();

            return save.Unlocked != null && save.Unlocked.Contains(recipeId);
        }

        public static bool IsUnlockedByIndex(int recipeIndex)
        {
            EnsureInit();

            if (recipeIndex < 0 || recipeIndex >= Recipes.Length)
                return false;

            return IsUnlocked(Recipes[recipeIndex].Id);
        }

        /// <summary>解封条件满足（碎片够且未解封）。</summary>
        public static bool CanUnlock(string recipeId)
        {
            EnsureInit();

            if (IsUnlocked(recipeId))
                return false;

            return GetFragments(recipeId) >= FragmentsToUnlock;
        }

        /// <summary>支付积分解封配方。成功返回 true。</summary>
        public static bool TryUnlock(string recipeId)
        {
            EnsureInit();

            if (!CanUnlock(recipeId))
                return false;

            if (!CurrencyController.HasAmount(CurrencyType.Coins, UnlockCost))
                return false;

            CurrencyController.Substract(CurrencyType.Coins, UnlockCost);
            CustomAnalytics.TrackCurrencySpend("coins", UnlockCost, "recipe_unlock");

            save.Unlocked.Add(recipeId);
            SaveController.MarkAsSaveIsRequired();

            OnRecipeUnlocked?.Invoke(recipeId);
            return true;
        }

        /// <summary>门店倍率：已解封配方蛋糕按配置倍率（默认 2×）售价。</summary>
        public static float GetRecipeMultiplier(string cakeId)
        {
            return IsUnlocked(cakeId) ? UnlockedMultiplier : 1f;
        }

        // ------------------------------------------------------------------ 工具

        private static int FindRecipeIndex(string recipeId)
        {
            if (Recipes == null)
                return -1;

            for (int i = 0; i < Recipes.Length; i++)
            {
                if (Recipes[i].Id == recipeId)
                    return i;
            }

            return -1;
        }

        private static void EnsureInit()
        {
            if (!isInitialized)
                Init();
        }
    }

    /// <summary>单个配方定义。</summary>
    [System.Serializable]
    public class RecipeDefinition
    {
        public string Id;
        public string DisplayName;

        public RecipeDefinition(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }
    }
}
