#pragma warning disable 0414

using System.Collections;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Initializer module that creates and configures <see cref="SaveController"/> at startup.
    /// Registered at order 900 so it runs early in the initialization sequence.
    /// </summary>
    [RegisterModule("Save Controller", core: true, order: 900)]
    public class SaveInitModule : InitModule
    {
        public override string ModuleName => "Save Controller";

        /// <summary>Interval in seconds between automatic saves; set to 0 to disable auto-save.</summary>
        [SerializeField] float autoSaveDelay = 0;

        [Space]
        /// <summary>localStorage key prefix for WebGL builds; must be unique per game to avoid cross-build data collisions.</summary>
        [SerializeField] string webGLPrefix = "gameName";

        [Space]
        [Tooltip("Additional save files to download from cloud on startup (e.g. \"world_1\", \"world_2\"). Leave empty if you only use the default save file.")]
        /// <summary>Extra save file names to cloud-sync on startup in addition to the default save file.</summary>
        [SerializeField] string[] namedFiles;

        /// <summary>Creates <see cref="SaveController"/>, configures the platform wrapper, and yields until cloud sync (or its timeout) completes.</summary>
        public override IEnumerator InitAsync(GameObject owner)
        {
            SaveController saveController = owner.AddComponent<SaveController>();

            saveController.Configure(new SaveWrapperConfig
            {
                WebGLPrefix = webGLPrefix
            });

            // Cloud handler will be auto-discovered from CloudSaveBehavior in scene
            // No manual configuration needed here!

            // Yield return ensures SaveController.InitAsync() completes (including cloud sync)
            yield return saveController.StartCoroutine(saveController.InitAsync(autoSaveDelay, namedFiles));

            LogManager.Log("[Save Controller]: SaveController fully initialized.", LogCategory.Systems);
        }
    }
}
