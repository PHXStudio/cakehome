using UnityEngine;

namespace Watermelon
{
    // Shared "fly a reward icon to its HUD/UI target" logic. Both real reward-application flows
    // (UILevelUpPopup, SpawnerQueueReward) and FlyingDebugSpawner call these same methods, so a
    // change here (or in CurrencyCloud/RewardFlyElement underneath) affects both at once.
    public static class RewardFlyController
    {
        public static void FlyCurrency(CurrencyType currencyType, RectTransform source, int amount = 6, SimpleCallback onDone = null)
        {
            UIHeader header = UIController.GetPage<UIHeader>();
            CurrencyUIPanelSimple panel = currencyType == CurrencyType.Gems ? header?.GemsPanel : header?.CoinsPanel;
            if (source == null || panel == null)
            {
                onDone?.Invoke();
                return;
            }

            CurrencyCloud.SpawnCurrency(currencyType.ToString(), source, panel.TextRectTransform, amount, "", onDone);
        }

        public static void FlyEnergy(RectTransform source, int amount = 6, SimpleCallback onDone = null)
        {
            UIHeader header = UIController.GetPage<UIHeader>();
            if (source == null || header?.EnergyPanel == null)
            {
                onDone?.Invoke();
                return;
            }

            CurrencyCloud.SpawnCurrency(EnergyUIPanel.CLOUD_KEY, source, header.EnergyPanel.TextRectTransform, amount, "", onDone);
        }

        // Mini-tutorial cue for SpawnerQueueReward: flies the received spawner's icon (via the
        // "Flying Reward" prefab — shine on appear, trail while flying, see RewardFlyElement) from
        // the screen center to the Main Menu's Back button, hinting the player to check the game
        // board. Runs through UIQueueController (UIQueuePriority.ExpFly) so it plays right alongside
        // any exp-fly the same reward triggered, after any dialog and before the level-up popup.
        public static void FlySpawner(string spawnerTypeId, int spawnerGrade, SimpleCallback onDone)
        {
            UIMainMenu menu = UIController.GetPage<UIMainMenu>();
            if (menu == null || !menu.IsPageDisplayed)
            {
                onDone?.Invoke();
                return;
            }

            UIHeader header = UIController.GetPage<UIHeader>();
            RectTransform flySource = header?.ScreenCenterRectTransform;
            GameObject flyPrefab = header?.FlyingRewardPrefab;
            if (flySource == null || flyPrefab == null)
            {
                onDone?.Invoke();
                return;
            }

            Sprite sprite = MergeDatabase.Instance?.GetItem(spawnerTypeId)?.GetGradeData(spawnerGrade)?.Sprite;
            if (sprite == null)
            {
                onDone?.Invoke();
                return;
            }

            string key = $"SpawnerFly_{spawnerTypeId}_{spawnerGrade}";
            if (!CurrencyCloud.IsRegistered(key))
            {
                CurrencyCloudSettings settings = new CurrencyCloudSettings(key, flyPrefab, sprite);
                settings.SetAudio(MergeDatabase.Instance?.AppearAudioClip, MergeDatabase.Instance?.CollectAudioClip);

                CurrencyCloud.RegisterCase(settings);
            }

            CurrencyCloud.SpawnCurrency(key, flySource, menu.BackButtonRectTransform, 1, "", () => onDone?.Invoke());
        }
    }
}
