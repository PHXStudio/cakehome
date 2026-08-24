using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Place in any scene and build to device (no Development Build required).
    /// Listens for haptic pattern JSON on <see cref="RECEIVE_PORT"/> via UDP and plays it.
    /// Broadcasts its IP every 2 s so the Haptic Pattern Editor can auto-discover the device.
    /// </summary>
    public class HapticNetworkReceiver : MonoBehaviour
    {
        public const int RECEIVE_PORT   = 7878;
        public const int BROADCAST_PORT = 7879;

        private UdpClient  receiver;
        private UdpClient  broadcaster;
        private Thread     receiveThread;
        private Thread     broadcastThread;
        private readonly ConcurrentQueue<string> pendingMessages = new ConcurrentQueue<string>();
        private volatile bool running;
        private string localIP = "";

        private void Awake()
        {
            if (!Haptic.IsInitialized)
                new Haptic();

            Haptic.IsActive = true;
            localIP = GetLocalIP();
        }

        private void Start()
        {
            running = true;
            receiveThread   = new Thread(ReceiveLoop)    { IsBackground = true };
            broadcastThread = new Thread(BroadcastLoop)  { IsBackground = true };
            receiveThread.Start();
            broadcastThread.Start();
        }

        private void Update()
        {
            while (pendingMessages.TryDequeue(out string json))
                PlayPattern(json);
        }

        private void OnDestroy()
        {
            running = false;
            receiver?.Close();
            broadcaster?.Close();
            Haptic.Unload();
        }

        // ── Network loops (background threads) ────────────────────────────────

        private void ReceiveLoop()
        {
            try
            {
                receiver = new UdpClient(RECEIVE_PORT);
                var ep = new IPEndPoint(IPAddress.Any, 0);
                while (running)
                {
                    byte[] data = receiver.Receive(ref ep);
                    pendingMessages.Enqueue(Encoding.UTF8.GetString(data));
                }
            }
            catch (Exception e) { if (running) Debug.LogError($"[HapticReceiver] {e.Message}"); }
        }

        private void BroadcastLoop()
        {
            try
            {
                broadcaster = new UdpClient();
                broadcaster.EnableBroadcast = true;
                byte[]     msg = Encoding.UTF8.GetBytes($"HAPTIC:{localIP}");
                IPEndPoint ep  = new IPEndPoint(IPAddress.Broadcast, BROADCAST_PORT);
                while (running)
                {
                    broadcaster.Send(msg, msg.Length, ep);
                    Thread.Sleep(2000);
                }
            }
            catch (Exception e) { if (running) Debug.LogError($"[HapticBroadcast] {e.Message}"); }
        }

        // ── Pattern playback (main thread) ────────────────────────────────────

        private void PlayPattern(string json)
        {
            HapticPattern pattern = JsonUtility.FromJson<HapticPattern>(json);
            if (pattern == null || string.IsNullOrEmpty(pattern.ID)) return;

            Haptic.RegisterPattern(pattern);
            Haptic.Play(pattern.ID);
        }

        // ── IP display ────────────────────────────────────────────────────────

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize  = 14,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = Color.white },
            };
            GUI.Box(new Rect(10, 10, 300, 32), $"Haptic Test  •  {localIP}:{RECEIVE_PORT}", style);
        }

        private static string GetLocalIP()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Connect("8.8.8.8", 65530);
                    return ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
                }
            }
            catch { return "?.?.?.?"; }
        }
    }
}
