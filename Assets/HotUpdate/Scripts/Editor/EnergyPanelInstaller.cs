using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    /// <summary>
    /// Installs the merge-style Energy Panel (EnergyUIPanel) into the hub pages that used to show
    /// the lives indicator: saves the UI Header's Energy Panel subtree as a prefab, then swaps the
    /// Lives Indicator instance in each host prefab for it (same anchors/position/sibling index).
    /// Also removes the retired UI Add Lives Panel page from the Game scene.
    /// </summary>
    public static class EnergyPanelInstaller
    {
        private const string GAME_SCENE_PATH = "Assets/Project Files/Game/Scenes/Game.unity";
        private const string ENERGY_PANEL_PREFAB_PATH = "Assets/Project Files/Game/Prefabs/UI/Energy Panel.prefab";

        private static readonly string[] HOST_PREFABS =
        {
            "Assets/Project Files/Game/Prefabs/UI/Canvas/UI Main Menu.prefab",
            "Assets/Project Files/Game/Prefabs/UI/Canvas/UI Game Over.prefab",
            "Assets/Project Files/Game/Prefabs/UI Store/UI IAP Store.prefab",
        };

        [MenuItem("Actions/Merge/6. Install Energy Panels")]
        private static void Install()
        {
            // --- 1. 从 UI Header 页提取 Energy Panel 为 prefab ---
            Scene gameScene = EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);

            GameObject energyPanel = FindByPath(gameScene, "UI Main Canvas/UI Header/Safe Area/Top Panel/Resources/Energy Panel");
            if (energyPanel == null)
            {
                Debug.LogError("[EnergyPanelInstaller] 找不到 UI Header 下的 Energy Panel 节点");
                return;
            }

            GameObject panelPrefab = PrefabUtility.SaveAsPrefabAsset(energyPanel, ENERGY_PANEL_PREFAB_PATH);
            Debug.Log($"[EnergyPanelInstaller] 已保存 {ENERGY_PANEL_PREFAB_PATH}");

            // --- 2. 删除场景里的 UI Add Lives Panel 页面 ---
            GameObject canvas = FindRoot(gameScene, "UI Main Canvas");
            Transform addLives = canvas != null ? canvas.transform.Find("UI Add Lives Panel") : null;
            if (addLives != null)
            {
                Object.DestroyImmediate(addLives.gameObject);
                EditorSceneManager.MarkSceneDirty(gameScene);
                EditorSceneManager.SaveScene(gameScene);
                Debug.Log("[EnergyPanelInstaller] 已删除场景中的 UI Add Lives Panel 页面");
            }

            // --- 3. 替换三个宿主 prefab 中的 Lives Indicator ---
            foreach (string hostPath in HOST_PREFABS)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(hostPath);
                Transform lives = FindRecursive(root.transform, "Lives Indicator");
                if (lives == null)
                {
                    Debug.LogWarning($"[EnergyPanelInstaller] {hostPath} 中没有 Lives Indicator，跳过");
                    PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                Transform parent = lives.parent;
                int siblingIndex = lives.GetSiblingIndex();
                RectTransform livesRect = (RectTransform)lives;
                Vector2 anchorMin = livesRect.anchorMin, anchorMax = livesRect.anchorMax;
                Vector2 anchoredPos = livesRect.anchoredPosition, sizeDelta = livesRect.sizeDelta;
                Vector2 offsetMin = livesRect.offsetMin, offsetMax = livesRect.offsetMax;

                Object.DestroyImmediate(lives.gameObject);

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab);
                instance.name = "Energy Panel";
                instance.transform.SetParent(parent, false);
                instance.transform.SetSiblingIndex(siblingIndex);

                RectTransform rect = (RectTransform)instance.transform;
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.anchoredPosition = anchoredPos;
                rect.sizeDelta = sizeDelta;
                rect.offsetMin = offsetMin;
                rect.offsetMax = offsetMax;

                PrefabUtility.SaveAsPrefabAsset(root, hostPath);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log($"[EnergyPanelInstaller] {hostPath} 已替换 Lives Indicator → Energy Panel");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[EnergyPanelInstaller] 完成。");
        }

        [MenuItem("Actions/Merge/7. Fix Energy Panel References")]
        private static void FixReferences()
        {
            // UIMainMenu.livesIndicatorScalable.transform 原本指向 Lives Indicator 内部节点，
            // 换成 Energy Panel 后引用断裂 —— 重指到新面板根的 RectTransform。
            const string mainMenuPath = "Assets/Project Files/Game/Prefabs/UI/Canvas/UI Main Menu.prefab";

            GameObject root = PrefabUtility.LoadPrefabContents(mainMenuPath);
            try
            {
                UIMainMenu page = root.GetComponent<UIMainMenu>();
                Transform energyPanel = FindRecursive(root.transform, "Energy Panel");
                if (page == null || energyPanel == null)
                {
                    Debug.LogError($"[EnergyPanelInstaller] 引用修复失败: page={page != null} energyPanel={energyPanel != null}");
                    return;
                }

                SerializedObject so = new SerializedObject(page);
                SerializedProperty prop = so.FindProperty("livesIndicatorScalable.transform");
                if (prop == null)
                {
                    Debug.LogError("[EnergyPanelInstaller] 找不到 livesIndicatorScalable.transform 属性");
                    return;
                }

                prop.objectReferenceValue = energyPanel;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, mainMenuPath);
                Debug.Log("[EnergyPanelInstaller] UI Main Menu 的 livesIndicatorScalable 已重指到 Energy Panel");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static GameObject FindByPath(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            GameObject root = FindRoot(scene, parts[0]);
            if (root == null) return null;
            Transform cur = root.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                cur = cur.Find(parts[i]);
                if (cur == null) return null;
            }
            return cur.gameObject;
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
    }
}
