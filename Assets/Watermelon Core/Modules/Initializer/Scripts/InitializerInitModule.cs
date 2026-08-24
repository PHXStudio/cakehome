using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Initializer Settings", true, order: 999)]
    public class InitializerInitModule : InitModule
    {
        public override string ModuleName => "Initializer Settings";

        // 系统消息画布由 Initializer.prefab 上的 SystemMessagePreInitializer 统一初始化注册,
        // 此处不再重复实例化(否则会出现双画布、loading 面板常驻)。
        public override IEnumerator InitAsync(GameObject owner)
        {
            yield break;
        }
    }
}
