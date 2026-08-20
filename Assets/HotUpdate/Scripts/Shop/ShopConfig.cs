using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Shop Config", menuName = "Data/Shop/Shop Config")]
    public class ShopConfig : ScriptableObject
    {
        [SerializeField] int initialShelfCount = 4;
        [SerializeField] int maxShelfCount = 8;
        [SerializeField] int[] expandCosts = { 100, 250, 500, 1000 };
        [SerializeField] float managerRecommendHours = 2f;
        [SerializeField] float managerRecommendMultiplier = 2f;
        [SerializeField] float hotThemeMultiplier = 2f;
        [SerializeField] float maxOfflineHours = 8f;
        [SerializeField] float defaultCreditsPerHour = 10f;

        // M3 保鲜折扣：上架后新鲜期内 1.0 倍，之后线性衰减至过期倍率（催回访）
        [SerializeField] float freshHours = 2f;
        [SerializeField] float staleMultiplier = 0.5f;
        [SerializeField] float fullyStaleHours = 8f;

        public int InitialShelfCount => initialShelfCount;
        public int MaxShelfCount => maxShelfCount;
        public float ManagerRecommendHours => managerRecommendHours;
        public float ManagerRecommendMultiplier => managerRecommendMultiplier;
        public float HotThemeMultiplier => hotThemeMultiplier;
        public float MaxOfflineHours => maxOfflineHours;
        public float DefaultCreditsPerHour => defaultCreditsPerHour;
        public float FreshHours => freshHours;
        public float StaleMultiplier => staleMultiplier;
        public float FullyStaleHours => fullyStaleHours;

        public int GetExpandCost(int unlockedShelfCount)
        {
            int index = unlockedShelfCount - initialShelfCount;
            if (expandCosts == null || expandCosts.Length == 0)
                return 100;

            if (index < 0)
                return expandCosts[0];

            if (index >= expandCosts.Length)
                return expandCosts[expandCosts.Length - 1];

            return expandCosts[index];
        }

        public static ShopConfig CreateDefaultRuntimeConfig()
        {
            return CreateInstance<ShopConfig>();
        }
    }
}
