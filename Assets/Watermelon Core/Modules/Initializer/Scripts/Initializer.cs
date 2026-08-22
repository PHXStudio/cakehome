#pragma warning disable 0649

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Watermelon
{
    public class Initializer : MonoBehaviour
    {
        private static bool isInitialized;

        [SerializeField] ProjectInitSettings initSettings;
        [SerializeField] SDKInitializer sdkInitializer;
        [SerializeField] EventSystem eventSystem;

        private ConsentData consentData;

        public void Init()
        {
            if (isInitialized) return;

            isInitialized = true;

            if (eventSystem == null)
                LogManager.LogWarning("[Initializer]: EventSystem is not assigned — input may not work.", LogCategory.Systems);

            if (sdkInitializer == null)
                Debug.LogError("[Initializer]: SDKInitializer is not assigned — consent providers will not be registered.");

            consentData = new ConsentData(sdkInitializer?.gameObject);

            IPreInitializable[] preInitComponents = GetComponents<IPreInitializable>();
            LogManager.Log($"[Initializer]: Found {preInitComponents.Length} pre-initializable component(s).", LogCategory.Systems);
            foreach (IPreInitializable component in preInitComponents)
                component.PreInit();

            DontDestroyOnLoad(gameObject);
        }

        public void InitModules()
        {
            if (initSettings == null)
            {
                Debug.LogError("[Initializer]: InitSettings is not assigned — modules will not be initialized.");
                return;
            }

            // Use async initialization - StartCoroutine will wait for all modules to complete
            StartCoroutine(initSettings.InitAsync(gameObject, this));
        }

        public IEnumerator InitModulesAsync()
        {
            if (initSettings == null)
            {
                Debug.LogError("[Initializer]: InitSettings is not assigned — modules will not be initialized.");
                yield break;
            }

            // Yield until all modules finish initialization sequentially
            yield return StartCoroutine(initSettings.InitAsync(gameObject, this));
        }

        public void InitSDKs()
        {
            if (sdkInitializer == null)
            {
                LogManager.LogWarning("[Initializer]: SDKInitializer is not assigned — SDKs will not be initialized.", LogCategory.Systems);
                return;
            }

            sdkInitializer.Init();
        }

        private void OnDestroy()
        {
            initSettings.Unload();
            consentData?.Unload();

            isInitialized = false;
        }
    }
}
