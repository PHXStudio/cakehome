using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Cloud save handler for custom REST API backend.
    /// Add this component to a GameObject and configure the API URL.
    ///
    /// Usage:
    /// 1. Add to any GameObject in scene (e.g., a Manager)
    /// 2. Set API URL in Inspector (e.g., "https://api.example.com/saves")
    /// 3. SaveController will auto-discover this on Init()
    /// 4. Cloud sync automatically activates after user consent
    ///
    /// The handler will use User.GetId() to identify the player.
    /// </summary>
    public sealed class CustomBackendSaveHandler : CloudSaveBehavior
    {
        [SerializeField]
        private string apiUrl = "https://api.example.com/saves";

        private CustomBackendCloudHandler handler;

        public override void OnUserConsentReceived()
        {
            // Initialize handler when user consent is granted
            // This is called by the Initializer system
            Debug.Log($"[CloudSave]: CustomBackendSaveHandler initialized with API: {apiUrl}");
        }

        public override ICloudSaveHandler GetConfiguredHandler()
        {
            if (handler == null)
            {
                string userId = User.GetId();
                handler = new CustomBackendCloudHandler(apiUrl, userId);
            }

            return handler;
        }

        public override float GetSyncTimeout() => 10f;
    }
}
