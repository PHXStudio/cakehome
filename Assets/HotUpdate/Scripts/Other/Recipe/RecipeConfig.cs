using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 配方系统数值配置（可调，避免写死代码）。
    /// 放 Resources/Recipe/Recipe Config 下，由 RecipeController 运行时加载。
    /// </summary>
    [CreateAssetMenu(fileName = "Recipe Config", menuName = "Data/Shop/Recipe Config")]
    public class RecipeConfig : ScriptableObject
    {
        [SerializeField] int fragmentsToUnlock = 10;
        [SerializeField] int unlockCost = 200;
        [SerializeField] float dropChance = 0.15f;
        [SerializeField] float unlockedMultiplier = 2f;

        public int FragmentsToUnlock => fragmentsToUnlock;
        public int UnlockCost => unlockCost;
        public float DropChance => dropChance;
        public float UnlockedMultiplier => unlockedMultiplier;

        public static RecipeConfig CreateDefaultRuntimeConfig()
        {
            return CreateInstance<RecipeConfig>();
        }
    }
}
