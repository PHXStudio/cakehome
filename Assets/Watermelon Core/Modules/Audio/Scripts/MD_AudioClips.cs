using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Audio Clips", menuName = "Data/Core/Audio Clips")]
    public class AudioClips : ScriptableObject
    {
        [BoxGroup("UI", "UI")]
        public AudioClip buttonSound;
        [BoxGroup("UI")]
        public AudioClip levelComplete;
        [BoxGroup("UI")]
        public AudioClip levelFailed;

        [BoxGroup("Gameplay", "Gameplay")]
        public AudioClip tileClick;
        [BoxGroup("Gameplay")]
        public AudioClip tileClickBlocked;
        [BoxGroup("Gameplay")]
        public AudioClip mergeSound;

        [BoxGroup("Effects", "Effects")]
        public AudioClip iceCrackSound;
        [BoxGroup("Effects")]
        public AudioClip crateCrackSound;
        [BoxGroup("Effects")]
        public AudioClip rubberSound;

    }
}

// -----------------
// Audio Controller v 0.4
// -----------------