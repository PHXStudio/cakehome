using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Lives System", core: false)]
    public class LivesSystemInitModule : InitModule
    {
        public override string ModuleName => "Lives System";

        [SerializeField] LivesData livesData;

        public override IEnumerator InitAsync(GameObject owner)
        {
            if (livesData == null)
            {
                Debug.LogError("LivesData is not assigned in Project Init Settings", this);

                yield break;
            }

            LivesSystem.Init(livesData);
            yield break;
        }
    }
}