namespace Watermelon
{
    /// <summary>
    /// Result of a cloud sync operation - contains resolved save and conflict info.
    /// </summary>
    public class CloudSyncResult
    {
        /// <summary>
        /// Whether the sync operation completed successfully.
        /// </summary>
        public bool success;

        /// <summary>
        /// The resolved SaveFile after comparing local and cloud versions.
        /// If sync failed, this is null.
        /// </summary>
        public SaveFile resolvedSave;

        /// <summary>
        /// How the conflict between local and cloud was resolved.
        /// </summary>
        public CloudConflictResolution resolution = CloudConflictResolution.NoConflict;

        /// <summary>
        /// Local save metadata at time of comparison.
        /// </summary>
        public SaveFileMetadata localMetadata;

        /// <summary>
        /// Cloud save metadata at time of comparison.
        /// </summary>
        public SaveFileMetadata cloudMetadata;

        public enum CloudConflictResolution
        {
            /// <summary>
            /// No cloud save exists or both are identical.
            /// </summary>
            NoConflict,

            /// <summary>
            /// Local save was newer (higher saveCount or newer timestamp).
            /// Local save was kept.
            /// </summary>
            LocalPreferred,

            /// <summary>
            /// Cloud save was newer (higher saveCount or newer timestamp).
            /// Cloud save was loaded, overwriting local.
            /// </summary>
            CloudPreferred,

            /// <summary>
            /// Cloud save exists but is older. Local is used as-is.
            /// Cloud will be updated on next local save.
            /// </summary>
            LocalUnchanged,

            /// <summary>
            /// No local save exists. Cloud save was loaded.
            /// </summary>
            CloudNew,

            /// <summary>
            /// Sync failed due to error (network, permissions, etc).
            /// </summary>
            SyncFailed
        }
    }
}
