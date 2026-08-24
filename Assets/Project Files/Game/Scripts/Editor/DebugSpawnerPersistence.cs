using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Watermelon
{
    // Keeps IDebugPersistentComponent setups from being lost when Play Mode ends: on exit the
    // inspector values of every scene instance are snapshotted to EditorPrefs (keyed by
    // GlobalObjectId, so multiple instances of the same type are tracked independently), and once
    // the editor scene is restored they are applied back. Lets you tweak a debug component while
    // testing and return to exactly that setup after stopping.
    //
    // Applied changes go through Undo, so a restore can be reverted with Ctrl+Z.
    // Note: snapshots store Object references (character, dialog data) as instance IDs — they
    // survive play mode, but not an editor restart; plain fields (typeId, grade, etc.) always do.
    // Objects instantiated during play have no persistent id and are skipped.
    [InitializeOnLoad]
    public static class DebugSpawnerPersistence
    {
        private static readonly IEnumerable<Type> registeredTypes;

        static DebugSpawnerPersistence()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            Type monobehaviourType = typeof(MonoBehaviour);
            Type type = typeof(IDebugPersistentComponent);

            registeredTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => !p.IsAbstract && type.IsAssignableFrom(p) && p.IsSubclassOf(monobehaviourType));
        }

        private static string GetKey() => $"Watermelon.DebugPersistent|{Application.dataPath}";

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Capture();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                Restore();
            }
        }

        private static void Capture()
        {
            SnapshotList snapshots = new SnapshotList();

            foreach (Type type in registeredTypes)
            {
#if UNITY_6000_4_OR_NEWER
                foreach (Object component in Object.FindObjectsByType(type, FindObjectsInactive.Include))
#else
                foreach (Object component in Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None))
#endif
                {
                    GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(component);
                    if (id.targetObjectId == 0)
                        continue;

                    snapshots.items.Add(new Snapshot { id = id.ToString(), json = EditorJsonUtility.ToJson(component) });
                }
            }

            // Single overwritten key — stale entries of deleted objects don't pile up in EditorPrefs.
            if (snapshots.items.Count > 0)
            {
                EditorPrefs.SetString(GetKey(), JsonUtility.ToJson(snapshots));
            }
            else
            {
                EditorPrefs.DeleteKey(GetKey());
            }
        }

        private static void Restore()
        {
            if (!EditorPrefs.HasKey(GetKey()))
                return;

            SnapshotList snapshots = JsonUtility.FromJson<SnapshotList>(EditorPrefs.GetString(GetKey()));
            if (snapshots == null)
                return;

            foreach (Snapshot snapshot in snapshots.items)
            {
                if (!GlobalObjectId.TryParse(snapshot.id, out GlobalObjectId id))
                    continue;

                Object component = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                if (component == null)
                    continue;

                // Skip (and keep the scene clean) when nothing was changed during play.
                if (EditorJsonUtility.ToJson(component) == snapshot.json)
                    continue;

                Undo.RecordObject(component, "Apply Debug Component Test Setup");
                EditorJsonUtility.FromJsonOverwrite(snapshot.json, component);
            }
        }

        [Serializable]
        private class Snapshot
        {
            public string id;
            public string json;
        }

        [Serializable]
        private class SnapshotList
        {
            public List<Snapshot> items = new List<Snapshot>();
        }
    }
}
