using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    [System.Serializable]
    public sealed class EnergyRewardView : RewardView
    {
        [SerializeField] Image    icon;
        [SerializeField] TMP_Text amountText;

        protected override void OnInitialized()
        {
            if (reward is EnergyReward energy && amountText != null)
                amountText.text = $"+{energy.Amount}";
        }

        public override void Populate(Reward reward)
        {
            if (reward is EnergyReward energy && amountText != null)
                amountText.text = $"+{energy.Amount}";
        }
    }
}
