#pragma warning disable CS0162

using UnityEngine;
using System.Collections;

#if MODULE_IDFA && UNITY_IOS
using Unity.Advertisement.IosSupport;
#endif

namespace Watermelon
{
    public sealed class ATTTaskBehavior : MonoBehaviour, ISDKTaskBehavior, IConsentProvider
    {
        [SerializeField] bool isATTEnabled = true;

        private SDKInitializer initializer;

        private LoadingTask task;
        public LoadingTask Task => task;

#if MODULE_IDFA && UNITY_IOS
        public AuthorizationTrackingStatus ATTStatus { get; private set; } = AuthorizationTrackingStatus.NOT_DETERMINED;
#endif

        public void Init(SDKInitializer initializer)
        {
            this.initializer = initializer;

            task = new ATTLoadingTask(initializer, this);
        }

        public sealed class ATTLoadingTask : LoadingTask
        {
            private SDKInitializer initializer;
            private ATTTaskBehavior behavior;

            public ATTLoadingTask(SDKInitializer initializer, ATTTaskBehavior behavior) : base()
            {
                this.initializer = initializer;
                this.behavior = behavior;
            }

            public override void OnTaskActivated()
            {
                if(!behavior.isATTEnabled)
                {
                    LogManager.Log("[Loading]: ATT is disabled, skipping task.", LogCategory.Services);

                    CompleteTask(CompleteStatus.Skipped);

                    return;
                }

#if !MODULE_IDFA
                LogManager.Log("[Loading] ATT package has not been imported. Skipping task.", LogCategory.Services);

                CompleteTask(CompleteStatus.Skipped);

                return;
#endif

#if !UNITY_IOS
                LogManager.Log("[Loading] ATT is not supported on this platform, skipping task.", LogCategory.Services);

                CompleteTask(CompleteStatus.Skipped);

                return;
#endif

#if UNITY_EDITOR
                LogManager.Log("[Loading] ATT is not supported in the Unity Editor. Skipping task.", LogCategory.Services);

                CompleteTask(CompleteStatus.Skipped);

                return;
#endif

#if UNITY_IOS && MODULE_IDFA && !UNITY_EDITOR
                ATTrackingStatusBinding.AuthorizationTrackingStatus status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
                if (status != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    LogManager.Log($"[Loading]: IDFA status - {status} (already set, skipping request).", LogCategory.Services);

                    CompleteTask(CompleteStatus.Completed);

                    return;
                }
                
                LogManager.Log("[Loading]: Requesting IDFA..", LogCategory.Services);

                initializer.StartCoroutine(RequestWithDelay());

                return;
#else
                LogManager.Log("[Loading]: Something went wrong with ATT task, skipping.", LogCategory.Services);

                CompleteTask(CompleteStatus.Skipped);
#endif
            }

#if UNITY_IOS && MODULE_IDFA && !UNITY_EDITOR
            private IEnumerator RequestWithDelay()
            {
                // iOS silently drops ATT dialog if the app window isn't ready yet (common when Unity splash is disabled).
                // Application.isFocused becomes true once iOS has handed focus to the app window.
                while (!Application.isFocused)
                    yield return null;

                yield return new WaitForSecondsRealtime(0.5f);

                ATTrackingStatusBinding.RequestAuthorizationTracking();

                yield return CheckStatusCoroutine();
            }

            private IEnumerator CheckStatusCoroutine()
            {
                while (true)
                {
                    ATTrackingStatusBinding.AuthorizationTrackingStatus status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();

                    LogManager.Log($"[Loading]: IDFA status - {status}", LogCategory.Services);

                    if (status != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                    {
                        behavior.ATTStatus = (AuthorizationTrackingStatus)status;

                        CompleteTask(CompleteStatus.Completed);

                        yield break;
                    }

                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }
#endif
        }
    }
}
