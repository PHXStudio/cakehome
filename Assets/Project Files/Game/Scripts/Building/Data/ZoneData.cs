using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Zone Data", menuName = "Game/Zone Data")]
    public class ZoneData : ScriptableObject
    {
        [UniqueID]
        [SerializeField] string zoneId;

        [Space]
        [SerializeField] string zoneName;
        [SerializeField] int    minLevelToUnlock;
        [SerializeField] Sprite                 backgroundSprite;
        [SerializeField] Sprite                 zonePreview;
        [SerializeField] ZoneBehavior           zonePrefab;
        [SerializeField] ZoneProgressionStep[] progression;

        public string              ZoneId            => zoneId;
        public string              ZoneName          => zoneName;
        public int                 MinLevelToUnlock  => minLevelToUnlock;
        public Sprite              BackgroundSprite  => backgroundSprite;
        public Sprite              ZonePreview       => zonePreview;
        public ZoneBehavior        ZonePrefab        => zonePrefab;
        public ZoneProgressionStep[] Progression     => progression;

        public int TotalUpgrades
        {
            get
            {
                if (zonePrefab == null || zonePrefab.Buildings == null) return 0;

                int total = 0;
                foreach (var b in zonePrefab.Buildings) total += b.MaxUpgrades;
                return total;
            }
        }

        public ZoneProgressionStep GetStepAt(int upgradeIndex)
        {
            if (progression == null) return null;
            foreach (var step in progression)
                if (step.AtUpgrade == upgradeIndex) return step;
            return null;
        }
    }
}
