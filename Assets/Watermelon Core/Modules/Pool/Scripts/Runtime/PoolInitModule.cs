using System.Collections;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Init module that adds <see cref="PoolManager"/> to the initializer GameObject at startup.
    /// </summary>
    [RegisterModule("Pool", core: true, order: 800)]
    public class PoolInitModule : InitModule
    {
        public override string ModuleName => "Pool";
        
        public PoolInitModule()
        {
        }

        public override IEnumerator InitAsync(GameObject owner)
        {
            owner.AddComponent<PoolManager>();

            yield break;
        }
    }
}
