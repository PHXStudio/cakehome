#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomEditor(typeof(DialogDebugSpawner))]
    public class DialogDebugSpawnerEditor : Editor
    {
        SerializedProperty _dialogData;

        void OnEnable()
        {
            _dialogData = serializedObject.FindProperty("dialogData");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_dialogData);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            GUI.enabled = Application.isPlaying && _dialogData.objectReferenceValue != null;
            if (GUILayout.Button("Play Dialog", GUILayout.Height(30)))
                ((DialogDebugSpawner)target).PlayDialog();
            GUI.enabled = true;

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to play the dialog.", MessageType.Info);
            else if (_dialogData.objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign a Dialog Data asset above.", MessageType.Warning);
        }
    }
}
#endif
