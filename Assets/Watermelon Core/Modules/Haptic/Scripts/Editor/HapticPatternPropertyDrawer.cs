using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Draws a read-only timeline preview of a <see cref="HapticPattern"/> in the Inspector.
    /// Click "Edit" to open the full <see cref="HapticPatternEditorWindow"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(HapticPattern))]
    public class HapticPatternPropertyDrawer : PropertyDrawer
    {
        private const float PREVIEW_H = 56f;
        private const float SPACING   = 2f;
        private const float PADDING   = 8f;

        private static readonly Color COL_BG = new(0.13f, 0.13f, 0.13f);

        private float LineH => EditorGUIUtility.singleLineHeight;
        private float RowH  => LineH + SPACING;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h = RowH;

            if (property.isExpanded)
                h += PREVIEW_H + SPACING;

            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty idProp      = property.FindPropertyRelative("id");
            SerializedProperty patternProp = property.FindPropertyRelative("pattern");

            // ── Header row: foldout + Edit button ────────────────────────────
            const float EDIT_BTN_W = 38f;

            Rect foldoutRect = new(position.x, position.y, position.width - EDIT_BTN_W - 4f, LineH);
            Rect editBtnRect = new(position.xMax - EDIT_BTN_W, position.y, EDIT_BTN_W, LineH);

            string preview = string.IsNullOrEmpty(idProp.stringValue) ? "no id" : idProp.stringValue;
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, $"{label.text}  [{preview}]", true);

            if (GUI.Button(editBtnRect, "Edit", EditorStyles.miniButton))
                HapticPatternEditorWindow.Open(property.serializedObject, property.propertyPath);

            // ── Preview canvas ────────────────────────────────────────────────
            if (property.isExpanded)
            {
                Rect canvas = new(position.x, position.y + RowH, position.width, PREVIEW_H);
                DrawPreview(canvas, patternProp);
            }

            EditorGUI.EndProperty();
        }

        private void DrawPreview(Rect canvas, SerializedProperty eventsProp)
        {
            EditorGUI.DrawRect(canvas, COL_BG);

            int count = eventsProp.arraySize;
            if (count == 0)
            {
                GUI.Label(canvas, "Empty pattern", new GUIStyle(EditorStyles.centeredGreyMiniLabel));
                return;
            }

            // Scale to fit all events
            float totalDuration = 0f;
            for (int i = 0; i < count; i++)
            {
                SerializedProperty e = eventsProp.GetArrayElementAtIndex(i);
                float end = e.FindPropertyRelative("StartTime").floatValue
                          + e.FindPropertyRelative("Duration").floatValue;
                if (end > totalDuration) totalDuration = end;
            }

            if (totalDuration <= 0f) return;

            float maxH     = canvas.height - PADDING * 2f;
            float baseline = canvas.y + canvas.height - PADDING;
            float pps      = (canvas.width - PADDING * 2f) / totalDuration;
            float originX  = canvas.x + PADDING;

            for (int i = 0; i < count; i++)
            {
                SerializedProperty e = eventsProp.GetArrayElementAtIndex(i);
                float startTime = e.FindPropertyRelative("StartTime").floatValue;
                float duration  = e.FindPropertyRelative("Duration").floatValue;
                float intensity = e.FindPropertyRelative("Intensity").floatValue;
                float sharpness = e.FindPropertyRelative("Sharpness").floatValue;

                float ex = originX + startTime * pps;
                float ew = Mathf.Max(duration * pps, 2f);
                float eh = Mathf.Max(intensity * maxH, 2f);
                float ey = baseline - eh;

                HapticPatternEditorWindow.DrawEventVisual(ex, ey, ew, eh, baseline, sharpness, false);
            }
        }
    }
}
