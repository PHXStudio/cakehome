using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomPropertyDrawer(typeof(MergeGradeData))]
    public class MergeGradeDataDrawer : PropertyDrawer
    {
        const float INDENT = 12f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty customShadow = property.FindPropertyRelative("customShadow");

            float vpad = EditorGUIUtility.standardVerticalSpacing;
            float y = position.y;

            DrawField(property, "sprite", position, ref y, vpad);
            DrawField(property, "displayName", position, ref y, vpad);
            DrawField(property, "description", position, ref y, vpad);
            DrawField(property, "config", position, ref y, vpad);
            DrawField(property, "customShadow", position, ref y, vpad);

            if (customShadow.boolValue)
            {
                DrawField(property, "customShadowSprite", position, ref y, vpad, INDENT);
                DrawField(property, "shadowPositionOffset", position, ref y, vpad, INDENT);
                DrawField(property, "customShadowScale", position, ref y, vpad, INDENT);
            }

            EditorGUI.EndProperty();
        }

        static void DrawField(SerializedProperty parent, string relativeName, Rect position, ref float y, float vpad, float indent = 0f)
        {
            SerializedProperty prop = parent.FindPropertyRelative(relativeName);
            float h = EditorGUI.GetPropertyHeight(prop, true);
            Rect rect = new Rect(position.x + indent, y, position.width - indent, h);
            EditorGUI.PropertyField(rect, prop, true);
            y += h + vpad;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float vpad = EditorGUIUtility.standardVerticalSpacing;

            float h = FieldHeight(property, "sprite", vpad)
                    + FieldHeight(property, "displayName", vpad)
                    + FieldHeight(property, "description", vpad)
                    + FieldHeight(property, "config", vpad)
                    + FieldHeight(property, "customShadow", vpad);

            if (property.FindPropertyRelative("customShadow").boolValue)
            {
                h += FieldHeight(property, "customShadowSprite", vpad)
                   + FieldHeight(property, "shadowPositionOffset", vpad)
                   + FieldHeight(property, "customShadowScale", vpad);
            }

            return h - vpad;
        }

        static float FieldHeight(SerializedProperty parent, string relativeName, float vpad) =>
            EditorGUI.GetPropertyHeight(parent.FindPropertyRelative(relativeName), true) + vpad;
    }
}
