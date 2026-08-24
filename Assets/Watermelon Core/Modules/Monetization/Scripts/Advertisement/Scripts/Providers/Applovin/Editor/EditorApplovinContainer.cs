using UnityEngine;
using UnityEditor;

namespace Watermelon
{
    [AdsEditorContainer(typeof(ApplovinContainer))]
    public class EditorApplovinContainer : EditorAdsContainer
    {
        protected override string ContainerDisplayName => "AppLovin";

        protected override void SpecialButtons()
        {
            GUILayout.Space(8);

            EditorGUILayout.HelpBox("Tested with Applovin v8.6.2", MessageType.Info);
        }
    }
}
