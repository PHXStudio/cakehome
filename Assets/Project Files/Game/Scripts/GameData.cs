using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Game Data", menuName = "Data/Game Data")]
    public class GameData : ScriptableObject
    {
        [SerializeField] LevelDatabase levelDatabase;
        public LevelDatabase LevelDatabase => levelDatabase;

        [Space]
        [Header("Bounce Animation")]
        [SerializeField] BounceSettings bounceSettings;
        public BounceSettings BounceSettings => bounceSettings;

        [SerializeField] BounceSettings energyBounceSettings;
        public BounceSettings EnergyBounceSettings => energyBounceSettings;

        [SerializeField] BounceSettings currencyBounceSettings;
        public BounceSettings CurrencyBounceSettings => currencyBounceSettings;

        [Header("Building Upgrade")]
        [SerializeField] Color buildingUpgradeFlashColor = new Color(1f, 1f, 1f, 0.6f);
        public Color BuildingUpgradeFlashColor => buildingUpgradeFlashColor;

        public static GameData Data { get; private set; }

        public void Init()
        {
            Data = this;

            BounceAnimation.Init(bounceSettings);
        }
    }
}
