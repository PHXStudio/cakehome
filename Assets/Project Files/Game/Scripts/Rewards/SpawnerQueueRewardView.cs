using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    [System.Serializable]
    public sealed class SpawnerQueueRewardView : RewardView
    {
        [SerializeField] Image    spawnerIcon;
        [SerializeField] TMP_Text label;

        protected override void OnInitialized()
        {
            ApplyLabel(reward as SpawnerQueueReward);
        }

        public override void Populate(Reward reward)
        {
            ApplyLabel(reward as SpawnerQueueReward);

            if (spawnerIcon != null && reward is SpawnerQueueReward spawnerReward)
            {
                Sprite sprite = MergeDatabase.Instance?.GetItem(spawnerReward.SpawnerTypeId)?.GetGradeData(spawnerReward.SpawnerGrade)?.Sprite;
                if (sprite != null) spawnerIcon.sprite = sprite;
            }
        }

        private void ApplyLabel(SpawnerQueueReward spawnerReward)
        {
            if (label == null) return;

            MergeItemType itemType = MergeDatabase.Instance?.GetItem(spawnerReward?.SpawnerTypeId)?.ItemType ?? MergeItemType.Spawner;
            label.text = MergeItemTypeLabel.GetNewLabel(itemType);
        }
    }
}
