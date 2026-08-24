using System;
using UnityEngine;

namespace Watermelon
{
    public class ExperienceController
    {
        private static ExperienceController instance;

        public static int   CurrentLevel         { get; private set; } = 1;
        public static float CurrentLevelProgress { get; private set; }

        public static event Action<int, RewardBundle> OnLevelUp;         // new level; reward applied, except Currency/Energy which credit when their fly-cloud lands (see UILevelUpPopup)
        public static event Action                    OnProgressChanged; // level and/or progress changed

        public static Sprite ExpIcon => instance?.levelDatabase.ExpIcon;

        public static AudioClip AppearAudioClip  => instance?.levelDatabase.AppearAudioClip;
        public static AudioClip CollectAudioClip => instance?.levelDatabase.CollectAudioClip;

        private ExperienceSave     save;
        private ExperienceDatabase levelDatabase;

        public ExperienceController(ExperienceDatabase levelDatabase)
        {
            instance          = this;
            this.levelDatabase = levelDatabase;

            save                  = SaveController.GetSaveObject<ExperienceSave>("Experience");
            CurrentLevel          = save.CurrentLevel;
            CurrentLevelProgress  = save.CurrentLevelProgress;
        }

        // ─── Public API ──────────────────────────────────────────────────────────

        // Jumps directly to a designer-placed (level, progress) snapshot. No-op if the target
        // isn't ahead of current progress (zones progress in parallel, so a step from one zone
        // can arrive after another zone has already passed that point).
        public static bool GrantLevelProgress(LevelProgress target) =>
            instance?.GrantLevelProgressInternal(target) ?? false;

        private bool GrantLevelProgressInternal(LevelProgress target)
        {
            if (target == null || target.Level <= 0) return false;

            bool isAhead = target.Level > CurrentLevel ||
                          (target.Level == CurrentLevel && target.Progress > CurrentLevelProgress);
            if (!isAhead) return false;

            for (int lvl = CurrentLevel + 1; lvl <= target.Level; lvl++)
            {
                RewardBundle reward = levelDatabase.GetLevelReward(lvl);
                if (reward != null)
                {
                    foreach (Reward r in reward.GetRewards())
                    {
                        // Currency/Energy are credited when their fly-cloud lands in UILevelUpPopup
                        // instead of here, so the HUD number updates in sync with the animation.
                        if (r is CurrencyReward || r is EnergyReward) continue;

                        r.ApplyReward();
                    }
                }

                Checkpoint.Log($"Level up: {lvl}");

                OnLevelUp?.Invoke(lvl, reward);
            }

            CurrentLevel         = target.Level;
            CurrentLevelProgress = target.Progress;

            save.CurrentLevel         = CurrentLevel;
            save.CurrentLevelProgress = CurrentLevelProgress;

            SaveController.MarkAsSaveIsRequired();
            OnProgressChanged?.Invoke();

            return true;
        }

        public void Unload()
        {
            instance = null;

            OnLevelUp         = null;
            OnProgressChanged = null;
        }
    }
}
