using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Watermelon
{
    // Unity's default array inspector adds new elements by duplicating the last element's
    // serialized data. For [SerializeReference] fields (SimpleReward.reward) that copies the
    // managed-reference id, so the new slot and the previous last slot alias the SAME object —
    // editing one edits both. This drawer uses an explicit ReorderableList whose add callback
    // clears the new element's managed reference right after insertion, breaking the alias.
    [CustomPropertyDrawer(typeof(RewardBundle))]
    public class RewardBundlePropertyDrawer : PropertyDrawer
    {
        static Dictionary<string, ReorderableList> lists = new Dictionary<string, ReorderableList>();

        ReorderableList GetList(SerializedProperty rewardsProp)
        {
#if UNITY_6000_4_OR_NEWER
            string key = rewardsProp.propertyPath + rewardsProp.serializedObject.targetObject.GetEntityId().GetHashCode();
#else
            string key = rewardsProp.propertyPath + rewardsProp.serializedObject.targetObject.GetInstanceID();
#endif
            // A cached list can outlive its SerializedObject (external asset reimport rebuilds
            // the inspector's SerializedObject without a domain reload) — using it then throws
            // ArgumentNullException from native code, so rebuild on mismatch.
            if (lists.TryGetValue(key, out ReorderableList cached) &&
                cached.serializedProperty.serializedObject != rewardsProp.serializedObject)
            {
                lists.Remove(key);
            }
            if (!lists.TryGetValue(key, out ReorderableList list))
            {
                list = new ReorderableList(rewardsProp.serializedObject, rewardsProp, true, true, true, true);

                list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Rewards");

                list.elementHeightCallback = index =>
                    EditorGUI.GetPropertyHeight(rewardsProp.GetArrayElementAtIndex(index), true) + EditorGUIUtility.standardVerticalSpacing;

                list.drawElementCallback = (rect, index, active, focused) =>
                {
                    rect.y += 1;
                    rect.height -= EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, rewardsProp.GetArrayElementAtIndex(index), true);
                };

                list.onAddCallback = l =>
                {
                    int index = l.serializedProperty.arraySize;
                    l.serializedProperty.arraySize++;

                    SerializedProperty newElement = l.serializedProperty.GetArrayElementAtIndex(index);
                    newElement.FindPropertyRelative("reward").managedReferenceValue = null;
                };

                lists[key] = list;
            }

            return list;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty rewardsProp = property.FindPropertyRelative("rewards");
            GetList(rewardsProp).DoList(position);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty rewardsProp = property.FindPropertyRelative("rewards");
            return GetList(rewardsProp).GetHeight();
        }
    }
}
