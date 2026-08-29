// Android APK 构建器：batchmode / GitHub Actions 云构建出 APK
// 用法: Unity.exe -batchmode -projectPath . -buildTarget Android -executeMethod BuildAndroid.Build
// 签名：CI 提供 ANDROID_KEYSTORE_BASE64/PASS/KEY_ALIAS/KEY_PASS 环境变量则签名，否则用 Unity debug keystore（可安装测试）
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildAndroid
{
    private static bool IsCI => System.Environment.GetEnvironmentVariable("CI") == "true";

    public static void Build()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        // 架构：主流真机 arm64 + 兼容 armv7
        PlayerSettings.Android.targetArchitectures =
            AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;

        // 可选 keystore 签名（CI secrets 注入）
        string ksB64 = System.Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_BASE64");
        if (!string.IsNullOrEmpty(ksB64))
        {
            string ksPath = Path.Combine(Directory.GetCurrentDirectory(), "builds/release.keystore");
            Directory.CreateDirectory("builds");
            File.WriteAllBytes(ksPath, System.Convert.FromBase64String(ksB64));
            PlayerSettings.Android.keystoreName = ksPath;
            PlayerSettings.Android.keystorePass = System.Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS") ?? "";
            PlayerSettings.Android.keyaliasName = System.Environment.GetEnvironmentVariable("ANDROID_KEY_ALIAS") ?? "";
            PlayerSettings.Android.keyaliasPass = System.Environment.GetEnvironmentVariable("ANDROID_KEY_PASS") ?? "";
            Debug.Log("[BuildAndroid] keystore applied: " + ksPath);
        }
        else
        {
            Debug.Log("[BuildAndroid] no keystore secrets — will use Unity debug keystore (installable for testing)");
        }

        string[] scenes =
        {
            "Assets/Project Files/Game/Scenes/Init.unity",
            "Assets/Project Files/Game/Scenes/Game.unity"
        };

        string outPath = "builds/android/CakeHome.apk";
        var report = BuildPipeline.BuildPlayer(scenes, outPath, BuildTarget.Android, BuildOptions.None);

        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError("Android build FAILED: " + report.summary.result);
            for (int i = 0; i < report.steps.Length; i++)
            {
                foreach (var m in report.steps[i].messages)
                {
                    if (m.type == LogType.Error || m.type == LogType.Exception)
                        Debug.LogError($"  [{report.steps[i].name}] {m.content}");
                }
            }
            if (IsCI)
                throw new System.Exception("Android build failed: " + report.summary.result);
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log($"Android APK built OK -> {outPath} ({report.summary.totalSize / 1024 / 1024}MB)");
            if (!IsCI)
                EditorApplication.Exit(0);
        }
    }
}
