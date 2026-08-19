using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Watermelon
{
    [InitializeOnLoad]
    public static class AutoInitializerLoader
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void LoadMain()
        {
            // 兜底逻辑已禁用：
            // Initializer 已作为场景对象放置在 Init 场景（Assets/Project Files/Game/Prefabs/Initializer.prefab 的实例），
            // 由场景对象在 Awake/Start 中完整驱动核心模块初始化与场景加载（Init → Game）。
            // 此前在此处（BeforeSceneLoad）重复实例化 Initializer 会抢占 static 引用、造成
            // Event System 冲突、模块二次初始化与重复场景加载，故直接返回不兜底。
            return;
        }
    }
}