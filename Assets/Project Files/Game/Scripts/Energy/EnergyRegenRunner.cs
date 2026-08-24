using System.Collections;
using UnityEngine;

namespace Watermelon
{
    public class EnergyRegenRunner : MonoBehaviour
    {
        private EnergyController energyController;

        public void Init(EnergyController controller)
        {
            energyController = controller;
            StartCoroutine(RegenLoop());
        }

        // WaitForSecondsRealtime freezes while the app is suspended, so elapsed background
        // time is otherwise never accounted for until the next cold start. Resync on resume.
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) return;
            Resync();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;
            Resync();
        }

        private void Resync()
        {
            if (energyController == null) return;

            StopAllCoroutines();
            energyController.RecoverOffline();
            StartCoroutine(RegenLoop());
        }

        private IEnumerator RegenLoop()
        {
            yield return new WaitForSecondsRealtime(energyController.GetInitialRegenDelay());

            while (true)
            {
                energyController.Tick();

                if (EnergyController.Current >= EnergyController.Max)
                    yield return new WaitUntil(() => EnergyController.Current < EnergyController.Max);

                yield return new WaitForSecondsRealtime(energyController.RegenIntervalSeconds);
            }
        }
    }
}
