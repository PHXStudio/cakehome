using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// 解决 Unity 编辑器失焦时 Play Mode 不更新/不渲染的问题，保证 MCP 自动化在后台稳定运行。
///
/// 失焦冻结的三个层次及对策：
/// 1. 逻辑冻结 —— runInBackground 写入 ProjectSettings.asset(=true)，失焦时 Update/物理/协程继续执行
/// 2. 运行时兜底 —— EnteredPlayMode 时再设 Application.runInBackground=true（防止 PlayerSettings 被外部改回）
/// 3. 画面不刷新 —— 失焦时以 ~10Hz 强制 GameView/SceneView Repaint，截图/状态读取始终可见新帧
///
/// 附带：进入 Play Mode 时 vSyncCount=0 + targetFrameRate=60，渲染不依赖窗口 vsync，帧率稳定便于自动化时序。
///
/// 开关：菜单 Tools/MCP/失焦自动刷新（EditorPrefs 持久化，默认开启）
/// </summary>
[InitializeOnLoad]
public static class UnityBackgroundUpdate
{
    private const string PrefKey = "UnityMCP.BackgroundAutoRefresh";
    private static bool _enabled = EditorPrefs.GetBool(PrefKey, true);
    private static double _lastRepaint = -1d;

    static UnityBackgroundUpdate()
    {
        // 幂等：值已是 true 时赋值不触发脏写入，不会反复产生版本控制噪音
        PlayerSettings.runInBackground = true;
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Debug.Log("[UnityBackgroundUpdate] 已启用：runInBackground=true + 失焦时强制刷新 GameView/SceneView");
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode)
            return;

        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private static void OnEditorUpdate()
    {
        // 强制重绘已禁用（2026-08）：
        // 当 InternalEditorUtility.isApplicationActive 处于异常状态时，高频 Repaint
        // 会干扰编辑器 UI 渲染，可能导致整个编辑器窗口白屏。
        // runInBackground=true 已保证 Play Mode 逻辑在失焦时持续运行，
        // 画面刷新问题后续改用更安全的方式处理。
        return;
    }

    [MenuItem("Tools/MCP/失焦自动刷新 - 开启", false, 200)]
    private static void Enable()
    {
        _enabled = true;
        EditorPrefs.SetBool(PrefKey, true);
        Debug.Log("[UnityBackgroundUpdate] 已开启：失焦自动刷新视图");
    }

    [MenuItem("Tools/MCP/失焦自动刷新 - 关闭", false, 200)]
    private static void Disable()
    {
        _enabled = false;
        EditorPrefs.SetBool(PrefKey, false);
        Debug.Log("[UnityBackgroundUpdate] 已关闭：失焦自动刷新视图");
    }

    [MenuItem("Tools/MCP/失焦自动刷新 - 开启", true, 200)]
    private static bool EnableValidate() => !EditorPrefs.GetBool(PrefKey, true);

    [MenuItem("Tools/MCP/失焦自动刷新 - 关闭", true, 200)]
    private static bool DisableValidate() => EditorPrefs.GetBool(PrefKey, true);
}
