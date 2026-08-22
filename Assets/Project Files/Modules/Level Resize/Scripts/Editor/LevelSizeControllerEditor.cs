using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.DeviceSimulation;
using UnityEngine;

namespace Watermelon
{
    [CustomEditor(typeof(LevelSizeController))]
    public class LevelSizeControllerEditor : CustomInspector
    {
        private SerializedProperty screenSizesProperty;

        private LevelSizeController levelSizeController;

        protected override void OnEnable()
        {
            screenSizesProperty = serializedObject.FindProperty("screenSizes");

            levelSizeController = (LevelSizeController)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            if (GUILayout.Button("Add or Override Screen Size"))
            {
                AddOrOverrideScreenSize(levelSizeController);
            }

            if(GUILayout.Button("Apply Recommended"))
            {
                int sizeIndex = levelSizeController.GetRecommendedScreenSizeIndex();
                if(sizeIndex != -1)
                {
                    SerializedProperty property = screenSizesProperty.GetArrayElementAtIndex(sizeIndex);

                    LevelSizeControllerEditor.ApplyScreenSize(property);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddOrOverrideScreenSize(LevelSizeController levelSizeController)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("Main Camera is not available.");
                return;
            }

            float width = camera.pixelWidth;
            float height = camera.pixelHeight;
            float aspectRatio = Mathf.Max(width, height) / Mathf.Min(width, height);

            LevelSizeController.ScreenSize newScreenSize = new LevelSizeController.ScreenSize(width, height, aspectRatio);

            // Collect custom data start
            //LevelDockBehavior levelDockBehavior = FindFirstObjectByType<LevelDockBehavior>(FindObjectsInactive.Include);
            // Collect custom data end

            SerializedProperty targetProperty = null;

            for (int i = 0; i < screenSizesProperty.arraySize; i++)
            {
                SerializedProperty screenSizeProperty = screenSizesProperty.GetArrayElementAtIndex(i);
                float existingWidth = screenSizeProperty.FindPropertyRelative("width").floatValue;
                float existingHeight = screenSizeProperty.FindPropertyRelative("height").floatValue;

                if (Mathf.Approximately(existingWidth, width) && Mathf.Approximately(existingHeight, height))
                {
                    targetProperty = screenSizeProperty;
                    break;
                }
            }

            if (targetProperty == null)
            {
                screenSizesProperty.InsertArrayElementAtIndex(screenSizesProperty.arraySize);
                targetProperty = screenSizesProperty.GetArrayElementAtIndex(screenSizesProperty.arraySize - 1);
                targetProperty.FindPropertyRelative("note").stringValue = "";
            }

            targetProperty.FindPropertyRelative("width").floatValue = width;
            targetProperty.FindPropertyRelative("height").floatValue = height;
            targetProperty.FindPropertyRelative("aspectRatio").floatValue = aspectRatio;

            // Apply custom data start
            //if (levelDockBehavior != null)
            //    targetProperty.FindPropertyRelative("dockPosition").vector3Value = levelDockBehavior.transform.position;
            // Apply custom data end

            // Add note based on the device name in Simulator Game View
            SerializedProperty noteProperty = targetProperty.FindPropertyRelative("note");
            if (string.IsNullOrEmpty(noteProperty.stringValue))
            {
                string note = GetSimulatorName();
                if (!string.IsNullOrEmpty(note))
                {
                    note += $" ({width}x{height})";
                }
                else
                {
                    note += $"{width}x{height}";
                }

                noteProperty.stringValue = note;
            }

            EditorUtility.SetDirty(levelSizeController);
        }

        public static void ApplyScreenSize(SerializedProperty property)
        {
            //LevelDockBehavior levelDockBehavior = FindFirstObjectByType<LevelDockBehavior>(FindObjectsInactive.Include);
            //if (levelDockBehavior != null)
            //{
            //    levelDockBehavior.transform.position = property.FindPropertyRelative("dockPosition").vector3Value;

            //    EditorUtility.SetDirty(levelDockBehavior);
            //}
        }

        private string GetSimulatorName()
        {
            try
            {
                // Get the assembly containing the DeviceSimulator
                Assembly assembly = Assembly.GetAssembly(typeof(DeviceSimulator));

                // Get the SimulatorWindow type
                Type windowType = assembly.GetType("UnityEditor.DeviceSimulation.SimulatorWindow");

                // Get the m_Main field from the SimulatorWindow type
                FieldInfo mainField = windowType.GetField("m_Main", BindingFlags.Instance | BindingFlags.NonPublic);

                // Check if the SimulatorWindow is open
                Type editorWindowType = typeof(EditorWindow);
                MethodInfo hasOpenInstancesMethod = editorWindowType.GetMethod("HasOpenInstances", BindingFlags.Static | BindingFlags.Public);
                MethodInfo genericMethod = hasOpenInstancesMethod.MakeGenericMethod(windowType);

                if (!(bool)genericMethod.Invoke(null, null))
                    return "";

                // Get the DeviceSimulatorMain type
                Type mainType = assembly.GetType("UnityEditor.DeviceSimulation.DeviceSimulatorMain");

                // Get the deviceIndex property from the DeviceSimulatorMain type
                PropertyInfo deviceIndexProperty = mainType.GetProperty("deviceIndex", BindingFlags.Instance | BindingFlags.Public);

                // Get the m_Devices field from the DeviceSimulatorMain type
                FieldInfo devicesField = mainType.GetField("m_Devices", BindingFlags.Instance | BindingFlags.NonPublic);

                // Find the SimulatorWindow instance
                object simulatorWindow = UnityEngine.Resources.FindObjectsOfTypeAll(windowType).FirstOrDefault();

                // Get the main instance from the SimulatorWindow
                object main = mainField.GetValue(simulatorWindow);

                // Get the devices array from the main instance
                object[] devices = devicesField.GetValue(main) as object[];

                // Get the current device index
                int index = (int)deviceIndexProperty.GetValue(main);

                // Get the current device
                object device = devices[index];

                // Get the deviceInfo field from the device
                FieldInfo infoField = device.GetType().GetField("deviceInfo", BindingFlags.Instance | BindingFlags.Public);

                // Get the deviceInfo value
                object infoValue = infoField.GetValue(device);

                // Get the friendlyName field from the deviceInfo
                FieldInfo friendlyNameField = infoValue.GetType().GetField("friendlyName", BindingFlags.Instance | BindingFlags.Public);

                // Get the friendly name of the device
                return friendlyNameField.GetValue(infoValue) as string;
            }
            catch
            {
                return "";
            }
        }
    }
}