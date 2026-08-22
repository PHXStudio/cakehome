#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomEditor(typeof(TaskDebugSpawner))]
    public class TaskDebugSpawnerEditor : Editor
    {
        SerializedProperty _taskType;
        SerializedProperty _character, _orderItems, _coinsReward;
        SerializedProperty _spawnerTypeId, _spawnerGrade;

        void OnEnable()
        {
            _taskType      = serializedObject.FindProperty("taskType");
            _character     = serializedObject.FindProperty("character");
            _orderItems    = serializedObject.FindProperty("orderItems");
            _coinsReward   = serializedObject.FindProperty("coinsReward");
            _spawnerTypeId = serializedObject.FindProperty("spawnerTypeId");
            _spawnerGrade  = serializedObject.FindProperty("spawnerGrade");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_taskType);
            EditorGUILayout.Space(4);

            var type = (TaskDebugSpawner.DebugTaskType)_taskType.enumValueIndex;

            switch (type)
            {
                case TaskDebugSpawner.DebugTaskType.ClientOrder:
                    EditorGUILayout.PropertyField(_character);
                    EditorGUILayout.PropertyField(_orderItems, includeChildren: true);
                    EditorGUILayout.PropertyField(_coinsReward);
                    break;

                case TaskDebugSpawner.DebugTaskType.SpawnEntry:
                    MergeItemPickerGUI.DrawTypeGradeField("Spawner", _spawnerTypeId, _spawnerGrade);
                    break;
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Add Task", GUILayout.Height(30)))
                ((TaskDebugSpawner)target).AddSelectedTask();
            GUI.enabled = true;

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to add tasks.", MessageType.Info);
        }
    }
}
#endif
