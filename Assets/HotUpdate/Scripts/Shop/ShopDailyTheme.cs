using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public static class ShopDailyTheme
    {
        public static CakeElement CurrentTheme { get; private set; }

        public static string GetDisplayName(CakeElement element)
        {
            switch (element)
            {
                case CakeElement.Strawberry: return "草莓甜品";
                case CakeElement.Chocolate: return "巧克力甜品";
                case CakeElement.Matcha: return "抹茶甜品";
                case CakeElement.Blueberry: return "蓝莓甜品";
                case CakeElement.Lemon: return "柠檬甜品";
                case CakeElement.Cream: return "奶油甜品";
                default: return element.ToString();
            }
        }

        public static void Refresh(ShopSave save, CakeCatalog catalog)
        {
            double today = TimeUtils.GetCurrentDayUnixTimestamp();
            if (Math.Abs(save.DailyThemeDayUnix - today) < 0.5)
            {
                CurrentTheme = (CakeElement)save.DailyThemeElement;
                return;
            }

            List<CakeElement> pool = catalog != null ? catalog.CollectUniqueElements() : null;
            if (pool == null || pool.Count == 0)
            {
                CurrentTheme = CakeElement.Strawberry;
            }
            else
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                CurrentTheme = pool[index];
            }

            save.DailyThemeElement = (int)CurrentTheme;
            save.DailyThemeDayUnix = today;
            SaveController.MarkAsSaveIsRequired();
        }
    }
}
