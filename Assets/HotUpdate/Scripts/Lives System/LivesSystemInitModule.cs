using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Lives System", core: false)]
    public class LivesSystemInitModule : InitModule
    {
        public override string ModuleName => "Lives System";

        // LivesData 资产已随能量统一废弃（数值全在 Energy Data），模块无需再挂配置
        public override IEnumerator InitAsync(GameObject owner)
        {
            LivesSystem.Init();
            yield break;
        }
    }
}