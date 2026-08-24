#pragma warning disable 0649

using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(menuName = "Data/Level/Level Database", fileName = "Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField, LevelEditorSetting] ZoneData[] zones;
        public ZoneData[] Zones => zones;

        public ZoneData GetZone(string zoneId)
        {
            if (zones == null) return null;
            foreach (ZoneData zone in zones)
                if (zone.ZoneId == zoneId) return zone;
            return null;
        }

        [SerializeField] MergeLevelData mergeLevelData;
        public MergeLevelData MergeLevelData => mergeLevelData;

        [SerializeField] Sprite halfLockedItemSprite;
        public Sprite HalfLockedItemSprite => halfLockedItemSprite;

        [SerializeField] Sprite lockedItemSprite;
        public Sprite LockedItemSprite => lockedItemSprite;

        [Space]
        [SerializeField] ActionButtonIcon[] actionButtonIcons;

        [Space]
        [SerializeField] Color cellColorA = Color.white;
        public Color CellColorA => cellColorA;

        [SerializeField] Color cellColorB = Color.white;
        public Color CellColorB => cellColorB;

        [Space]
        [SerializeField] float magnetRadius = 160f;
        public float MagnetRadius => magnetRadius;

        [SerializeField] float magnetStrength = 0.8f;
        public float MagnetStrength => magnetStrength;

        [SerializeField] float mergeThreshold = 50f;
        public float MergeThreshold => mergeThreshold;

        public void Init()
        {

        }

        public Sprite GetActionButtonIcon(ActionButtonType type)
        {
            if (actionButtonIcons == null) return null;

            for (int i = 0; i < actionButtonIcons.Length; i++)
            {
                if (actionButtonIcons[i].Type == type)
                    return actionButtonIcons[i].Icon;
            }

            return null;
        }
    }
}
