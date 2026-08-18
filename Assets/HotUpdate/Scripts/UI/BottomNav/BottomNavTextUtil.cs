using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public static class BottomNavTextUtil
    {
        private static Font chineseFont;

        public static Font GetChineseFont()
        {
            if (chineseFont != null)
                return chineseFont;

            string[] candidates =
            {
                "PingFang SC",
                "Heiti SC",
                "STHeiti",
                "Microsoft YaHei",
                "Noto Sans CJK SC",
                "Arial Unicode MS",
                "Arial"
            };

            chineseFont = Font.CreateDynamicFontFromOSFont(candidates, 40);
            return chineseFont;
        }

        public static void Apply(Text text, string value, int fontSize)
        {
            if (text == null)
                return;

            text.font = GetChineseFont();
            text.fontSize = fontSize;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }
}
