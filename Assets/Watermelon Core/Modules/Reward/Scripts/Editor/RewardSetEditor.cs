using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Custom inspector for <see cref="RewardsSet"/> ScriptableObjects.
    /// Renders the polymorphic rewards list with foldout entries, a type-picker add menu
    /// (with duplicate prevention), remove buttons with confirmation, and a free-form notes field.
    /// </summary>
    [CustomEditor(typeof(RewardsSet))]
    public class RewardSetEditor : Editor
    {
        private SerializedProperty scriptProp;
        private SerializedProperty rewardsProp;
        private SerializedProperty notesProp;

        private IEnumerable<SerializedProperty> ungroupedProps;

        private GUIContent addIcon;
        private GUIStyle removeButtonStyle;

        private RewardsSet targetRewardSet;

        [InitializeOnLoadMethod]
        private static void InvalidateTypeCache() => RewardsEditorUtils.InvalidateCache();

        private void OnEnable()
        {
            targetRewardSet = (RewardsSet)target;

            scriptProp = serializedObject.FindProperty("m_Script");
            rewardsProp = serializedObject.FindProperty("rewards");
            notesProp = serializedObject.FindProperty("notes");

            ungroupedProps = serializedObject.GetUngroupProperties();

            addIcon = new GUIContent("", EditorCustomStyles.GetIcon("icon_add"));

            removeButtonStyle = new GUIStyle(EditorCustomStyles.buttonRed)
            {
                padding = new RectOffset(0, 0, 0, 0),
                fontSize = 9,
                fontStyle = FontStyle.Bold
            };

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            serializedObject.Update();

            Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // m_Script (readonly)
            if (scriptProp != null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(scriptProp);
            }

            EditorGUILayout.Space(4);

            Rect boxRect = EditorGUILayout.BeginVertical(EditorCustomStyles.box);
            EditorGUILayout.LabelField("Rewards", EditorCustomStyles.labelBold);

            // Rewards array block
            if (rewardsProp != null)
            {
                int size = rewardsProp.isArray ? rewardsProp.arraySize : 0;

                if (size > 0)
                {
                    for (int i = 0; i < size; i++)
                    {
                        var element = rewardsProp.GetArrayElementAtIndex(i);

                        using (new EditorGUILayout.VerticalScope(EditorCustomStyles.box))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                string typeName = RewardsEditorUtils.GetNiceTypeName(element.managedReferenceFullTypename);

                                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, new GUIContent(typeName), true);

                                using (new EditorGUI.DisabledScope(rewardsProp.arraySize <= 0))
                                {
                                    if (GUILayout.Button("X", removeButtonStyle, GUILayout.Width(14), GUILayout.Height(14)))
                                    {
                                        bool confirm = EditorUtility.DisplayDialog(
                                            "Remove Reward",
                                            $"Are you sure you want to remove [{i}] {typeName}?",
                                            "Remove", "Cancel");

                                        if (confirm)
                                        {
                                            Undo.RecordObject(target, "Remove Reward");
                                            rewardsProp.DeleteArrayElementAtIndex(i);

                                            serializedObject.ApplyModifiedProperties();
                                            serializedObject.Update();
                                            GUIUtility.ExitGUI();
                                            return;
                                        }
                                    }
                                }
                            }

                            if (element.isExpanded)
                            {
                                EditorGUI.indentLevel++;
                                RewardsEditorUtils.DrawManagedReferenceContents(element);
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("List is empty.");
                }
            }

            EditorGUILayout.EndVertical();

            // --- ADD BUTTON (bottom-right of box) ---
            GUILayout.Space(20);
            Rect buttonsPanelRect = new Rect(boxRect.x + boxRect.width - 35, boxRect.y + boxRect.height - 1, 24, 16);
            Rect addButtonRect = new Rect(buttonsPanelRect.x + 5, buttonsPanelRect.y, 14, 14);

            GUI.Box(buttonsPanelRect, "", EditorCustomStyles.boxBottomPanel);
            GUI.Label(addButtonRect, addIcon, EditorCustomStyles.labelCentered);

            if (GUI.Button(buttonsPanelRect, GUIContent.none, GUIStyle.none))
            {
                ShowAddMenu();
                GUIUtility.ExitGUI();
                return;
            }

            if (ungroupedProps != null)
            {
                foreach (SerializedProperty prop in ungroupedProps)
                {
                    EditorGUILayout.PropertyField(prop, includeChildren: true);
                }
            }

            EditorGUILayout.PrefixLabel("Notes");

            GUIStyle style = EditorStyles.textArea;

            float height = Mathf.Clamp(style.CalcHeight(new GUIContent(notesProp.stringValue), EditorGUIUtility.currentViewWidth), 60, float.MaxValue);

            notesProp.stringValue = EditorGUILayout.TextArea(notesProp.stringValue, style, GUILayout.Height(height));

            serializedObject.ApplyModifiedProperties();
        }

        // Build GenericMenu with duplicate blocking
        private void ShowAddMenu()
        {
            var menu = new GenericMenu();

            // Collect already present Reward TYPES to block duplicates
            var existingRewardTypes = new HashSet<Type>();
            if (rewardsProp != null && rewardsProp.isArray)
            {
                for (int i = 0; i < rewardsProp.arraySize; i++)
                {
                    var el = rewardsProp.GetArrayElementAtIndex(i);
                    var t = RewardsEditorUtils.GetManagedReferenceSystemType(el);
                    if (t != null) existingRewardTypes.Add(t);
                }
            }

            foreach (var rewardType in RewardsEditorUtils.GetAllRewardTypes())
            {
                string label = rewardType.Name.AddSpaces();

                if (existingRewardTypes.Contains(rewardType))
                {
                    menu.AddDisabledItem(new GUIContent($"{label}  (Already Added)"));
                }
                else
                {
                    menu.AddItem(new GUIContent(label), false, () => AddRewardInstance(rewardType));
                }
            }

            menu.ShowAsContext();
        }

        private void AddRewardInstance(Type rewardType)
        {
            if (rewardsProp == null || rewardType == null)
                return;

            serializedObject.Update();

            Undo.RecordObject(target, "Add Reward");

            int newIndex = rewardsProp.arraySize;
            rewardsProp.InsertArrayElementAtIndex(newIndex);
            var el = rewardsProp.GetArrayElementAtIndex(newIndex);

            object instance = null;
            try
            {
                instance = Activator.CreateInstance(rewardType);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create reward of type {rewardType.FullName}: {e.Message}");
            }

            el.managedReferenceValue = instance;
            el.isExpanded = true;

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            EditorUtility.SetDirty(target);
        }

    }
}
