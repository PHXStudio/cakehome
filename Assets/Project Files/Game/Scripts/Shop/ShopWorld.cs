using UnityEngine;

namespace Watermelon
{
    public class ShopWorld : MonoBehaviour
    {
        public const int LayoutVersion = 3;

        private static ShopWorld instance;

        [Tooltip("When true this object is edited in Game.unity and will never be auto-destroyed/rebuilt.")]
        [SerializeField] bool sceneAuthored;
        [SerializeField] int layoutVersion;
        [SerializeField] GameObject root;
        [SerializeField] Camera shopCamera;
        [SerializeField] ShopShelfSlot[] shelves;
        [SerializeField] ShopIdleProducer idleProducer;
        [SerializeField] Transform freezerProp;
        [SerializeField] Transform displayStage;
        [SerializeField] Transform environmentRoot;

        private bool isVisible;

        public static bool IsVisible => instance != null && instance.isVisible;
        public bool IsSceneAuthored => sceneAuthored;

        private void Awake()
        {
            instance = this;

            if (root == null)
                root = gameObject;

            if (idleProducer == null)
                idleProducer = GetComponent<ShopIdleProducer>();

            if (idleProducer == null)
                idleProducer = gameObject.AddComponent<ShopIdleProducer>();

            EnsureShelfIndices();

            // Scene-authored worlds start inactive; do not call SetVisible(false) here —
            // Awake often runs as a side-effect of Enter() activating the root, and that
            // would immediately reverse the show request.
            isVisible = false;
            if (shopCamera != null)
                shopCamera.enabled = false;

            if (idleProducer != null)
                idleProducer.SetActive(false);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            ShopController.StateChanged -= RefreshVisuals;
        }

        private void OnEnable()
        {
            ShopController.StateChanged += RefreshVisuals;
        }

        private void OnDisable()
        {
            ShopController.StateChanged -= RefreshVisuals;
        }

        public static void SetVisible(bool visible)
        {
            if (!EnsureInstance(createIfMissing: visible))
                return;

            instance.SetVisibleInternal(visible);
        }

        public static void Hide()
        {
            if (instance == null)
            {
                instance = Object.FindObjectOfType<ShopWorld>(true);
                if (instance == null)
                    return;
            }

            instance.SetVisibleInternal(false);
        }

        public static void Refresh()
        {
            if (instance != null)
                instance.RefreshVisuals();
        }

        /// <summary>
        /// Bind references from editor setup / runtime builder.
        /// </summary>
        public void Bind(Camera camera, ShopShelfSlot[] shelfSlots, ShopIdleProducer producer, Transform stage, Transform environment, bool authored)
        {
            shopCamera = camera;
            shelves = shelfSlots;
            idleProducer = producer;
            displayStage = stage;
            environmentRoot = environment;
            sceneAuthored = authored;
            layoutVersion = LayoutVersion;
            if (root == null)
                root = gameObject;
        }

        private static bool EnsureInstance(bool createIfMissing)
        {
            if (instance != null && CanUse(instance))
                return true;

            if (instance != null && !CanUse(instance))
            {
                Object.Destroy(instance.gameObject);
                instance = null;
            }

            ShopWorld found = Object.FindObjectOfType<ShopWorld>(true);
            if (found != null)
            {
                if (CanUse(found))
                {
                    instance = found;
                    return true;
                }

                // Outdated runtime blob only
                if (!found.sceneAuthored)
                {
                    Object.Destroy(found.gameObject);
                }
                else
                {
                    // Scene-authored always wins even if version field lags
                    instance = found;
                    found.layoutVersion = LayoutVersion;
                    return true;
                }
            }

            if (!createIfMissing)
                return false;

            instance = BuildRuntimeWorld();
            return instance != null;
        }

        private static bool CanUse(ShopWorld world)
        {
            if (world == null)
                return false;

            if (world.sceneAuthored)
                return true;

            return world.layoutVersion == LayoutVersion;
        }

        private void SetVisibleInternal(bool visible)
        {
            isVisible = visible;

            if (root != null)
                root.SetActive(visible);

            if (shopCamera != null)
                shopCamera.enabled = visible;

            if (idleProducer != null)
                idleProducer.SetActive(visible);

            if (visible)
            {
                ShopController.EnsureInitialized();
                ShopController.OnShopOpened();
                // After Awake may have reset camera; re-enable and reframe center.
                if (shopCamera != null)
                    shopCamera.enabled = true;
                if (idleProducer != null)
                    idleProducer.SetActive(true);
                ReframeToContent();
                RefreshVisuals();
            }
        }

        private void EnsureShelfIndices()
        {
            if (shelves == null)
                return;

            for (int i = 0; i < shelves.Length; i++)
            {
                if (shelves[i] != null)
                    shelves[i].Setup(i);
            }
        }

        private void RefreshVisuals()
        {
            if (shelves == null)
                return;

            for (int i = 0; i < shelves.Length; i++)
            {
                if (shelves[i] != null)
                    shelves[i].Refresh();
            }
        }

        private static ShopWorld BuildRuntimeWorld()
        {
            GameObject worldRoot = new GameObject("ShopWorld");
            ShopWorld world = worldRoot.AddComponent<ShopWorld>();
            world.root = worldRoot;
            world.sceneAuthored = false;
            world.layoutVersion = LayoutVersion;

            CreateCamera(worldRoot.transform, out Camera cam);
            world.shopCamera = cam;

            // Prefer authentic shop art from Resources if available, else placeholder boxes
            Transform environment = TryInstantiateEnvironment(worldRoot.transform);
            if (environment == null)
                environment = BuildPlaceholderEnvironment(worldRoot.transform).transform;

            world.environmentRoot = environment;

            Transform stage = BuildCenterDisplayStage(worldRoot.transform, out ShopShelfSlot[] slots);
            world.displayStage = stage;
            world.shelves = slots;

            world.idleProducer = worldRoot.AddComponent<ShopIdleProducer>();
            world.ReframeToContent();
            worldRoot.SetActive(false);
            return world;
        }

        private static Transform TryInstantiateEnvironment(Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>("Shop/ShopEnvironment");
            if (prefab == null)
            {
                // Also allow direct Prefabs path via Resources alias
                prefab = Resources.Load<GameObject>("Shop/Shop0");
            }

            if (prefab == null)
                return null;

            GameObject env = Object.Instantiate(prefab, parent);
            env.name = "Environment";
            env.transform.localPosition = Vector3.zero;
            env.transform.localRotation = Quaternion.identity;
            env.transform.localScale = Vector3.one;
            CenterEnvironmentOnOrigin(env.transform);
            return env.transform;
        }

        /// <summary>
        /// Shift environment so its combined renderer bounds sit on world origin (XZ + floor Y).
        /// </summary>
        public static void CenterEnvironmentOnOrigin(Transform environment)
        {
            if (environment == null)
                return;

            Renderer[] renderers = environment.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Vector3 shift = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            environment.position -= shift;
        }

        /// <summary>
        /// Place ortho isometric camera so lookTarget fills the screen center
        /// (slight upward bias to clear the bottom nav).
        /// </summary>
        public static void CreateCamera(Transform parent, out Camera cam)
        {
            GameObject camGo = new GameObject("ShopCamera");
            camGo.transform.SetParent(parent, false);

            cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.86f, 0.88f, 0.9f, 1f);
            cam.depth = 10;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
            cam.enabled = false;

            FrameCamera(cam, Vector3.zero, size: 6.2f);
        }

        public static void FrameCamera(Camera cam, Vector3 lookTarget, float size = 6.2f)
        {
            if (cam == null)
                return;

            // Fixed isometric dollhouse angle
            Quaternion rotation = Quaternion.Euler(30f, 45f, 0f);
            const float distance = 22f;

            // Aim a bit below content so room appears higher (clears bottom nav)
            Vector3 aim = lookTarget + Vector3.down * 0.45f;
            Vector3 position = aim - rotation * Vector3.forward * distance;

            cam.transform.SetPositionAndRotation(position, rotation);
            cam.orthographicSize = size;
        }

        /// <summary>
        /// Re-center env + reframe camera around environment bounds (and display stage if any).
        /// </summary>
        public void ReframeToContent()
        {
            if (environmentRoot != null)
                CenterEnvironmentOnOrigin(environmentRoot);

            Vector3 target = Vector3.zero;
            float size = 6.2f;

            if (TryGetContentBounds(out Bounds bounds))
            {
                target = new Vector3(bounds.center.x, Mathf.Lerp(bounds.min.y, bounds.center.y, 0.45f), bounds.center.z);
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.z, bounds.extents.y * 0.75f);
                size = Mathf.Clamp(radius * 1.15f, 4.5f, 9f);
            }
            else if (displayStage != null)
            {
                target = displayStage.position + Vector3.up * 0.6f;
            }

            if (shopCamera != null)
                FrameCamera(shopCamera, target, size);
        }

        private bool TryGetContentBounds(out Bounds bounds)
        {
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool any = false;

            if (environmentRoot != null)
            {
                Renderer[] rends = environmentRoot.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < rends.Length; i++)
                {
                    if (!any)
                    {
                        bounds = rends[i].bounds;
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(rends[i].bounds);
                    }
                }
            }

            if (displayStage != null)
            {
                Renderer[] stageRends = displayStage.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < stageRends.Length; i++)
                {
                    if (!any)
                    {
                        bounds = stageRends[i].bounds;
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(stageRends[i].bounds);
                    }
                }
            }

            return any;
        }

        private static GameObject BuildPlaceholderEnvironment(Transform parent)
        {
            GameObject env = new GameObject("Environment");
            env.transform.SetParent(parent, false);

            Color wallDark = new Color(0.28f, 0.3f, 0.32f);
            Color wallStripe = new Color(0.35f, 0.72f, 0.42f);
            Color floorColor = new Color(0.88f, 0.86f, 0.9f);

            CreateBox(env.transform, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(12f, 0.2f, 10f), floorColor);
            CreateBox(env.transform, "WallBack", new Vector3(0f, 2.2f, 4.6f), new Vector3(12f, 4.4f, 0.35f), wallDark);
            CreateBox(env.transform, "WallLeft", new Vector3(-5.8f, 2.2f, 0f), new Vector3(0.35f, 4.4f, 9.5f), wallDark);
            CreateBox(env.transform, "StripeBack", new Vector3(0f, 1.6f, 4.4f), new Vector3(11.6f, 0.55f, 0.12f), wallStripe);
            return env;
        }

        public static Transform BuildCenterDisplayStage(Transform parent, out ShopShelfSlot[] shelves)
        {
            GameObject stage = new GameObject("DisplayStage");
            stage.transform.SetParent(parent, false);
            // Cake display sits at origin after environment is centered
            stage.transform.localPosition = new Vector3(0f, 0f, 0f);

            CreateBox(stage.transform, "StagePad", new Vector3(0f, 0.02f, 0f), new Vector3(5.4f, 0.08f, 4.4f), new Color(0.82f, 0.8f, 0.85f));

            int maxShelves = 8;
            if (ShopController.IsInitialized && ShopController.Config != null)
                maxShelves = ShopController.Config.MaxShelfCount;

            shelves = new ShopShelfSlot[maxShelves];
            int cols = 4;
            for (int i = 0; i < maxShelves; i++)
            {
                int row = i / cols;
                int col = i % cols;

                GameObject shelfGo = new GameObject($"DisplayCase_{i}");
                shelfGo.transform.SetParent(stage.transform, false);
                // Symmetric grid around origin
                float x = (col - (cols - 1) * 0.5f) * 1.2f;
                float z = (row - 0.5f) * 1.4f;
                shelfGo.transform.localPosition = new Vector3(x, 0.1f, z);

                CreateBox(shelfGo.transform, "Pedestal", new Vector3(0f, 0.2f, 0f), new Vector3(0.95f, 0.4f, 0.85f), new Color(0.72f, 0.74f, 0.76f));
                GameObject glass = CreateBox(shelfGo.transform, "Glass", new Vector3(0f, 0.85f, 0f), new Vector3(0.85f, 0.9f, 0.75f), new Color(0.75f, 0.88f, 0.95f, 0.28f));
                GameObject locked = CreateBox(shelfGo.transform, "Locked", new Vector3(0f, 0.7f, 0f), new Vector3(0.35f, 0.35f, 0.35f), new Color(0.4f, 0.4f, 0.42f));

                GameObject recommend = CreateBox(shelfGo.transform, "RecommendBadge", new Vector3(0.32f, 1.4f, 0f), new Vector3(0.22f, 0.12f, 0.05f), new Color(1f, 0.75f, 0.2f));
                recommend.SetActive(false);

                GameObject hot = CreateBox(shelfGo.transform, "HotBadge", new Vector3(-0.32f, 1.4f, 0f), new Vector3(0.22f, 0.12f, 0.05f), new Color(1f, 0.35f, 0.45f));
                hot.SetActive(false);

                GameObject anchor = new GameObject("CakeAnchor");
                anchor.transform.SetParent(shelfGo.transform, false);
                anchor.transform.localPosition = new Vector3(0f, 0.45f, 0f);

                ShopShelfSlot slot = shelfGo.AddComponent<ShopShelfSlot>();
                slot.Setup(i, anchor.transform, locked, null, recommend, hot);
                shelves[i] = slot;

                Collider glassCol = glass.GetComponent<Collider>();
                if (glassCol != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(glassCol);
                    else
                        Object.DestroyImmediate(glassCol);
                }
            }

            return stage.transform;
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            SetColor(go, color);
            return go;
        }

        private static void SetColor(GameObject go, Color color)
        {
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
                return;

            Material mat = new Material(shader) { color = color };
            if (color.a < 0.99f)
            {
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }

            renderer.material = mat;
        }
    }
}
