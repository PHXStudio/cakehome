using UnityEditor;

/// <summary>
/// 一次性开启 MCP for Unity 的 HTTP 自动启动：
/// 设置 AutoStartOnLoad + UseHttpTransport 预置项，
/// 下次 domain reload 时 HttpAutoStartHandler 会自动拉起
/// 本地 HTTP MCP 服务器（uvx mcpforunityserver）并连接桥接。
/// </summary>
[InitializeOnLoad]
public static class UnityMcpAutoStart
{
    private const string DONE_KEY = "UnityMcpAutoStart.Done";

    static UnityMcpAutoStart()
    {
        if (SessionState.GetBool(DONE_KEY, false))
            return;

        SessionState.SetBool(DONE_KEY, true);

        EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
        EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
        // 本地作用域（默认 local 即可，绑定 127.0.0.1）
        EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");

        UnityEngine.Debug.Log("[UnityMcpAutoStart] AutoStartOnLoad + UseHttpTransport 已开启，等待下次 reload 自动启动 HTTP 桥接");
    }
}
// touch 2026年08月18日 22:26:16
