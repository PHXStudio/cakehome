using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Editor-side UDP sender for the Haptic Pattern Editor.
    /// Auto-discovers devices running <see cref="HapticNetworkReceiver"/> on the local network.
    /// </summary>
    public static class HapticNetworkSender
    {
        public const int SEND_PORT      = HapticNetworkReceiver.RECEIVE_PORT;
        public const int DISCOVERY_PORT = HapticNetworkReceiver.BROADCAST_PORT;

        private const string PREF_IP      = "Watermelon.Haptic.DeviceIP";
        private const string PREF_HISTORY  = "Watermelon.Haptic.IPHistory";
        private const int    MAX_HISTORY   = 6;
        private const char   HISTORY_SEP   = '|';

        private static UdpClient     discoveryClient;
        private static Thread        discoveryThread;
        private static volatile bool running;
        private static string        discoveredIP = "";

        /// <summary>Most recently auto-discovered device IP.</summary>
        public static string DiscoveredIP => discoveredIP;

        /// <summary>Manually configured or auto-filled device IP (persisted via EditorPrefs).</summary>
        public static string DeviceIP
        {
            get => EditorPrefs.GetString(PREF_IP, "");
            set => EditorPrefs.SetString(PREF_IP, value);
        }

        // ── Discovery ─────────────────────────────────────────────────────────

        public static void StartDiscovery()
        {
            running         = true;
            discoveryThread = new Thread(DiscoveryLoop) { IsBackground = true, Name = "HapticDiscovery" };
            discoveryThread.Start();
        }

        public static void StopDiscovery()
        {
            running = false;
            discoveryClient?.Close();
        }

        private static void DiscoveryLoop()
        {
            try
            {
                discoveryClient = new UdpClient(DISCOVERY_PORT);
                discoveryClient.Client.ReceiveTimeout = 5000;
                var ep = new IPEndPoint(IPAddress.Any, 0);

                while (running)
                {
                    try
                    {
                        byte[] data = discoveryClient.Receive(ref ep);
                        string msg  = Encoding.UTF8.GetString(data);

                        if (msg.StartsWith("HAPTIC:"))
                        {
                            discoveredIP = msg.Substring("HAPTIC:".Length).Trim();

                            // Auto-fill if empty
                            if (string.IsNullOrEmpty(DeviceIP))
                                DeviceIP = discoveredIP;
                        }
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut) { }
                }
            }
            catch (Exception e)
            {
                if (running) Debug.LogWarning($"[HapticSender] Discovery error: {e.Message}");
            }
        }

        // ── IP History ────────────────────────────────────────────────────────

        /// <summary>Recently used device IPs, newest first.</summary>
        public static string[] IPHistory
        {
            get
            {
                string raw = EditorPrefs.GetString(PREF_HISTORY, "");
                if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
                return raw.Split(HISTORY_SEP);
            }
        }

        /// <summary>Adds <paramref name="ip"/> to the front of the history, capped at <see cref="MAX_HISTORY"/>.</summary>
        public static void AddToHistory(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return;

            var list = new System.Collections.Generic.List<string>(IPHistory);
            list.Remove(ip);          // remove duplicate
            list.Insert(0, ip);       // newest first
            if (list.Count > MAX_HISTORY)
                list.RemoveRange(MAX_HISTORY, list.Count - MAX_HISTORY);

            EditorPrefs.SetString(PREF_HISTORY, string.Join(HISTORY_SEP.ToString(), list));
        }

        // ── Send ──────────────────────────────────────────────────────────────

        /// <summary>Sends raw bytes to the stored <see cref="DeviceIP"/> and records it in history.</summary>
        public static bool Send(byte[] data)
        {
            string ip = DeviceIP;
            if (string.IsNullOrEmpty(ip)) return false;

            try
            {
                using (var client = new UdpClient())
                    client.Send(data, data.Length, new IPEndPoint(IPAddress.Parse(ip), SEND_PORT));

                AddToHistory(ip);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[HapticSender] Send failed to {ip}: {e.Message}");
                return false;
            }
        }
    }
}
