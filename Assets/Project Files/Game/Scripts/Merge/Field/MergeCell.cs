using UnityEngine;

namespace Watermelon
{
    public class MergeCell
    {
        public MergeFieldObject Occupant { get; set; }
        public Vector2Int Position { get; set; }

        public CellState State
        {
            get
            {
                if (Occupant == null) return CellState.Empty;
                if (Occupant is LockedCell) return CellState.Locked;
                if (Occupant.IsHalfLocked) return CellState.HalfLockedItem;
                return CellState.Active;
            }
        }

        public bool IsEmpty => Occupant == null;
        public bool CanAcceptDrop => IsEmpty || State == CellState.HalfLockedItem;
    }
}
