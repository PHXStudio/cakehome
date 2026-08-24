using System.Collections.Generic;
using UnityEngine;

#if MODULE_ADMOB
using GoogleMobileAds.Ump.Api;
#endif

namespace Watermelon
{
    public sealed class UMPTaskBehavior : MonoBehaviour, ISDKTaskBehavior, IConsentProvider
    {
        public bool IsConsentGiven { get; private set; }

        [SerializeField] bool isUMPEnabled = true;
        public bool IsUMPEnabled => isUMPEnabled;

        [ShowIf("isUMPEnabled")]
        [Tooltip("Set TagForUnderAgeOfConsent (TFUA) to indicate whether a user is under the age of consent. Consent is not requested from the user when TFUA is set to true. Mixed audience apps should set this parameter for child users to ensure consent is not requested.")]
        [SerializeField] bool umpTagForUnderAgeOfConsent = false;
        public bool UMPTagForUnderAgeOfConsent => umpTagForUnderAgeOfConsent;

        [Space]
        [ShowIf("isUMPEnabled")]
        [SerializeField] bool umpDebugMode = false;
        public bool UMPDebugMode => umpDebugMode;

        [ShowIf("isUMPEnabled")]
        [SerializeField] DebugGeography umpDebugGeography;
        public DebugGeography UMPDebugGeography => umpDebugGeography;

        [ShowIf("isUMPEnabled")]
        [SerializeField] List<string> testDevices;
        public List<string> TestDevices => testDevices;

        private SDKInitializer initializer;

        private LoadingTask task;
        public LoadingTask Task => task;

        public void Init(SDKInitializer initializer)
        {
            this.initializer = initializer;

            task = new UMPLoadingTask(initializer, this);
        }

        public sealed class UMPLoadingTask : LoadingTask
        {
            private SDKInitializer initializer;
            private UMPTaskBehavior behavior;

            public UMPLoadingTask(SDKInitializer initializer, UMPTaskBehavior behavior) : base()
            {
                this.initializer = initializer;
                this.behavior = behavior;
            }

            public override void OnTaskActivated()
            {
                if(!behavior.isUMPEnabled)
                {
                    LogManager.Log("[Loading]: UMP is disabled, skipping task.", LogCategory.Services);

                    CompleteTask(CompleteStatus.Skipped);

                    return;
                }

#if MODULE_ADMOB
                if (ConsentInformation.CanRequestAds())
                {
                    LogManager.Log("[Loading]: UMP is already completed, ad can be loaded.", LogCategory.Services);

                    behavior.IsConsentGiven = true;

                    CompleteTask(CompleteStatus.Completed);

                    return;
                }

                ConsentRequestParameters requestParameters = GetRequestParameters();

                ConsentInformation.Update(requestParameters, (FormError updateError) =>
                {
                    if (updateError != null)
                    {
                        Debug.LogError("[Loading]: Failed to gather consent: " + updateError.Message);

                        CompleteTask(CompleteStatus.Failed);

                        return;
                    }

                    ConsentForm.LoadAndShowConsentFormIfRequired((FormError showError) =>
                    {
                        if (showError != null)
                        {
                            Debug.LogError("[Loading]: Failed to show consent: " + showError.Message);

                            CompleteTask(CompleteStatus.Failed);

                            return;
                        }

                        if (ConsentInformation.CanRequestAds())
                        {
                            LogManager.Log("[Loading]: UMP successfully completed.", LogCategory.Services);

                            behavior.IsConsentGiven = true;

                            CompleteTask(CompleteStatus.Completed);
                        }
                    });

#if UNITY_EDITOR
                    // Workaround for AdMob UMP canvas sorting order
                    GameObject canvasObject = GameObject.Find("ConsentForm(Clone)");
                    if (canvasObject != null)
                    {
                        Canvas canvas = canvasObject.GetComponent<Canvas>();
                        if (canvas != null)
                        {
                            canvas.sortingOrder = 9999;
                        }
                    }
#endif
                });
#else
                LogManager.Log("[Loading]: UMP SDK is missing, skipping task.", LogCategory.Services);

                CompleteTask(CompleteStatus.Skipped);
#endif
            }

#if MODULE_ADMOB
            private ConsentRequestParameters GetRequestParameters()
            {
                ConsentRequestParameters requestParameters = new ConsentRequestParameters();

                if (behavior.UMPDebugMode)
                {
                    requestParameters.ConsentDebugSettings = new ConsentDebugSettings()
                    {
                        DebugGeography = (GoogleMobileAds.Ump.Api.DebugGeography)behavior.UMPDebugGeography,
                        TestDeviceHashedIds = behavior.TestDevices
                    };
                }

                return requestParameters;
            }
#endif
        }

        public enum DebugGeography
        {
            Disabled = 0,
            EEA = 1,
            NotEEA = 2
        }
    }
}
