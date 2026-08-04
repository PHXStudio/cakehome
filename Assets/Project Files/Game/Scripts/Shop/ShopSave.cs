using System.Collections.Generic;

namespace Watermelon
{
    [System.Serializable]
    public class ShopSave : ISaveObject
    {
        public const int EMPTY_SLOT = -1;

        public List<OwnedCake> Cakes = new List<OwnedCake>();
        public int[] ShelfSlots = new int[0];
        public int UnlockedShelfCount = 4;
        public long LastSettleBinary;
        public double PendingCredits;
        public int DailyThemeElement = 0;
        public double DailyThemeDayUnix;
        public int NextInstanceCounter = 1;

        public void Flush()
        {
        }

        public void EnsureShelfSize(int maxShelves)
        {
            if (ShelfSlots != null && ShelfSlots.Length == maxShelves)
                return;

            int[] next = new int[maxShelves];
            for (int i = 0; i < maxShelves; i++)
                next[i] = EMPTY_SLOT;

            if (ShelfSlots != null)
            {
                int copy = System.Math.Min(ShelfSlots.Length, maxShelves);
                for (int i = 0; i < copy; i++)
                    next[i] = ShelfSlots[i];
            }

            ShelfSlots = next;
        }
    }
}
