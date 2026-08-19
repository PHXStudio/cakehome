using System.Collections.Generic;

namespace Watermelon
{
    /// <summary>
    /// 配方存档：碎片计数（按配方索引）+ 已解封配方 Id 列表。
    /// </summary>
    [System.Serializable]
    public class RecipeSave : ISaveObject
    {
        public int[] Fragments = new int[0];
        public List<string> Unlocked = new List<string>();

        public void EnsureInit(int recipeCount)
        {
            if (Fragments == null || Fragments.Length != recipeCount)
            {
                int[] next = new int[recipeCount];
                if (Fragments != null)
                {
                    int copy = System.Math.Min(Fragments.Length, recipeCount);
                    for (int i = 0; i < copy; i++)
                        next[i] = Fragments[i];
                }

                Fragments = next;
            }

            if (Unlocked == null)
                Unlocked = new List<string>();
        }

        public void Flush()
        {
        }
    }
}
