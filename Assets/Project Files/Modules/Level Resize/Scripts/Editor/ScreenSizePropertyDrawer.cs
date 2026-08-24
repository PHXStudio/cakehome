using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomPropertyDrawer(typeof(LevelSizeController.ScreenSize))]
    public class ScreenSizePropertyDrawer : PropertyDrawer
    {
        private static Dictionary<string, PropertyData> propertyData = new Dictionary<string, PropertyData>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = position.x;
            float y = position.y;
            float width = position.width;
            float height = EditorGUIUtility.singleLineHeight;

            PropertyData data = GetPropertyData(property);

            Rect rect = new Rect(x, y, width, height);
            property.isExpanded = EditorGUI.Foldout(new Rect(x, y, width, height), property.isExpanded, label, true);

            if (property.isExpanded)
            {
                rect.y += height + 2; // Add vertical offset

                EditorGUI.indentLevel++;

                foreach (string propertyName in data.CachedProperties)
                {
                    SerializedProperty subProperty = property.FindPropertyRelative(propertyName);

                    EditorGUI.PropertyField(rect, subProperty, true);

                    rect.y += EditorGUI.GetPropertyHeight(subProperty, true) + 2; // Add vertical offset
                }

                rect.x += 12;
                rect.width -= 12;

                if (GUI.Button(rect, "Apply"))
                {
                    LevelSizeControllerEditor.ApplyScreenSize(property);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label, false);

            if (property.isExpanded)
            {
                PropertyData data = GetPropertyData(property);

                // Add properties height
                foreach (var propertyName in data.CachedProperties)
                {
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(propertyName), true) + 2;
                }

                // Add button height
                height += EditorGUIUtility.singleLineHeight + 2;

                return height;
            }

            return height;
        }

        private PropertyData GetPropertyData(SerializedProperty property)
        {
            string propertyPath = property.propertyPath;
            if (!propertyData.TryGetValue(propertyPath, out PropertyData data))
            {
                data = new PropertyData(property);
                propertyData.Add(propertyPath, data);
            }

            return data;
        }

        private class PropertyData
        {
            public string[] CachedProperties { get; private set; }

            public PropertyData(SerializedProperty property)
            {
                CachedProperties = CacheProperties(property);
            }

            private string[] CacheProperties(SerializedProperty property)
            {
                List<string> properties = new List<string>();

                SerializedProperty iterator = property.Copy();
                SerializedProperty end = iterator.GetEndProperty();

                iterator.NextVisible(enterChildren: true);

                int propertyDepth = iterator.depth;

                do
                {
                    if (SerializedProperty.EqualContents(iterator, end))
                        break;

                    if (propertyDepth != iterator.depth)
                        continue;

                    properties.Add(iterator.name);
                }
                while (iterator.NextVisible(enterChildren: true));

                return properties.ToArray();
            }
        }
    }
}
