#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// 创建动态中文字体（微软雅黑）TMP 资产，并设为 FredokaOne 的 fallback，
    /// 解决 FredokaOne 不含中文字形导致的方框（□）显示问题。
    /// 菜单：Actions/Fonts/Setup Chinese Fallback
    /// </summary>
    public static class ChineseFontSetup
    {
        private const string FONT_PATH = "Assets/Project Files/Game/Fonts/FredokaOne/FredokaOne 50/FredokaOne 50.asset";
        private const string CHINESE_FONT_PATH = "Assets/Project Files/Game/Fonts/ChineseDynamic.asset";
        private const string CHINESE_SOURCE_FONT_PATH = "Assets/Project Files/Game/Fonts/Resources/ChineseFont.ttf";

        [MenuItem("Actions/Fonts/Setup Chinese Fallback")]
        public static void Setup()
        {
            // 1. Create (or load) dynamic Chinese TMP font from asset font (黑体 simhei)
            TMP_FontAsset chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CHINESE_FONT_PATH);
            if (chineseFont == null)
            {
                Font assetFont = AssetDatabase.LoadAssetAtPath<Font>(CHINESE_SOURCE_FONT_PATH);
                if (assetFont == null)
                {
                    Debug.LogError($"[Fonts] Chinese font asset missing: {CHINESE_SOURCE_FONT_PATH}");
                    return;
                }

                chineseFont = TMP_FontAsset.CreateFontAsset(
                    assetFont, 90, 9,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    1024, 1024,
                    TMPro.AtlasPopulationMode.Dynamic, true);
                AssetDatabase.CreateAsset(chineseFont, CHINESE_FONT_PATH);
                Debug.Log($"[Fonts] Created Chinese font: {CHINESE_FONT_PATH}");
            }

            // 2. Add as fallback to FredokaOne
            TMP_FontAsset fredoka = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
            if (fredoka == null)
            {
                Debug.LogError("[Fonts] FredokaOne font not found.");
                return;
            }

            if (fredoka.fallbackFontAssetTable == null || !fredoka.fallbackFontAssetTable.Contains(chineseFont))
            {
                SerializedObject so = new SerializedObject(fredoka);
                SerializedProperty fallback = so.FindProperty("m_FallbackFontAssetTable");
                bool exists = false;
                for (int i = 0; i < fallback.arraySize; i++)
                {
                    if (fallback.GetArrayElementAtIndex(i).objectReferenceValue == chineseFont)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    fallback.InsertArrayElementAtIndex(fallback.arraySize);
                    fallback.GetArrayElementAtIndex(fallback.arraySize - 1).objectReferenceValue = chineseFont;
                }

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(fredoka);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[Fonts] Chinese fallback attached to FredokaOne.");
            Selection.activeObject = chineseFont;
        }
    }
}
#endif
