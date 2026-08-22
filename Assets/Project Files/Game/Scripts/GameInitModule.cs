using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Game Settings")]
    public class GameInitModule : InitModule
    {
        public override string ModuleName => "Game Settings";

        [SerializeField] GameData gameData;

        public override IEnumerator InitAsync(GameObject owner)
        {
            gameData.Init();

            yield break;
        }
    }
}
