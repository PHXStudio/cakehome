using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Watermelon
{
    public class AudioController
    {
        private static AudioController instance;
        private bool isInitialized;

        private List<AudioSourceCase> audioSourcesPool;

        private AudioRegistry registry;
        private AudioListener audioListener;
        private AudioSave save;

        // Default 3D audio settings
        private readonly float maxDistance;
        private readonly float spread;
        private readonly AnimationCurve rolloffCurve;

        private event OnVolumeChangedCallback volumeChanged;

        private Dictionary<AudioType, float> volumeDictionary;
        private AudioType[] audioTypes;

        // ─── Constructor ─────────────────────────────────────────────────

        /// <param name="poolSize">Initial AudioSource pool size.</param>
        /// <param name="registry">Optional. Enables GetClip(name) lookups.</param>
        /// <param name="maxDistance">Default max distance for 3D sounds.</param>
        /// <param name="spread">Default spread for 3D sounds (0–360).</param>
        /// <param name="rolloffCurve">Custom rolloff curve. Pass null for linear default.</param>
        public AudioController(
            int poolSize,
            AudioRegistry registry = null,
            float maxDistance = 30,
            float spread = 180,
            AnimationCurve rolloffCurve = null)
        {
            if (instance != null)
                Debug.LogWarning("[AudioController]: Previous instance replaced. Call Unload() before creating a new one.");

            instance = this;

            this.maxDistance = maxDistance;
            this.spread = spread;
            this.rolloffCurve = rolloffCurve ?? new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 0.0f));
            this.registry = registry;

            registry?.BuildRuntimeLookup();

            save = SaveController.GetSaveObject<AudioSave>("audio");

            audioTypes = EnumUtils.GetEnumArray<AudioType>();
            volumeDictionary = new Dictionary<AudioType, float>();
            if (save.VolumeDatas != null)
            {
                foreach (AudioSave.VolumeData volumeData in save.VolumeDatas)
                {
                    volumeDictionary.Add(volumeData.AudioType, volumeData.Volume);
                }
            }

            CreateAudioListener();

            audioSourcesPool = new List<AudioSourceCase>();
            for (int i = 0; i < poolSize; i++)
            {
                audioSourcesPool.Add(new AudioSourceCase());
            }

            isInitialized = true;
        }

        // ─── Static facade: properties ───────────────────────────────────

        public static AudioListener AudioListener => instance?.audioListener;

        // 蛋糕版兼容:旧版通过 AudioInitModule 注入的 AudioClips 资产
        private static AudioClips legacyAudioClips;
        public static AudioClips AudioClips => legacyAudioClips;
        public static void SetLegacyAudioClips(AudioClips clips) => legacyAudioClips = clips;

        // ─── Static facade: event ────────────────────────────────────────

        public static event OnVolumeChangedCallback VolumeChanged
        {
            add
            {
                if (!CheckInitialized()) return;
                instance.volumeChanged += value;
            }
            remove
            {
                if (instance != null)
                    instance.volumeChanged -= value;
            }
        }

        // ─── Static facade: playback ─────────────────────────────────────

        public static void PlaySound(AudioClip clip, float volumePercentage = 1.0f, float pitch = 1.0f, float minDelay = 0f)
        {
            if (!CheckInitialized()) return;
            instance.PlaySoundInternal(clip, volumePercentage, pitch);
        }

        public static void PlaySound(AudioClip clip, Vector3 position, float volumePercentage = 1.0f, float pitch = 1.0f, float minDelay = 0f)
        {
            if (!CheckInitialized()) return;
            instance.PlaySoundInternal(clip, position, volumePercentage, pitch);
        }

        // ─── Static facade: registry lookup ─────────────────────────────

        public static AudioClip GetClip(string name)
        {
            if (!CheckInitialized()) return null;

            if (instance.registry == null)
            {
                Debug.LogWarning("[AudioController]: No AudioRegistry assigned. Cannot get clip by name.");
                return null;
            }

            return instance.registry.GetClip(name);
        }

        // ─── Static facade: volume ───────────────────────────────────────

        public static float GetVolume(AudioType audioType)
        {
            if (!CheckInitialized()) return 1.0f;
            return instance.GetVolumeInternal(audioType);
        }

        public static void SetVolume(AudioType audioType, float volume)
        {
            if (!CheckInitialized()) return;
            instance.SetVolumeInternal(audioType, volume);
        }

        public static bool IsAudioTypeActive(AudioType audioType)
        {
            if (!CheckInitialized()) return false;
            return instance.GetVolumeInternal(audioType) == 1.0f;
        }

        // ─── Static facade: sources & listener ──────────────────────────

        public static void ReleaseSources()
        {
            if (!CheckInitialized()) return;
            instance.ReleaseSourcesInternal();
        }

        // Called by AudioSourceCase during pool creation — no init warning needed.
        public static void ApplyDefaultSettings(ref AudioSource audioSource)
        {
            if (instance == null) return;
            instance.ApplyDefaultSettingsInternal(ref audioSource);
        }

        public static Transform AttachAudioListener(Transform parentObject)
        {
            if (!CheckInitialized()) return null;
            return instance.AttachAudioListenerInternal(parentObject);
        }

        public static void ResetAudioListenerParent()
        {
            if (!CheckInitialized()) return;
            instance.ResetAudioListenerParentInternal();
        }

        // ─── Helpers ─────────────────────────────────────────────────────

        private static bool CheckInitialized([CallerMemberName] string caller = "")
        {
            if (instance == null || !instance.isInitialized)
            {
                Debug.LogWarning($"[AudioController]: '{caller}' called before initialization.");
                return false;
            }

            return true;
        }

        // ─── Instance implementation ──────────────────────────────────────

        private void ApplyDefaultSettingsInternal(ref AudioSource audioSource)
        {
            audioSource.maxDistance = maxDistance;
            audioSource.spread = spread;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloffCurve);
        }

        private void CreateAudioListener()
        {
            if (audioListener != null)
                return;

            GameObject listenerObject = new GameObject("[AUDIO LISTENER]");
            listenerObject.transform.position = Vector3.zero;

            GameObject.DontDestroyOnLoad(listenerObject);

            audioListener = listenerObject.AddComponent<AudioListener>();
        }

        private Transform AttachAudioListenerInternal(Transform parentObject)
        {
            if (audioListener == null)
                CreateAudioListener();

            Transform t = audioListener.transform;
            t.SetParent(parentObject);
            t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            return t;
        }

        private void ResetAudioListenerParentInternal()
        {
            if (audioListener == null) return;

            audioListener.transform.SetParent(null);

            GameObject.DontDestroyOnLoad(audioListener.gameObject);
        }

        private void ReleaseSourcesInternal()
        {
            foreach (AudioSourceCase sourceCase in audioSourcesPool)
            {
                if (sourceCase.IsPlaying)
                    sourceCase.AudioSource.Stop();
            }
        }

        private void PlaySoundInternal(AudioClip clip, float volumePercentage, float pitch)
        {
            if (clip == null)
            {
                Debug.LogError("[AudioController]: Audio clip is null");
                return;
            }

            AudioSourceCase sourceCase = GetAudioSource();

            AudioSource source = sourceCase.AudioSource;
            source.spatialBlend = 0.0f; // 2D sound
            source.pitch = pitch;

            sourceCase.Play(clip, volumePercentage, AudioType.Sound);
        }

        private void PlaySoundInternal(AudioClip clip, Vector3 position, float volumePercentage, float pitch)
        {
            if (clip == null)
            {
                Debug.LogError("[AudioController]: Audio clip is null");
                return;
            }

            AudioSourceCase sourceCase = GetAudioSource();

            AudioSource source = sourceCase.AudioSource;
            source.transform.position = position;
            source.spatialBlend = 1.0f; // 3D sound
            source.pitch = pitch;

            sourceCase.Play(clip, volumePercentage, AudioType.Sound);
        }

        private AudioSourceCase GetAudioSource()
        {
            foreach (AudioSourceCase audioSource in audioSourcesPool)
            {
                if (!audioSource.IsPlaying)
                    return audioSource;
            }

            AudioSourceCase createdSource = new AudioSourceCase();
            audioSourcesPool.Add(createdSource);

            return createdSource;
        }

        private float GetVolumeInternal(AudioType audioType)
        {
            if (volumeDictionary.ContainsKey(audioType))
                return volumeDictionary[audioType];

            return 1.0f;
        }

        private void SetVolumeInternal(AudioType audioType, float volume)
        {
            foreach (AudioSourceCase audioSource in audioSourcesPool)
            {
                audioSource.OverrideVolume(audioType, volume);
            }

            volumeDictionary[audioType] = volume;

            FlushVolumesToSave();
            SaveController.MarkAsSaveIsRequired();

            volumeChanged?.Invoke(audioType, volume);
        }

        private void FlushVolumesToSave()
        {
            save.VolumeDatas = new AudioSave.VolumeData[audioTypes.Length];
            for (int i = 0; i < audioTypes.Length; i++)
                save.VolumeDatas[i] = new AudioSave.VolumeData { AudioType = audioTypes[i], Volume = GetVolumeInternal(audioTypes[i]) };
        }

        public void Unload()
        {
            if (save != null && volumeDictionary != null)
                FlushVolumesToSave();

            audioSourcesPool = null;
            registry = null;
            audioListener = null;
            save = null;
            volumeDictionary = null;
            audioTypes = null;
            volumeChanged = null;
            isInitialized = false;
            instance = null;
        }

        public delegate void OnVolumeChangedCallback(AudioType audioType, float volume);
    }

    public enum AudioType
    {
        Music = 0,
        Sound = 1
    }
}
