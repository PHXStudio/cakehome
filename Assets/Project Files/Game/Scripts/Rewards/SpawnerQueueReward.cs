using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [Serializable]
    [RegisterReward(typeof(SpawnerQueueRewardView))]
    public sealed class SpawnerQueueReward : Reward
    {
        [SerializeField] string spawnerTypeId;
        public string SpawnerTypeId => spawnerTypeId;

        [SerializeField] int spawnerGrade;
        public int SpawnerGrade => spawnerGrade;

        [SerializeField] bool spawnFloatingImage = true;
        public bool SpawnFloatingImage => spawnFloatingImage;

        public SpawnerQueueReward() { }
        public SpawnerQueueReward(string spawnerTypeId) { this.spawnerTypeId = spawnerTypeId; }

        public override void ApplyReward()
        {
            TaskController.EnqueueSpawner(spawnerTypeId, spawnerGrade);

            UIQueueController.Enqueue(UIQueuePriority.ExpFly, onDone =>
            {
                if(spawnFloatingImage)
                    RewardFlyController.FlySpawner(spawnerTypeId, spawnerGrade, () => onDone());
                else
                    onDone();
            });
        }

        public override List<IRewardPreview> GetRewardPreviews()
        {
            MergeItemData item = MergeDatabase.Instance?.GetItem(spawnerTypeId);
            Sprite sprite = item?.GetGradeData(spawnerGrade)?.Sprite;
            string label = MergeItemTypeLabel.GetNewLabel(item?.ItemType ?? MergeItemType.Spawner);

            return new List<IRewardPreview>
            {
                new RewardPreview(sprite, label, 0)
            };
        }
    }
}
