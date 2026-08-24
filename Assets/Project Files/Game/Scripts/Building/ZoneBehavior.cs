using UnityEngine;

namespace Watermelon
{
    public class ZoneBehavior : MonoBehaviour, ISceneSavingReceiver
    {
        [SerializeField, ReadOnly] BuildingBehavior[] buildings;

        public BuildingBehavior[] Buildings => buildings;
        public ZoneData           ZoneData  => zoneData;

        private ZoneData zoneData;

        public void Init(ZoneData zone)
        {
            zoneData = zone;

            if (buildings == null) return;

            foreach (BuildingBehavior building in buildings)
                building?.Init(this);
        }

        public void OnSceneSaving()
        {
            buildings = GetComponentsInChildren<BuildingBehavior>(true);
            RuntimeEditorUtils.SetDirty(this);
        }
    }
}
