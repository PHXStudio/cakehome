#pragma warning disable 0649

using UnityEngine;
using UnityEngine.EventSystems;

namespace Watermelon
{
    [DefaultExecutionOrder(-999)]
    public class Initializer : MonoBehaviour
    {
        private static Initializer initializer;

        [SerializeField] ProjectInitSettings initSettings;
        [SerializeField] EventSystem eventSystem;

        public static GameObject GameObject { get; private set; }
        public static Transform Transform { get; private set; }

        public static ProjectInitSettings InitSettings { get; private set; }

        private bool manualActivation;

        public void Awake()
        {
            if (initializer != null)
                return;

            initializer = this;

            manualActivation = false;

            InitSettings = initSettings;

            GameObject = gameObject;
            Transform = transform;

#if MODULE_INPUT_SYSTEM
            try
            {
                eventSystem.gameObject.GetOrSetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Initializer]: Failed to set up InputSystemUIInputModule: " + e.Message);
            }
#else
            eventSystem.gameObject.GetOrSetComponent<StandaloneInputModule>();
#endif

            DontDestroyOnLoad(gameObject);

            if (initSettings == null)
            {
                Debug.LogError("[Initializer]: initSettings is not assigned, core modules were NOT initialized!");
                return;
            }

            Debug.Log("[Initializer]: Awake calling initSettings.Init");

            try
            {
                initSettings.Init(this);
                Debug.Log("[Initializer]: initSettings.Init completed OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Initializer]: initSettings.Init FAILED — " + e);
            }
        }

        public void Start()
        {
            if (!manualActivation)
                LoadGame(true);
        }

        public void LoadGame(bool loadingScene)
        {
            if (loadingScene)
            {
                GameLoading.LoadGameScene();
            }
            else
            {
                GameLoading.SimpleLoad();
            }
        }

        public void EnableManualActivation()
        {
            manualActivation = true;
        }
    }
}
