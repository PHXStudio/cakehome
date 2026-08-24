using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RequireComponent(typeof(AudioSource))]
    public class MusicSource : MonoBehaviour
    {
        private const float DEFAULT_FADE_DURATION = 0.3f;

        private static MusicSource defaultMusicSource;

        private static MusicSource activeMusicSource;
        public static MusicSource ActiveMusicSource => activeMusicSource;

        [SerializeField] bool activateAutomatically = false;

        private AudioSource audioSource;
        public AudioSource AudioSource => audioSource;

        // Raw 0–1 volume, independent of global volume and volumeMultiplier
        private float currentRawVolume = 0f;
        private Coroutine fadeCoroutine;

        private float volumeMultiplier = 1.0f;

        private bool isInitialized;

        private void Start()
        {
            if(activateAutomatically)
            {
                Init();
                Activate();
            }
        }

        public void Init()
        {
            if (isInitialized) return;
            isInitialized = true;

            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;

            volumeMultiplier = audioSource.volume;
            currentRawVolume = 0f;
            audioSource.volume = 0f;

            AudioController.VolumeChanged += OnVolumeChanged;
        }

        public void Unload()
        {
            if (!isInitialized) return;

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            AudioController.VolumeChanged -= OnVolumeChanged;
            isInitialized = false;
        }

        private void OnDestroy()
        {
            Unload();
        }

        public void SetAsDefault()
        {
            defaultMusicSource = this;
        }

        public void Activate()
        {
            if (activeMusicSource == this) return;

            if(activeMusicSource != null)
            {
                activeMusicSource.audioSource.volume = 0.0f;
                activeMusicSource.audioSource.Stop();
                activeMusicSource.currentRawVolume = 0f;
            }

            audioSource.Play();

            Fade(1.0f, DEFAULT_FADE_DURATION);

            activeMusicSource = this;
        }

        public void SetVolume(float volume)
        {
            currentRawVolume = volume;
            audioSource.volume = volume * AudioController.GetVolume(AudioType.Music) * volumeMultiplier;
        }

        public void Fade(float targetVolume, float duration, float delay = 0, SimpleCallback onComplete = null)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeRoutine(targetVolume, duration, delay, onComplete));
        }

        private IEnumerator FadeRoutine(float target, float duration, float delay, SimpleCallback onComplete)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            float start = currentRawVolume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                currentRawVolume = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                audioSource.volume = currentRawVolume * AudioController.GetVolume(AudioType.Music) * volumeMultiplier;
                yield return null;
            }

            currentRawVolume = target;
            audioSource.volume = currentRawVolume * AudioController.GetVolume(AudioType.Music) * volumeMultiplier;

            fadeCoroutine = null;
            onComplete?.Invoke();
        }

        private void OnVolumeChanged(AudioType audioType, float volume)
        {
            if (audioType != AudioType.Music) return;

            audioSource.volume = currentRawVolume * volume * volumeMultiplier;
        }

        public bool IsActive()
        {
            return activeMusicSource == this;
        }

        public static void ActivateDefault()
        {
            if (activeMusicSource == defaultMusicSource) return;

            if (defaultMusicSource != null)
                defaultMusicSource.Activate();
        }

    }
}