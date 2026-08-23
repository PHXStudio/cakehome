using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    /// <summary>
    /// One-shot importer that grafts the merge gameplay hierarchy (from the mergedev Game scene,
    /// copied to Assets/__MergeImport/MergeSource.unity) into this project's Game.unity.
    /// Provides a Dry Run report, the Execute step, and a re-runnable Verify pass.
    /// </summary>
    public static class MergeSceneImporter
    {
        private const string SOURCE_SCENE_PATH = "Assets/__MergeImport/MergeSource.unity";
        private const string GAME_SCENE_PATH = "Assets/Project Files/Game/Scenes/Game.unity";

        // page name in source scene -> name it should get in the Game scene (null = keep)
        private static readonly (string sourceName, string newName)[] PAGES =
        {
            ("UI Game", "UI Merge Game"),
            ("UI Main Menu", "UI Merge Menu"),
            ("UI Building", null),
            ("UI Zones", null),
            ("UI Info Window", null),
            ("UI Recover Energy", null),
            ("UI Level Up", null),
            ("UI Dialog", null),
            ("UI Rewards Confirmation Popup", null),
            ("UI Header", null),
            ("Tutorial Overlay", null),
        };

        private static readonly string[] TUTORIAL_OBJECTS =
        {
            "First Start Tutorial",
            "Spawner Reward Tutorial",
            "Building Upgrade Hint Tutorial",
        };

        // components to copy from the source Scripts Holder onto the Game scene Scripts Holder
        private static readonly string[] COMPONENTS_TO_COPY =
        {
            "Watermelon.MergeController",
            "Watermelon.SpawnerController",
            "Watermelon.TaskController",
            "Watermelon.ClientOrderHighlightController",
            "Watermelon.CurrencyCloud",
            "Watermelon.TutorialController",
        };

        [MenuItem("Actions/Merge/1. Import Merge Scene (Dry Run)")]
        private static void DryRun()
        {
            StringBuilder report = new StringBuilder("[MergeImporter] Dry Run\n");

            if (!System.IO.File.Exists(SOURCE_SCENE_PATH))
            {
                Debug.LogError($"[MergeImporter] 源场景不存在: {SOURCE_SCENE_PATH}\n请先把 mergedev 的 Game.unity 复制到该路径。");
                return;
            }

            Scene sourceScene = EditorSceneManager.OpenScene(SOURCE_SCENE_PATH, OpenSceneMode.Additive);
            try
            {
                foreach (var (sourceName, newName) in PAGES)
                {
                    GameObject page = FindRoot(sourceScene, sourceName);
                    report.AppendLine(page != null
                        ? $"  ✓ 页面 {sourceName}{(newName != null ? $" → {newName}" : "")}"
                        : $"  ✗ 页面缺失: {sourceName}");
                }

                GameObject scriptsHolder = FindRoot(sourceScene, "Scripts Holder");
                foreach (string tutorialName in TUTORIAL_OBJECTS)
                {
                    Transform t = scriptsHolder?.transform.Find(tutorialName);
                    report.AppendLine(t != null ? $"  ✓ 教程对象 {tutorialName}" : $"  ✗ 教程对象缺失: {tutorialName}");
                }

                foreach (string typeName in COMPONENTS_TO_COPY)
                {
                    Component c = scriptsHolder?.GetComponent(typeName);
                    report.AppendLine(c != null ? $"  ✓ 组件 {typeName}" : $"  ✗ 组件缺失: {typeName}");
                }

                // missing script scan on the whitelist subtrees
                int missing = 0;
                foreach (var (sourceName, _) in PAGES)
                {
                    GameObject page = FindRoot(sourceScene, sourceName);
                    if (page == null) continue;
                    foreach (MonoBehaviour mb in page.GetComponentsInChildren<MonoBehaviour>(true))
                        if (mb == null) missing++;
                }
                report.AppendLine(missing == 0 ? "  ✓ 搬运子树无 missing script" : $"  ✗ 搬运子树有 {missing} 个 missing script!");
            }
            finally
            {
                EditorSceneManager.CloseScene(sourceScene, true);
            }

            Debug.Log(report.ToString());
        }

        [MenuItem("Actions/Merge/2. Import Merge Scene (Execute)")]
        private static void Execute()
        {
            if (!EditorUtility.DisplayDialog("Merge Scene Importer",
                "将把 MergeSource.unity 的合成玩法内容搬进 Game.unity。\n请确认 Game.unity 已提交 git 或有备份。继续？",
                "执行", "取消"))
                return;

            Scene gameScene = EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);
            Scene sourceScene = EditorSceneManager.OpenScene(SOURCE_SCENE_PATH, OpenSceneMode.Additive);
            try
            {
                GameObject canvas = FindRoot(gameScene, "UI Main Canvas");
                GameObject scriptsHolder = FindRoot(gameScene, "Scripts Holder");
                GameObject bottomNav = FindDirectChild(canvas, "Bottom Nav Bar");
                if (canvas == null || scriptsHolder == null)
                {
                    Debug.LogError("[MergeImporter] Game.unity 中找不到 UI Main Canvas 或 Scripts Holder!");
                    return;
                }

                // --- 1. 搬运页面 ---
                int insertIndex = bottomNav != null ? bottomNav.transform.GetSiblingIndex() : canvas.transform.childCount;
                foreach (var (sourceName, newName) in PAGES)
                {
                    GameObject page = FindRoot(sourceScene, sourceName);
                    if (page == null)
                    {
                        Debug.LogWarning($"[MergeImporter] 源场景缺少页面 {sourceName}，跳过");
                        continue;
                    }
                    if (FindDirectChild(canvas, newName ?? sourceName) != null)
                    {
                        Debug.LogWarning($"[MergeImporter] Game.unity 已存在 {newName ?? sourceName}，跳过（幂等）");
                        continue;
                    }

                    SceneManager.MoveGameObjectToScene(page, gameScene);
                    page.transform.SetParent(canvas.transform, false);
                    page.transform.SetSiblingIndex(insertIndex++);
                    if (newName != null) page.name = newName;
                    Debug.Log($"[MergeImporter] 搬入页面 {sourceName}{(newName != null ? $" → {newName}" : "")}");
                }

                // --- 2. 搬运教程对象 ---
                GameObject sourceScriptsHolder = FindRoot(sourceScene, "Scripts Holder");
                foreach (string tutorialName in TUTORIAL_OBJECTS)
                {
                    Transform t = sourceScriptsHolder?.transform.Find(tutorialName);
                    if (t == null) continue;
                    if (scriptsHolder.transform.Find(tutorialName) != null) continue;

                    SceneManager.MoveGameObjectToScene(t.gameObject, gameScene);
                    t.SetParent(scriptsHolder.transform, false);
                    Debug.Log($"[MergeImporter] 搬入教程对象 {tutorialName}");
                }

                // --- 3. 复制控制器组件（SerializedObject 逐字段复制，引用保持在已搬入的活体对象上） ---
                foreach (string typeName in COMPONENTS_TO_COPY)
                {
                    Component source = sourceScriptsHolder?.GetComponent(typeName);
                    if (source == null)
                    {
                        Debug.LogWarning($"[MergeImporter] 源 Scripts Holder 缺少组件 {typeName}，跳过");
                        continue;
                    }
                    if (scriptsHolder.GetComponent(typeName) != null)
                    {
                        Debug.LogWarning($"[MergeImporter] Game.unity Scripts Holder 已有 {typeName}，跳过（幂等）");
                        continue;
                    }

                    Component target = scriptsHolder.AddComponent(source.GetType());
                    SerializedObject sourceSo = new SerializedObject(source);
                    SerializedObject targetSo = new SerializedObject(target);
                    SerializedProperty prop = sourceSo.GetIterator();
                    while (prop.NextVisible(true))
                    {
                        // 跳过 Unity 对象头字段（m_ObjectHideFlags/m_GameObject/m_Script 等），只拷自定义序列化字段
                        if (prop.propertyPath.StartsWith("m_")) continue;
                        targetSo.CopyFromSerializedProperty(prop);
                    }
                    targetSo.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log($"[MergeImporter] 复制组件 {typeName}");
                }

                EditorSceneManager.MarkSceneDirty(gameScene);
                EditorSceneManager.SaveScene(gameScene);
                Debug.Log("[MergeImporter] 完成！已保存 Game.unity。请运行 Actions/Merge/3. Verify Import 验证。");
            }
            finally
            {
                EditorSceneManager.CloseScene(sourceScene, true);
            }
        }

        [MenuItem("Actions/Merge/5. Add Fragment Exchange Entry")]
        private static void AddFragmentEntry()
        {
            Scene gameScene = EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);

            GameObject mergeGame = FindRoot(gameScene, "UI Main Canvas")?.transform.Find("UI Merge Game")?.gameObject;
            if (mergeGame == null)
            {
                Debug.LogError("[MergeImporter] 找不到 UI Merge Game 页面，请先执行步骤 2 (Import Merge Scene)。");
                return;
            }

            Transform bottomPanel = FindRecursive(mergeGame.transform, "Bottom Panel");
            if (bottomPanel == null)
            {
                Debug.LogError("[MergeImporter] UI Merge Game 内找不到 Bottom Panel。");
                return;
            }

            if (bottomPanel.Find("Fragment Button") != null)
            {
                Debug.Log("[MergeImporter] Fragment Button 已存在（幂等跳过）。");
                return;
            }

            // 在 Map Button 旁加一个小兑换按钮
            GameObject buttonGo = new GameObject("Fragment Button", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(OpenFragmentPanelButton));
            buttonGo.layer = 5;
            buttonGo.transform.SetParent(bottomPanel, false);

            RectTransform rect = (RectTransform)buttonGo.transform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(140f, 140f);
            rect.anchoredPosition = new Vector2(-20f, 0f);

            UnityEngine.UI.Image image = buttonGo.GetComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.85f, 0.55f, 0.3f, 1f);

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.layer = 5;
            labelGo.transform.SetParent(buttonGo.transform, false);
            RectTransform labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            UnityEngine.UI.Text label = labelGo.AddComponent<UnityEngine.UI.Text>();
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Project Files/Game/Fonts/Resources/ChineseFont.ttf");
            label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = "碎片";
            label.fontSize = 40;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            Debug.Log("[MergeImporter] 已在 UI Merge Game/Bottom Panel 添加碎片兑换入口按钮。");
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        [MenuItem("Actions/Merge/4. Remove Old Shop From Scene")]
        private static void RemoveOldShop()
        {
            Scene gameScene = EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);

            int removed = 0;

            // 根节点 ShopWorld（含 ShopCamera / Environment_Shop1 / DisplayStage / ShopIdleProducer）
            GameObject shopWorld = FindRoot(gameScene, "ShopWorld");
            if (shopWorld != null)
            {
                Object.DestroyImmediate(shopWorld);
                removed++;
                Debug.Log("[MergeImporter] 已删除 ShopWorld 根节点");
            }

            // 画布下的 UIShopPage prefab 实例
            GameObject canvas = FindRoot(gameScene, "UI Main Canvas");
            if (canvas != null)
            {
                for (int i = canvas.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = canvas.transform.GetChild(i);
                    if (child.name == "UIShopPage" || child.name == "UI Shop Page" || child.name.StartsWith("UIShopPage ("))
                    {
                        Object.DestroyImmediate(child.gameObject);
                        removed++;
                        Debug.Log($"[MergeImporter] 已删除画布子节点 {child.name}");
                    }
                }
            }

            if (removed == 0)
            {
                Debug.Log("[MergeImporter] 没有找到旧门店对象（可能已清理）");
                return;
            }

            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene);
            Debug.Log($"[MergeImporter] 旧门店场景清理完成，共删除 {removed} 个对象。");
        }

        [MenuItem("Actions/Merge/3. Verify Import")]
        private static void Verify()
        {
            StringBuilder report = new StringBuilder("[MergeImporter] Verify\n");
            int errors = 0;

            Scene gameScene = EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);
            GameObject canvas = FindRoot(gameScene, "UI Main Canvas");
            GameObject scriptsHolder = FindRoot(gameScene, "Scripts Holder");

            foreach (var (sourceName, newName) in PAGES)
            {
                string name = newName ?? sourceName;
                GameObject page = FindDirectChild(canvas, name);
                if (page == null) { report.AppendLine($"  ✗ 页面缺失: {name}"); errors++; }
            }

            foreach (string tutorialName in TUTORIAL_OBJECTS)
                if (scriptsHolder.transform.Find(tutorialName) == null) { report.AppendLine($"  ✗ 教程对象缺失: {tutorialName}"); errors++; }

            foreach (string typeName in COMPONENTS_TO_COPY)
                if (scriptsHolder.GetComponent(typeName) == null) { report.AppendLine($"  ✗ 组件缺失: {typeName}"); errors++; }

            // missing script 扫描（整个画布 + Scripts Holder）
            int missing = 0;
            foreach (MonoBehaviour mb in Object.FindObjectsOfType<MonoBehaviour>())
                if (mb == null) missing++;
            if (missing > 0) { report.AppendLine($"  ✗ 场景共有 {missing} 个 missing script"); errors++; }
            else report.AppendLine("  ✓ 无 missing script");

            // 关键序列化引用非空检查
            errors += CheckField(report, scriptsHolder, "Watermelon.MergeController", "mergeGrid");
            errors += CheckField(report, scriptsHolder, "Watermelon.MergeController", "database");
            errors += CheckField(report, scriptsHolder, "Watermelon.SpawnerController", "mergeGrid");
            errors += CheckField(report, scriptsHolder, "Watermelon.ClientOrderHighlightController", "highlightPrefab");
            errors += CheckField(report, scriptsHolder, "Watermelon.TutorialController", "tutorialCanvasController");

            report.AppendLine(errors == 0 ? "\n全部通过 ✓" : $"\n共 {errors} 个问题 ✗");
            Debug.Log(report.ToString());
        }

        private static int CheckField(StringBuilder report, GameObject holder, string typeName, string field)
        {
            Component c = holder.GetComponent(typeName);
            if (c == null) return 0; // 已在上一步报告
            SerializedObject so = new SerializedObject(c);
            SerializedProperty prop = so.FindProperty(field);
            bool ok = prop != null && prop.objectReferenceValue != null;
            if (!ok) { report.AppendLine($"  ✗ {typeName}.{field} 为空"); return 1; }
            report.AppendLine($"  ✓ {typeName}.{field} → {prop.objectReferenceValue.name}");
            return 0;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static GameObject FindDirectChild(GameObject parent, string name)
        {
            if (parent == null) return null;
            Transform t = parent.transform.Find(name);
            return t != null ? t.gameObject : null;
        }
    }
}
