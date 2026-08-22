using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Watermelon.IAPStore;

namespace Watermelon
{
    public class UIRecoverEnergy : UIPage
    {
        public override bool IsPopup => true;

        [Header("Free state")]
        [SerializeField] GameObject freeButton;

        [Header("Paid state")]
        [SerializeField] GameObject paidButton;
        [SerializeField] TMP_Text   paidCostText;
        [SerializeField] GameObject adButton;
        [SerializeField] TMP_Text   adAmountText;

        [Header("Close")]
        [SerializeField] Button closeButton;

        [Header("Recover timer")]
        [SerializeField] TMP_Text recoverText;

        [SerializeField] EnergyData energyData;

        private Coroutine timerCoroutine;
        private readonly StringBuilder sb = new StringBuilder();

        public override void Init()
        {
            if (energyData != null)
                Setup(energyData);

            freeButton.GetComponent<Button>().onClick.AddListener(OnFreeClicked);
            paidButton.GetComponent<Button>().onClick.AddListener(OnPaidClicked);
            adButton.GetComponent<Button>().onClick.AddListener(OnAdClicked);
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        public void Setup(EnergyData data)
        {
            energyData = data;
        }

        public static void Show()
        {
            UIController.ShowPage<UIRecoverEnergy>();
        }

        protected override void OnShow()
        {
            ResourcesSave save = SaveController.GetSaveObject<ResourcesSave>("Resources");
            bool isFirstTime   = save.IsEnergyFirstTimeFree;

            freeButton.SetActive(isFirstTime);
            paidButton.SetActive(!isFirstTime);
            adButton.SetActive(!isFirstTime);

            if (!isFirstTime)
            {
                paidCostText.text = $"{energyData.PaidEnergyCostGems}";
                adAmountText.text = $"+{energyData.AdEnergyAmount}";
            }

            timerCoroutine = StartCoroutine(RecoverTimerLoop());

            NotifyOpened();
        }

        protected override void OnHide()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }

            NotifyClosed();
        }

        private IEnumerator RecoverTimerLoop()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (true)
            {
                RedrawRecoverText();
                yield return wait;
            }
        }

        private void RedrawRecoverText()
        {
            if (EnergyController.Current >= EnergyController.Max)
            {
                recoverText.text = "Energy is full";
                return;
            }

            TimeSpan time = TimeSpan.FromSeconds(EnergyController.GetSecondsUntilFull());
            recoverText.text = $"You'll recover {EnergyController.Max - EnergyController.Current} in {FormatTimer(time)}";
        }

        private string FormatTimer(TimeSpan timeSpan)
        {
            sb.Clear();

            if (timeSpan.Hours > 0)
            {
                sb.Append(timeSpan.Hours);
                sb.Append(':');
            }

            sb.Append(timeSpan.Minutes.ToString("00"));
            sb.Append(':');
            sb.Append(timeSpan.Seconds.ToString("00"));

            return sb.ToString();
        }

        private void OnFreeClicked()
        {
            AudioController.PlaySound(AudioController.GetClip("button_sound"));

            ResourcesSave save = SaveController.GetSaveObject<ResourcesSave>("Resources");
            save.IsEnergyFirstTimeFree = false;
            EnergyController.Add(energyData.FreeEnergyAmount, ignoreCap: true);
            AudioController.PlaySound(AudioController.GetClip("energy_restore"));

            UIController.HidePage<UIRecoverEnergy>();
        }

        private void OnPaidClicked()
        {
            AudioController.PlaySound(AudioController.GetClip("button_sound"));

            if (!CurrencyController.HasAmount(CurrencyType.Gems, energyData.PaidEnergyCostGems))
            {
                // TODO(模板迁移): UIStore.OpenAsOverlay 为模板 API,待接入蛋糕 UIStore
                return;
            }

            CurrencyController.Substract(CurrencyType.Gems, energyData.PaidEnergyCostGems, "energy_recover");

            Checkpoint.Log("Energy recovered via gems", gameObject);

            EnergyController.Add(energyData.PaidEnergyAmount, ignoreCap: true);
            AudioController.PlaySound(AudioController.GetClip("energy_restore"));

            UIController.HidePage<UIRecoverEnergy>();
        }

        private void OnAdClicked()
        {
            AudioController.PlaySound(AudioController.GetClip("button_sound"));

            AdsManager.ShowRewardBasedVideo((hasReward) =>
            {
                if (!hasReward)
                    return;

                Checkpoint.Log("Energy recovered via rewarded ad", gameObject);

                EnergyController.Add(energyData.AdEnergyAmount, ignoreCap: true);
                AudioController.PlaySound(AudioController.GetClip("energy_restore"));

                UIController.HidePage<UIRecoverEnergy>();
            }, "energy_recover");
        }

        private void OnCloseClicked()
        {
            AudioController.PlaySound(AudioController.GetClip("button_sound"));

            UIController.HidePage<UIRecoverEnergy>();
        }
    }
}
