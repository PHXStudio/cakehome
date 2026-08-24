using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    public class ArrayElementGUIRenderer
    {
        private GUIRenderer[] renderers;
        private SerializedProperty elementProperty;
        private int index;

        private Action<int> onSelect;
        private Action<int> onStartDrag;
        private Func<int, bool> isSelected;

        private MethodInfo customTitleMethod;
        private object elementObject;

        private static readonly Color selectedColor = new Color(0.24f, 0.49f, 0.91f, 0.3f);
        private static readonly Color dragHandleColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        private static readonly GUIContent dragHandleContent = new GUIContent("☰");

        private Rect lastElementRect;

        public Rect LastRect => lastElementRect;

        public ArrayElementGUIRenderer(CustomInspector editor, SerializedProperty elementProperty, object elementObject, List<Type> elementNestedTypes, int index, Action<int> onSelect, Func<int, bool> isSelected, Action<int> onStartDrag)
        {
            this.elementProperty = elementProperty;
            this.index = index;
            this.onSelect = onSelect;
            this.onStartDrag = onStartDrag;
            this.isSelected = isSelected;
            this.elementObject = elementObject;

            List<GUIRenderer> childRenderers = new List<GUIRenderer>();

            SerializedProperty prop = elementProperty.Copy();
            SerializedProperty end = prop.GetEndProperty();

            if (prop.NextVisible(true))
            {
                int propertyDepth = prop.depth;

                do
                {
                    if (SerializedProperty.EqualContents(prop, end)) break;
                    if (prop.depth != propertyDepth) continue;

                    FieldInfo fieldInfo = null;
                    foreach (Type t in elementNestedTypes)
                    {
                        fieldInfo = t.GetField(prop.name, ReflectionUtils.FLAGS_INSTANCE);
                        if (fieldInfo != null) break;
                    }

                    if (fieldInfo == null) continue;

                    SerializedProperty childProp = editor.serializedObject.FindProperty(prop.propertyPath);
                    if (childProp == null) continue;

                    if (childProp.isArray && childProp.propertyType == SerializedPropertyType.Generic)
                        childRenderers.Add(new SerializedArrayGUIRenderer(editor, childProp, fieldInfo, elementObject, elementNestedTypes));
                    else
                        childRenderers.Add(new SerializedPropertyGUIRenderer(editor, childProp, fieldInfo, elementObject, elementNestedTypes));
                }
                while (prop.NextVisible(true));
            }

            renderers = PropertyUtility.GroupRenderers(editor, childRenderers, $"[{index}]_");

            if (elementObject != null)
                customTitleMethod = elementObject.GetType().GetMethod("GetCustomArrayTitle", ReflectionUtils.FLAGS_INSTANCE, null, new[] { typeof(int) }, null);
        }

        public void OnGUI()
        {
            bool selected = isSelected(index);
            EventType evtType = Event.current.type;

            Rect elementRect = EditorGUILayout.BeginVertical();

            if (evtType == EventType.Repaint && elementRect.width > 0)
                lastElementRect = elementRect;

            if (selected && evtType == EventType.Repaint && elementRect.width > 0)
            {
                Rect highlightRect = new Rect(
                    elementRect.x - 14,
                    elementRect.y,
                    elementRect.width + 18,
                    elementRect.height
                );
                EditorGUI.DrawRect(highlightRect, selectedColor);
            }

            EditorGUILayout.BeginHorizontal();

            // Drag handle
            Rect handleRect = GUILayoutUtility.GetRect(dragHandleContent, EditorStyles.label, GUILayout.Width(16), GUILayout.ExpandHeight(false));
            handleRect.x -= 12;

            if (evtType == EventType.Repaint)
            {
                Color prev = GUI.color;
                GUI.color = dragHandleColor;
                GUI.Label(handleRect, dragHandleContent);
                GUI.color = prev;
            }
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.Pan);

            string title = customTitleMethod != null
                ? (string)customTitleMethod.Invoke(elementObject, new object[] { index }) ?? $"Element {index}"
                : $"Element {index}";

            elementProperty.isExpanded = EditorGUILayout.Foldout(elementProperty.isExpanded, title, true);

            EditorGUILayout.EndHorizontal();

            if (elementProperty.isExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (GUIRenderer renderer in renderers)
                    renderer.OnGUI();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();

            // Live-rect hit test: elementRect and handleRect are current layout values (not cached).
            // This avoids stale-rect selection errors when layout changed since last Repaint.
            if (evtType == EventType.MouseDown && Event.current.button == 0)
            {
                Vector2 mouse = Event.current.mousePosition;
                bool onHandle = handleRect.Contains(mouse);
                bool onElement = elementRect.width > 0 && elementRect.Contains(mouse);

                if (onHandle)
                {
                    onStartDrag?.Invoke(index);
                    Event.current.Use();
                }
                else if (onElement && !isSelected(index))
                {
                    onSelect(index);
                }
            }
        }

        public void OnGUIChanged()
        {
            foreach (GUIRenderer renderer in renderers)
                renderer.OnGUIChanged();
        }
    }
}
