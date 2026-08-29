using System;
using System.Collections.Generic;
using System.Linq;

namespace Watermelon
{
    public class ClientOrderTask : IFieldAwareTask
    {
        public string TaskId { get; }
        public CharacterData Character { get; }
        public List<OrderItem> Items { get; }
        public int CoinsReward { get; }

        public bool IsComplete => Items.TrueForAll(i => i.collected);

        public event Action OnProgressChanged;
        public event Action<MergeFieldObject> OnItemBound; // start bounce on this instance

        public ClientOrderTask(string taskId, CharacterData character, List<OrderItem> items, int coinsReward)
        {
            TaskId      = taskId;
            Character   = character;
            Items       = items;
            CoinsReward = coinsReward;
        }

        // Re-scans the live grid: drops slots whose bound instance no longer exists, then tries
        // to bind any still-unfulfilled slot to a matching instance not already claimed by
        // another slot of this same order.
        public void OnFieldChanged(MergeGrid grid)
        {
            bool changed = false;

            foreach (OrderItem item in Items)
            {
                // grid.Contains, not boundObject == null: Destroy() defers actual destruction to
                // end of frame, so a just-merged/deleted instance still compares non-null here —
                // the grid's Occupant slots are the authoritative, synchronously-updated source.
                // IsHalfLocked: a bound instance that got locked is no longer eligible (mirrors
                // the FindUnbound bind-time filter).
                if (item.collected && (!grid.Contains(item.boundObject) || item.boundObject.IsHalfLocked))
                {
                    item.collected   = false;
                    item.boundObject = null;
                    changed = true;
                }
            }

            if (Items.Exists(i => !i.collected))
            {
                var bound = new HashSet<MergeFieldObject>(Items.Where(i => i.collected).Select(i => i.boundObject));

                // 排除其它订单已认领的实例——避免两张订单同时显示"可交付"同一个物品
                HashSet<MergeFieldObject> claimed = TaskController.Instance?.GetClaimedInstances(this);
                if (claimed != null) bound.UnionWith(claimed);

                foreach (OrderItem item in Items)
                {
                    if (item.collected) continue;

                    MergeFieldObject match = grid.FindUnbound(item.typeId, item.grade, bound);
                    if (match == null) continue;

                    item.collected   = true;
                    item.boundObject = match;
                    bound.Add(match);
                    changed = true;
                    OnItemBound?.Invoke(match);
                }
            }

            if (changed) OnProgressChanged?.Invoke();
        }
    }
}
