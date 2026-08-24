using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomEditor(typeof(GameLoading))]
    public class GameLoadingEditor : Editor
    {
        private SerializedProperty initializerProperty;
        private SerializedProperty useManualControlProperty;

        private void OnEnable()
        {
            initializerProperty = serializedObject.FindProperty("initializer");
            useManualControlProperty = serializedObject.FindProperty("useManualControl");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(initializerProperty);
            EditorGUILayout.PropertyField(useManualControlProperty);

            EditorGUILayout.Space(4);

            DrawLoadingSteps();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLoadingSteps()
        {
            EditorGUILayoutCustom.BeginBoxGroup("Loading Steps (on Initializer)");

            Initializer initializer = initializerProperty.objectReferenceValue as Initializer;
            if (initializer == null)
            {
                EditorGUILayout.HelpBox("Assign Initializer to see loading steps.", MessageType.Info);
                EditorGUILayoutCustom.EndBoxGroup();
                return;
            }

            ILoadingStep[] steps = initializer.GetComponents<ILoadingStep>();
            if (steps.Length == 0)
            {
                EditorGUILayout.HelpBox("No ILoadingStep components found on Initializer.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < steps.Length; i++)
                {
                    MonoBehaviour mb = steps[i] as MonoBehaviour;

                    EditorGUILayout.BeginHorizontal(EditorCustomStyles.box);

                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20));

                    if (mb != null)
                    {
                        EditorGUILayout.LabelField(mb.GetType().Name, EditorCustomStyles.labelBold);
                        EditorGUILayout.LabelField(steps[i].LoadingMessage, EditorCustomStyles.label, GUILayout.ExpandWidth(true));

                        if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                            Selection.activeObject = mb;
                    }
                    else
                    {
                        EditorGUILayout.LabelField("(non-MonoBehaviour ILoadingStep)", EditorCustomStyles.label);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayoutCustom.EndBoxGroup();
        }
    }
}
