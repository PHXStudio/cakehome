using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomPropertyDrawer(typeof(MergeGradeConfig), true)]
    public class MergeGradeConfigDrawer : PropertyDrawer
    {
        static Type[] configTypes;
        static string[] configDisplay;
        const string NONE = "None";

        static MergeGradeConfigDrawer()
        {
            RefreshTypes();
        }

        static void RefreshTypes()
        {
            TypeCache.TypeCollection all = TypeCache.GetTypesDerivedFrom<MergeGradeConfig>();
            configTypes = all.Where(t => !t.IsAbstract && !t.IsInterface)
                              .OrderBy(t => t.Name)
                              .ToArray();

            List<string> names = new List<string> { NONE };
            names.AddRange(configTypes.Select(t => t.Name));

            configDisplay = names.ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float vpad = EditorGUIUtility.standardVerticalSpacing;
            float btnW = 60f;
            float lblW = EditorGUIUtility.labelWidth;

            Rect headerRect = new Rect(position.x, position.y, position.width, line);
            Rect labelRect = new Rect(headerRect.x, headerRect.y, lblW, line);

            float popupW = headerRect.width - lblW - btnW - 4f;
            Rect popupRect = new Rect(labelRect.xMax, headerRect.y, popupW, line);
            Rect btnRect = new Rect(popupRect.xMax + 4f, headerRect.y, btnW, line);

            EditorGUI.LabelField(labelRect, label);

            int currentIndex = 0;

            if (property.managedReferenceValue != null)
            {
                Type currentType = property.managedReferenceValue.GetType();
                int idx = Array.IndexOf(configTypes, currentType);
                if (idx >= 0) currentIndex = idx + 1; // +1 because index 0 is None
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(popupRect, currentIndex, configDisplay);
            if (EditorGUI.EndChangeCheck())
            {
                TryChangeTypeWithConfirm(property, currentIndex, newIndex);
            }

            using (new EditorGUI.DisabledScope(property.managedReferenceValue == null))
            {
                if (GUI.Button(btnRect, "Reset"))
                {
                    if (EditorUtility.DisplayDialog("Reset config", "This will clear the current config. Continue?", "Yes", "No"))
                    {
                        ApplyToAllTargets(property, (obj) =>
                        {
                            var sp = new SerializedObject(obj).FindProperty(property.propertyPath);
                            sp.managedReferenceValue = null;
                            sp.serializedObject.ApplyModifiedProperties();
                        });
                    }
                }
            }

            if (property.managedReferenceValue != null)
            {
                const float indent = 12f;
                float y = headerRect.yMax + vpad;
                foreach (SerializedProperty child in GetChildren(property))
                {
                    float h = EditorGUI.GetPropertyHeight(child, true);
                    Rect childRect = new Rect(position.x + indent, y, position.width - indent, h);
                    EditorGUI.PropertyField(childRect, child, true);
                    y += h + vpad;
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float vpad = EditorGUIUtility.standardVerticalSpacing;
            float h = EditorGUIUtility.singleLineHeight; // header

            if (property.managedReferenceValue != null)
            {
                foreach (SerializedProperty child in GetChildren(property))
                {
                    h += vpad + EditorGUI.GetPropertyHeight(child, true);
                }
            }
            return h;
        }

        // Children are plain fields of the concrete config (e.g. int, Sprite, WeightedList<SpawnEntry>) —
        // never MergeGradeConfig itself, so this can't re-enter this drawer via useForChildren.
        static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property)
        {
            SerializedProperty copy = property.Copy();
            SerializedProperty end = copy.GetEndProperty();
            bool enterChildren = true;
            while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
            {
                enterChildren = false;
                if (copy.name == "m_Script") continue;
                yield return copy.Copy();
            }
        }

        void TryChangeTypeWithConfirm(SerializedProperty property, int currentIndex, int newIndex)
        {
            if (newIndex == currentIndex) return;

            bool needConfirm = property.managedReferenceValue != null && currentIndex != 0;
            if (needConfirm)
            {
                if (!EditorUtility.DisplayDialog("Change config type",
                    "Changing the type will replace existing data for this grade's config. Continue?",
                    "Yes", "No"))
                {
                    return;
                }
            }

            Type targetType = (newIndex <= 0) ? null : configTypes[newIndex - 1];

            ApplyToAllTargets(property, (obj) =>
            {
                var root = new SerializedObject(obj);
                var sp = root.FindProperty(property.propertyPath);

                object newConfig = CreateInstanceSafe(targetType);
                sp.managedReferenceValue = newConfig;

                root.ApplyModifiedProperties();
            });
        }

        static object CreateInstanceSafe(Type t)
        {
            if (t == null) return null;
            try
            {
                return Activator.CreateInstance(t, true);
            }
            catch
            {
                try
                {
                    return FormatterServices.GetUninitializedObject(t);
                }
                catch
                {
                    Debug.LogWarning($"[MergeGradeConfigDrawer] Can't create instance of {t.FullName}");
                    return null;
                }
            }
        }

        static void ApplyToAllTargets(SerializedProperty property, Action<UnityEngine.Object> action)
        {
            var so = property.serializedObject;
            Undo.RecordObjects(so.targetObjects, "Change MergeGradeConfig");
            foreach (var o in so.targetObjects)
            {
                action(o);
                EditorUtility.SetDirty(o);
            }
        }
    }
}
