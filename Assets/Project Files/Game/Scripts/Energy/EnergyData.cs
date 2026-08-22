using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Energy Data", menuName = "Game/Energy Data")]
    public class EnergyData : ScriptableObject
    {
        [SerializeField] int maxEnergy = 50;
        [SerializeField] int regenIntervalSeconds = 120; // 2 min = 1 energy
        [SerializeField] int freeEnergyAmount = 100;
        [SerializeField] int paidEnergyAmount = 100;
        [SerializeField] int paidEnergyCostGems = 20;
        [SerializeField] int adEnergyAmount = 25;

        [Space]
        [SerializeField] Sprite icon;

        public int MaxEnergy            => maxEnergy;
        public int RegenIntervalSeconds => regenIntervalSeconds;
        public int FreeEnergyAmount     => freeEnergyAmount;
        public int PaidEnergyAmount     => paidEnergyAmount;
        public int PaidEnergyCostGems   => paidEnergyCostGems;
        public int AdEnergyAmount       => adEnergyAmount;
        public Sprite Icon              => icon;
    }
}
