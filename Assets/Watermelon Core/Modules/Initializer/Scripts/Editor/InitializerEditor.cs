using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    [CustomEditor(typeof(Initializer))]
    public class InitializerEditor : Editor
    {
        private SerializedProperty initSettingsProperty;
        private SerializedProperty sdkInitializerProperty;
        private SerializedProperty eventSystemProperty;

        private static readonly GUIContent addContent = new GUIContent("+");

        private void OnEnable()
        {
            initSettingsProperty = serializedObject.FindProperty("initSettings");
            sdkInitializerProperty = serializedObject.FindProperty("sdkInitializer");
            eventSystemProperty = serializedObject.FindProperty("eventSystem");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(initSettingsProperty);
            EditorGUILayout.PropertyField(sdkInitializerProperty);
            EditorGUILayout.PropertyField(eventSystemProperty);

            EditorGUILayout.Space(4);

            Initializer initializer = (Initializer)target;

            DrawPreInitSection(initializer);
            EditorGUILayout.Space(4);
            DrawLoadingStepsSection(initializer);
            EditorGUILayout.Space(4);
            DrawModulesSection();
            EditorGUILayout.Space(4);
            DrawSDKSection();

            serializedObject.ApplyModifiedProperties();
        }

        // ─── ① Pre-Init ───────────────────────────────────────────────────────────

        private void DrawPreInitSection(Initializer initializer)
        {
            bool addClicked = EditorGUILayoutCustom.BeginButtonBoxGroup(
                "① Pre-Init",
                addContent,
                EditorCustomStyles.buttonMini,
                false
            );

            IPreInitializable[] components = initializer.GetComponents<IPreInitializable>();

            if (components.Length == 0)
            {
                EditorGUILayout.HelpBox("No IPreInitializable components on Initializer.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < components.Length; i++)
                    DrawComponentRow(i + 1, components[i] as MonoBehaviour, null, null);
            }

            EditorGUILayoutCustom.EndBoxGroup();

            if (addClicked)
                ShowAddComponentMenu(initializer.gameObject, typeof(IPreInitializable));
        }

        // ─── ② Loading Steps ──────────────────────────────────────────────────────

        private void DrawLoadingStepsSection(Initializer initializer)
        {
            bool addClicked = EditorGUILayoutCustom.BeginButtonBoxGroup(
                "② Loading Steps",
                addContent,
                EditorCustomStyles.buttonMini,
                false
            );

            ILoadingStep[] steps = initializer.GetComponents<ILoadingStep>();

            if (steps.Length == 0)
            {
                EditorGUILayout.HelpBox("No ILoadingStep components on Initializer.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < steps.Length; i++)
                    DrawComponentRow(i + 1, steps[i] as MonoBehaviour, steps[i].LoadingMessage, null);
            }

            EditorGUILayoutCustom.EndBoxGroup();

            if (addClicked)
                ShowAddComponentMenu(initializer.gameObject, typeof(ILoadingStep));
        }

        // ─── ③ Init Modules ───────────────────────────────────────────────────────

        private void DrawModulesSection()
        {
            ProjectInitSettings settings = initSettingsProperty.objectReferenceValue as ProjectInitSettings;

            GenericMenu modulesMenu = BuildModulesMenu(settings);
            EditorGUILayoutCustom.BeginMenuBoxGroup("③ Init Modules", modulesMenu);

            if (settings == null)
            {
                EditorGUILayout.HelpBox("Assign Init Settings to see modules.", MessageType.Info);
                EditorGUILayoutCustom.EndBoxGroup();
                return;
            }

            InitModule[] modules = settings.Modules;

            if (modules == null || modules.Length == 0)
            {
                EditorGUILayout.HelpBox("No modules in Init Settings.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < modules.Length; i++)
                {
                    if (modules[i] == null) continue;

                    EditorGUILayout.BeginHorizontal(EditorCustomStyles.box);
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(20));
                    EditorGUILayout.LabelField(modules[i].ModuleName, EditorCustomStyles.labelBold);
                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        Selection.activeObject = settings;
                        EditorGUIUtility.PingObject(settings);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayoutCustom.EndBoxGroup();
        }

        private GenericMenu BuildModulesMenu(ProjectInitSettings settings)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(new GUIContent("Open Init Settings"), false, () =>
            {
                if (settings != null)
                {
                    Selection.activeObject = settings;
                    EditorGUIUtility.PingObject(settings);
                }
            });

            if (settings == null)
                return menu;

            menu.AddSeparator("");

            InitModule[] currentModules = settings.Modules;

            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<InitModule>();
            bool hasItems = false;

            foreach (Type type in types)
            {
                if (type.IsAbstract) continue;

                RegisterModuleAttribute attr = (RegisterModuleAttribute)Attribute.GetCustomAttribute(type, typeof(RegisterModuleAttribute));
                if (attr == null || attr.Core) continue;

                bool isAlreadyActive = currentModules != null && currentModules.Any(m => m != null && m.GetType() == type);
                if (isAlreadyActive)
                {
                    menu.AddDisabledItem(new GUIContent("Add Module/" + attr.Path), false);
                }
                else
                {
                    Type capturedType = type;
                    menu.AddItem(new GUIContent("Add Module/" + attr.Path), false, () => AddModule(settings, capturedType));
                    hasItems = true;
                }
            }

            if (!hasItems)
                menu.AddDisabledItem(new GUIContent("Add Module/(no modules available)"), false);

            return menu;
        }

        private void AddModule(ProjectInitSettings settings, Type moduleType)
        {
            Undo.RecordObject(settings, "Add Module");

            SerializedObject settingsObj = new SerializedObject(settings);
            settingsObj.Update();

            SerializedProperty modulesProperty = settingsObj.FindProperty("modules");
            modulesProperty.arraySize++;

            InitModule module = (InitModule)ScriptableObject.CreateInstance(moduleType);
            module.name = moduleType.ToString();
            module.hideFlags = HideFlags.HideInHierarchy;

            AssetDatabase.AddObjectToAsset(module, settings);
            modulesProperty.GetArrayElementAtIndex(modulesProperty.arraySize - 1).objectReferenceValue = module;

            settingsObj.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Editor editor = Editor.CreateEditor(module);
            (editor as InitModuleEditor)?.OnCreated();
            DestroyImmediate(editor);
        }

        // ─── ④ SDK Initialization ─────────────────────────────────────────────────

        private void DrawSDKSection()
        {
            SDKInitializer sdkInitializer = sdkInitializerProperty.objectReferenceValue as SDKInitializer;

            bool addClicked = EditorGUILayoutCustom.BeginButtonBoxGroup(
                "④ SDK Initialization",
                addContent,
                EditorCustomStyles.buttonMini,
                sdkInitializer == null
            );

            if (sdkInitializer == null)
            {
                EditorGUILayout.HelpBox("Assign SDKInitializer to see SDK behaviors.", MessageType.Info);
                EditorGUILayoutCustom.EndBoxGroup();
                return;
            }

            SDKBehavior[] behaviors = sdkInitializer.GetComponents<SDKBehavior>();
            ISDKTaskBehavior[] tasks = sdkInitializer.GetComponents<ISDKTaskBehavior>();

            bool hasContent = behaviors.Length > 0;

            int rowIndex = 1;

            for (int i = 0; i < behaviors.Length; i++)
                DrawComponentRow(rowIndex++, behaviors[i], null, "[SDK]");

            for (int i = 0; i < tasks.Length; i++)
            {
                MonoBehaviour mb = tasks[i] as MonoBehaviour;
                if (mb != null && mb is SDKBehavior) continue;

                DrawComponentRow(rowIndex++, mb, null, "[Task]");
                hasContent = true;
            }

            if (!hasContent)
                EditorGUILayout.HelpBox("No SDKBehavior or ISDKTaskBehavior components on SDKInitializer.", MessageType.Info);

            EditorGUILayoutCustom.EndBoxGroup();

            if (addClicked)
                ShowAddSDKMenu(sdkInitializer.gameObject);
        }

        private void ShowAddSDKMenu(GameObject sdkGo)
        {
            GenericMenu menu = new GenericMenu();
            bool hasItems = false;

            foreach (Type type in TypeCache.GetTypesDerivedFrom<SDKBehavior>())
            {
                if (type.IsAbstract) continue;
                if (sdkGo.GetComponent(type) != null) continue;

                Type capturedType = type;
                menu.AddItem(new GUIContent("Add SDK Behavior/" + type.Name), false, () =>
                {
                    Undo.AddComponent(sdkGo, capturedType);
                    EditorUtility.SetDirty(sdkGo);
                });
                hasItems = true;
            }

            foreach (Type type in TypeCache.GetTypesDerivedFrom<ISDKTaskBehavior>())
            {
                if (type.IsAbstract || !type.IsSubclassOf(typeof(MonoBehaviour))) continue;
                if (sdkGo.GetComponent(type) != null) continue;

                Type capturedType = type;
                menu.AddItem(new GUIContent("Add SDK Task/" + type.Name), false, () =>
                {
                    Undo.AddComponent(sdkGo, capturedType);
                    EditorUtility.SetDirty(sdkGo);
                });
                hasItems = true;
            }

            if (!hasItems)
                menu.AddDisabledItem(new GUIContent("No available components"));

            menu.ShowAsContext();
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void ShowAddComponentMenu(GameObject go, Type interfaceType)
        {
            GenericMenu menu = new GenericMenu();
            bool hasItems = false;

            foreach (Type type in TypeCache.GetTypesDerivedFrom(interfaceType))
            {
                if (type.IsAbstract || !type.IsSubclassOf(typeof(MonoBehaviour))) continue;
                if (go.GetComponent(type) != null) continue;

                Type capturedType = type;
                menu.AddItem(new GUIContent(type.Name), false, () =>
                {
                    Undo.AddComponent(go, capturedType);
                    EditorUtility.SetDirty(go);
                });
                hasItems = true;
            }

            if (!hasItems)
                menu.AddDisabledItem(new GUIContent("No available components"));

            menu.ShowAsContext();
        }

        private void DrawComponentRow(int index, MonoBehaviour mb, string subtitle, string tag)
        {
            EditorGUILayout.BeginHorizontal(EditorCustomStyles.box);

            EditorGUILayout.LabelField($"{index}.", GUILayout.Width(20));

            if (mb != null)
            {
                if (tag != null)
                    EditorGUILayout.LabelField(tag, EditorCustomStyles.label, GUILayout.Width(40));

                EditorGUILayout.LabelField(mb.GetType().Name, EditorCustomStyles.labelBold);

                if (!string.IsNullOrEmpty(subtitle))
                    EditorGUILayout.LabelField(subtitle, EditorCustomStyles.label, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                    Selection.activeObject = mb;
            }
            else
            {
                EditorGUILayout.LabelField("(non-MonoBehaviour component)", EditorCustomStyles.label);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
