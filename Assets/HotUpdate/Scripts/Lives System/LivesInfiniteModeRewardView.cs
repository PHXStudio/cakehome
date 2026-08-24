using System;
using TMPro;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// View counterpart of <see cref="LivesInfiniteModeReward"/>. Shows the formatted
    /// infinite-lives duration on the owning offer prefab.
    /// </summary>
    [Serializable]
    public sealed class LivesInfiniteModeRewardView : RewardView
    {
        [SerializeField] TextMeshProUGUI durationText;
        [SerializeField] string durationFormat = "{hh}hrs";

        protected override void OnInitialized()
        {
            UpdateText();
        }

        public override void Populate(Reward reward)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            if (durationText == null) return;
            if (reward is not LivesInfiniteModeReward livesReward) return;

            durationText.text = TimeUtils.GetFormatedTime(livesReward.DurationInMinutes, durationFormat);
        }
    }
}
