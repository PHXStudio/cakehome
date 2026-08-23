using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    public static class LivesSystemEditorActions
    {
        [MenuItem("Actions/Energy System/Full Energy")]
        private static void FullEnergy()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            EnergyController.Add(EnergyController.Max, ignoreCap: false);

            Debug.Log("FullEnergy action performed");
        }

        [MenuItem("Actions/Energy System/No Energy")]
        private static void NoEnergy()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            EnergyController.Set(0);

            Debug.Log("NoEnergy action performed");
        }

        [MenuItem("Actions/Energy System/-10 Energy")]
        private static void TakeEnergy()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            EnergyController.TrySpend(10);

            Debug.Log("-10 Energy action performed");
        }

        [MenuItem("Actions/Energy System/+10 Energy")]
        private static void AddEnergy()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            EnergyController.Add(10, ignoreCap: true);

            Debug.Log("+10 Energy action performed");
        }

        [MenuItem("Actions/Energy System/Show Recover Energy Panel")]
        private static void ShowRecoverEnergyPanel()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            UIRecoverEnergy.Show();
        }

        [MenuItem("Actions/Energy System/Enable Infinite Mode (30 seconds)")]
        private static void EnableInfiniteMode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            LivesSystem.EnableInfiniteMode(30);

            Debug.Log("EnableInfiniteMode action performed");
        }

        [MenuItem("Actions/Energy System/Disable Infinite Mode")]
        private static void DisableInfiniteMode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            LivesSystem.DisableInfiniteMode();

            Debug.Log("DisableInfiniteMode action performed");
        }
    }
}
