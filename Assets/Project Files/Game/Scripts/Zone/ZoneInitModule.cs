using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Zone")]
    public class ZoneInitModule : InitModule
    {
        public override string ModuleName => "Zone";

        [SerializeField] LevelDatabase levelDatabase;

        private ZoneController zoneController;

        public override IEnumerator InitAsync(GameObject owner)
        {
            zoneController = new ZoneController(levelDatabase);
            yield break;
        }

        public override void Unload()
        {
            zoneController?.Unload();
        }
    }
}
