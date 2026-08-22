using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Simple one-shot haptic descriptor used with <see cref="Haptic.Play(HapticData)"/>.
    /// </summary>
    [System.Serializable]
    public class HapticData
    {
        [SerializeField] float duration = 0.05f;
        [SerializeField] float intensity = 0.0f;

        /// <summary>Duration of the vibration in seconds.</summary>
        public float Duration => duration;
        /// <summary>Strength of the vibration (0–1).</summary>
        public float Intensity => intensity;

        public HapticData(float duration, float intensity = 0.0f)
        {
            this.duration = duration;
            this.intensity = intensity;
        }
    }
}
