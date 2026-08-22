using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Dialog")]
    public class DialogInitModule : InitModule
    {
        public override string ModuleName => "Dialog";

        private DialogController dialogController;

        public override IEnumerator InitAsync(GameObject owner)
        {
            dialogController = new DialogController();
            yield break;
        }

        public override void Unload()
        {
            dialogController?.Unload();
        }
    }
}
