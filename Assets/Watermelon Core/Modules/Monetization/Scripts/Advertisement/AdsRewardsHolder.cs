using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public sealed class AdsRewardsHolder : RewardsHolder
    {
        [Group("Settings"), UniqueID]
        [SerializeField] string rewardID;

        [Group("Settings"), Space]
        [SerializeField] Button adsButton;

        [Group("Settings")]
        [SerializeField] bool disableAfterPurchase;

        [Group("Settings"), Space]
        [SerializeField] string analyticsEvent = "Default";

        private SimpleBoolSave save;

        private void Start()
        {
            InitializeComponents();

            save = SaveController.GetSaveObject<SimpleBoolSave>($"CurrencyProduct_{rewardID}");

            if (disableAfterPurchase && save.Value)
            {
                // Disable holder game object
                gameObject.SetActive(false);

                return;
            }

            adsButton.onClick.AddListener(OnPurchased);
        }

        private void OnPurchased()
        {
#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_HARD);
#endif

            AudioController.PlaySound(AudioController.GetClip("button_sound"));

            AdsManager.ShowRewardBasedVideo((reward) =>
            {
                if (reward)
                {
                    rewardSet.ApplyReward();

                    save.Value = true;

                    if (disableAfterPurchase)
                    {
                        // Disable holder game object
                        gameObject.SetActive(false);
                    }

                    SaveController.MarkAsSaveIsRequired();
                }
            }, analyticsEvent);
        }
    }
}
