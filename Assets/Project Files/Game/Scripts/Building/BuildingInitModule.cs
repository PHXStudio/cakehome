using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Building")]
    public class BuildingInitModule : InitModule
    {
        public override string ModuleName => "Building";

        [SerializeField] LevelDatabase levelDatabase;

        private BuildingController buildingController;

        public override IEnumerator InitAsync(GameObject owner)
        {
            buildingController = new BuildingController(levelDatabase);
            yield break;
        }

        public override void Unload()
        {
            buildingController?.Unload();
        }
    }
}
