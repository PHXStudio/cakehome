using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Energy")]
    public class EnergyInitModule : InitModule
    {
        public override string ModuleName => "Energy";

        [SerializeField] EnergyData energyData;

        private EnergyController energyController;

        public override IEnumerator InitAsync(GameObject owner)
        {
            energyController = new EnergyController(energyData);

            EnergyRegenRunner runner = owner.AddComponent<EnergyRegenRunner>();
            runner.Init(energyController);

            yield break;
        }

        public override void Unload()
        {
            energyController?.Unload();
        }
    }
}
