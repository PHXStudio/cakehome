using System;
using System.Collections.Generic;

namespace Watermelon
{
    public class BuildingController
    {
        // Gives the building's bounce → wait → particle → wait → sprite-swap → bounce sequence
        // (see BuildingBehavior.OnUpgradeCompleted) time to fully play before exp, dialog and the
        // level-up popup kick in (see UpgradeInternal/TriggerProgression).
        // Passed as the UIQueueController entry's delay.
        private const float PROGRESSION_DELAY = 1.8f;

        private static BuildingController instance;

        public static event Action<ZoneData, string, int> OnUpgradeCompleted; // zone, buildingId, totalUpgradesInZone
        public static event Action<ZoneData>      OnZoneCompleted;

        private BuildingSave save;
        private ZoneData[]   allZones;

        // upgradeCount per zone: zoneId → count
        private Dictionary<string, int> upgradeCountByZone = new Dictionary<string, int>();
        // currentStep per building: buildingId → stepIndex
        private Dictionary<string, int> buildingStepByBuilding = new Dictionary<string, int>();

        public BuildingController(LevelDatabase levelDatabase)
        {
            instance = this;
            allZones = levelDatabase?.Zones ?? System.Array.Empty<ZoneData>();

            save = SaveController.GetSaveObject<BuildingSave>("Building");
            LoadFromSave();

            // Live zone switches happen inside the Game scene (UIZones under UIMainMenu) —
            // boot-time entry is handled by GameController.Start instead.
            ZoneController.OnZoneChanged += TriggerInitialProgressionInternal;
        }

        private void LoadFromSave()
        {
            foreach (var entry in save.Buildings)
                buildingStepByBuilding[entry.BuildingId] = entry.UpgradeStep;

            foreach (ZoneData zone in allZones)
            {
                int count = 0;
                BuildingBehavior[] zoneBuildings = zone.ZonePrefab != null ? zone.ZonePrefab.Buildings : null;
                if (zoneBuildings != null)
                    foreach (var building in zoneBuildings)
                        if (building != null && buildingStepByBuilding.TryGetValue(BuildKey(zone.ZoneId, building.BuildingId), out int step))
                            count += step;
                upgradeCountByZone[zone.ZoneId] = count;
            }
        }

        // ─── Public API ──────────────────────────────────────────────────────────

        public static bool CanUpgrade(BuildingBehavior building)
        {
            if (instance == null || building == null || building.Zone == null) return false;
            int step = instance.GetBuildingStepInternal(building.Zone.ZoneData.ZoneId, building.BuildingId);
            if (step >= building.MaxUpgrades) return false;

            int cost = building.Upgrades[step].Cost;
            return CurrencyController.HasAmount(CurrencyType.Coins, cost);
        }

        public static void Upgrade(BuildingBehavior building)
        {
            instance?.UpgradeInternal(building);
        }

        public static int GetBuildingStep(BuildingBehavior building)
        {
            if (instance == null || building == null || building.Zone == null) return 0;
            return instance.GetBuildingStepInternal(building.Zone.ZoneData.ZoneId, building.BuildingId);
        }

        public static int GetZoneUpgradeCount(string zoneId) =>
            instance != null && instance.upgradeCountByZone.TryGetValue(zoneId, out int count) ? count : 0;

        public static ZoneData CurrentZone => ZoneController.CurrentZone;

        public static ZoneData[] AllZones => instance?.allZones ?? Array.Empty<ZoneData>();

        /// <summary>Total building upgrade steps across every zone (avatar title conditions, profile stats).</summary>
        public static int TotalUpgrades
        {
            get
            {
                if (instance == null) return 0;
                int total = 0;
                foreach (int count in instance.upgradeCountByZone.Values)
                    total += count;
                return total;
            }
        }

        // Checked against prefab-authored building data directly (same pattern as IsZoneComplete)
        // so callers don't need a live, instantiated BuildingBehavior — e.g. the task panel's
        // hammer-card visibility, which must react to affordability from the Game scene, before
        // the Main Menu zone diorama has ever been instantiated.
        public static bool CanUpgradeAnyBuilding(ZoneData zone)
        {
            if (instance == null || zone == null) return false;
            return instance.CanUpgradeAnyBuildingInternal(zone);
        }

        // Cheapest upgrade cost among buildings that still have an upgrade step left, regardless
        // of current affordability (unlike CanUpgradeAnyBuilding). -1 if none has one left.
        public static int GetMinAvailableUpgradeCost(ZoneData zone)
        {
            if (instance == null || zone == null) return -1;
            return instance.GetMinAvailableUpgradeCostInternal(zone);
        }

        // The atUpgrade: 0 step never goes through the upgrade flow (zone upgrade counts start
        // at 1 — see UpgradeInternal's GetStepAt(zoneCount)), so its reward/dialog/level are
        // granted here, once per zone, when the zone's gameplay first becomes active.
        public static void TriggerInitialProgression(ZoneData zone)
        {
            instance?.TriggerInitialProgressionInternal(zone);
        }

        // ─── Internal ────────────────────────────────────────────────────────────

        // buildingId is only unique by convention (not enforced like ZoneData.zoneId), so every
        // lookup/save key is namespaced by zone — a copy-pasted building in another zone can never
        // collide with or overwrite a different zone's progress.
        private static string BuildKey(string zoneId, string buildingId) => $"{zoneId}_{buildingId}";

        private int GetBuildingStepInternal(string zoneId, string buildingId) =>
            buildingStepByBuilding.TryGetValue(BuildKey(zoneId, buildingId), out int step) ? step : 0;

        private void UpgradeInternal(BuildingBehavior building)
        {
            ZoneData zone = building.Zone.ZoneData;
            string key = BuildKey(zone.ZoneId, building.BuildingId);

            int currentStep = GetBuildingStepInternal(zone.ZoneId, building.BuildingId);
            if (currentStep >= building.MaxUpgrades) return;

            UpgradeStepData upgradeStepData = building.Upgrades[currentStep];
            int cost = upgradeStepData.Cost;
            if (!CurrencyController.HasAmount(CurrencyType.Coins, cost)) return;

            CurrencyController.Substract(CurrencyType.Coins, cost);

            buildingStepByBuilding[key] = currentStep + 1;
            upgradeCountByZone.TryGetValue(zone.ZoneId, out int zoneCount);
            zoneCount++;
            upgradeCountByZone[zone.ZoneId] = zoneCount;

            SaveUpgrade(key, currentStep + 1);

            Checkpoint.Log($"Building upgraded: {building.BuildingId} (zone {zone.ZoneId}, step {currentStep + 1})");

            OnUpgradeCompleted?.Invoke(zone, building.BuildingId, zoneCount);

            ZoneProgressionStep step = zone.GetStepAt(zoneCount);
            if (step != null || upgradeStepData.HasCompletionThought)
                UIQueueController.Enqueue(UIQueuePriority.BuildingProgression, onDone =>
                {
                    TriggerProgression(step, upgradeStepData.HasCompletionThought ? upgradeStepData.CompletionThought : null);
                    onDone();
                }, PROGRESSION_DELAY);

            bool zoneComplete = IsZoneComplete(zone);
            if (zoneComplete) OnZoneCompleted?.Invoke(zone);
        }

        private void TriggerInitialProgressionInternal(ZoneData zone)
        {
            if (zone == null) return;
            if (save.InitialProgressionZones.Contains(zone.ZoneId)) return;

            // OnZoneChanged 直达路径也要过引导门控（此前只有 TriggerInitialProgressionIfOnboarded 有检查）
            if (!MergeViewController.IsOnboardingCompleted()) return;

            // Opt-in: only zones whose progression explicitly STARTS with an atUpgrade: 0 step
            // get a zone-start grant. Normally the tutorial walks the player to the first
            // upgrade and progression begins at atUpgrade: 1 — a designer who removes the
            // tutorial adds an atUpgrade: 0 step as the first element to get this hook.
            ZoneProgressionStep[] progression = zone.Progression;
            if (progression == null || progression.Length == 0 || progression[0].AtUpgrade != 0) return;

            UIQueueController.Enqueue(UIQueuePriority.BuildingProgression, onDone =>
            {
                // 发放动作真正执行时才记账——先记账会在 1.8s 延迟期间杀进程时永久丢开局奖励
                if (!save.InitialProgressionZones.Contains(zone.ZoneId))
                {
                    save.InitialProgressionZones.Add(zone.ZoneId);
                    SaveController.MarkAsSaveIsRequired();
                }

                TriggerProgression(progression[0], null);
                onDone();
            }, PROGRESSION_DELAY);
        }

        // Runs once the building animation delay elapses. Grants exp first — if that triggers
        // a level-up, UILevelUpPopup enqueues its own turn on UIQueueController and waits for
        // the dialog (queued right below, higher priority) to close before showing.
        // completionThought (from the building's UpgradeStepData, if any) is spliced in front
        // of the zone progression's own dialog steps, or played alone if there is no progression dialog.
        private void TriggerProgression(ZoneProgressionStep step, BuildingCompletionThought completionThought)
        {
            if (step != null && step.HasTargetLevel)
                ExperienceController.GrantLevelProgress(step.TargetLevel);

            DialogData progressionDialog = step?.Dialog;
            DialogData dialogToPlay = progressionDialog;

            if (completionThought != null)
            {
                ThoughtStep thoughtStep = new ThoughtStep
                {
                    character = completionThought.Character,
                    emotion   = completionThought.Emotion,
                    text      = completionThought.Text
                };

                var steps = new List<DialogStep> { thoughtStep };
                if (progressionDialog != null) steps.AddRange(progressionDialog.Steps);

                string chapterTitle = progressionDialog != null
                    ? progressionDialog.ChapterTitle
                    : "Construction Complete";

                dialogToPlay = DialogData.CreateRuntime(chapterTitle, steps.ToArray());
                dialogToPlay.name = progressionDialog != null ? progressionDialog.name : "Construction Complete";
            }

            if (dialogToPlay != null)
                UIQueueController.Enqueue(UIQueuePriority.Dialog, dialogDone =>
                    DialogController.Play(dialogToPlay, () =>
                    {
                        step?.Reward?.ApplyReward();
                        dialogDone();
                    }));
            else
                step?.Reward?.ApplyReward();
        }

        private bool CanUpgradeAnyBuildingInternal(ZoneData zone)
        {
            BuildingBehavior[] zoneBuildings = zone.ZonePrefab != null ? zone.ZonePrefab.Buildings : null;
            if (zoneBuildings == null) return false;

            foreach (var building in zoneBuildings)
            {
                if (building == null) continue;

                int step = GetBuildingStepInternal(zone.ZoneId, building.BuildingId);
                if (step >= building.MaxUpgrades) continue;

                if (CurrencyController.HasAmount(CurrencyType.Coins, building.Upgrades[step].Cost))
                    return true;
            }

            return false;
        }

        private int GetMinAvailableUpgradeCostInternal(ZoneData zone)
        {
            BuildingBehavior[] zoneBuildings = zone.ZonePrefab != null ? zone.ZonePrefab.Buildings : null;
            if (zoneBuildings == null) return -1;

            int minCost = -1;
            foreach (var building in zoneBuildings)
            {
                if (building == null) continue;

                int step = GetBuildingStepInternal(zone.ZoneId, building.BuildingId);
                if (step >= building.MaxUpgrades) continue;

                int cost = building.Upgrades[step].Cost;
                if (minCost < 0 || cost < minCost) minCost = cost;
            }

            return minCost;
        }

        private bool IsZoneComplete(ZoneData zone)
        {
            BuildingBehavior[] zoneBuildings = zone.ZonePrefab != null ? zone.ZonePrefab.Buildings : null;
            if (zoneBuildings == null) return false;

            foreach (var building in zoneBuildings)
                if (building == null || GetBuildingStepInternal(zone.ZoneId, building.BuildingId) < building.MaxUpgrades)
                    return false;
            return true;
        }

        private void SaveUpgrade(string buildingId, int newStep)
        {
            var entry = save.Buildings.Find(b => b.BuildingId == buildingId);
            if (entry == null)
            {
                entry = new BuildingUpgradeSaveData { BuildingId = buildingId };
                save.Buildings.Add(entry);
            }
            entry.UpgradeStep = newStep;
            SaveController.MarkAsSaveIsRequired();
        }

        public void Unload()
        {
            ZoneController.OnZoneChanged -= TriggerInitialProgressionInternal;
            instance = null;
        }
    }
}
