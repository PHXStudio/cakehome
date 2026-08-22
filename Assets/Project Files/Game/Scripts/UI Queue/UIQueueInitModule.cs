using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("UI Queue")]
    public class UIQueueInitModule : InitModule
    {
        public override string ModuleName => "UI Queue";

        private UIQueueController uiQueueController;

        public override IEnumerator InitAsync(GameObject owner)
        {
            uiQueueController = new UIQueueController();
            yield break;
        }

        public override void Unload()
        {
            uiQueueController?.Unload();
        }
    }
}
