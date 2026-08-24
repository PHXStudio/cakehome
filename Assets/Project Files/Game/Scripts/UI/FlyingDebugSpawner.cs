#if UNITY_EDITOR
using UnityEngine;

namespace Watermelon
{
    // Debug-only: triggers the real "reward fly" code paths (RewardFlyController,
    // ExperienceController) so UI/VFX polish can be tested without playing through a real
    // reward. Never calls its own animation logic — every button here goes through the exact
    // same code production uses, so a fix/tweak there is instantly visible here too.
    public class FlyingDebugSpawner : MonoBehaviour, IDebugPersistentComponent
    {
        public enum FlyType { Exp, Energy, Coins, Gems, Spawner }

        [SerializeField] FlyType flyType;

        // Exp
        [SerializeField] int levelsToAdd = 1;

        // Energy / Coins / Gems
        [SerializeField] int amount = 6;

        // Spawner
        [SerializeField] string spawnerTypeId = "Kettle Spawner";
        [SerializeField] int spawnerGrade = 1;

        public void PlayFly()
        {
            RectTransform source = UIController.GetPage<UIHeader>()?.ScreenCenterRectTransform;
            if (source == null)
            {
                Debug.LogWarning("[FlyingDebugSpawner] UIHeader isn't active — open a page with the header visible.");
                return;
            }

            switch (flyType)
            {
                case FlyType.Exp:
                    LevelProgress target = new LevelProgress(ExperienceController.CurrentLevel + Mathf.Max(1, levelsToAdd));
                    ExperienceController.GrantLevelProgress(target);
                    break;

                case FlyType.Energy:
                    RewardFlyController.FlyEnergy(source, amount);
                    break;

                case FlyType.Coins:
                    RewardFlyController.FlyCurrency(CurrencyType.Coins, source, amount);
                    break;

                case FlyType.Gems:
                    RewardFlyController.FlyCurrency(CurrencyType.Gems, source, amount);
                    break;

                case FlyType.Spawner:
                    UIMainMenu menu = UIController.GetPage<UIMainMenu>();
                    if (menu == null || !menu.IsPageDisplayed)
                    {
                        Debug.LogWarning("[FlyingDebugSpawner] Open the Main Menu to test the spawner fly (it flies to the Back button).");
                        return;
                    }

                    RewardFlyController.FlySpawner(spawnerTypeId, spawnerGrade, null);
                    break;
            }
        }
    }
}
#endif
