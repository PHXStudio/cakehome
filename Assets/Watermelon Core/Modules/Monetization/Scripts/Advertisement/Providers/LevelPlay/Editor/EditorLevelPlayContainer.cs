using UnityEngine;
using UnityEditor;

namespace Watermelon
{
    [AdsEditorContainer(typeof(LevelPlayContainer))]
    public class EditorLevelPlayContainer : EditorAdsContainer
    {
        protected override string ContainerDisplayName => "LevelPlay";

        protected override void SpecialButtons()
        {
            GUILayout.Space(8);

            if (GUILayout.Button("Getting Started Guide", EditorCustomStyles.button))
            {
                Application.OpenURL(@"https://docs.unity.com/en-us/grow/levelplay/sdk/unity/get-started");
            }

            if (GUILayout.Button("Integration Testing", EditorCustomStyles.button))
            {
                Application.OpenURL(@"https://docs.unity.com/en-us/grow/levelplay/sdk/unity/integration-test-suite");
            }

            GUILayout.Space(8);

            EditorGUILayout.HelpBox("Tested with LevelPlay v9.3.0", MessageType.Info);
        }
    }
}
