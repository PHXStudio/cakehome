using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Watermelon
{
    public class ShopWorld : MonoBehaviour
    {
        public const int LayoutVersion = 5;

        // Fixed dollhouse angle: 45° elevation + 45° yaw (no orbit).
        private const float DefaultPitch = 45f;
        private const float DefaultYaw = 45f;
        private const float DefaultDistance = 22f;
        private const float DragDeadZonePixels = 4f;
        private const float PanMargin = 1.5f;

        private static ShopWorld instance;
        private static readonly List<RaycastResult> uiRaycastBuffer = new List<RaycastResult>(16);

        [Tooltip("When true this object is edited in Game.unity and will never be auto-destroyed/rebuilt.")]
        [SerializeField] bool sceneAuthored;
        [SerializeField] int layoutVersion;
        [SerializeField] GameObject root;
        [SerializeField] Camera shopCamera;
        [SerializeField] ShopIdleProducer idleProducer;
        [SerializeField] Transform freezerProp;
        [SerializeField] Transform environmentRoot;
        [SerializeField] Light sunLight;
        [Tooltip("Shop0 skybox material (owned by this ShopWorld / camera).")]
        [SerializeField] Material shopSkybox;

        private bool isVisible;
        private Skybox cameraSkybox;
        private UnityEngine.Rendering.AmbientMode previousAmbientMode;
        private Color previousAmbientSky;
        private Color previousAmbientEquator;
        private Color previousAmbientGround;
        private Color previousAmbientLight;
        private bool atmosphereApplied;
        private Material runtimeSkyMaterial;

        // Fixed-angle framing; drag pans the look pivot on XZ.
        private Vector3 orbitPivot;
        private float orbitDistance = DefaultDistance;
        private float orbitYaw = DefaultYaw;
        private float orbitPitch = DefaultPitch;
        private float orbitOrthoSize = 6.2f;
        private Bounds contentBounds;
        private bool hasContentBounds;

        private bool isDragging;
        private bool dragPastDeadZone;
        private Vector2 lastPointerPos;

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

            // Legacy cake DisplayStage removed — only shop environment loads.
            StripCakeDisplayStage();
            EnsureCameraSkyboxComponent();

            // SetVisible may activate this GO and run Awake mid-call after already setting isVisible=true.
            // Only cold-start should force-hide camera/idle; never clobber a live show request.
            if (!isVisible)
            {
                if (shopCamera != null)
                    shopCamera.enabled = false;

                if (idleProducer != null)
                    idleProducer.SetActive(false);

                if (cameraSkybox != null)
                    cameraSkybox.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;

            ShopController.StateChanged -= RefreshVisuals;
            RestoreSkyAtmosphere();

            if (runtimeSkyMaterial != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(runtimeSkyMaterial);
                else
                    Object.DestroyImmediate(runtimeSkyMaterial);
                runtimeSkyMaterial = null;
            }
        }

        private void OnEnable()
        {
            ShopController.StateChanged += RefreshVisuals;
        }

        private void OnDisable()
        {
            ShopController.StateChanged -= RefreshVisuals;
            EndPanDrag();
        }

        private void Update()
        {
            if (!isVisible || shopCamera == null || !shopCamera.enabled)
                return;

            HandlePanInput();
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
        public void Bind(Camera camera, ShopIdleProducer producer, Transform environment, bool authored)
        {
            shopCamera = camera;
            idleProducer = producer;
            environmentRoot = environment;
            sceneAuthored = authored;
            layoutVersion = LayoutVersion;
            if (root == null)
                root = gameObject;
        }

        /// <summary>
        /// Drop leftover DisplayStage / shelf pedestals from earlier layouts.
        /// </summary>
        private void StripCakeDisplayStage()
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = children.Length - 1; i >= 0; i--)
            {
                Transform t = children[i];
                if (t == null || t == transform)
                    continue;

                string n = t.name;
                if (n == "DisplayStage" || n.StartsWith("DisplayCase_", System.StringComparison.Ordinal))
                {
                    if (Application.isPlaying)
                        Object.Destroy(t.gameObject);
                    else
                        Object.DestroyImmediate(t.gameObject);
                }
            }
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
                    found.StripCakeDisplayStage();
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
                    found.StripCakeDisplayStage();
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

            if (shopCamera == null)
                shopCamera = GetComponentInChildren<Camera>(true);

            // Activate before applying camera flags so components are enabled for Update.
            if (root != null)
                root.SetActive(visible);
            else
                gameObject.SetActive(visible);

            // Re-assert after possible mid-activation Awake on first show.
            isVisible = visible;

            if (shopCamera != null)
                shopCamera.enabled = visible;

            if (idleProducer != null)
                idleProducer.SetActive(visible);

            if (visible)
            {
                ShopController.EnsureInitialized();
                ShopController.OnShopOpened();
                StripCakeDisplayStage();
                if (shopCamera != null)
                    shopCamera.enabled = true;
                if (idleProducer != null)
                    idleProducer.SetActive(true);
                ApplySkyAtmosphere();
                ReframeToContent();
            }
            else
            {
                EndPanDrag();
                RestoreSkyAtmosphere();
            }
        }

        /// <summary>
        /// Sky lives on ShopWorld camera via Unity Skybox component (not global RenderSettings).
        /// </summary>
        private void ApplySkyAtmosphere()
        {
            EnsureCameraSkyboxComponent();
            Material activeSky = ResolveShopSkybox();
            if (activeSky != null)
                ConfigureShop0SkyMaterial(activeSky);

            if (shopCamera != null)
            {
                shopCamera.clearFlags = CameraClearFlags.Skybox;
                shopCamera.backgroundColor = Color.black;
            }

            if (cameraSkybox != null)
            {
                cameraSkybox.material = activeSky;
                cameraSkybox.enabled = activeSky != null;
            }

            EnsureSunLight();

            if (!atmosphereApplied)
            {
                previousAmbientMode = RenderSettings.ambientMode;
                previousAmbientSky = RenderSettings.ambientSkyColor;
                previousAmbientEquator = RenderSettings.ambientEquatorColor;
                previousAmbientGround = RenderSettings.ambientGroundColor;
                previousAmbientLight = RenderSettings.ambientLight;
                atmosphereApplied = true;
            }

            // Dimmer room lighting for a softer shop mood.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.28f, 0.34f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.38f, 0.36f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.14f, 0.12f);

            if (sunLight != null)
                sunLight.enabled = true;
        }

        private void RestoreSkyAtmosphere()
        {
            if (sunLight != null)
                sunLight.enabled = false;

            if (cameraSkybox != null)
                cameraSkybox.enabled = false;

            if (!atmosphereApplied)
                return;

            RenderSettings.ambientMode = previousAmbientMode;
            RenderSettings.ambientSkyColor = previousAmbientSky;
            RenderSettings.ambientEquatorColor = previousAmbientEquator;
            RenderSettings.ambientGroundColor = previousAmbientGround;
            RenderSettings.ambientLight = previousAmbientLight;
            atmosphereApplied = false;
        }

        private void EnsureCameraSkyboxComponent()
        {
            if (shopCamera == null)
                shopCamera = GetComponentInChildren<Camera>(true);

            if (shopCamera == null)
                return;

            if (cameraSkybox == null)
                cameraSkybox = shopCamera.GetComponent<Skybox>();

            if (cameraSkybox == null)
                cameraSkybox = shopCamera.gameObject.AddComponent<Skybox>();
        }

        /// <summary>
        /// Skybox material owned by this ShopWorld: inspector → Resources → runtime procedural.
        /// </summary>
        private Material ResolveShopSkybox()
        {
            if (shopSkybox != null)
                return shopSkybox;

            Material fromResources = Resources.Load<Material>("Shop/Shop0Skybox");
            if (fromResources != null)
            {
                shopSkybox = fromResources;
                return shopSkybox;
            }

            if (runtimeSkyMaterial != null)
                return runtimeSkyMaterial;

            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
                return null;

            runtimeSkyMaterial = new Material(shader)
            {
                name = "Shop0Sky_Runtime"
            };
            ConfigureShop0SkyMaterial(runtimeSkyMaterial);
            shopSkybox = runtimeSkyMaterial;
            return shopSkybox;
        }

        public static void ConfigureShop0SkyMaterial(Material mat)
        {
            if (mat == null)
                return;

            mat.SetFloat("_SunSize", 0.035f);
            mat.SetFloat("_SunSizeConvergence", 5f);
            mat.SetFloat("_AtmosphereThickness", 0.95f);
            mat.SetColor("_SkyTint", new Color(0.22f, 0.32f, 0.55f));
            mat.SetColor("_GroundColor", new Color(0.28f, 0.26f, 0.22f));
            mat.SetFloat("_Exposure", 0.78f);
        }

        private void EnsureSunLight()
        {
            if (sunLight == null)
            {
                Transform existing = transform.Find("ShopSun");
                if (existing != null)
                    sunLight = existing.GetComponent<Light>();
            }

            if (sunLight == null)
            {
                GameObject sunGo = new GameObject("ShopSun");
                sunGo.transform.SetParent(root != null ? root.transform : transform, false);
                sunLight = sunGo.AddComponent<Light>();
            }

            sunLight.type = LightType.Directional;
            sunLight.color = new Color(0.92f, 0.9f, 0.86f);
            sunLight.intensity = 0.48f;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowStrength = 0.55f;
            // Align key light with fixed 45° camera so forms still read.
            sunLight.transform.rotation = Quaternion.Euler(45f, -45f, 0f);
            sunLight.enabled = true;
        }

        private void HandlePanInput()
        {
            // Project is New Input System only; use InputController like Map/Raycast.
            InputAction click = InputController.ClickAction;
            if (click == null)
                return;

            Vector2 pos = InputController.MousePosition;

            if (click.WasPressedThisFrame())
            {
                if (IsBlockingUi(pos))
                    return;

                BeginPanDrag(pos);
                return;
            }

            if (!isDragging)
                return;

            if (click.WasReleasedThisFrame())
            {
                EndPanDrag();
                return;
            }

            if (click.IsPressed())
                ContinuePanDrag(pos);
        }

        private void BeginPanDrag(Vector2 screenPos)
        {
            isDragging = true;
            dragPastDeadZone = false;
            lastPointerPos = screenPos;
        }

        private void ContinuePanDrag(Vector2 screenPos)
        {
            Vector2 delta = screenPos - lastPointerPos;
            lastPointerPos = screenPos;

            if (!dragPastDeadZone)
            {
                if (delta.sqrMagnitude < DragDeadZonePixels * DragDeadZonePixels)
                    return;

                dragPastDeadZone = true;
            }

            // Drag grabs the shop: pivot moves opposite to finger (map-style pan).
            float unitsPerPixel = (2f * orbitOrthoSize) / Mathf.Max(1f, Screen.height);
            Vector3 right = shopCamera.transform.right;
            right.y = 0f;
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.right;
            else
                right.Normalize();

            Vector3 forward = shopCamera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            else
                forward.Normalize();

            orbitPivot -= (right * delta.x + forward * delta.y) * unitsPerPixel;
            ClampPivotToContent();
            ApplyOrbitCamera();
        }

        private void EndPanDrag()
        {
            isDragging = false;
            dragPastDeadZone = false;
        }

        private void ClampPivotToContent()
        {
            if (!hasContentBounds)
                return;

            float halfX = Mathf.Max(0.5f, contentBounds.extents.x - PanMargin);
            float halfZ = Mathf.Max(0.5f, contentBounds.extents.z - PanMargin);
            orbitPivot.x = Mathf.Clamp(orbitPivot.x, contentBounds.center.x - halfX, contentBounds.center.x + halfX);
            orbitPivot.z = Mathf.Clamp(orbitPivot.z, contentBounds.center.z - halfZ, contentBounds.center.z + halfZ);
        }

        private static bool IsBlockingUi(Vector2 screenPos)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            PointerEventData data = new PointerEventData(eventSystem) { position = screenPos };
            uiRaycastBuffer.Clear();
            eventSystem.RaycastAll(data, uiRaycastBuffer);

            for (int i = 0; i < uiRaycastBuffer.Count; i++)
            {
                GameObject hit = uiRaycastBuffer[i].gameObject;
                if (hit == null)
                    continue;

                if (hit.GetComponentInParent<Selectable>() != null)
                    return true;

                Graphic graphic = hit.GetComponent<Graphic>();
                if (graphic != null && graphic.raycastTarget && graphic.color.a > 0.01f)
                    return true;
            }

            return false;
        }

        private void ApplyOrbitCamera()
        {
            if (shopCamera == null)
                return;

            // Angle is fixed; only pivot pans.
            orbitYaw = DefaultYaw;
            orbitPitch = DefaultPitch;

            Quaternion rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            Vector3 aim = orbitPivot + Vector3.down * 0.45f;
            Vector3 position = aim - rotation * Vector3.forward * orbitDistance;

            shopCamera.transform.SetPositionAndRotation(position, rotation);
            shopCamera.orthographic = true;
            shopCamera.orthographicSize = orbitOrthoSize;
            shopCamera.clearFlags = CameraClearFlags.Skybox;
        }

        private void RefreshVisuals()
        {
            // Environment-only view; cake shelf mesh refresh removed.
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
            world.shopSkybox = Resources.Load<Material>("Shop/Shop0Skybox");

            // Prefer Shop environment art from Resources if available, else placeholder boxes
            Transform environment = TryInstantiateEnvironment(worldRoot.transform);
            if (environment == null)
                environment = BuildPlaceholderEnvironment(worldRoot.transform).transform;

            world.environmentRoot = environment;
            world.idleProducer = worldRoot.AddComponent<ShopIdleProducer>();
            world.EnsureCameraSkyboxComponent();
            world.ReframeToContent();
            worldRoot.SetActive(false);
            return world;
        }

        private static Transform TryInstantiateEnvironment(Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>("Shop/ShopEnvironment");
            if (prefab == null)
            {
                // Fallback aliases if ShopEnvironment was not copied into Resources
                prefab = Resources.Load<GameObject>("Shop/Shop1");
                if (prefab == null)
                    prefab = Resources.Load<GameObject>("Shop/Shop0");
            }

            if (prefab == null)
                return null;

            GameObject env = Object.Instantiate(prefab, parent);
            env.name = "Environment";
            env.transform.localPosition = Vector3.zero;
            env.transform.localRotation = Quaternion.identity;
            // Prefer prefab-authored scale (Shop1 uses 0.5); only force 1 if unset.
            if (env.transform.localScale == Vector3.one)
                env.transform.localScale = Vector3.one * 0.5f;
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
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = Color.black;
            cam.depth = 10;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
            cam.enabled = false;

            // Skybox owned by ShopWorld camera (local override, not project RenderSettings)
            Skybox sky = camGo.AddComponent<Skybox>();
            sky.enabled = false;

            FrameCamera(cam, Vector3.zero, size: 6.2f);
        }

        public static void FrameCamera(Camera cam, Vector3 lookTarget, float size = 6.2f)
        {
            if (cam == null)
                return;

            Quaternion rotation = Quaternion.Euler(DefaultPitch, DefaultYaw, 0f);
            Vector3 aim = lookTarget + Vector3.down * 0.45f;
            Vector3 position = aim - rotation * Vector3.forward * DefaultDistance;

            cam.transform.SetPositionAndRotation(position, rotation);
            cam.orthographicSize = size;
        }

        /// <summary>
        /// Re-center env + reframe camera around environment bounds.
        /// Resets to fixed 45° view centered on content.
        /// </summary>
        public void ReframeToContent()
        {
            if (environmentRoot != null)
                CenterEnvironmentOnOrigin(environmentRoot);

            Vector3 target = Vector3.zero;
            float size = 6.2f;
            hasContentBounds = TryGetContentBounds(out contentBounds);

            if (hasContentBounds)
            {
                Bounds bounds = contentBounds;
                target = new Vector3(bounds.center.x, Mathf.Lerp(bounds.min.y, bounds.center.y, 0.45f), bounds.center.z);
                float radius = Mathf.Max(bounds.extents.x, bounds.extents.z, bounds.extents.y * 0.75f);
                size = Mathf.Clamp(radius * 1.15f, 4.5f, 9f);
            }

            orbitPivot = target;
            orbitDistance = DefaultDistance;
            orbitYaw = DefaultYaw;
            orbitPitch = DefaultPitch;
            orbitOrthoSize = size;
            EndPanDrag();

            ApplyOrbitCamera();
        }

        private bool TryGetContentBounds(out Bounds bounds)
        {
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool any = false;

            if (environmentRoot == null)
                return false;

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
