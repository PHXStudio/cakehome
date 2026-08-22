using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    // Layout (EditorGUILayout) version of the type/grade picker used by the OrderItem /
    // SpawnEntry property drawers — for custom Editors that draw raw typeId + grade fields
    // (debug spawners) and can't rely on a [CustomPropertyDrawer].
    public static class MergeItemPickerGUI
    {
        private const float PREVIEW_SIZE = 40f;

        private static MergeDatabase cachedDatabase;

        public static void DrawTypeGradeField(string label, SerializedProperty typeIdProp, SerializedProperty gradeProp)
        {
            float height = Mathf.Max(EditorGUIUtility.singleLineHeight * 2 + 2, PREVIEW_SIZE);
            Rect position = EditorGUILayout.GetControlRect(true, height);

            MergeItemData itemData = FindItemData(typeIdProp.stringValue);
            MergeGradeData gradeData = itemData?.GetGradeData(gradeProp.intValue);

            Rect contentRect = EditorGUI.PrefixLabel(position, new GUIContent(label));

            Rect previewRect = new Rect(contentRect.x, contentRect.y, PREVIEW_SIZE, Mathf.Min(PREVIEW_SIZE, contentRect.height));
            DrawPreview(previewRect, gradeData);

            Rect buttonsRect = new Rect(contentRect.x + PREVIEW_SIZE + 4, contentRect.y, contentRect.width - PREVIEW_SIZE - 4, contentRect.height);

            Rect typeButtonRect = new Rect(buttonsRect.x, buttonsRect.y, buttonsRect.width, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(typeButtonRect, GetTypeButtonText(typeIdProp.stringValue, itemData)))
            {
                MergeTypePickerWindow.PickType(typeIdProp, gradeProp);
            }

            Rect gradeButtonRect = new Rect(buttonsRect.x, typeButtonRect.y + EditorGUIUtility.singleLineHeight + 2, buttonsRect.width, EditorGUIUtility.singleLineHeight);
            using (new EditorGUI.DisabledScope(disabled: itemData == null))
            {
                if (GUI.Button(gradeButtonRect, GetGradeButtonText(gradeProp.intValue, gradeData)))
                {
                    MergeTypePickerWindow.PickGrade(gradeProp, itemData);
                }
            }
        }

        private static string GetTypeButtonText(string typeId, MergeItemData itemData)
        {
            if (string.IsNullOrEmpty(typeId))
                return "<None>";

            if (itemData == null)
                return $"Missing: {typeId}";

            string displayName = itemData.GetGradeData(1)?.DisplayName;

            return string.IsNullOrEmpty(displayName) ? typeId : $"{displayName} ({typeId})";
        }

        private static string GetGradeButtonText(int grade, MergeGradeData gradeData)
        {
            string displayName = gradeData?.DisplayName;

            return string.IsNullOrEmpty(displayName) ? $"Grade {grade}" : $"Grade {grade} - {displayName}";
        }

        private static void DrawPreview(Rect rect, MergeGradeData gradeData)
        {
            GUI.Box(rect, GUIContent.none);

            Texture2D previewTexture = gradeData?.Sprite != null ? AssetPreview.GetAssetPreview(gradeData.Sprite) : null;
            if (previewTexture != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), previewTexture);
            }
            else
            {
                GUI.DrawTexture(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), EditorCustomStyles.GetMissingIcon());
            }
        }

        private static MergeItemData FindItemData(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
            {
                Debug.LogError("[MergeItemPickerGUI] FindItemData: null returned, typeId is null or empty.");

                return null;
            }

            if (cachedDatabase == null)
                cachedDatabase = EditorUtils.GetAsset<MergeDatabase>();

            if (cachedDatabase == null)
            {
                Debug.LogError("[MergeItemPickerGUI] FindItemData: null returned, MergeDatabase asset not found.");

                return null;
            }

            foreach (MergeItemData item in cachedDatabase.Items)
            {
                if (item.TypeId == typeId)
                    return item;
            }

            Debug.LogError($"[MergeItemPickerGUI] FindItemData: null returned, no item found with typeId: {typeId}");

            return null;
        }
    }
}
