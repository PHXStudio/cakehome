using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Abstract base for cloud save behavior providers.
    /// Extends SDKBehavior to integrate with Initializer system.
    ///
    /// Each concrete implementation represents a different cloud storage provider:
    /// - Firebase (FirebaseSaveHandler)
    /// - Custom REST API (CustomBackendSaveHandler)
    /// - Other services (extend this class)
    ///
    /// Only ONE CloudSaveBehavior will be used per game instance.
    /// SaveController auto-discovers the first one in the scene via FindObjectOfType.
    /// </summary>
    public abstract class CloudSaveBehavior : SDKBehavior
    {
        /// <summary>
        /// Return the configured ICloudSaveHandler, or null if not ready.
        /// This is called by SaveController after Init() and OnUserConsentReceived().
        /// </summary>
        public abstract ICloudSaveHandler GetConfiguredHandler();

        /// <summary>
        /// Optional: Control timeout for cloud sync operations (in seconds).
        /// Default is 10 seconds.
        /// </summary>
        public virtual float GetSyncTimeout() => 10f;

        /// <summary>
        /// Called by Initializer after user consent is received.
        /// Override this to initialize your cloud provider.
        /// </summary>
        public override void OnUserConsentReceived()
        {
            // Override in subclasses
        }
    }
}
