using System;

namespace Watermelon
{
    /// <summary>
    /// Snapshot of save file versioning data used for cloud conflict resolution.
    /// </summary>
    [Serializable]
    public class SaveFileMetadata
    {
        /// <summary>Unix timestamp (UTC seconds) of the last local save.</summary>
        public long timestampUnix;
        /// <summary>Monotonically incrementing version number; incremented by one on each flush.</summary>
        public int saveCount;

        /// <summary>Creates a zeroed metadata instance representing a file that has never been saved.</summary>
        public SaveFileMetadata()
        {
            timestampUnix = 0;
            saveCount = 0;
        }

        /// <summary>Creates a metadata instance with the given timestamp and save count.</summary>
        public SaveFileMetadata(long timestamp, int count)
        {
            timestampUnix = timestamp;
            saveCount = count;
        }

        /// <summary>
        /// Determines if this metadata represents a newer save than the other.
        /// Comparison priority: saveCount > timestampUnix
        /// </summary>
        public bool IsNewerThan(SaveFileMetadata other)
        {
            if (other == null)
                return true;

            if (saveCount != other.saveCount)
                return saveCount > other.saveCount;

            return timestampUnix > other.timestampUnix;
        }

        /// <summary>
        /// Create metadata snapshot for current save moment
        /// </summary>
        public static SaveFileMetadata CreateCurrent(int nextSaveCount)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return new SaveFileMetadata(timestamp, nextSaveCount);
        }

        public override string ToString()
        {
            return $"SaveFileMetadata(count={saveCount}, time={DateTimeOffset.FromUnixTimeSeconds(timestampUnix).LocalDateTime:g})";
        }
    }
}
