using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class AudioClipToggle : ToggleType<AudioClip>
    {
        public AudioClipToggle(bool enabled, AudioClip value) : base(enabled, value) { }
    }
}