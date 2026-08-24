using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Watermelon
{
    [CustomEditor(typeof(PoolManager))]
    sealed internal class PoolManagerEditor : Editor
    {
        private const float TYPE_WIDTH = 130f;
        private const float COUNT_WIDTH = 55f;

        private string searchText = string.Empty;

        private GUIStyle centeredLabel;
        private GUIStyle headerLabel;
        private bool stylesInitialized;

        private void InitStyles()
        {
            if (stylesInitialized) return;
            stylesInitialized = true;

            centeredLabel = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11
            };

            headerLabel = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        public override void OnInspectorGUI()
        {
            InitStyles();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Runtime pool list is only available during Play Mode.", MessageType.Info);
                return;
            }

            IReadOnlyList<IPool> pools = PoolManager.Pools;

            // Search bar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            searchText = GUILayout.TextField(searchText, GUI.skin.FindStyle("ToolbarSearchTextField"), GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(searchText))
            {
                if (GUILayout.Button(GUIContent.none, GUI.skin.FindStyle("ToolbarSearchCancelButton")))
                {
                    searchText = "";
                    GUI.FocusControl(null);
                }
            }
            else
            {
                GUILayout.Button(GUIContent.none, GUI.skin.FindStyle("ToolbarSearchCancelButtonEmpty"));
            }
            GUILayout.EndHorizontal();

            if (pools == null || pools.Count == 0)
            {
                EditorGUILayout.HelpBox("No pools registered.", MessageType.Info);
                Repaint();
                return;
            }

            // Column headers
            float nameWidth = EditorGUIUtility.currentViewWidth - TYPE_WIDTH - COUNT_WIDTH * 2 - 30f;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Name", headerLabel, GUILayout.Width(nameWidth));
            GUILayout.Label("Type", headerLabel, GUILayout.Width(TYPE_WIDTH));
            GUILayout.Label("Total", headerLabel, GUILayout.Width(COUNT_WIDTH));
            GUILayout.Label("Active", headerLabel, GUILayout.Width(COUNT_WIDTH));
            EditorGUILayout.EndHorizontal();

            // Pool rows
            int visibleIndex = 0;
            foreach (IPool pool in pools)
            {
                if (!string.IsNullOrEmpty(searchText) &&
                    pool.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                Rect rowRect = EditorGUILayout.BeginHorizontal();

                if (Event.current.type == EventType.Repaint)
                {
                    Color bg = visibleIndex % 2 == 0
                        ? new Color(0f, 0f, 0f, 0.06f)
                        : Color.clear;
                    EditorGUI.DrawRect(rowRect, bg);
                }

                GUILayout.Label(pool.Name, GUILayout.Width(nameWidth));
                GUILayout.Label(GetTypeName(pool), centeredLabel, GUILayout.Width(TYPE_WIDTH));

                (int total, int active) = GetCounts(pool);

                GUILayout.Label(total.ToString(), centeredLabel, GUILayout.Width(COUNT_WIDTH));

                Color prevColor = GUI.contentColor;
                if (active > 0) GUI.contentColor = new Color(0.3f, 1f, 0.3f);
                GUILayout.Label(active.ToString(), centeredLabel, GUILayout.Width(COUNT_WIDTH));
                GUI.contentColor = prevColor;

                EditorGUILayout.EndHorizontal();
                visibleIndex++;
            }

            GUILayout.Space(4f);
            EditorGUILayout.LabelField("Pools: " + pools.Count, EditorStyles.miniLabel);

            Repaint();
        }

        private string GetTypeName(IPool pool)
        {
            Type t = pool.GetType();
            if (t.IsGenericType)
            {
                string baseName = t.GetGenericTypeDefinition().Name;
                baseName = baseName.Substring(0, baseName.IndexOf('`'));
                string argName = t.GetGenericArguments()[0].Name;
                return baseName + "<" + argName + ">";
            }
            return t.Name;
        }

        private (int total, int active) GetCounts(IPool pool)
        {
            if (pool is Pool)
            {
                FieldInfo field = typeof(Pool).GetField("pooledObjects", BindingFlags.NonPublic | BindingFlags.Instance);
                List<GameObject> list = field?.GetValue(pool) as List<GameObject>;
                if (list == null) return (0, 0);

                int active = 0;
                for (int i = 0; i < list.Count; i++)
                    if (list[i] != null && list[i].activeSelf) active++;

                return (list.Count, active);
            }

            if (pool is PoolMultiple)
            {
                FieldInfo field = typeof(PoolMultiple).GetField("multiPooledObjects", BindingFlags.NonPublic | BindingFlags.Instance);
                List<List<GameObject>> multiList = field?.GetValue(pool) as List<List<GameObject>>;
                if (multiList == null) return (0, 0);

                int total = 0, active = 0;
                for (int i = 0; i < multiList.Count; i++)
                {
                    total += multiList[i].Count;
                    for (int j = 0; j < multiList[i].Count; j++)
                        if (multiList[i][j] != null && multiList[i][j].activeSelf) active++;
                }
                return (total, active);
            }

            // PoolGeneric<T>
            {
                FieldInfo field = pool.GetType().GetField("pooledObjects", BindingFlags.NonPublic | BindingFlags.Instance);
                System.Collections.IList list = field?.GetValue(pool) as System.Collections.IList;
                if (list == null) return (0, 0);

                int active = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    Component c = list[i] as Component;
                    if (c != null && c.gameObject.activeSelf) active++;
                }
                return (list.Count, active);
            }
        }
    }
}

// -----------------
// Pool Manager v 1.6.5
// -----------------
