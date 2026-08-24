using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Base class for all platform-specific haptic implementations.
    /// </summary>
    public abstract class BaseHapticWrapper
    {
        /// <summary>Initializes the platform haptic system.</summary>
        public abstract void Init();

        /// <summary>Plays a one-shot haptic with the given duration and intensity.</summary>
        public abstract void Play(float duration = 0.3f, float intensity = 1.0f);

        /// <summary>Plays a registered haptic pattern by ID.</summary>
        public abstract void Play(string patternID);

        /// <summary>Registers a pattern so it can be played by ID.</summary>
        public abstract void RegisterPattern(HapticPattern pattern);

        protected void Log(string message)
        {
            LogManager.Log($"[Haptic]: {message}", LogCategory.Systems);
        }

        protected void Try(SimpleCallback action, string errorMessage)
        {
            try
            {
                action();
            }
            catch (System.Exception e)
            {
                Debug.LogError(string.Format("[Haptic]: {0}", errorMessage));
                Debug.LogException(e);
            }
        }
    }
}
