using System.Collections.Generic;

namespace Watermelon
{
    // "New X" label shown the first time a reward/spawner of a given MergeItemType appears,
    // e.g. spawner popup banner (SpawnerQueueReward, SpawnerQueueRewardView).
    public static class MergeItemTypeLabel
    {
        private const string DEFAULT_LABEL = "New Item";

        private static readonly Dictionary<MergeItemType, string> newLabels = new Dictionary<MergeItemType, string>
        {
            { MergeItemType.Item, "New Item" },
            { MergeItemType.CurrencyItem, "New Currency" },
            { MergeItemType.EnergyItem, "New Energy" },
            { MergeItemType.Spawner, "New Spawner" },
            { MergeItemType.Chest, "New Chest" },
        };

        public static string GetNewLabel(MergeItemType itemType)
        {
            return newLabels.TryGetValue(itemType, out string label) ? label : DEFAULT_LABEL;
        }
    }
}
