using System;

namespace Watermelon
{
    public class ZoneController
    {
        private static ZoneController instance;

        public static event Action<ZoneData> OnZoneChanged;

        private ZoneSave  save;
        private ZoneData[] allZones;

        public ZoneController(LevelDatabase levelDatabase)
        {
            instance = this;
            allZones = levelDatabase?.Zones ?? Array.Empty<ZoneData>();

            save = SaveController.GetSaveObject<ZoneSave>("Zone");

            if (string.IsNullOrEmpty(save.CurrentZoneId) && allZones.Length > 0)
            {
                save.CurrentZoneId = allZones[0].ZoneId;
                SaveController.MarkAsSaveIsRequired();
            }
        }

        public static ZoneData[] AllZones => instance?.allZones ?? Array.Empty<ZoneData>();

        public static ZoneData CurrentZone => instance?.GetCurrentZoneInternal();

        public static bool IsUnlocked(ZoneData zone) =>
            zone != null && ExperienceController.CurrentLevel >= zone.MinLevelToUnlock;

        public static void SelectZone(string zoneId) => instance?.SelectZoneInternal(zoneId);

        private ZoneData GetCurrentZoneInternal()
        {
            if (allZones.Length == 0) return null;

            if (!string.IsNullOrEmpty(save.CurrentZoneId))
                foreach (ZoneData zone in allZones)
                    if (zone.ZoneId == save.CurrentZoneId) return zone;

            return allZones[0];
        }

        private void SelectZoneInternal(string zoneId)
        {
            if (zoneId == save.CurrentZoneId) return;

            ZoneData zone = Array.Find(allZones, z => z.ZoneId == zoneId);
            if (zone == null || !IsUnlocked(zone)) return;

            save.CurrentZoneId = zoneId;
            SaveController.MarkAsSaveIsRequired();

            Checkpoint.Log($"Zone switched to {zoneId}");

            OnZoneChanged?.Invoke(zone);
        }

        public void Unload()
        {
            instance = null;
        }
    }
}
