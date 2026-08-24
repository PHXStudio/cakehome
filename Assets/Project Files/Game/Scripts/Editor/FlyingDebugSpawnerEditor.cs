#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomEditor(typeof(FlyingDebugSpawner))]
    public class FlyingDebugSpawnerEditor : Editor
    {
        SerializedProperty _flyType;
        SerializedProperty _levelsToAdd;
        SerializedProperty _amount;
        SerializedProperty _spawnerTypeId, _spawnerGrade;

        void OnEnable()
        {
            _flyType      = serializedObject.FindProperty("flyType");
            _levelsToAdd  = serializedObject.FindProperty("levelsToAdd");
            _amount       = serializedObject.FindProperty("amount");
            _spawnerTypeId = serializedObject.FindProperty("spawnerTypeId");
            _spawnerGrade  = serializedObject.FindProperty("spawnerGrade");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_flyType);
            EditorGUILayout.Space(4);

            var type = (FlyingDebugSpawner.FlyType)_flyType.enumValueIndex;

            switch (type)
            {
                case FlyingDebugSpawner.FlyType.Exp:
                    EditorGUILayout.PropertyField(_levelsToAdd, new GUIContent("Levels To Add"));
                    break;

                case FlyingDebugSpawner.FlyType.Energy:
                case FlyingDebugSpawner.FlyType.Coins:
                case FlyingDebugSpawner.FlyType.Gems:
                    EditorGUILayout.PropertyField(_amount);
                    break;

                case FlyingDebugSpawner.FlyType.Spawner:
                    MergeItemPickerGUI.DrawTypeGradeField("Spawner", _spawnerTypeId, _spawnerGrade);
                    break;
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Play Fly", GUILayout.Height(30)))
                ((FlyingDebugSpawner)target).PlayFly();
            GUI.enabled = true;

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to test flights.", MessageType.Info);
        }
    }
}
#endif
