using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Cake Catalog", menuName = "Data/Shop/Cake Catalog")]
    public class CakeCatalog : ScriptableObject
    {
        [SerializeField] CakeDefinition[] cakes;

        public CakeDefinition[] Cakes => cakes;

        public CakeDefinition GetById(string id)
        {
            if (cakes == null || string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < cakes.Length; i++)
            {
                if (cakes[i] != null && cakes[i].Id == id)
                    return cakes[i];
            }

            return null;
        }

        public CakeDefinition GetByIndex(int index)
        {
            if (cakes == null || index < 0 || index >= cakes.Length)
                return null;

            return cakes[index];
        }

        public int Count => cakes != null ? cakes.Length : 0;

        public List<CakeElement> CollectUniqueElements()
        {
            List<CakeElement> result = new List<CakeElement>();
            if (cakes == null)
                return result;

            for (int i = 0; i < cakes.Length; i++)
            {
                CakeDefinition cake = cakes[i];
                if (cake == null || cake.Elements == null)
                    continue;

                for (int e = 0; e < cake.Elements.Length; e++)
                {
                    CakeElement element = cake.Elements[e];
                    if (!result.Contains(element))
                        result.Add(element);
                }
            }

            return result;
        }

        public static CakeCatalog CreateDefaultRuntimeCatalog()
        {
            CakeCatalog catalog = CreateInstance<CakeCatalog>();
            catalog.cakes = BuildDefaultCakes();
            return catalog;
        }

        public static CakeDefinition[] BuildDefaultCakes()
        {
            return new[]
            {
                new CakeDefinition("strawberry_shortcake", "草莓奶油蛋糕", new[] { CakeElement.Strawberry, CakeElement.Cream }, 12f, new Color(1f, 0.55f, 0.65f)),
                new CakeDefinition("choco_tower", "巧克力塔", new[] { CakeElement.Chocolate, CakeElement.Cream }, 14f, new Color(0.45f, 0.28f, 0.18f)),
                new CakeDefinition("matcha_roll", "抹茶卷", new[] { CakeElement.Matcha, CakeElement.Cream }, 11f, new Color(0.55f, 0.75f, 0.45f)),
                new CakeDefinition("berry_parfait", "蓝莓芭菲", new[] { CakeElement.Blueberry, CakeElement.Cream }, 13f, new Color(0.45f, 0.5f, 0.9f)),
                new CakeDefinition("lemon_tart", "柠檬塔", new[] { CakeElement.Lemon, CakeElement.Cream }, 10f, new Color(1f, 0.9f, 0.4f)),
                new CakeDefinition("triple_berry", "莓果三重奏", new[] { CakeElement.Strawberry, CakeElement.Blueberry }, 16f, new Color(0.85f, 0.35f, 0.55f)),
            };
        }
    }
}
