using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    public static class OpenVSCodeMenu
    {
        [MenuItem("Help/Open Visual Studio Code", priority = 156)]
        public static void OpenVSCode()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "code",
                    Arguments = $"\"{projectRoot}\"",
                    UseShellExecute = true,
                });
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[Watermelon] Failed to open Visual Studio Code: {e.Message}. Make sure 'code' is available in PATH (VS Code > Shell Command: Install 'code' command in PATH).");
            }
        }
    }
}
