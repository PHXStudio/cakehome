using System;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace Watermelon
{
    /// <summary>
    /// Place in a Development Build scene to enable "Play on Device" from the
    /// <see cref="HapticPatternEditorWindow"/>. Connect the device to the Editor
    /// (USB or WiFi) with a Development Build running — the editor will send pattern
    /// data over <see cref="UnityEngine.Networking.PlayerConnection.PlayerConnection"/>.
    /// </summary>
    public class HapticTestReceiver : MonoBehaviour
    {
        /// <summary>Shared channel GUID — must match the editor-side sender.</summary>
        public const string PLAY_PATTERN_GUID = "2e7f4a1b-8c3d-4e9f-b5a6-1d2c4f6a8b3e";
        public static readonly Guid PlayPatternMessage = new Guid(PLAY_PATTERN_GUID);

        private BaseHapticWrapper hapticWrapper;

        private void Awake()
        {
            hapticWrapper = Haptic.GetPlatformWrapper();
        }

        private void OnEnable()
        {
            PlayerConnection.instance.Register(PlayPatternMessage, OnPlayPattern);
        }

        private void OnDisable()
        {
            PlayerConnection.instance.Unregister(PlayPatternMessage, OnPlayPattern);
        }

        private void OnPlayPattern(MessageEventArgs args)
        {
            string json = System.Text.Encoding.UTF8.GetString(args.data);
            HapticPattern pattern = JsonUtility.FromJson<HapticPattern>(json);
            if (pattern == null || string.IsNullOrEmpty(pattern.ID)) return;

            hapticWrapper.RegisterPattern(pattern);
            hapticWrapper.Play(pattern.ID);
        }
    }
}
