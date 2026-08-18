using System;
using UnityEngine;

namespace HotUpdate
{
    /// <summary>
    /// 热更程序集入口
    /// 在 LoadDll 加载完 dll 后通过反射调用
    /// </summary>
    public static class Entry
    {
        public static void Start()
        {
            Debug.Log("[HotUpdate] Entry.Start() called — HotUpdate assembly is alive!");
            // 在这里初始化热更逻辑
            // 例如：注册热更 MonoBehaviour、初始化热更模块等
        }
    }
}