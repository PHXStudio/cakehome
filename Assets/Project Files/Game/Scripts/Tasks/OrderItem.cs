using System;

namespace Watermelon
{
    [Serializable]
    public class OrderItem
    {
        public string typeId;
        public int    grade;
        public bool   collected;

        // Not serialized — the live field instance currently fulfilling this slot. Used to
        // detect when that instance disappears (sold/deleted/merged away) before Give is
        // pressed, so the slot can revert to uncollected.
        [NonSerialized] public MergeFieldObject boundObject;
    }
}
