using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomPropertyDrawer(typeof(MergeTypePickerAttribute))]
    public class MergeTypePickerPropertyDrawer : PropertyDrawer
    {
        private static MergeDatabase cachedDatabase;
        private static Dictionary<string, string> buttonTextCache = new Dictionary<string, string>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position, "Incorrect property type!", MessageType.Error);

                EditorGUI.EndProperty();

                return;
            }

            position = EditorGUI.PrefixLabel(position, label);

            string typeId = property.stringValue;

            if (GUI.Button(position, GetButtonText(property.propertyPath, typeId), EditorStyles.popup))
            {
                MergeTypePickerWindow.PickType(property);
            }

            EditorGUI.EndProperty();
        }

        private static string GetButtonText(string propertyPath, string typeId)
        {
            string cacheKey = propertyPath + "|" + typeId;
            if (!buttonTextCache.TryGetValue(cacheKey, out string buttonText))
            {
                buttonText = BuildButtonText(typeId);
                buttonTextCache[cacheKey] = buttonText;
            }

            return buttonText;
        }

        private static string BuildButtonText(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
                return "<None>";

            MergeItemData itemData = FindItemData(typeId);
            if (itemData == null)
                return $"Missing: {typeId}";

            string displayName = itemData.GetGradeData(1)?.DisplayName;
            return string.IsNullOrEmpty(displayName) ? typeId : $"{displayName} ({typeId})";
        }

        private static MergeDatabase GetDatabase()
        {
            if (cachedDatabase == null)
                cachedDatabase = EditorUtils.GetAsset<MergeDatabase>();

            return cachedDatabase;
        }

        private static MergeItemData FindItemData(string typeId)
        {
            MergeDatabase database = GetDatabase();
            if (database == null)
            {
                Debug.LogError("[MergeTypePickerPropertyDrawer] FindItemData: null returned, MergeDatabase asset not found.");

                return null;
            }

            foreach (MergeItemData item in database.Items)
            {
                if (item.TypeId == typeId)
                    return item;
            }

            Debug.LogError($"[MergeTypePickerPropertyDrawer] FindItemData: null returned, no item found with typeId: {typeId}");

            return null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
