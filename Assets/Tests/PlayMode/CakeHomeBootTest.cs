using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

/// <summary>
/// PlayMode 引导冒烟测试：加载 Init(0) → 等 GameLoading 自动切到 Game(1) → 断言启动期间无 Error/Exception。
/// batchmode 下验证整个启动链路（模块初始化 + 场景切换 + Game 场景启动）。
/// </summary>
public class CakeHomeBootTest
{
    private static bool sawError;

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            sawError = true;
    }

    [UnityTest]
    public IEnumerator InitScene_BootsInto_GameScene()
    {
        sawError = false;
        Application.logMessageReceived += OnLog;
        Application.logMessageReceivedThreaded += OnLog;
        try
        {
            // 加载 Init（build index 0）
            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            yield return null;

            // 给 GameLoading 时间跑模块并 LoadSceneAsync(1)
            int maxFrames = 900; // ~15s
            for (int i = 0; i < maxFrames; i++)
            {
                if (SceneManager.GetActiveScene().buildIndex == 1)
                    break;
                yield return null;
            }

            Assert.AreEqual(1, SceneManager.GetActiveScene().buildIndex,
                "Init 场景未在限时内自动切入 Game 场景（GameLoading 卡住）");

            // Game 场景内再跑一会，让 GameController.Awake/Start 的连锁初始化完成
            for (int i = 0; i < 120; i++)
                yield return null;

            Assert.IsFalse(sawError, "启动期间出现运行期 Error/Exception，详见测试日志（含 NullReference/PU 等）");
        }
        finally
        {
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceivedThreaded -= OnLog;
        }
    }
}
