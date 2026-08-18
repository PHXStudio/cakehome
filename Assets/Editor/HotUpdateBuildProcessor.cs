using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public static class HotUpdateBuildProcessor
{
    private const string HotUpdateDir = "Assets/HotUpdate";
    private const string StreamingTargetDir = "Assets/StreamingAssets/HotUpdate";
    private const string HotUpdateAssemblyName = "HotUpdate";

    [MenuItem("HotUpdate/Build HotUpdate Dlls")]
    public static void BuildHotUpdateDlls()
    {
        // 确保输出目录存在
        if (!Directory.Exists(StreamingTargetDir))
            Directory.CreateDirectory(StreamingTargetDir);

        // 构建设置
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        var buildGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);

        // 构建 HotUpdate 程序集
        var asmdefPath = Path.Combine(HotUpdateDir, "HotUpdate.asmdef");
        if (!File.Exists(asmdefPath))
        {
            Debug.LogError($"[HotUpdateBuild] HotUpdate.asmdef not found at {asmdefPath}");
            return;
        }

        // 使用 AssemblyBuilder 编译 HotUpdate 程序集
        var assemblyBuilder = new AssemblyBuilder(
            Path.GetFullPath(Path.Combine(StreamingTargetDir, $"{HotUpdateAssemblyName}.dll")),
            asmdefPath
        );
        assemblyBuilder.buildTarget = buildTarget;
        assemblyBuilder.buildTargetGroup = buildGroup;

        assemblyBuilder.Build();

        // 等待编译完成
        while (EditorApplication.isCompiling)
        {
            System.Threading.Thread.Sleep(100);
        }

        Debug.Log($"[HotUpdateBuild] HotUpdate assembly built to: {StreamingTargetDir}/{HotUpdateAssemblyName}.dll");

        // 刷新 AssetDatabase
        AssetDatabase.Refresh();
    }

    [MenuItem("HotUpdate/Build AOT Dlls (for reference)")]
    public static void CopyAOTDlls()
    {
        // 复制 AOT 程序集到 StreamingAssets（方便调试和对照）
        var outputDir = "Assets/StreamingAssets/AOT";
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        // 从 Unity 安装目录复制基础 AOT dll
        var editorPath = Path.GetDirectoryName(EditorApplication.applicationPath);
        var managedDir = Path.Combine(editorPath, "Data", "Managed", "UnityEngine");
        if (Directory.Exists(managedDir))
        {
            foreach (var dll in Directory.GetFiles(managedDir, "*.dll"))
            {
                var dest = Path.Combine(outputDir, Path.GetFileName(dll));
                File.Copy(dll, dest, true);
            }
            Debug.Log($"[HotUpdateBuild] AOT dlls copied to: {outputDir}");
        }

        AssetDatabase.Refresh();
    }

    [MenuItem("HotUpdate/Build All (AOT + HotUpdate)")]
    public static void BuildAll()
    {
        CopyAOTDlls();
        BuildHotUpdateDlls();
        Debug.Log("[HotUpdateBuild] All done!");
    }
}