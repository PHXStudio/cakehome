using System.Collections;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Init module that creates the <see cref="Haptic"/> instance during project initialization.
    /// </summary>
    [RegisterModule("Haptic")]
    public class HapticInitModule : InitModule
    {
        public override string ModuleName => "Haptic";

        public override IEnumerator InitAsync(GameObject owner)
        {
            new Haptic();
            
            yield break;
        }
    }
}
