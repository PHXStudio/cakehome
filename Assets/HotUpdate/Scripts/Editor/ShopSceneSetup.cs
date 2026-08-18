#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Places an editable ShopWorld under Game.unity so the Shop hub tab shows a real scene.
    /// Uses Prefabs/Shop/Shop1.prefab as the dollhouse environment.
    /// </summary>
    public static class ShopSceneSetup
    {
        private const string GAME_SCENE_PATH = "Assets/Project Files/Game/Scenes/Game.unity";
        private const string SHOP_PREFAB_PATH = "Assets/Project Files/Game/Prefabs/Shop/Shop1.prefab";
        private const string RESOURCES_ENV_PATH = "Assets/Project Files/Game/Resources/Shop/ShopEnvironment.prefab";
        private const string DATA_FOLDER = "Assets/Project Files/Data/Shop";
        private const string RESOURCES_FOLDER = "Assets/Project Files/Game/Resources/Shop";
        private const string CATALOG_PATH = DATA_FOLDER + "/Cake Catalog.asset";
        private const string CONFIG_PATH = DATA_FOLDER + "/Shop Config.asset";
        private const string RESOURCES_CATALOG_PATH = RESOURCES_FOLDER + "/Cake Catalog.asset";
        private const string RESOURCES_CONFIG_PATH = RESOURCES_FOLDER + "/Shop Config.asset";

        private const string SHOP0_SKYBOX_PATH = "Assets/Project Files/Game/Materials/Shop/Shop0 Skybox.mat";
        private const string RESOURCES_SKYBOX_PATH = RESOURCES_FOLDER + "/Shop0Skybox.mat";

        [MenuItem("Actions/Setup Shop System")]
        public static void Setup()
        {
            EnsureDataAssets(out CakeCatalog catalog, out _);
            EnsureShop0Skybox();
            EnsureResourcesEnvironmentLink();
            PlaceShopWorldInGameScene(forceRebuild: false);
            SoftenShopPageBackground();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("[Shop] Setup complete. ShopWorld is in Game.unity and bound to the Shop tab.");
            Selection.activeObject = catalog;
        }

        [MenuItem("Actions/Shop/Place ShopWorld In Game Scene")]
        public static void PlaceOnly()
        {
            EnsureShop0Skybox();
            EnsureResourcesEnvironmentLink();
            PlaceShopWorldInGameScene(forceRebuild: true);
            SoftenShopPageBackground();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[Shop] ShopWorld placed/rebuilt in Game.unity (inactive until Shop tab).");
        }

        [MenuItem("Actions/Shop/Create Shop0 Skybox")]
        public static void CreateShop0SkyboxMenu()
        {
            Material mat = EnsureShop0Skybox();
            AssetDatabase.SaveAssets();
            Selection.activeObject = mat;
            Debug.Log($"[Shop] Shop0 skybox ready at {SHOP0_SKYBOX_PATH}");
        }

        [MenuItem("Actions/Create Shop Data Assets")]
        public static void CreateDataAssetsOnly()
        {
            EnsureDataAssets(out _, out _);
            EnsureShop0Skybox();
            AssetDatabase.SaveAssets();
            Debug.Log("[Shop] Cake Catalog and Shop Config created/updated.");
        }

        /// <summary>
        /// Creates/refreshes the dedicated Shop0 procedural skybox material.
        /// </summary>
        public static Material EnsureShop0Skybox()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Project Files/Game/Materials"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Project Files/Game"))
                    AssetDatabase.CreateFolder("Assets/Project Files", "Game");
                AssetDatabase.CreateFolder("Assets/Project Files/Game", "Materials");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Project Files/Game/Materials/Shop"))
                AssetDatabase.CreateFolder("Assets/Project Files/Game/Materials", "Shop");

            if (!AssetDatabase.IsValidFolder("Assets/Project Files/Game/Resources"))
                AssetDatabase.CreateFolder("Assets/Project Files/Game", "Resources");
            if (!AssetDatabase.IsValidFolder(RESOURCES_FOLDER))
                AssetDatabase.CreateFolder("Assets/Project Files/Game/Resources", "Shop");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(SHOP0_SKYBOX_PATH);
            if (mat == null)
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader == null)
                {
                    Debug.LogError("[Shop] Skybox/Procedural shader not found.");
                    return null;
                }

                mat = new Material(shader) { name = "Shop0 Skybox" };
                ShopWorld.ConfigureShop0SkyMaterial(mat);
                AssetDatabase.CreateAsset(mat, SHOP0_SKYBOX_PATH);
            }
            else
            {
                ShopWorld.ConfigureShop0SkyMaterial(mat);
                EditorUtility.SetDirty(mat);
            }

            // Mirror into Resources for runtime ResolveShopSkybox()
            Material resMat = AssetDatabase.LoadAssetAtPath<Material>(RESOURCES_SKYBOX_PATH);
            if (resMat == null)
            {
                AssetDatabase.CopyAsset(SHOP0_SKYBOX_PATH, RESOURCES_SKYBOX_PATH);
            }
            else
            {
                ShopWorld.ConfigureShop0SkyMaterial(resMat);
                EditorUtility.SetDirty(resMat);
            }

            AssetDatabase.SaveAssets();
            return mat;
        }

        private static void EnsureResourcesEnvironmentLink()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Project Files/Game/Resources"))
                AssetDatabase.CreateFolder("Assets/Project Files/Game", "Resources");
            if (!AssetDatabase.IsValidFolder(RESOURCES_FOLDER))
                AssetDatabase.CreateFolder("Assets/Project Files/Game/Resources", "Shop");

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SHOP_PREFAB_PATH);
            if (source == null)
            {
                Debug.LogWarning($"[Shop] Missing environment prefab at {SHOP_PREFAB_PATH}");
                return;
            }

            // Copy as Resources asset so runtime fallback can instantiate the same art.
            if (AssetDatabase.LoadAssetAtPath<Object>(RESOURCES_ENV_PATH) != null)
                AssetDatabase.DeleteAsset(RESOURCES_ENV_PATH);

            if (!AssetDatabase.CopyAsset(SHOP_PREFAB_PATH, RESOURCES_ENV_PATH))
                Debug.LogWarning("[Shop] Failed to copy Shop1 into Resources/Shop/ShopEnvironment.prefab");
            else
                AssetDatabase.SaveAssets();
        }

        private static void PlaceShopWorldInGameScene(bool forceRebuild)
        {
            var scene = EditorSceneManager.OpenScene(GAME_SCENE_PATH, OpenSceneMode.Single);

            ShopWorld existing = Object.FindObjectOfType<ShopWorld>(true);
            if (existing != null)
            {
                if (!forceRebuild)
                {
                    // Upgrade flags on existing
                    SerializedObject soExist = new SerializedObject(existing);
                    soExist.FindProperty("sceneAuthored").boolValue = true;
                    soExist.FindProperty("layoutVersion").intValue = ShopWorld.LayoutVersion;
                    soExist.ApplyModifiedPropertiesWithoutUndo();
                    existing.gameObject.SetActive(false);
                    Debug.Log("[Shop] ShopWorld already in scene — marked sceneAuthored, left as-is. Use Actions/Shop/Place ShopWorld In Game Scene to rebuild.");
                    return;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject worldRoot = new GameObject("ShopWorld");
            worldRoot.SetActive(false);

            ShopWorld world = worldRoot.AddComponent<ShopWorld>();
            ShopIdleProducer producer = worldRoot.AddComponent<ShopIdleProducer>();

            ShopWorld.CreateCamera(worldRoot.transform, out Camera cam);

            // Environment from Shop1
            Transform environment = null;
            GameObject shopPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SHOP_PREFAB_PATH);
            if (shopPrefab != null)
            {
                GameObject envInstance = (GameObject)PrefabUtility.InstantiatePrefab(shopPrefab, worldRoot.transform);
                envInstance.name = "Environment_Shop1";
                envInstance.transform.localPosition = Vector3.zero;
                envInstance.transform.localRotation = Quaternion.identity;
                // Kenney room modules are large; keep shop dollhouse a bit smaller on mobile.
                envInstance.transform.localScale = Vector3.one * 0.5f;
                environment = envInstance.transform;
                ShopWorld.CenterEnvironmentOnOrigin(environment);
            }
            else
            {
                Debug.LogWarning("[Shop] Shop1.prefab not found — only empty ShopWorld + camera.");
            }

            // Bind via SerializedObject so private fields stick in the scene
            Material sky = EnsureShop0Skybox();
            SerializedObject so = new SerializedObject(world);
            so.FindProperty("sceneAuthored").boolValue = true;
            so.FindProperty("layoutVersion").intValue = ShopWorld.LayoutVersion;
            so.FindProperty("root").objectReferenceValue = worldRoot;
            so.FindProperty("shopCamera").objectReferenceValue = cam;
            so.FindProperty("idleProducer").objectReferenceValue = producer;
            so.FindProperty("environmentRoot").objectReferenceValue = environment;
            if (sky != null)
                so.FindProperty("shopSkybox").objectReferenceValue = sky;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Temporarily enable so bounds/camera reframe runs with real mesh bounds
            worldRoot.SetActive(true);
            world.ReframeToContent();
            worldRoot.SetActive(false);

            // Keep under scene root, sibling of Map / UI
            worldRoot.transform.SetAsLastSibling();

            Selection.activeGameObject = worldRoot;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Shop] Created scene-authored ShopWorld with Shop1 environment only (no cake stage).");
        }

        private static void EnsureDataAssets(out CakeCatalog catalog, out ShopConfig config)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Project Files/Data/Shop"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Project Files/Data"))
                    AssetDatabase.CreateFolder("Assets/Project Files", "Data");
                AssetDatabase.CreateFolder("Assets/Project Files/Data", "Shop");
            }

            catalog = AssetDatabase.LoadAssetAtPath<CakeCatalog>(CATALOG_PATH);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CakeCatalog>();
                SerializedObject so = new SerializedObject(catalog);
                SerializedProperty cakes = so.FindProperty("cakes");
                CakeDefinition[] defaults = CakeCatalog.BuildDefaultCakes();
                cakes.arraySize = defaults.Length;
                for (int i = 0; i < defaults.Length; i++)
                {
                    SerializedProperty element = cakes.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("id").stringValue = defaults[i].Id;
                    element.FindPropertyRelative("displayName").stringValue = defaults[i].DisplayName;
                    element.FindPropertyRelative("creditsPerHour").floatValue = defaults[i].CreditsPerHour;
                    element.FindPropertyRelative("displayColor").colorValue = defaults[i].DisplayColor;

                    SerializedProperty elements = element.FindPropertyRelative("elements");
                    elements.arraySize = defaults[i].Elements.Length;
                    for (int e = 0; e < defaults[i].Elements.Length; e++)
                        elements.GetArrayElementAtIndex(e).enumValueIndex = (int)defaults[i].Elements[e];
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(catalog, CATALOG_PATH);
            }

            config = AssetDatabase.LoadAssetAtPath<ShopConfig>(CONFIG_PATH);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ShopConfig>();
                AssetDatabase.CreateAsset(config, CONFIG_PATH);
            }

            AssetDatabase.SaveAssets();

            if (!AssetDatabase.IsValidFolder("Assets/Project Files/Game/Resources"))
                AssetDatabase.CreateFolder("Assets/Project Files/Game", "Resources");
            if (!AssetDatabase.IsValidFolder(RESOURCES_FOLDER))
                AssetDatabase.CreateFolder("Assets/Project Files/Game/Resources", "Shop");

            if (AssetDatabase.LoadAssetAtPath<Object>(RESOURCES_CATALOG_PATH) != null)
                AssetDatabase.DeleteAsset(RESOURCES_CATALOG_PATH);
            if (AssetDatabase.LoadAssetAtPath<Object>(RESOURCES_CONFIG_PATH) != null)
                AssetDatabase.DeleteAsset(RESOURCES_CONFIG_PATH);

            AssetDatabase.CopyAsset(CATALOG_PATH, RESOURCES_CATALOG_PATH);
            AssetDatabase.CopyAsset(CONFIG_PATH, RESOURCES_CONFIG_PATH);
            AssetDatabase.SaveAssets();
        }

        private static void SoftenShopPageBackground()
        {
            UIShopPage shopPage = Object.FindObjectOfType<UIShopPage>(true);
            if (shopPage == null)
                return;

            Transform bg = shopPage.transform.Find("Background");
            if (bg == null)
                return;

            Object.DestroyImmediate(bg.gameObject);
            EditorUtility.SetDirty(shopPage.gameObject);
        }
    }
}
#endif
