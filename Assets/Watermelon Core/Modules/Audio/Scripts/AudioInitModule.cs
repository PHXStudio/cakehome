#pragma warning disable 0649

using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Audio Controller", core: true)]
    public class AudioInitModule : InitModule
    {
        public override string ModuleName => "Audio Controller";

        [SerializeField] AudioRegistry audioRegistry;
        [SerializeField] AudioClips audioSettings;
        [SerializeField] int audioSourcesPoolSize = 4;

        [Header("3D")]
        [SerializeField] float maxDistance = 30;

        [Slider(0.0f, 360.0f)]
        [SerializeField] float spread = 180;
        [SerializeField] AnimationCurve rolloffCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 0.0f));

        [Header("Music")]
        [SerializeField] bool spawnGlobalMusicSource = true;
        [SerializeField] AudioClip globalMusicClip;

        private AudioController audioController;
        
        public override IEnumerator InitAsync(GameObject owner)
        {
            audioController = new AudioController(audioSourcesPoolSize, audioRegistry, maxDistance, spread, rolloffCurve);
            AudioController.SetLegacyAudioClips(audioSettings);

            if(spawnGlobalMusicSource)
            {
                GameObject globalMusicSourceObject = new GameObject("[GLOBAL MUSIC SOURCE]");
                globalMusicSourceObject.transform.ResetGlobal();

                DontDestroyOnLoad(globalMusicSourceObject);

                AudioSource audioSource = globalMusicSourceObject.AddComponent<AudioSource>();
                audioSource.clip = globalMusicClip;

                MusicSource musicSource = globalMusicSourceObject.AddComponent<MusicSource>();
                musicSource.Init();
                musicSource.Activate();
            }

            yield break;
        }

        override public void Unload()
        {
            audioController.Unload();
            audioController = null;
        }
    }
}
