using System;

namespace Watermelon
{
    /// <summary>
    /// Save for <see cref="MergeStatsController"/>. Lifetime merge-side statistics
    /// used by profile display and avatar title conditions.
    /// </summary>
    [Serializable]
    public class MergeStatsSave : ISaveObject
    {
        public int OrdersCompleted;

        public void OnBeforeSave() { }
    }
}
