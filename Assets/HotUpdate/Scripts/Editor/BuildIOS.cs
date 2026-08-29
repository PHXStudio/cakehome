// iOS Xcode 工程构建器：batchmode 出工程（供 Mac mini 构建最终 IPA）
// 用法: Unity.exe -batchmode -projectPath . -buildTarget iOS -executeMethod BuildIOS.Build
using UnityEditor;
using UnityEngine;

public static class BuildIOS
{
    // game-ci/unity-builder 在 GitHub Actions 里设 CI=true；本地 batchmode 无此变量
    private static bool IsCI => System.Environment.GetEnvironmentVariable("CI") == "true";

    public static void Build()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);

        // 签名：自动签名开，Team ID 由 CI secrets / 本地 Xcode 里配置
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;

        string[] scenes =
        {
            "Assets/Project Files/Game/Scenes/Init.unity",
            "Assets/Project Files/Game/Scenes/Game.unity"
        };

        string outPath = "builds/ios";

        var report = BuildPipeline.BuildPlayer(scenes, outPath, BuildTarget.iOS, BuildOptions.None);

        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError("iOS build FAILED: " + report.summary.result);
            for (int i = 0; i < report.steps.Length; i++)
            {
                foreach (var m in report.steps[i].messages)
                {
                    if (m.type == LogType.Error || m.type == LogType.Exception)
                        Debug.LogError($"  [{report.steps[i].name}] {m.content}");
                }
            }
            if (IsCI)
                throw new System.Exception("iOS build failed: " + report.summary.result);
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log($"iOS Xcode project built OK -> {outPath} ({report.summary.totalSize / 1024 / 1024}MB)");
            if (!IsCI)
                EditorApplication.Exit(0);
        }
    }
}
