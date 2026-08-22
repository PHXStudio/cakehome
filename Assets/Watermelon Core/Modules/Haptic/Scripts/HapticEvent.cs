namespace Watermelon
{
    /// <summary>
    /// A single vibration event within a <see cref="HapticPattern"/>.
    /// </summary>
    [System.Serializable]
    public class HapticEvent
    {
        /// <summary>Vibration strength (0–1).</summary>
        public float Intensity;
        /// <summary>Texture of the vibration — lower values feel more rumble-like, higher values feel more crisp (0–1).</summary>
        public float Sharpness;
        /// <summary>Time in seconds from the start of the pattern when this event begins.</summary>
        public float StartTime;
        /// <summary>Duration of this vibration event in seconds.</summary>
        public float Duration;
    }
}
