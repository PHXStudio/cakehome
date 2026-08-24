using System;

namespace Watermelon
{
    /// <summary>
    /// Abstract interface for cloud save storage.
    /// Implementations can support Firebase, custom backend, or other cloud services.
    /// All callbacks should be called on main thread.
    /// </summary>
    public interface ICloudSaveHandler
    {
        /// <summary>
        /// Initialize the cloud handler. Called once at startup.
        /// Should use User.GetId() to identify the current user.
        /// </summary>
        void Init();

        /// <summary>
        /// Download a save file from cloud storage.
        /// Returns (success, json, metadata) tuple via callback.
        /// If file doesn't exist, returns (false, null, null).
        /// </summary>
        void Download(string fileName, Action<bool, string, SaveFileMetadata> onComplete);

        /// <summary>
        /// Upload a save file to cloud storage.
        /// Should atomically store both json content and metadata together.
        /// Called from the main thread. Callback (onComplete) must also be invoked on the main thread.
        /// </summary>
        void Upload(string fileName, string json, SaveFileMetadata metadata, Action<bool> onComplete);

        /// <summary>
        /// Delete a save file from cloud storage.
        /// Used for cleanup or when user deletes save manually.
        /// </summary>
        void Delete(string fileName, Action<bool> onComplete);

        /// <summary>
        /// Check if cloud service is ready to use (auth complete, connection available).
        /// Returns true if cloud operations can be performed.
        /// </summary>
        bool IsAvailable { get; }
    }
}
