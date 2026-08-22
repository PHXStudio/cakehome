using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Manages haptic feedback across platforms (iOS, Android, WebGL, Editor).
    /// <para>Create an instance via <c>new Haptic()</c> (e.g. from <see cref="HapticInitModule"/>) to activate the module,
    /// then use the static facade methods to trigger feedback.</para>
    /// </summary>
    public class Haptic
    {
        private static Haptic instance;

        /// <summary>Predefined light haptic feedback (short, low intensity).</summary>
        public static readonly HapticData HAPTIC_LIGHT = new HapticData(0.14f, 0.4f);
        /// <summary>Predefined medium haptic feedback.</summary>
        public static readonly HapticData HAPTIC_MEDIUM = new HapticData(0.14f, 0.6f);
        /// <summary>Predefined strong haptic feedback.</summary>
        public static readonly HapticData HAPTIC_HARD = new HapticData(0.14f, 0.8f);

        /// <summary>Predefined light haptic pattern.</summary>
        public static readonly HapticPattern PATTERN_LIGHT = new HapticPattern("light", new HapticEvent[]
        {
            new() { Duration = 0.3f, Intensity = 1.0f, Sharpness = 0.0f, StartTime = 0.0f }
        });

        private bool isActive;
        private readonly HapticSave save;
        private readonly BaseHapticWrapper wrapper;

        /// <summary>Fires when haptic is enabled or disabled.</summary>
        public static event SimpleBoolCallback StateChanged;

        /// <summary>
        /// Whether haptic feedback is currently enabled. Persisted across sessions.
        /// Returns <c>false</c> if the module has not been created yet.
        /// </summary>
        public static bool IsActive
        {
            get => IsInitialized && instance.isActive;
            set
            {
                if (!IsInstanceExists()) return;

                instance.isActive = value;
                instance.save.IsActive = value;

                SaveController.MarkAsSaveIsRequired();

                LogManager.Log($"[Haptic]: State changed: {(value ? "Active" : "Disabled")}", LogCategory.Systems);

                StateChanged?.Invoke(value);
            }
        }

        /// <summary>Returns <c>true</c> if an instance has been created.</summary>
        public static bool IsInitialized => instance != null;

        /// <summary>
        /// Creates the haptic module instance, loads saved state and sets up the platform wrapper.
        /// The constructor registers itself as the active instance.
        /// </summary>
        public Haptic()
        {
            instance = this;

            save = SaveController.GetSaveObject<HapticSave>("haptic");
            isActive = save.IsActive;

            wrapper = GetPlatformWrapper();

            if (wrapper == null)
            {
                LogManager.LogWarning("[Haptic]: Unsupported platform", LogCategory.Systems);
                return;
            }

            wrapper.Init();
            wrapper.RegisterPattern(PATTERN_LIGHT);
        }

        /// <summary>
        /// Registers a custom haptic pattern so it can be triggered via <see cref="Play(HapticPattern)"/> or <see cref="Play(string)"/>.
        /// </summary>
        public static void RegisterPattern(HapticPattern hapticPattern)
        {
            if (!IsInstanceExists()) return;
            if (instance.wrapper == null) return;

            instance.wrapper.RegisterPattern(hapticPattern);
        }

        /// <summary>Plays haptic feedback using a <see cref="HapticData"/> preset.</summary>
        public static void Play(HapticData hapticData)
        {
            Play(hapticData.Duration, hapticData.Intensity);
        }

        /// <summary>Plays haptic feedback with the given duration (seconds) and intensity (0–1).</summary>
        public static void Play(float duration, float intensity = 1.0f)
        {
            if (!IsActive) return;
            if (instance.wrapper == null) return;
            if (duration <= 0) return;

            instance.wrapper.Play(duration, intensity);
        }

        /// <summary>Plays a registered <see cref="HapticPattern"/>.</summary>
        public static void Play(HapticPattern pattern)
        {
            if (!IsActive) return;
            if (instance.wrapper == null) return;

            instance.wrapper.Play(pattern.ID);
        }

        /// <summary>Plays a registered haptic pattern by its string ID.</summary>
        public static void Play(string patternID)
        {
            if (!IsActive) return;
            if (instance.wrapper == null) return;

            instance.wrapper.Play(patternID);
        }

        /// <summary>Releases the module instance and clears all state.</summary>
        public static void Unload()
        {
            instance = null;
            StateChanged = null;
        }

        private static bool IsInstanceExists()
        {
            if (instance == null)
            {
                LogManager.LogWarning("[Haptic]: Module is not initialized. Create a Haptic instance first.", LogCategory.Systems);
                return false;
            }

            return true;
        }

        public static BaseHapticWrapper GetPlatformWrapper()
        {
#if UNITY_EDITOR
            return new EditorHapticWrapper();
#elif UNITY_IOS
            return new IOSHapticWrapper();
#elif UNITY_ANDROID
            return new AndroidHapticWrapper();
#elif UNITY_WEBGL
            return new WebGLHapticWrapper();
#else
            return null;
#endif
        }
    }
}
