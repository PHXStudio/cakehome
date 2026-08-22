using UnityEngine;
using UnityEditor;
using System.IO;

namespace Watermelon
{
    [CustomEditor(typeof(AudioInitModule))]
    public class AudioInitModuleEditor : InitModuleEditor
    {
        public override void OnCreated()
        {
            AudioRegistry registry = EditorUtils.GetAsset<AudioRegistry>();
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<AudioRegistry>();
                registry.name = "Audio Registry";

                string referencePath = AssetDatabase.GetAssetPath(target);
                string directoryPath = Path.GetDirectoryName(referencePath);

                string assetPath = Path.Combine(directoryPath, registry.name + ".asset");
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                AssetDatabase.CreateAsset(registry, assetPath);
                AssetDatabase.SaveAssets();

                EditorUtility.SetDirty(target);
            }

            serializedObject.Update();
            serializedObject.FindProperty("audioRegistry").objectReferenceValue = registry;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
