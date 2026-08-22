using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomPropertyDrawer(typeof(LevelProgress))]
    public class LevelProgressPropertyDrawer : PropertyDrawer
    {
        private const float LEVEL_FIELD_WIDTH = 40f;
        private const float SPACING = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty levelProp    = property.FindPropertyRelative("level");
            SerializedProperty progressProp = property.FindPropertyRelative("progress");

            Rect contentRect = EditorGUI.PrefixLabel(position, label);

            Rect levelRect = new Rect(contentRect.x, contentRect.y, LEVEL_FIELD_WIDTH, contentRect.height);
            Rect sliderRect = new Rect(levelRect.xMax + SPACING, contentRect.y, contentRect.width - LEVEL_FIELD_WIDTH - SPACING, contentRect.height);

            EditorGUI.BeginChangeCheck();

            int level = EditorGUI.IntField(levelRect, levelProp.intValue);
            float progress = EditorGUI.Slider(sliderRect, progressProp.floatValue, 0f, 1f);

            if (EditorGUI.EndChangeCheck())
            {
                levelProp.intValue = level;
                progressProp.floatValue = progress;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
