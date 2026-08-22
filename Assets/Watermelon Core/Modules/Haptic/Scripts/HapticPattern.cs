using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// A named sequence of <see cref="HapticEvent"/> entries that defines a multi-step haptic effect.
    /// Register via <see cref="Haptic.RegisterPattern"/> and play via <see cref="Haptic.Play(HapticPattern)"/>.
    /// </summary>
    [System.Serializable]
    public class HapticPattern
    {
        [SerializeField] string id;
        [SerializeField] HapticEvent[] pattern;

        /// <summary>Unique string identifier used to register and trigger this pattern.</summary>
        public string ID => id;
        /// <summary>Ordered list of haptic events that make up this pattern.</summary>
        public HapticEvent[] Pattern => pattern;

        public HapticPattern(string id, HapticEvent[] pattern)
        {
            this.id = id;
            this.pattern = pattern;
        }
    }
}
