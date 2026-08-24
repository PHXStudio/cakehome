namespace Watermelon
{
    // Editor-only marker: inspector values of every scene instance are snapshotted when Play Mode
    // ends and applied back once the editor scene is restored (see DebugSpawnerPersistence).
    // Each instance is tracked independently (keyed by GlobalObjectId).
    public interface IDebugPersistentComponent { }
}
