using UnityEngine;
using UnityEditor;

namespace Watermelon
{
    public static class EnergyActionsMenu
    {
        [MenuItem("Actions/Debug/Energy/Get 100 Energy")]
        private static void GetEnergy()
        {
            EnergyController.Set(100);
        }

        [MenuItem("Actions/Debug/Energy/Get 100 Energy", true)]
        private static bool GetEnergyValidation()
        {
            return Application.isPlaying;
        }

        [MenuItem("Actions/Debug/Energy/No Energy")]
        private static void NoEnergy()
        {
            EnergyController.Set(0);
        }

        [MenuItem("Actions/Debug/Energy/No Energy", true)]
        private static bool NoEnergyValidation()
        {
            return Application.isPlaying;
        }
    }
}
