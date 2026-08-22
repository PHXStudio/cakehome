using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomEditor(typeof(OverlayPreInitializer))]
    public class OverlayPreInitializerEditor : Editor
    {
        private static readonly GUIContent PANEL_FOUND_LABEL    = new GUIContent("Custom Panel");
        private static readonly GUIContent PANEL_NOT_FOUND_LABEL = new GUIContent("Dummy Panel");

        private SerializedProperty scriptProperty;

        private void OnEnable()
        {
            EditorCustomStyles.CheckStyles();

            scriptProperty = serializedObject.FindProperty("m_Script");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(scriptProperty);

            EditorGUILayout.Space(4);

            OverlayPreInitializer overlayInitializer = (OverlayPreInitializer)target;
            bool hasPanel = FindOverlayPanel(overlayInitializer.transform);

            DrawStatusBox(hasPanel);

            EditorGUILayout.Space(4);

            DrawBehaviorDescription(hasPanel);
        }

        private void DrawStatusBox(bool hasPanel)
        {
            Rect rect = EditorGUILayout.BeginVertical(EditorCustomStyles.Skin.box);

            EditorGUILayout.BeginHorizontal();

            GUIContent icon = hasPanel
                ? EditorGUIUtility.IconContent("TestPassed")
                : EditorGUIUtility.IconContent("console.warnicon.sml");

            EditorGUILayout.LabelField(icon, GUILayout.Width(20), GUILayout.Height(18));

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.normal.textColor = hasPanel
                ? new Color(0.3f, 0.8f, 0.3f)
                : new Color(1.0f, 0.7f, 0.1f);

            EditorGUILayout.LabelField(
                hasPanel ? PANEL_FOUND_LABEL : PANEL_NOT_FOUND_LABEL,
                labelStyle
            );

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawBehaviorDescription(bool hasPanel)
        {
            EditorGUILayout.BeginVertical(EditorCustomStyles.Skin.box);

            EditorGUILayout.LabelField("How it works", EditorStyles.boldLabel);

            EditorGUILayout.Space(2);

            EditorGUILayout.HelpBox(
                "On PreInit(), searches direct children for a component implementing IOverlayPanel.",
                MessageType.None
            );

            EditorGUILayout.Space(2);

            if (hasPanel)
            {
                EditorGUILayout.HelpBox(
                    "IOverlayPanel found in children.\n\n" +
                    "The custom panel will be used for overlay transitions. " +
                    "Show/Hide animations and loading state display are defined by the panel implementation.",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No IOverlayPanel found in children.\n\n" +
                    "A DummyOverlayPanel will be created automatically at runtime:\n" +
                    "  • Invisible canvas (sorting order 999)\n" +
                    "  • Black Image that fades in/out via Tween\n" +
                    "  • Loading state (SetLoadingState) is ignored\n\n" +
                    "To use a custom overlay, add a component implementing IOverlayPanel to a child GameObject.",
                    MessageType.Warning
                );
            }

            EditorGUILayout.EndVertical();
        }

        private static bool FindOverlayPanel(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child.GetComponent(typeof(IOverlayPanel)) != null)
                    return true;
            }

            return false;
        }
    }
}
