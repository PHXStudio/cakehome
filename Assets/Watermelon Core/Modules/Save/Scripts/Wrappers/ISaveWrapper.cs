namespace Watermelon
{
    /// <summary>
    /// Platform abstraction for save file I/O. Implement this interface to add support for new storage backends.
    /// </summary>
    public interface ISaveWrapper
    {
        /// <summary>Initializes the wrapper; called once before any load or save operations.</summary>
        void Init();

        /// <summary>Loads and deserializes a save file by name; returns an empty initialized <see cref="SaveFile"/> if the file does not exist.</summary>
        SaveFile Load(string fileName);

        /// <summary>
        /// Saves pre-serialized JSON to the target file. Serialization always happens on the main thread
        /// before calling this, allowing safe background thread file writes.
        /// </summary>
        void SaveRaw(string fileName, string json);

        /// <summary>Deletes the save file and any associated temporary files.</summary>
        void Delete(string fileName);

        /// <summary>Returns true if this wrapper supports writing on background threads via <see cref="System.Threading.ThreadPool"/>.</summary>
        bool UseThreads();

        /// <summary>Applies platform-specific configuration (e.g. WebGL localStorage prefix).</summary>
        void Configure(SaveWrapperConfig config);
    }
}
