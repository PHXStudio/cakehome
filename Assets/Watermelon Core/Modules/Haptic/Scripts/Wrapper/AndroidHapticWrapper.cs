using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public sealed class AndroidHapticWrapper : BaseHapticWrapper
    {
        private Dictionary<int, AndroidHapticPattern> registeredPatterns;

        private AndroidJavaObject vibrationService;
        private int sdkVersion;

        public override void Init()
        {
            registeredPatterns = new Dictionary<int, AndroidHapticPattern>();

#if UNITY_ANDROID
            Try(() =>
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    vibrationService = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                // Get the Android SDK version
                using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    sdkVersion = versionClass.GetStatic<int>("SDK_INT");
                }
            }, "Failed to Initialize haptic module!");
#endif
        }

        public override void Play(float duration = 0.3f, float intensity = 1.0f)
        {
            if (vibrationService == null) return;

#if UNITY_ANDROID
            Try(() =>
            {
                if (sdkVersion >= 26)
                {
                    using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)(duration * 1000), (int)Mathf.Lerp(1, 255, intensity));

                        vibrationService.Call("vibrate", vibrationEffect);
                    }
                }
                else
                {
                    vibrationService.Call("vibrate", duration);
                }
            }, "Failed to play haptic!");
#endif
        }

        public override void Play(string patternID)
        {
            int patternHash = patternID.GetHashCode();

            if (!registeredPatterns.ContainsKey(patternHash)) return;

            if (vibrationService == null) return;

#if UNITY_ANDROID
            Try(() =>
            {
                AndroidHapticPattern androidHapticPattern = registeredPatterns[patternHash];

                if (sdkVersion >= 26)
                {
                    using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", androidHapticPattern.Pattern, androidHapticPattern.Amplitudes, -1);

                        vibrationService.Call("vibrate", vibrationEffect);
                    }
                }
                else
                {
                    vibrationService.Call("vibrate", androidHapticPattern.Pattern, -1);
                }
            }, string.Format("Failed to play pattern with ID: {0}!", patternID));
#endif
        }

        public override void RegisterPattern(HapticPattern pattern)
        {
            int patternHash = pattern.ID.GetHashCode();

            if (registeredPatterns.ContainsKey(patternHash)) return;

            registeredPatterns.Add(patternHash, new AndroidHapticPattern(pattern));
        }

        private class AndroidHapticPattern
        {
            public long[] Pattern { get; private set; }
            public int[] Amplitudes { get; private set; }

            public AndroidHapticPattern(HapticPattern hapticPattern)
            {
                // Sort by StartTime so overlaps are predictable regardless of authoring order
                HapticEvent[] sorted = (HapticEvent[])hapticPattern.Pattern.Clone();
                System.Array.Sort(sorted, (a, b) => a.StartTime.CompareTo(b.StartTime));

                List<long> patternList   = new List<long>();
                List<int>  amplitudeList = new List<int>();

                float previousEndTime = 0f;

                for (int i = 0; i < sorted.Length; i++)
                {
                    HapticEvent ev = sorted[i];

                    // Overlap resolution: truncate this event so it ends when the next one starts
                    float effectiveDuration = ev.Duration;
                    if (i < sorted.Length - 1)
                        effectiveDuration = Mathf.Min(effectiveDuration, sorted[i + 1].StartTime - ev.StartTime);

                    if (effectiveDuration <= 0f) continue;

                    // Pause before this event
                    if (ev.StartTime > previousEndTime)
                    {
                        patternList.Add((long)((ev.StartTime - previousEndTime) * 1000));
                        amplitudeList.Add(0);
                    }

                    patternList.Add((long)(effectiveDuration * 1000));
                    amplitudeList.Add((int)Mathf.Lerp(1, 255, ev.Intensity));

                    previousEndTime = ev.StartTime + effectiveDuration;
                }

                Pattern    = patternList.ToArray();
                Amplitudes = amplitudeList.ToArray();
            }
        }
    }
}
