using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomEditor(typeof(AdsSettings))]
    public class AdsSettingsEditor : CustomInspector
    {
        private EditorAdsContainer[] adsContainers;
        private string[] availableProviderNames;
        private SerializedProperty activeProviderProperty;

        protected override void OnEnable()
        {
            base.OnEnable();

            activeProviderProperty = serializedObject.FindProperty("activeProvider");

            EnsureContainersPopulated();
            BuildEditorContainers();

            serializedObject.ApplyModifiedProperties();
        }

        private void EnsureContainersPopulated()
        {
            var containersProp = serializedObject.FindProperty("providerContainers");
            var containerTypes = TypeCache.GetTypesDerivedFrom<AdsProviderContainer>()
                .Where(t => !t.IsAbstract);

            bool modified = false;

            foreach (var type in containerTypes)
            {
                bool exists = false;
                for (int i = 0; i < containersProp.arraySize; i++)
                {
                    if (containersProp.GetArrayElementAtIndex(i).managedReferenceValue?.GetType() == type)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    containersProp.arraySize++;
                    containersProp.GetArrayElementAtIndex(containersProp.arraySize - 1).managedReferenceValue =
                        Activator.CreateInstance(type);
                    modified = true;
                }
            }

            if (modified)
                serializedObject.ApplyModifiedProperties();
        }

        private void BuildEditorContainers()
        {
            var containersProp = serializedObject.FindProperty("providerContainers");

            // Map ContainerType → EditorAdsContainer type via [AdsEditorContainer] attribute
            var editorContainerMap = new Dictionary<Type, Type>();
            foreach (var editorType in TypeCache.GetTypesDerivedFrom<EditorAdsContainer>())
            {
                if (editorType.IsAbstract) continue;

                var attr = editorType.GetCustomAttribute<AdsEditorContainerAttribute>();
                if (attr != null)
                    editorContainerMap[attr.ContainerType] = editorType;
            }

            // Collect available provider names directly from containers in settings
            var providerNames = new List<string>();
            for (int i = 0; i < containersProp.arraySize; i++)
            {
                var container = containersProp.GetArrayElementAtIndex(i).managedReferenceValue as AdsProviderContainer;
                if (container != null)
                    providerNames.Add(container.ProviderName);
            }
            availableProviderNames = providerNames.ToArray();

            // Build editor containers matched to list elements
            var containers = new List<EditorAdsContainer>();
            for (int i = 0; i < containersProp.arraySize; i++)
            {
                var element = containersProp.GetArrayElementAtIndex(i);
                var containerType = element.managedReferenceValue?.GetType();
                if (containerType == null) continue;

                if (editorContainerMap.TryGetValue(containerType, out var editorType))
                {
                    var editorContainer = (EditorAdsContainer)Activator.CreateInstance(editorType);
                    editorContainer.Init(element);
                    containers.Add(editorContainer);
                }
            }

            adsContainers = containers.ToArray();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            DrawProviderSection();

            GUILayout.Space(8);

            if (adsContainers != null)
            {
                for (int i = 0; i < adsContainers.Length; i++)
                {
                    adsContainers[i].DrawContainer();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProviderSection()
        {
            if (availableProviderNames == null || availableProviderNames.Length == 0)
                return;

            EditorGUILayoutCustom.BeginBoxGroup("Active Provider");

            int currentIndex = Array.IndexOf(availableProviderNames, activeProviderProperty.stringValue);

            if (currentIndex < 0)
            {
                EditorGUILayout.HelpBox($"Unknown provider '{activeProviderProperty.stringValue}'. Select a valid one.", MessageType.Error);
                currentIndex = 0;
            }

            int newIndex = EditorGUILayout.Popup("Provider", currentIndex, availableProviderNames);

            if (newIndex != currentIndex)
                activeProviderProperty.stringValue = availableProviderNames[newIndex];

            EditorGUILayoutCustom.EndBoxGroup();
        }
    }
}
