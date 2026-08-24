using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomEditor(typeof(MergeDebugSpawner))]
    public class MergeDebugSpawnerEditor : Editor
    {
        SerializedProperty _itemTypeId, _grade;

        void OnEnable()
        {
            _itemTypeId = serializedObject.FindProperty("itemTypeId");
            _grade      = serializedObject.FindProperty("grade");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            MergeItemPickerGUI.DrawTypeGradeField("Item", _itemTypeId, _grade);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Spawn Object", GUILayout.Height(30)))
                ((MergeDebugSpawner)target).SpawnObject();
            GUI.enabled = true;

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to spawn objects.", MessageType.Info);
        }
    }
}
