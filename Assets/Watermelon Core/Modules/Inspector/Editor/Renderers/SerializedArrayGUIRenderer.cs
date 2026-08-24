using System;
using System.Reflection;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Watermelon
{
    public sealed class SerializedArrayGUIRenderer : SerializedPropertyGUIRenderer
    {
        private bool containsUniqueID;
        private FieldInfo[] uniqueIDFields;

        private bool useCustomRendering;
        private List<ArrayElementGUIRenderer> elementRenderers;
        private int cachedArraySize = -1;

        private FieldInfo arrayFieldInfo;
        private object targetObject;
        private CustomInspector editor;
        private List<Type> elementNestedTypes;

        private int selectedIndex = -1;

        private bool isDragging;
        private int dragFromIndex = -1;
        private int dragToIndex = -1;
        private int dragControlId;

        private Rect lastBoxRect;

        private static readonly Color dropLineColor = new Color(0.24f, 0.49f, 0.91f, 0.9f);
        private static readonly Color draggedElementColor = new Color(1f, 1f, 1f, 0.15f);

        private static GUIStyle rlFooterStyle;
        private static GUIStyle rlFooterButtonStyle;
        private static GUIContent iconPlus;
        private static GUIContent iconMinus;

        private static void EnsureFooterStyles()
        {
            if (rlFooterStyle != null) return;
            rlFooterStyle = new GUIStyle("RL Footer");
            rlFooterButtonStyle = new GUIStyle("RL FooterButton");
            iconPlus = EditorGUIUtility.IconContent("Toolbar Plus");
            iconMinus = EditorGUIUtility.IconContent("Toolbar Minus");
        }

        public SerializedArrayGUIRenderer(CustomInspector editor, SerializedProperty serializedProperty, FieldInfo fieldInfo, object targetObject, List<Type> nestedTypes) : base(editor, serializedProperty, fieldInfo, targetObject, nestedTypes)
        {
            this.editor = editor;
            this.arrayFieldInfo = fieldInfo;
            this.targetObject = targetObject;

            Type elementType = fieldInfo.FieldType.GetElementType();
            if (elementType == null && fieldInfo.FieldType.IsGenericType)
                elementType = fieldInfo.FieldType.GetGenericArguments()[0];

            if (elementType != null)
            {
                if (elementType.IsSubclassOf(typeof(UnityEngine.ScriptableObject)) || elementType.IsSubclassOf(typeof(UnityEngine.MonoBehaviour)))
                {
                    containsUniqueID = false;
                    useCustomRendering = false;
                }
                else
                {
                    uniqueIDFields = ReflectionUtils.GetParentTypes(elementType)
                        .SelectMany(x => x.GetFields(ReflectionUtils.FLAGS_INSTANCE))
                        .Where(f => Attribute.GetCustomAttribute(f, typeof(UniqueIDAttribute)) != null)
                        .ToArray();

                    if (!uniqueIDFields.IsNullOrEmpty())
                        containsUniqueID = true;

                    useCustomRendering = ElementTypeHasCustomAttributes(elementType);

                    if (useCustomRendering)
                    {
                        elementNestedTypes = PropertyUtility.GetClassNestedTypes(elementType);
                        elementRenderers = new List<ArrayElementGUIRenderer>();
                    }
                }
            }
        }

        private static bool ElementTypeHasCustomAttributes(Type type)
        {
            if (type.GetCustomAttribute<CustomArrayElementAttribute>() != null)
                return true;

            return ReflectionUtils.GetParentTypes(type)
                .SelectMany(t => t.GetFields(ReflectionUtils.FLAGS_INSTANCE))
                .Any(f => f.GetCustomAttribute<InlineButtonAttribute>() != null
                       || f.GetCustomAttribute<GroupAttribute>() != null
                       || f.GetCustomAttribute<ConditionAttribute>() != null
                       || f.GetCustomAttribute<DisableIfAttribute>() != null
                       || f.GetCustomAttribute<EnableIfAttribute>() != null);
        }

        private void RebuildElementRenderers()
        {
            elementRenderers.Clear();
            IList list = arrayFieldInfo.GetValue(targetObject) as IList;

            for (int i = 0; i < serializedProperty.arraySize; i++)
            {
                object elementObj = (list != null && i < list.Count) ? list[i] : null;
                SerializedProperty elementProp = serializedProperty.GetArrayElementAtIndex(i);
                elementRenderers.Add(new ArrayElementGUIRenderer(editor, elementProp, elementObj, elementNestedTypes, i, OnElementSelected, IsElementSelected, OnElementDragStart));
            }

            cachedArraySize = serializedProperty.arraySize;
        }

        private void OnElementSelected(int index)
        {
            selectedIndex = (selectedIndex == index) ? -1 : index;
            editor.Repaint();
        }

        private bool IsElementSelected(int index) => selectedIndex == index;

        private void OnElementDragStart(int index)
        {
            isDragging = true;
            dragFromIndex = index;
            dragToIndex = index;
            GUIUtility.hotControl = dragControlId;
        }

        public override void OnGUI()
        {
            if (!useCustomRendering)
            {
                HandleDefaultRendering();
                return;
            }

            if (!IsVisible) return;

            if (serializedProperty.arraySize != cachedArraySize)
                RebuildElementRenderers();

            // Header: foldout + size field inline
            EditorGUILayout.BeginHorizontal();
            serializedProperty.isExpanded = EditorGUILayout.Foldout(serializedProperty.isExpanded, serializedProperty.displayName, true);

            EditorGUI.BeginChangeCheck();
            int newSize = EditorGUILayout.DelayedIntField(serializedProperty.arraySize, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck() && newSize >= 0)
            {
                ResizeArray(newSize);
                RebuildElementRenderers();
            }

            EditorGUILayout.EndHorizontal();

            if (!serializedProperty.isExpanded) return;

            Rect boxRect = EditorGUILayout.BeginVertical(EditorCustomStyles.box);
            if (Event.current.type == EventType.Repaint && boxRect.width > 0)
                lastBoxRect = boxRect;

            Event evt = Event.current;
            dragControlId = GUIUtility.GetControlID(FocusType.Passive);

            for (int i = 0; i < elementRenderers.Count; i++)
            {
                if (isDragging && dragFromIndex == i && evt.type == EventType.Repaint)
                {
                    Rect r = elementRenderers[i].LastRect;
                    if (r.width > 0)
                        EditorGUI.DrawRect(new Rect(r.x - 14, r.y, r.width + 18, r.height), draggedElementColor);
                }

                elementRenderers[i].OnGUI();
            }

            if (isDragging)
            {
                if (evt.type == EventType.MouseDrag)
                {
                    dragToIndex = ComputeDropIndex(evt.mousePosition);
                    editor.Repaint();
                    evt.Use();
                }

                if (evt.type == EventType.Repaint && elementRenderers.Count > 0)
                    DrawDropLine(dragToIndex);

                if (evt.type == EventType.MouseUp)
                {
                    GUIUtility.hotControl = 0;

                    int from = dragFromIndex;
                    int to = dragToIndex;
                    bool moved = to != from && to != from + 1;

                    isDragging = false;
                    dragFromIndex = -1;
                    dragToIndex = -1;

                    if (moved)
                    {
                        int dest = to > from ? to - 1 : to;
                        serializedProperty.MoveArrayElement(from, dest);
                        serializedProperty.serializedObject.ApplyModifiedProperties();
                        RebuildElementRenderers();
                    }

                    editor.Repaint();
                    evt.Use();
                }
            }

            EditorGUILayout.EndVertical();

            // Add / Remove buttons
            EnsureFooterStyles();

            Rect footerBoxRect = GUILayoutUtility.GetLastRect();
            float footerHeight = 16f;
            // Compensate box bottom margin so footer attaches flush
            GUILayout.Space(-EditorCustomStyles.box.margin.bottom);
            Rect footerRect = GUILayoutUtility.GetRect(footerBoxRect.width, footerHeight, GUILayout.Height(footerHeight));
            footerRect.x = footerBoxRect.width - 60;
            footerRect.width = 61;

            if (Event.current.type == EventType.Repaint)
                rlFooterStyle.Draw(footerRect, false, false, false, false);

            float btnWidth = 30f;
            Rect addRect = new Rect(footerRect.xMax - 60, footerRect.y, btnWidth, footerRect.height);
            Rect removeRect = new Rect(footerRect.xMax - 31, footerRect.y, btnWidth, footerRect.height);

            if (GUI.Button(addRect, iconPlus, rlFooterButtonStyle))
            {
                int prevSize = serializedProperty.arraySize;
                serializedProperty.arraySize++;

                SerializedProperty addedElement = serializedProperty.GetArrayElementAtIndex(prevSize);
                ClearManagedReferencesRecursive(addedElement);

                if (containsUniqueID)
                {
                    foreach (FieldInfo field in uniqueIDFields)
                        addedElement.FindPropertyRelative(field.Name).stringValue = "";
                }
            }

            using (new EditorGUI.DisabledScope(selectedIndex < 0 || selectedIndex >= serializedProperty.arraySize))
            {
                if (GUI.Button(removeRect, iconMinus, rlFooterButtonStyle))
                {
                    serializedProperty.DeleteArrayElementAtIndex(selectedIndex);
                    selectedIndex = -1;
                }
            }
        }

        private int ComputeDropIndex(Vector2 mousePos)
        {
            for (int i = 0; i < elementRenderers.Count; i++)
            {
                Rect r = elementRenderers[i].LastRect;
                if (r.height > 0 && mousePos.y < r.y + r.height * 0.5f)
                    return i;
            }
            return elementRenderers.Count;
        }

        private void DrawDropLine(int insertBefore)
        {
            float y;
            Rect sample = elementRenderers[0].LastRect;

            if (insertBefore <= 0)
                y = elementRenderers[0].LastRect.y;
            else if (insertBefore >= elementRenderers.Count)
                y = elementRenderers[elementRenderers.Count - 1].LastRect.yMax;
            else
                y = elementRenderers[insertBefore].LastRect.y;

            Rect line = new Rect(sample.x - 14, y - 1, sample.width + 18, 2);
            EditorGUI.DrawRect(line, dropLineColor);
        }

        private void ResizeArray(int newSize)
        {
            int prevSize = serializedProperty.arraySize;
            serializedProperty.arraySize = newSize;

            if (newSize > prevSize)
            {
                for (int i = prevSize; i < newSize; i++)
                {
                    SerializedProperty addedElement = serializedProperty.GetArrayElementAtIndex(i);
                    ClearManagedReferencesRecursive(addedElement);

                    if (containsUniqueID)
                    {
                        foreach (FieldInfo field in uniqueIDFields)
                            addedElement.FindPropertyRelative(field.Name).stringValue = "";
                    }
                }
            }

            if (selectedIndex >= newSize)
                selectedIndex = -1;
        }

        // Unity's array-resize duplicates the last element's data, including managed-reference
        // ids for [SerializeReference] fields — newly added elements would silently alias the
        // previous element's reference (e.g. a Reward) instead of getting their own.
        private static void ClearManagedReferencesRecursive(SerializedProperty property)
        {
            SerializedProperty end = property.GetEndProperty();
            SerializedProperty current = property.Copy();
            bool enterChildren = true;

            while (current.NextVisible(enterChildren) && !SerializedProperty.EqualContents(current, end))
            {
                enterChildren = true;

                if (current.propertyType == SerializedPropertyType.ManagedReference)
                {
                    current.managedReferenceValue = null;
                    enterChildren = false;
                }
            }
        }

        private void HandleDefaultRendering()
        {
            if (containsUniqueID)
            {
                int tempArraySize = serializedProperty.arraySize;
                base.OnGUI();
                if (serializedProperty.arraySize > tempArraySize)
                {
                    SerializedProperty addedElement = serializedProperty.GetArrayElementAtIndex(serializedProperty.arraySize - 1);
                    foreach (FieldInfo field in uniqueIDFields)
                        addedElement.FindPropertyRelative(field.Name).stringValue = "";
                }
            }
            else
            {
                base.OnGUI();
            }
        }

        public override void OnGUIChanged()
        {
            base.OnGUIChanged();

            if (useCustomRendering && elementRenderers != null)
            {
                foreach (ArrayElementGUIRenderer er in elementRenderers)
                    er.OnGUIChanged();
            }
        }
    }
}
