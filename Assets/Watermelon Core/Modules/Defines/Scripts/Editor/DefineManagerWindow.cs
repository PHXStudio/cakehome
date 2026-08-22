using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Watermelon
{
    public class DefineManagerWindow : EditorWindow
    {      
        private Define[] projectDefines;

        private bool isDefinesSame;
        private bool isRequireInit;

        [MenuItem("Window/Watermelon/Tools/Define Manager", priority = -50)]
        public static void ShowWindow()
        {
            DefineManagerWindow window = GetWindow<DefineManagerWindow>(true);
            window.minSize = new Vector2(300, 200);
            window.titleContent = new GUIContent("Define Manager");
        }

        protected void OnEnable()
        {
            isRequireInit = true;

            CacheVariables();
        }

        private void CacheVariables()
        {
            List<Define> defines = new List<Define>();

            // Project-type: [DefineAttribute] with no AssemblyType — a manual flag, not auto-detected.
            List<DefineAttribute> defineAttributes = DefineManager.GetAllDefineAttributes();
            foreach (DefineAttribute defineAttribute in defineAttributes)
            {
                if (string.IsNullOrEmpty(defineAttribute.AssemblyType) && defines.FindIndex(x => x.define == defineAttribute.Define) == -1)
                    defines.Add(new Define(defineAttribute.Define, Define.Type.Project));
            }

            // Auto-type: anything DefineManager tracks and manages itself (module presence/absence).
            // Everything else already present in PlayerSettings is ThirdParty — either orphaned
            // leftovers, or a define some third-party SDK sets and manages on its own. We deliberately
            // leave those untouched from CheckAutoDefines: toggling a define we don't own would race
            // against whatever set it and recompile the project forever.
            List<RegisteredDefine> registeredDefines = DefineManager.GetDynamicDefines(defineAttributes);

            foreach (string define in DefineManager.GetActiveDefines())
            {
                if (registeredDefines.FindIndex(x => x.Define == define) != -1)
                    defines.Add(new Define(define, Define.Type.Auto, true));
                else if (defines.FindIndex(x => x.define == define) == -1)
                    defines.Add(new Define(define, Define.Type.ThirdParty, true));
            }

            projectDefines = defines.ToArray();

            LoadActiveDefines();
        }

        private void LoadActiveDefines()
        {
            foreach (string define in DefineManager.GetActiveDefines())
            {
                int defineIndex = Array.FindIndex(projectDefines, x => x.define == define);
                if (defineIndex != -1)
                    projectDefines[defineIndex].isEnabled = true;
            }
        }

        private bool CompareDefines()
        {
            string[] currentDefines = DefineManager.GetActiveDefines();

            foreach (Define define in projectDefines)
            {
                if (define.isEnabled != currentDefines.Contains(define.define))
                    return false;
            }

            return true;
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorCustomStyles.Skin.box);
            
            if(!projectDefines.IsNullOrEmpty())
            {
                EditorGUI.BeginChangeCheck();

                int customDefineIndex = 0;

                for (int i = 0; i < projectDefines.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    switch (projectDefines[i].type)
                    {
                        case Define.Type.Auto:

                            projectDefines[i].isEnabled = EditorGUILayout.Toggle(projectDefines[i].isEnabled, GUILayout.Width(20));
                            EditorGUILayout.LabelField(projectDefines[i].define + " (Auto)");

                            break;
                        case Define.Type.Project:
                            projectDefines[i].isEnabled = EditorGUILayout.Toggle(projectDefines[i].isEnabled, GUILayout.Width(20));
                            EditorGUILayout.LabelField(projectDefines[i].define);

                            break;
                        case Define.Type.ThirdParty:
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.Toggle(true, GUILayout.Width(20));
                            EditorGUI.EndDisabledGroup();

                            EditorGUILayout.LabelField(projectDefines[i].define + " (Thrid Party)");

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("X", EditorCustomStyles.buttonRed, GUILayout.Height(18), GUILayout.Width(18)))
                            {
                                if (EditorUtility.DisplayDialog("Remove define", "Are you sure you want to remove define? This removes it from every synced platform, not just the active one.", "Remove", "Cancel"))
                                {
                                    DefineManager.DisableDefineForAllPlatforms(projectDefines[i].define);

                                    // projectDefines is only rebuilt on OnEnable — without this the
                                    // removed entry stays in the list with isEnabled still true, and
                                    // a later "Apply Defines" click would write it right back.
                                    CacheVariables();
                                    isRequireInit = true;

                                    return;
                                }
                            }

                            customDefineIndex++;
                            break;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if(EditorGUI.EndChangeCheck())
                {
                    isRequireInit = true;
                }
            }
            else
            {
                EditorGUILayout.LabelField("There are no defines in project.");
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorCustomStyles.Skin.box);
            
            if (isRequireInit)
            {
                isDefinesSame = CompareDefines();

                isRequireInit = false;
            }

            EditorGUI.BeginDisabledGroup(isDefinesSame);

            if (GUILayout.Button("Apply Defines", EditorCustomStyles.button))
            {
                DefineManager.SetActiveDefines(projectDefines.Where(x => x.isEnabled).Select(x => x.define));

                return;
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Check Auto Defines", EditorCustomStyles.button))
            {
                DefineManager.CheckAutoDefines();

                return;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayoutCustom.DrawCompileWindow(new Rect(0, 0, Screen.width, Screen.height));
        }

        [System.Serializable]
        private class Define
        {
            public string define;
            public Type type;

            public bool isEnabled;

            public Define(string define, Type type, bool isEnabled = false)
            {
                this.define = define;
                this.type = type;
                this.isEnabled = isEnabled;
            }

            public enum Type
            {
                Project = 1,
                ThirdParty = 2,
                Auto = 3
            }
        }
    }
}