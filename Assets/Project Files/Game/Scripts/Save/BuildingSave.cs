using System;
using System.Collections.Generic;

namespace Watermelon
{
    [Serializable]
    public class BuildingSave : ISaveObject
    {
        public int TotalUpgradesInZone;
        public List<BuildingUpgradeSaveData> Buildings = new List<BuildingUpgradeSaveData>();
        // Zones whose atUpgrade: 0 progression step (reward/dialog) has already been granted —
        // it fires once at zone start, outside the upgrade-count flow (see BuildingController).
        public List<string> InitialProgressionZones = new List<string>();

        public void OnBeforeSave() { }
    }
}
