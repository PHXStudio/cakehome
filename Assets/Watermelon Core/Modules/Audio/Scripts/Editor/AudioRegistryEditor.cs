using System.IO;
using UnityEngine;
using UnityEditor;

namespace Watermelon
{
    [CustomEditor(typeof(AudioRegistry))]
    public class AudioRegistryEditor : Editor
    {
        private SerializedProperty ignorePatternsProp;

        private string newPattern = "";
        private string searchFilter = "";
        private Vector2 entriesScroll;

        private void OnEnable()
        {
            EditorCustomStyles.CheckStyles();

            ignorePatternsProp = serializedObject.FindProperty("ignorePatterns");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AudioRegistry registry = (AudioRegistry)target;

            // ─── Collect button ────────────────────────────────────────────

            if (GUILayout.Button("Collect AudioClips", EditorCustomStyles.buttonBlue, GUILayout.Height(30)))
                AudioRegistryCollector.CollectAll();

            GUILayout.Space(6);

            // ─── Entries ───────────────────────────────────────────────────

            EditorGUILayoutCustom.BeginBoxGroup($"Entries ({registry.Entries.Count})");

            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            GUILayout.Space(4);

            entriesScroll = EditorGUILayout.BeginScrollView(entriesScroll, GUILayout.MaxHeight(300));

            if (registry.Entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No clips collected yet. Press \"Collect AudioClips\".", MessageType.Info);
            }
            else
            {
                foreach (AudioRegistryEntry entry in registry.Entries)
                {
                    if (!string.IsNullOrEmpty(searchFilter) &&
                        entry.ClipName.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    EditorGUILayout.BeginHorizontal(EditorCustomStyles.box);

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(entry.ClipName, EditorCustomStyles.labelBold);
                    EditorGUILayout.LabelField(entry.AssetPath, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical(GUILayout.Width(100));

                    if (GUILayout.Button("Ignore", EditorCustomStyles.buttonRed, GUILayout.ExpandWidth(true)))
                        AddIgnorePattern(entry.ClipName);

                    if (GUILayout.Button("Ignore Folder", EditorCustomStyles.buttonRed, GUILayout.ExpandWidth(true)))
                        AddIgnorePattern("/" + Path.GetDirectoryName(entry.AssetPath).Replace('\\', '/') + "/*");

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayoutCustom.EndBoxGroup();

            GUILayout.Space(6);

            // ─── Ignore Patterns ───────────────────────────────────────────

            EditorGUILayoutCustom.BeginBoxGroup("Ignore Patterns");

            EditorGUILayout.HelpBox(
                "Starts with '/' → matched against asset path. Otherwise → matched against clip name.\n" +
                "'*' matches any sequence of characters.  Example:  MD_*   /Assets/Music/*   *_temp",
                MessageType.None);

            GUILayout.Space(4);

            for (int i = 0; i < ignorePatternsProp.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                SerializedProperty el = ignorePatternsProp.GetArrayElementAtIndex(i);
                el.stringValue = EditorGUILayout.TextField(el.stringValue);

                if (GUILayout.Button("✕", EditorCustomStyles.buttonRed, GUILayout.Width(26)))
                {
                    ignorePatternsProp.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    AudioRegistryCollector.CollectAll();
                    return;
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();

            newPattern = EditorGUILayout.TextField(newPattern);

            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(newPattern));

            if (GUILayout.Button("Add", EditorCustomStyles.buttonBlue, GUILayout.Width(52)))
            {
                AddIgnorePattern(newPattern.Trim());
                newPattern = "";
                GUI.FocusControl(null);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayoutCustom.EndBoxGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void AddIgnorePattern(string pattern)
        {
            for (int i = 0; i < ignorePatternsProp.arraySize; i++)
            {
                if (ignorePatternsProp.GetArrayElementAtIndex(i).stringValue == pattern)
                    return;
            }

            ignorePatternsProp.arraySize++;
            ignorePatternsProp.GetArrayElementAtIndex(ignorePatternsProp.arraySize - 1).stringValue = pattern;
            serializedObject.ApplyModifiedProperties();

            AudioRegistryCollector.CollectAll();
        }
    }
}
