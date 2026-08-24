using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

namespace Watermelon
{
    public class GameLoading : MonoBehaviour
    {
        private const float MINIMUM_LOADING_TIME = 2.0f;

        private static GameLoading gameLoading;

        [SerializeField] Initializer initializer;

        [Space]
        [Tooltip("If manual mode is enabled, the loading screen will be active until GameLoading.MarkAsReadyToHide method has been called.")]
        [SerializeField] bool useManualControl;

        public static bool IsActive { get; private set; }
        public static event Action OnLoadingCompleted;

        private static AsyncOperation loadingOperation;

        private static bool isReadyToHide;

        private static string loadingMessage;
        private static List<LoadingTask> loadingTasks = new List<LoadingTask>();

        private bool retryRequested;

        public static int LoadingSceneBuildIndex = -1;
        private ILoadingGraphics loadingGraphics;

        private void Awake()
        {
            gameLoading = this;
            IsActive = true;

            DontDestroyOnLoad(gameObject);

            loadingGraphics = GetComponentInChildren<ILoadingGraphics>(true);
            if (loadingGraphics == null)
                LogManager.LogWarning("[GameLoading]: No ILoadingGraphics found in children — loading UI will not work.", LogCategory.Systems);
            else
                loadingGraphics.Init(this);

            StartCoroutine(BootstrapCoroutine());
        }

        private IEnumerator BootstrapCoroutine()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            initializer.Init();

            yield return LoadingStepsCoroutine();
        }

        public void RetryConnection()
        {
            retryRequested = true;
        }

        private IEnumerator LoadingStepsCoroutine()
        {
            ILoadingStep[] steps = initializer.GetComponents<ILoadingStep>();

            for (int i = 0; i < steps.Length; i++)
            {
                ILoadingStep step = steps[i];
                float progress = (float)i / steps.Length * 0.1f;
                bool success = false;

                while (!success)
                {
                    loadingGraphics?.HideErrorMessage();
                    loadingGraphics?.SetLoadingState(progress, step.LoadingMessage);

                    yield return step.Execute(initializer, (state) => success = state);

                    if (!success)
                    {
                        loadingGraphics?.ShowErrorMessage(step.ErrorMessage);

                        retryRequested = false;
                        yield return new WaitUntil(() => retryRequested);
                    }
                }
            }

            loadingGraphics?.SetLoadingState(0.1f, "Loading..");

            yield return initializer.InitModulesAsync();
            initializer.InitSDKs();

            int taskIndex = 0;
            while (taskIndex < loadingTasks.Count)
            {
                if (!loadingTasks[taskIndex].IsActive)
                    loadingTasks[taskIndex].Activate();

                if (loadingTasks[taskIndex].IsFinished)
                {
                    taskIndex++;
                }

                yield return null;
            }

            yield return null;
            yield return null;
            yield return null;

            float realtimeSinceStartup = Time.realtimeSinceStartup;

            int sceneIndex = LoadingSceneBuildIndex;
            if(sceneIndex == -1)
            {
                sceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
                if (SceneManager.sceneCount < sceneIndex)
                    Debug.LogError("[Loading]: First scene is missing!");
            }

            float minimumFinishTime = realtimeSinceStartup + MINIMUM_LOADING_TIME;

            loadingOperation = SceneManager.LoadSceneAsync(sceneIndex);

            yield return null;

            loadingMessage = "Loading..";

            while (!loadingOperation.isDone || realtimeSinceStartup < minimumFinishTime)
            {
                yield return null;

                realtimeSinceStartup = Time.realtimeSinceStartup;

                loadingGraphics?.SetLoadingState(Mathf.Lerp(0.2f, 0.9f, loadingOperation.progress), loadingMessage);
            }

            loadingGraphics?.SetLoadingState(1.0f, "Completed");

            if (useManualControl)
            {
                float debugTimeout = Time.realtimeSinceStartup + 10f;

                while (!isReadyToHide)
                {
                    if (Time.realtimeSinceStartup >= debugTimeout)
                    {
                        Debug.LogError("[Loading]: Seems like you forget to call MarkAsReadyToHide method to finish the loading process.");
                        debugTimeout = float.MaxValue;
                    }

                    yield return null;
                }
            }


            loadingGraphics?.OnLoadingFinished();
        }

        public void OnLoadingFinished()
        {
            IsActive = false;
            OnLoadingCompleted?.Invoke();
            Destroy(gameObject);
        }

        public static void SetLoadingMessage(string message)
        {
            loadingMessage = message;

            float progress = 0.0f;
            if (loadingOperation != null)
                progress = loadingOperation.progress;
        }

        public static void AddTask(LoadingTask loadingTask)
        {
            loadingTasks.Add(loadingTask);
        }

        public static void MarkAsReadyToHide()
        {
            isReadyToHide = true;
        }

        private void OnDestroy()
        {
            IsActive = false;
            isReadyToHide = false;
            loadingTasks.Clear();
            OnLoadingCompleted = null;
        }

        public delegate void LoadingCallback(float state, string message);
    }
}